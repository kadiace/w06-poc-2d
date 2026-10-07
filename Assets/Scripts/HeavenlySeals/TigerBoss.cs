using System;
using System.Collections.Generic;
using UnityEngine;

public enum TigerBossPhase { Waiting, Phase1, Transition, Phase2, Dead, Stopped }
public enum BossBeatEventKind { Warning, Impact, Projectile, Tempo, PatternEnd, Phase, Death }

public readonly struct BossBeatEvent
{
    public BossBeatEventKind Kind { get; }
    public TigerBossPhase Phase { get; }
    public string PatternName { get; }
    public double PlannedBeat { get; }
    public double EmittedBeat { get; }
    public Rect? Bounds { get; }
    public Vector2 Position { get; }

    public BossBeatEvent(BossBeatEventKind kind, TigerBossPhase phase, string patternName,
        double plannedBeat, double emittedBeat, Rect? bounds, Vector2 position)
    {
        Kind = kind;
        Phase = phase;
        PatternName = patternName;
        PlannedBeat = plannedBeat;
        EmittedBeat = emittedBeat;
        Bounds = bounds;
        Position = position;
    }
}

[DefaultExecutionOrder(30)]
public sealed class TigerBoss : MonoBehaviour, IDamageable
{
    private sealed class ScheduledAction
    {
        public double Beat;
        public long Order;
        public Action<double> Execute;
        public bool IsHit;
    }

    private static readonly Color NormalColor = new Color(1f, 0.52f, 0.12f);
    private static readonly Color WarningColor = new Color(1f, 0.75f, 0.2f, 0.4f);
    private static readonly Color HitColor = new Color(1f, 0.2f, 0.1f, 0.65f);
    private readonly List<ScheduledAction> scheduled = new List<ScheduledAction>();
    private readonly List<TigerProjectile> projectiles = new List<TigerProjectile>();
    private PlayerCombat player;
    private Collider2D playerCollider;
    private BossArea gate;
    private Rect arena;
    private TigerBossView view;
    private double observedBeat;
    private long nextOrder;
    private int patternIndex;
    private int facing = -1;
    private int attackFacing = -1;
    private int lastAttackSequence = -1;
    private int lastParrySequence = -1;
    private bool initialized;
    private bool transitionReserved;
    private bool transitionRequested;
    private bool visible = true;
    private Rect? attackBounds;
    private double attackEnds;
    private double patternEnd;
    private double recoveryUntil;
    private double transitionBeat;
    private bool moving;
    private Vector2 moveFrom;
    private Vector2 moveTo;
    private double moveStart;
    private double moveEnd;
    private float moveArc;
    private Rect? warningBounds;
    private bool landingVisible;
    private Vector2 landingPosition;
    private bool dashVisible;
    private Vector2 dashFrom;
    private Vector2 dashTo;
    private bool spawnVisible;
    private Vector2 spawnPosition;
    private bool projectilePreviewVisible;
    private BoxCollider2D spawnReservation;

    public float MaxHealth => 120f;
    public float CurrentHealth { get; private set; } = 120f;
    public TigerBossPhase Phase { get; private set; } = TigerBossPhase.Waiting;
    public string PatternName { get; private set; } = "Waiting";
    public int TransitionCount { get; private set; }
    public int ProjectileCount => projectiles.Count;
    public bool IsAttackActive => attackBounds.HasValue && observedBeat < attackEnds && IsFighting;
    public Rect? ActiveAttackBounds => IsAttackActive ? attackBounds : null;
    public Rect BodyBounds => new Rect((Vector2)transform.position - new Vector2(0.55f, 0.45f), new Vector2(1.1f, 0.9f));
    public double? NextHitBeat
    {
        get
        {
            if (!IsFighting) return null;
            foreach (ScheduledAction action in scheduled)
                if (action.IsHit) return action.Beat;
            return null;
        }
    }
    public event Action<BossBeatEvent> BeatEventEmitted;
    private bool IsFighting => Phase == TigerBossPhase.Phase1 || Phase == TigerBossPhase.Transition || Phase == TigerBossPhase.Phase2;
    private float FloorCenter => arena.yMin + 0.45f;
    private float Ceiling => Mathf.Min(3.8f, arena.yMax - 0.3f);
    private Vector2 ProjectileOrigin => (Vector2)transform.position + new Vector2(facing * 0.55f, 0.1f);

    public void Initialize(PlayerCombat combat, BossArea bossArea, Rect arenaBounds)
    {
        player = combat;
        playerCollider = combat.GetComponent<Collider2D>();
        gate = bossArea;
        arena = arenaBounds;
        view = GetComponent<TigerBossView>();
        if (view == null) view = gameObject.AddComponent<TigerBossView>();
        view.Initialize();
        view.SetAppearance(facing, NormalColor, true);
        GameObject reservation = new GameObject("Telegraphed spawn space");
        reservation.transform.SetParent(transform, false);
        spawnReservation = reservation.AddComponent<BoxCollider2D>();
        spawnReservation.size = new Vector2(1.1f, 0.9f);
        spawnReservation.enabled = false;
        player.Died += Stop;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || Phase == TigerBossPhase.Dead || Phase == TigerBossPhase.Stopped) return;
        observedBeat = Managers.Beat.GetClockTick().TotalBeats;
        if (player.IsDead) { Stop(); return; }
        if (Phase == TigerBossPhase.Waiting)
        {
            if (!gate.HasArrived) return;
            Phase = TigerBossPhase.Phase1;
            Emit(BossBeatEventKind.Phase, observedBeat);
            double start = Math.Floor(observedBeat) + 1d;
            patternEnd = start + 4d;
            Queue(start, BeginPattern);
        }
        while (scheduled.Count > 0 && scheduled[0].Beat <= observedBeat && IsFighting)
        {
            ScheduledAction action = scheduled[0];
            scheduled.RemoveAt(0);
            AdvanceMovement(action.Beat);
            AdvanceProjectiles(action.Beat);
            if (!IsFighting) break;
            action.Execute(action.Beat);
        }
        if (!IsFighting) return;
        AdvanceMovement(observedBeat);
        AdvanceProjectiles(observedBeat);
        if (!IsFighting) return;
        if (attackBounds.HasValue && observedBeat >= attackEnds) attackBounds = null;
        if (!IsFighting) return;
        OrbitActions actions = player.Actions;
        if (visible && actions.IsAttacking && actions.AttackSequence != lastAttackSequence && actions.AttackBounds.Overlaps(BodyBounds))
        {
            lastAttackSequence = actions.AttackSequence;
            TryTakeDamage(10f * actions.AttackDamageMultiplier, actions.AttackCenter);
        }
        if (IsFighting) RefreshView();
    }

    private void Queue(double beat, Action<double> execute, bool isHit = false)
    {
        ScheduledAction action = new ScheduledAction { Beat = beat, Order = nextOrder++, Execute = execute, IsHit = isHit };
        int index = scheduled.FindIndex(existing => existing.Beat > beat || (existing.Beat == beat && existing.Order > action.Order));
        if (index < 0) scheduled.Add(action); else scheduled.Insert(index, action);
    }

    private void BeginPattern(double beat)
    {
        ClearTelegraphs();
        recoveryUntil = 0d;
        int index = patternIndex++ % 3;
        if (Phase == TigerBossPhase.Phase1)
        {
            if (index == 0) Claw(beat, false);
            else if (index == 1) Leap(beat, false);
            else Dash(beat, false);
        }
        else
        {
            if (index == 0) Claw(beat, true);
            else if (index == 1) Leap(beat, true);
            else Dash(beat, true);
        }
    }

    private void Claw(double beat, bool triple)
    {
        double hitBeat = Math.Ceiling(beat + 2d);
        beat = hitBeat - 2d;
        PatternName = triple ? "P2 Triple Claw" : "P1 Claw";
        Vector2 target = player.transform.position;
        facing = target.x < transform.position.x ? -1 : 1;
        Vector2 approach = Ground(target.x - facing * 1.15f);
        StartMove(beat, beat + 1d, approach, 0f);
        Emit(BossBeatEventKind.Warning, beat);
        Queue(beat + 1d, WarnClaw);
        Queue(hitBeat, HitClaw, true);
        if (triple)
        {
            Queue(beat + 2d, WarnClaw);
            Queue(beat + 3d, HitClaw, true);
            Queue(beat + 3d, WarnClaw);
            Queue(beat + 4d, HitClaw, true);
        }
        EndAt(beat + (triple ? 7d : 4d));
    }

    private Rect ClawBounds()
    {
        Vector2 center = (Vector2)transform.position + Vector2.right * (facing * 1.05f);
        return new Rect(center - new Vector2(0.85f, 0.55f), new Vector2(1.7f, 1.1f));
    }

    private void WarnClaw(double beat)
    {
        facing = player.transform.position.x < transform.position.x ? -1 : 1;
        warningBounds = ClawBounds();
        Emit(BossBeatEventKind.Warning, beat, warningBounds);
    }

    private void HitClaw(double beat)
    {
        Rect bounds = warningBounds.Value;
        warningBounds = null;
        Impact(beat, bounds);
    }

    private void Leap(double beat, bool followup)
    {
        double hitBeat = Math.Ceiling(beat + 3d);
        beat = hitBeat - 3d;
        PatternName = followup ? "P2 Leap + Claw" : "P1 Leap";
        Vector2 target = Ground(player.transform.position.x);
        landingPosition = new Vector2(target.x, arena.yMin + 0.06f);
        landingVisible = true;
        Rect landingBounds = new Rect(target - new Vector2(1.1f, 0.6f), new Vector2(2.2f, 1.2f));
        Emit(BossBeatEventKind.Warning, beat, landingBounds);
        Queue(hitBeat - 2d, takeoff => StartMove(takeoff, hitBeat, target, 3.3f));
        Queue(hitBeat, landing =>
        {
            SetPosition(target);
            moving = false;
            landingVisible = false;
            Impact(landing, landingBounds);
            if (followup) WarnClaw(landing);
        }, true);
        if (followup) Queue(beat + 4d, HitClaw, true);
        EndAt(beat + (followup ? 6d : 5d));
    }

    private void Dash(double beat, bool teleport)
    {
        double hitBeat = Math.Ceiling(beat + (teleport ? 3d : 2d));
        beat = hitBeat - (teleport ? 3d : 2d);
        PatternName = teleport ? "P2 Behind Teleport + Dash" : "P1 Dash";
        Vector2 target = player.transform.position;
        Vector2 start = transform.position;
        double moveBeat = hitBeat - 1d;
        if (teleport)
        {
            int playerFacing = player.Player.Facing;
            start = Ground(target.x - playerFacing * 1.3f);
            facing = playerFacing;
            spawnPosition = start;
            spawnVisible = true;
            visible = false;
            spawnReservation.transform.position = new Vector3(start.x, start.y, 0f);
            spawnReservation.enabled = true;
            Physics2D.SyncTransforms();
            Emit(BossBeatEventKind.Warning, beat, new Rect(start - new Vector2(0.55f, 0.45f), new Vector2(1.1f, 0.9f)));
            Vector2 lockedStart = start;
            Queue(beat + 1d, appear =>
            {
                spawnReservation.enabled = false;
                SetPosition(lockedStart);
                visible = true;
                spawnVisible = false;
                dashVisible = true;
                warningBounds = new Rect(dashTo - new Vector2(0.8f, 0.45f), new Vector2(1.6f, 0.9f));
                Emit(BossBeatEventKind.Warning, appear, warningBounds);
            });
        }
        else facing = target.x < start.x ? -1 : 1;
        Vector2 end = Ground(target.x);
        dashFrom = start;
        dashTo = end;
        dashVisible = !teleport;
        Rect hitBounds = new Rect(end - new Vector2(0.8f, 0.45f), new Vector2(1.6f, 0.9f));
        if (!teleport)
        {
            warningBounds = hitBounds;
            Emit(BossBeatEventKind.Warning, beat, hitBounds);
        }
        double lockedHitBeat = hitBeat;
        Queue(moveBeat, movingBeat => StartMove(movingBeat, lockedHitBeat, end, 0f));
        Queue(hitBeat, impact =>
        {
            SetPosition(end);
            moving = false;
            dashVisible = false;
            Impact(impact, hitBounds);
        }, true);
        EndAt(hitBeat + 2d);
    }

    private void Impact(double beat, Rect bounds)
    {
        warningBounds = null;
        attackBounds = bounds;
        attackFacing = facing;
        attackEnds = beat + 0.15d;
        // The short active window is visual only. Contact is sampled once at the impact beat.
        Emit(BossBeatEventKind.Impact, beat, bounds);
        if (IsFighting && bounds.Overlaps(player.Bounds))
        {
            ResolvePlayerContact(transform.position);
        }
    }

    private void EndAt(double beat)
    {
        patternEnd = beat;
        Queue(beat, EndPattern);
    }

    private void EndPattern(double beat)
    {
        if (recoveryUntil > beat) { Queue(recoveryUntil, EndPattern); return; }
        ClearTelegraphs();
        Emit(BossBeatEventKind.PatternEnd, beat);
        if (!IsFighting) return;
        if (transitionRequested && Phase == TigerBossPhase.Phase1)
        {
            ReserveTransition(beat);
            return;
        }
        BeginPattern(beat);
    }

    public bool TryTakeDamage(float damage, Vector2 source)
    {
        if (!IsFighting || damage <= 0f) return false;
        observedBeat = Managers.Beat.GetClockTick().TotalBeats;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        if (CurrentHealth <= 0f)
        {
            Cleanup(TigerBossPhase.Dead);
            PatternName = "Dead";
            Emit(BossBeatEventKind.Death, observedBeat);
            BeatEventEmitted = null;
        }
        else if (CurrentHealth <= MaxHealth * 0.5f && Phase == TigerBossPhase.Phase1)
            transitionRequested = true;
        return true;
    }

    private void ReserveTransition(double completedBeat)
    {
        if (transitionReserved) return;
        double now = Managers.Beat.GetClockTick().TotalBeats;
        transitionBeat = Math.Max(Math.Ceiling(completedBeat + 1d), Math.Floor(now) + 1d);
        transitionReserved = true;
        Managers.Beat.ScheduleTempoMultiplier(0.5f, (long)transitionBeat);
        Managers.Beat.ScheduleTempoMultiplier(1f, (long)transitionBeat + 10L);
        Queue(transitionBeat, BeginTransition);
    }

    private void BeginTransition(double beat)
    {
        Phase = TigerBossPhase.Transition;
        TransitionCount++;
        PatternName = "Transition Rise / Adapt";
        ClearTelegraphs();
        Emit(BossBeatEventKind.Phase, beat);
        Emit(BossBeatEventKind.Tempo, beat);
        Vector2 air = new Vector2(arena.center.x, Ceiling - 0.55f);
        StartMove(beat, beat + 1d, air, 0f);
        Queue(beat + 1d, volley => PatternName = "Transition Aimed Volley");
        for (int projectileIndex = 0; projectileIndex < 16; projectileIndex++)
        {
            double hitBeat = Math.Ceiling(beat + 1d + projectileIndex * 0.5d + TigerProjectile.MinimumFlightBeats);
            double spawnBeat = hitBeat - TigerProjectile.MinimumFlightBeats - (projectileIndex % 2 == 0 ? 0d : 0.5d);
            Queue(spawnBeat - 0.5d, warning =>
            {
                projectilePreviewVisible = true;
                Vector2 from = ProjectileOrigin;
                Vector2 target = player.Bounds.center;
                Emit(BossBeatEventKind.Warning, warning, Rect.MinMaxRect(Mathf.Min(from.x, target.x), Mathf.Min(from.y, target.y),
                    Mathf.Max(from.x, target.x), Mathf.Max(from.y, target.y)));
            });
            Queue(spawnBeat, spawn => SpawnProjectile(spawn, hitBeat));
        }
        Queue(beat + 9d, cleanup =>
        {
            PatternName = "Transition Projectile Drain";
            projectilePreviewVisible = false;
        });
        Queue(beat + 10d, descend =>
        {
            PatternName = "Transition Restore / Descend";
            Emit(BossBeatEventKind.Tempo, descend);
            StartMove(descend, beat + 11d, Ground(air.x), 0f);
        });
        Queue(beat + 11d, phaseTwo =>
        {
            Phase = TigerBossPhase.Phase2;
            patternIndex = 0;
            Emit(BossBeatEventKind.Phase, phaseTwo);
            BeginPattern(phaseTwo);
        });
    }

    private void SpawnProjectile(double beat, double hitBeat)
    {
        Vector2 origin = ProjectileOrigin;
        Vector2 target = player.Bounds.center;
        GameObject projectileObject = new GameObject("Tiger aimed projectile");
        TigerProjectile projectile = projectileObject.AddComponent<TigerProjectile>();
        projectile.Initialize(this, origin, target, beat, hitBeat);
        projectiles.Add(projectile);
        Queue(hitBeat, impact => projectile.Impact(), true);
        Emit(BossBeatEventKind.Projectile, beat,
            new Rect(origin - Vector2.one * TigerProjectile.Radius, Vector2.one * TigerProjectile.Radius * 2f));
    }

    private void AdvanceProjectiles(double beat)
    {
        for (int projectileIndex = projectiles.Count - 1; projectileIndex >= 0; projectileIndex--)
        {
            TigerProjectile projectile = projectiles[projectileIndex];
            if (projectile.AdvanceTo(beat))
            {
                if (!IsFighting) return;
                projectiles.RemoveAt(projectileIndex);
                Destroy(projectile.gameObject);
            }
            if (!IsFighting) return;
        }
    }

    public bool ResolvePlayerContact(Vector2 source)
    {
        if (!IsFighting || player.IsDead) return false;
        OrbitActions actions = player.Actions;
        if (actions.IsParrying)
        {
            Vector2 nearest = new Vector2(Mathf.Clamp(actions.CounterCenter.x, BodyBounds.xMin, BodyBounds.xMax),
                Mathf.Clamp(actions.CounterCenter.y, BodyBounds.yMin, BodyBounds.yMax));
            if (actions.ParrySequence != lastParrySequence && actions.CounterDamageMultiplier > 0f &&
                (nearest - actions.CounterCenter).sqrMagnitude <= actions.CounterRadius * actions.CounterRadius)
            {
                lastParrySequence = actions.ParrySequence;
                if (actions.CounterCausesGroggy) recoveryUntil = Math.Max(recoveryUntil, patternEnd + 1d);
                TryTakeDamage(10f * actions.CounterDamageMultiplier, actions.CounterCenter);
            }
            return true;
        }
        player.TryTakeDamage(10f, source);
        return true;
    }

    public void ResolveProjectileImpact(double beat, Vector2 position, float radius)
    {
        if (!IsFighting) return;
        Emit(BossBeatEventKind.Impact, beat, new Rect(position - Vector2.one * radius, Vector2.one * radius * 2f));
        if (!IsFighting) return;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, radius))
        {
            if (hit != playerCollider) continue;
            ResolvePlayerContact(position);
            break;
        }
    }

    private Vector2 Ground(float x) => new Vector2(Mathf.Clamp(x, arena.xMin + 0.6f, arena.xMax - 0.6f), FloorCenter);

    private void StartMove(double start, double end, Vector2 target, float arc)
    {
        moveFrom = transform.position;
        moveTo = target;
        moveStart = start;
        moveEnd = end;
        moveArc = arc;
        moving = true;
    }

    private void AdvanceMovement(double beat)
    {
        if (!moving) return;
        float progress = Mathf.Clamp01((float)((beat - moveStart) / (moveEnd - moveStart)));
        Vector2 position = Vector2.Lerp(moveFrom, moveTo, progress);
        position.y += 4f * moveArc * progress * (1f - progress);
        SetPosition(position);
        if (beat >= moveEnd) { SetPosition(moveTo); moving = false; }
    }

    private void SetPosition(Vector2 position) => transform.position = new Vector3(position.x, position.y, transform.position.z);

    private void RefreshView()
    {
        Color stateColor = Phase == TigerBossPhase.Transition ? new Color(0.4f, 0.85f, 1f) :
            IsAttackActive ? new Color(1f, 0.2f, 0.1f) :
            NextHitBeat.HasValue ? new Color(1f, 0.75f, 0.2f) : new Color(0.45f, 0.95f, 0.5f);
        view.SetAppearance(IsAttackActive ? attackFacing : facing, stateColor, visible);
        view.ShowAttack(ActiveAttackBounds ?? default, HitColor, IsAttackActive);
        view.ShowWarning(warningBounds ?? default, WarningColor, warningBounds.HasValue);
        view.ShowLanding(landingPosition, 2.2f, landingVisible);
        view.ShowDash(dashFrom, dashTo, dashVisible);
        view.ShowSpawn(spawnPosition, spawnVisible);
        view.ShowProjectilePath(ProjectileOrigin, player.Bounds.center, projectilePreviewVisible);
    }

    private void ClearTelegraphs()
    {
        warningBounds = null;
        attackBounds = null;
        landingVisible = dashVisible = spawnVisible = projectilePreviewVisible = false;
        spawnReservation.enabled = false;
        view.HideTelegraphs();
    }

    private void Emit(BossBeatEventKind kind, double plannedBeat, Rect? bounds = null)
    {
        BeatEventEmitted?.Invoke(new BossBeatEvent(kind, Phase, PatternName, plannedBeat,
            observedBeat, bounds, transform.position));
    }

    public void Stop()
    {
        if (!initialized || Phase == TigerBossPhase.Dead || Phase == TigerBossPhase.Stopped) return;
        Cleanup(TigerBossPhase.Stopped);
        PatternName = "Stopped";
        BeatEventEmitted = null;
    }

    private void Cleanup(TigerBossPhase terminalPhase)
    {
        Phase = terminalPhase;
        scheduled.Clear();
        moving = false;
        ClearTelegraphs();
        foreach (TigerProjectile projectile in projectiles)
        {
            projectile.Retire();
            Destroy(projectile.gameObject);
        }
        projectiles.Clear();
        if (Application.isPlaying) Managers.Beat.ReleaseTempoOverride();
        player.Died -= Stop;
        visible = terminalPhase != TigerBossPhase.Dead;
        view.SetAppearance(facing, Color.gray, visible);
    }

    private void OnDestroy()
    {
        if (!initialized) return;
        Stop();
        player.Died -= Stop;
    }
}
