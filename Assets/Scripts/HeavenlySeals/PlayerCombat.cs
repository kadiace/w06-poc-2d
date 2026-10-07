using System;
using UnityEngine;

public interface IDamageable
{
    bool TryTakeDamage(float damage, Vector2 source);
}

[RequireComponent(typeof(PlatformPlayer), typeof(OrbitActions))]
[DefaultExecutionOrder(5)]
public sealed class PlayerCombat : MonoBehaviour, IDamageable
{
    public PlatformPlayer Player { get; private set; }
    public OrbitActions Actions { get; private set; }
    public float MaxHealth => 100f;
    public float CurrentHealth { get; private set; } = 100f;
    public bool IsDead => CurrentHealth <= 0f;
    public Rect Bounds
    {
        get
        {
            Bounds bounds = bodyCollider.bounds;
            return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
        }
    }
    public event Action Died;

    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private double protectedUntil;
    private BossArea arenaGate;
    private Rect arenaMovementBounds;

    private void Awake()
    {
        Player = GetComponent<PlatformPlayer>();
        Actions = GetComponent<OrbitActions>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
    }

    public void InitializeArena(Rect movementBounds, BossArea gate)
    {
        arenaMovementBounds = movementBounds;
        arenaGate = gate;
    }

    public bool TryTakeDamage(float damage, Vector2 source)
    {
        if (damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
            throw new ArgumentOutOfRangeException(nameof(damage));
        if (IsDead || Player.IsInvulnerable || Actions.IsParrying || Time.timeAsDouble < protectedUntil)
            return false;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        protectedUntil = Time.timeAsDouble + 0.45d;
        if (IsDead)
        {
            Player.StopMovement();
            Actions.CancelActions();
            Player.enabled = false;
            Actions.enabled = false;
            Died?.Invoke();
        }
        return true;
    }

    private void FixedUpdate()
    {
        if (IsDead || arenaGate == null || !arenaGate.HasArrived) return;
        Vector2 velocity = body.linearVelocity;
        velocity.x = Mathf.Clamp(velocity.x,
            (arenaMovementBounds.xMin - body.position.x) / Time.fixedDeltaTime,
            (arenaMovementBounds.xMax - body.position.x) / Time.fixedDeltaTime);
        body.linearVelocity = velocity;
    }

    private void OnGUI()
    {
        if (arenaGate == null || !arenaGate.HasArrived) return;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * (Screen.height / 720f));
        GUI.color = IsDead ? Color.red : new Color(0.3f, 0.8f, 1f);
        GUI.Label(new Rect(16f, 216f, 520f, 24f), IsDead ? "PLAYER DOWN - restart Play Mode to retry" : $"Player HP: {CurrentHealth:0}/{MaxHealth:0}");
        GUI.color = new Color(0.08f, 0.09f, 0.12f);
        GUI.DrawTexture(new Rect(16f, 242f, 200f, 12f), Texture2D.whiteTexture);
        GUI.color = new Color(0.3f, 0.8f, 1f);
        GUI.DrawTexture(new Rect(18f, 244f, 196f * CurrentHealth / MaxHealth, 8f), Texture2D.whiteTexture);
        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
    }
}
