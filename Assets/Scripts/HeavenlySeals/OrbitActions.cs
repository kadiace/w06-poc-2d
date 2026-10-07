using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[DefaultExecutionOrder(10)]
[RequireComponent(typeof(PlatformPlayer))]
public sealed class OrbitActions : MonoBehaviour
{
    public const float OrbitRadius = 0.5f;
    public float CircleDiameter
    {
        get
        {
            // Beyond half a beat, adjacent success windows already cover the entire cycle.
            float halfWindowAngle = Mathf.Min(AngularSpeed * EffectiveTimingToleranceSeconds, Mathf.PI / 3f);
            return 2f * OrbitRadius * Mathf.Sin(halfWindowAngle * 0.5f);
        }
    }
    public static float AngularSpeed => 2f * Mathf.PI / (3f * Managers.Beat.BeatInterval);
    public float TimingToleranceBonusSeconds { get; private set; }
    public float EffectiveTimingToleranceSeconds => Managers.Beat.GameInfo.TimingToleranceSeconds
        + Mathf.Min(TimingToleranceBonusSeconds, Managers.Beat.GameInfo.MaxParryToleranceBonusSeconds);
    public int ConsecutiveParryFailures { get; private set; }
    public int ConsecutiveParrySuccesses { get; private set; }
    public bool CanAct { get; private set; }
    public int StreakStage { get; private set; }
    public int ParryStage { get; private set; }
    public int AttackStage { get; private set; }
    public int InteractionStage { get; private set; }
    public int AttackSequence { get; private set; }
    public int ParrySequence { get; private set; }
    public bool IsParrying => Time.time - parryStarted < ParryDuration;
    public bool IsAttacking => Time.time - attackStarted < AttackDuration;
    public bool IsInteracting => Time.time - interactionStarted < InteractionDuration;
    public float CounterDamageMultiplier => Mathf.Max(0, ParryStage - 1) * 0.5f;
    public bool CounterCausesGroggy => ParryStage >= 2;
    public float CounterRadius => ParryStage >= 2 ? 0.8f : 0f;
    public Vector2 CounterCenter => transform.position;
    public float AttackDamageMultiplier => StageScale(AttackStage);
    public Vector2 AttackSize => new Vector2(1.1f, 0.8f) * StageScale(AttackStage);
    public Vector2 AttackCenter => (Vector2)transform.position + Vector2.right * (player.Facing * 0.75f * StageScale(AttackStage));
    public Rect AttackBounds => new Rect(AttackCenter - AttackSize * 0.5f, AttackSize);
    public float InteractionRadius => 1.25f * StageScale(InteractionStage);
    public Vector2 InteractionCenter => transform.position;
    public float InteractionPulseRadius { get; private set; }

    private PlatformPlayer player;
    private readonly Transform[] circles = new Transform[3];
    private Transform guide;
    private Transform parry;
    private Transform counter;
    private Transform attackArea;
    private Transform swing;
    private Transform interaction;
    private float parryStarted = -10f;
    private float attackStarted = -10f;
    private float interactionStarted = -10f;
    private bool interactionExpanding;
    private float interactionMaxRadius;
    private readonly HashSet<Component> interactionTargets = new HashSet<Component>();
    private const float ParryDuration = 0.18f;
    private const float AttackDuration = 0.22f;
    private const float InteractionDuration = 0.3f;

    private void Awake()
    {
        player = GetComponent<PlatformPlayer>();
        for (int i = 0; i < circles.Length; i++)
            circles[i] = Circle("Orbit circle " + (i + 1), CircleDiameter, new Color(1f, 0.76f, 0.3f), -0.2f);
        guide = Circle("Forward guide", CircleDiameter, new Color(0.3f, 1f, 0.65f, 0.25f), -0.15f);
        parry = Circle("Parry flash", 0.65f, new Color(0.3f, 0.9f, 1f, 0.6f), -0.3f);
        counter = Circle("Counter range (visual only)", 1.6f, new Color(0.3f, 0.9f, 1f, 0.25f), -0.25f);
        attackArea = PrototypeScene.Shape("Attack area (visual only)", transform, PrimitiveType.Cube,
            Vector3.zero, new Vector3(1.1f, 0.8f, 0.02f), new Color(1f, 0.4f, 0.2f, 0.22f));
        swing = PrototypeScene.Shape("Swing", transform, PrimitiveType.Cube,
            Vector3.zero, new Vector3(0.85f, 0.07f, 0.02f), new Color(1f, 0.85f, 0.5f));
        interaction = Circle("Interaction pulse", 1f, new Color(1f, 1f, 0.7f, 0.45f), -0.4f);
        parry.gameObject.SetActive(false);
        counter.gameObject.SetActive(false);
        attackArea.gameObject.SetActive(false);
        swing.gameObject.SetActive(false);
        interaction.gameObject.SetActive(false);
    }

    private Transform Circle(string objectName, float diameter, Color color, float depth)
    {
        return PrototypeScene.Shape(objectName, transform, PrimitiveType.Sphere,
            new Vector3(0f, 0f, depth), new Vector3(diameter, diameter, 0.025f), color);
    }

    private void UpdateTiming()
    {
        float phase = (float)(Managers.Beat.TotalBeats % circles.Length) * 2f * Mathf.PI / circles.Length;
        float circleDiameter = CircleDiameter;
        Vector3 circleScale = new Vector3(circleDiameter, circleDiameter, 0.025f);
        Vector2 guidePosition = Vector2.right * (player.Facing * OrbitRadius);
        guide.localPosition = new Vector3(guidePosition.x, 0f, -0.15f);
        guide.localScale = circleScale;
        CanAct = false;
        for (int i = 0; i < circles.Length; i++)
        {
            float angle = phase + i * 2f * Mathf.PI / 3f;
            Vector2 position = new Vector2(player.Facing * Mathf.Cos(angle), Mathf.Sin(angle)) * OrbitRadius;
            circles[i].localPosition = new Vector3(position.x, position.y, -0.2f);
            circles[i].localScale = circleScale;
            // Two equal circles overlap when their center distance is at most their diameter.
            CanAct |= (position - guidePosition).sqrMagnitude <= circleDiameter * circleDiameter;
        }

    }

    private void Update()
    {
        UpdateTiming();
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.wasPressedThisFrame)
            {
                bool success = AdvanceStage();
                if (success)
                {
                    ParryStage = StreakStage;
                    parryStarted = Time.time;
                    ParrySequence++;
                }
                AdjustParryTolerance(success);
            }
            if (keyboard.sKey.wasPressedThisFrame && AdvanceStage())
            {
                AttackStage = StreakStage;
                attackStarted = Time.time;
                AttackSequence++;
            }
            if (keyboard.dKey.wasPressedThisFrame && AdvanceStage())
            {
                InteractionStage = StreakStage;
                interactionStarted = Time.time;
                interactionMaxRadius = InteractionRadius;
                interactionExpanding = true;
                interactionTargets.Clear();
            }
        }
        if (player.DashRequestedThisFrame && AdvanceStage())
            player.StartDash(StreakStage);
        if (keyboard != null && keyboard.aKey.wasPressedThisFrame)
            UpdateTiming();
        UpdateFeedback();
    }

    private void AdjustParryTolerance(bool success)
    {
        GameInfo info = Managers.Beat.GameInfo;
        if (success)
        {
            ConsecutiveParryFailures = 0;
            ConsecutiveParrySuccesses++;
            TimingToleranceBonusSeconds = Mathf.Max(0f, TimingToleranceBonusSeconds
                - info.ParryToleranceStepSeconds * ConsecutiveParrySuccesses);
        }
        else
        {
            ConsecutiveParrySuccesses = 0;
            ConsecutiveParryFailures++;
            TimingToleranceBonusSeconds = Mathf.Min(info.MaxParryToleranceBonusSeconds,
                TimingToleranceBonusSeconds + info.ParryToleranceStepSeconds * ConsecutiveParryFailures);
        }
    }

    private bool AdvanceStage()
    {
        StreakStage = CanAct ? Mathf.Min(StreakStage + 1, 3) : 0;
        return CanAct;
    }

    public void CancelActions()
    {
        parryStarted = attackStarted = interactionStarted = -10f;
        interactionExpanding = false;
        interactionTargets.Clear();
        UpdateFeedback();
    }

    // Stage 0 uses the original base performance; stages 1-3 keep their existing scaling.
    private static float StageScale(int stage) => stage == 0 ? 1f : 1f + (stage - 1) * 0.5f;

    private void UpdateFeedback()
    {
        float parryProgress = (Time.time - parryStarted) / ParryDuration;
        parry.gameObject.SetActive(parryProgress < 1f);
        parry.localPosition = new Vector3(player.Facing * 0.55f, 0f, -0.3f);
        parry.localScale = new Vector3(0.25f, 0.65f, 0.025f) * (1f + parryProgress * 0.3f);
        SetAlpha(parry, 0.7f * (1f - parryProgress));
        counter.gameObject.SetActive(IsParrying && CounterCausesGroggy);
        counter.localScale = new Vector3(CounterRadius * 2f, CounterRadius * 2f, 0.025f);
        SetAlpha(counter, CounterDamageMultiplier * 0.4f * (1f - parryProgress));

        float attackProgress = (Time.time - attackStarted) / AttackDuration;
        attackArea.gameObject.SetActive(attackProgress < 1f);
        swing.gameObject.SetActive(attackProgress < 1f);
        float attackScale = StageScale(AttackStage);
        attackArea.localPosition = new Vector3(player.Facing * 0.75f * attackScale, 0f, -0.3f);
        attackArea.localScale = new Vector3(AttackSize.x, AttackSize.y, 0.02f);
        float angle = Mathf.Lerp(70f, -70f, Mathf.Clamp01(attackProgress)) * Mathf.Deg2Rad;
        swing.localPosition = new Vector3(player.Facing * Mathf.Cos(angle) * 0.65f * attackScale, Mathf.Sin(angle) * 0.65f * attackScale, -0.35f);
        swing.localScale = new Vector3(0.85f * attackScale, 0.07f * attackScale, 0.02f);
        swing.localRotation = Quaternion.Euler(0f, 0f, player.Facing * angle * Mathf.Rad2Deg);
        SetAlpha(attackArea, 0.25f * (1f - attackProgress));
        SetAlpha(swing, 1f - attackProgress);

        float interactionProgress = (Time.time - interactionStarted) / InteractionDuration;
        interaction.gameObject.SetActive(interactionExpanding);
        InteractionPulseRadius = interactionExpanding ? Mathf.Lerp(0.2f, interactionMaxRadius, Mathf.Clamp01(interactionProgress)) : 0f;
        float diameter = InteractionPulseRadius * 2f;
        interaction.localScale = new Vector3(diameter, diameter, 0.025f);
        SetAlpha(interaction, 0.55f * Mathf.Max(0.15f, 1f - interactionProgress));
        if (interactionExpanding)
        {
            foreach (Collider2D target in Physics2D.OverlapCircleAll(InteractionCenter, InteractionPulseRadius))
            {
                RestorablePlant plant = target.GetComponent<RestorablePlant>();
                if (plant != null && interactionTargets.Add(plant)) plant.Restore();
                VineLadder vine = target.GetComponent<VineLadder>();
                if (vine != null && interactionTargets.Add(vine)) vine.Restore();
            }
            if (interactionProgress >= 1f)
                interactionExpanding = false;
        }
    }

    private static void SetAlpha(Transform visual, float alpha)
    {
        Material material = visual.GetComponent<Renderer>().sharedMaterial;
        Color color = material.color;
        color.a = Mathf.Clamp01(alpha);
        material.color = color;
    }

    private void OnGUI()
    {
        GUI.matrix = Matrix4x4.Scale(Vector3.one * (Screen.height / 720f));
        GUI.color = Color.white;
        GUI.Label(new Rect(16f, 12f, 550f, 24f), "LEFT / RIGHT: Move    SPACE: Jump    SHIFT: Dash (invulnerable)");
        GUI.Label(new Rect(16f, 36f, 550f, 24f), "On overlap only: A Parry    S Attack    D Interact    SHIFT Dash");
        GUI.color = CanAct ? new Color(0.3f, 1f, 0.65f) : Color.gray;
        GUI.Label(new Rect(16f, 60f, 350f, 24f), CanAct ? "TIMING OPEN" : "Off timing: actions blocked");
        GUI.color = Color.white;
        GUI.Label(new Rect(16f, 84f, 550f, 24f), $"Streak: {StreakStage}/3 (miss resets)    A: {ParryStage}    S: {AttackStage}    D: {InteractionStage}");
        GUI.Label(new Rect(16f, 108f, 650f, 24f), $"Counter: {CounterDamageMultiplier:P0} / Groggy: {CounterCausesGroggy}    Attack: x{AttackDamageMultiplier:0.0}    Interact radius: {InteractionRadius:0.00}");
        GUI.Label(new Rect(16f, 132f, 720f, 24f), "D: restore blocks / grow vine    UP / DOWN: climb    SPACE: leave vine    RIGHT at top: exit");
        GUI.Label(new Rect(16f, 188f, 720f, 24f), $"Timing: +/- {EffectiveTimingToleranceSeconds:0.000}s    Parry assist: +{TimingToleranceBonusSeconds:0.000}s    Fail: {ConsecutiveParryFailures}    Success: {ConsecutiveParrySuccesses}");
        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
    }
}
