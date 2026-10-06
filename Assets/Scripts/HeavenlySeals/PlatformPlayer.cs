using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class PlatformPlayer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.35f;

    public int Facing { get; private set; } = 1;
    public bool IsInvulnerable => Time.time < dashEndsAt;
    public bool IsGrounded { get; private set; }

    private Rigidbody2D body;
    private Transform nose;
    private Material bodyMaterial;
    private float horizontal;
    private bool jumpRequested;
    private float dashEndsAt;
    private float nextDashAt;
    private readonly Color bodyColor = new Color(0.3f, 0.8f, 1f);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        Transform visual = PrototypeScene.Shape("Body", transform, PrimitiveType.Cube,
            Vector3.zero, new Vector3(0.44f, 0.8f, 0.12f), bodyColor);
        bodyMaterial = visual.GetComponent<Renderer>().sharedMaterial;
        nose = PrototypeScene.Shape("Facing marker", transform, PrimitiveType.Cube,
            new Vector3(0.16f, 0.16f, -0.1f), new Vector3(0.12f, 0.1f, 0.03f), Color.white);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        horizontal = keyboard == null ? 0f :
            (keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f);
        if (horizontal != 0f)
            Facing = horizontal > 0f ? 1 : -1;

        if (keyboard != null)
        {
            jumpRequested |= keyboard.spaceKey.wasPressedThisFrame;
            if ((keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)
                && Time.time >= nextDashAt)
            {
                dashEndsAt = Time.time + dashDuration;
                nextDashAt = Time.time + dashCooldown;
            }
        }

        nose.localPosition = new Vector3(Facing * 0.16f, 0.16f, -0.1f);
        bodyMaterial.color = IsInvulnerable ? Color.white : bodyColor;
    }

    private void FixedUpdate()
    {
        RaycastHit2D ground = Physics2D.BoxCast(body.position + Vector2.down * 0.4f,
            new Vector2(0.4f, 0.02f), 0f, Vector2.down, 0.06f, 1 << 6);
        IsGrounded = ground.collider != null && ground.normal.y > 0.5f;
        body.gravityScale = IsInvulnerable ? 0f : 3f;
        if (IsInvulnerable)
            body.linearVelocity = new Vector2(Facing * dashSpeed, 0f);
        else
            body.linearVelocity = new Vector2(horizontal * moveSpeed,
                jumpRequested && IsGrounded ? jumpSpeed : body.linearVelocity.y);
        jumpRequested = false;
    }
}
