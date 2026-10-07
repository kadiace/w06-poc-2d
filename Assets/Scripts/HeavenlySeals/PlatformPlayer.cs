using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class PlatformPlayer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashDurationPerStage = 0.02f;
    [SerializeField] private float climbSpeed = 3f;

    public int Facing { get; private set; } = 1;
    public bool IsInvulnerable => Time.time < invulnerableEndsAt;
    public bool IsGrounded { get; private set; }
    public bool DashRequestedThisFrame { get; private set; }
    public bool IsClimbing => ladder != null;

    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private Transform nose;
    private Material bodyMaterial;
    private float horizontal;
    private bool jumpRequested;
    private float dashEndsAt;
    private float invulnerableEndsAt;
    private VineLadder ladder;
    private float vertical;
    private float climbReattachAt;
    private readonly Color bodyColor = new Color(0.3f, 0.8f, 1f);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        Transform visual = PrototypeScene.Shape("Body", transform, PrimitiveType.Cube,
            Vector3.zero, new Vector3(0.44f, 0.8f, 0.12f), bodyColor);
        bodyMaterial = visual.GetComponent<Renderer>().sharedMaterial;
        nose = PrototypeScene.Shape("Facing marker", transform, PrimitiveType.Cube,
            new Vector3(0.16f, 0.16f, -0.1f), new Vector3(0.12f, 0.1f, 0.03f), Color.white);
    }

    private void Update()
    {
        DashRequestedThisFrame = false;
        Keyboard keyboard = Keyboard.current;
        horizontal = keyboard == null ? 0f :
            (keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f);
        if (horizontal != 0f)
            Facing = horizontal > 0f ? 1 : -1;
        vertical = 0f;

        if (keyboard != null)
        {
            vertical = (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f);
            if (!IsClimbing && vertical > 0f && Time.time >= climbReattachAt)
            {
                foreach (Collider2D area in Physics2D.OverlapBoxAll(body.position, new Vector2(0.44f, 0.8f), 0f))
                {
                    VineLadder candidate = area.GetComponent<VineLadder>();
                    if (candidate == null || !candidate.CanClimbAt(bodyCollider.bounds)) continue;
                    ladder = candidate;
                    dashEndsAt = Time.time;
                    invulnerableEndsAt = Time.time;
                    jumpRequested = false;
                    body.linearVelocity = Vector2.zero;
                    break;
                }
            }
            if (IsClimbing && keyboard.spaceKey.wasPressedThisFrame)
            {
                LeaveLadder();
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
                jumpRequested = false;
            }
            else
                jumpRequested |= keyboard.spaceKey.wasPressedThisFrame;
            if (IsClimbing && ((horizontal != 0f && body.position.y >= ladder.ClimbTopY - 0.02f)
                || (vertical < 0f && body.position.y <= ladder.BottomY + 0.42f)))
                LeaveLadder();
            if (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)
            {
                DashRequestedThisFrame = true;
            }
        }

        nose.localPosition = new Vector3(Facing * 0.16f, 0.16f, -0.1f);
        bodyMaterial.color = IsInvulnerable ? Color.white : bodyColor;
    }

    public void StartDash(int stage)
    {
        if (IsClimbing) LeaveLadder();
        dashEndsAt = Time.time + dashDuration + dashDurationPerStage * stage;
        climbReattachAt = dashEndsAt;
        invulnerableEndsAt = stage > 0 ? Time.time + dashDuration : Time.time;
        bodyMaterial.color = IsInvulnerable ? Color.white : bodyColor;
    }

    private void LeaveLadder()
    {
        ladder = null;
        body.gravityScale = 3f;
        climbReattachAt = Time.time + 0.2f;
    }

    public void StopMovement()
    {
        ladder = null;
        dashEndsAt = invulnerableEndsAt = Time.time;
        horizontal = vertical = 0f;
        jumpRequested = DashRequestedThisFrame = false;
        body.linearVelocity = Vector2.zero;
        body.gravityScale = 3f;
    }

    private void FixedUpdate()
    {
        RaycastHit2D ground = Physics2D.BoxCast(body.position + Vector2.down * 0.4f,
            new Vector2(0.4f, 0.02f), 0f, Vector2.down, 0.06f, 1 << 6);
        IsGrounded = ground.collider != null && ground.normal.y > 0.5f;
        bool isDashing = Time.time < dashEndsAt;
        body.gravityScale = isDashing || IsClimbing ? 0f : 3f;
        if (isDashing)
            body.linearVelocity = new Vector2(Facing * dashSpeed, 0f);
        else if (IsClimbing)
        {
            float climbVelocity = Mathf.Clamp(vertical * climbSpeed,
                (ladder.BottomY + 0.4f - body.position.y) / Time.fixedDeltaTime,
                (ladder.ClimbTopY - body.position.y) / Time.fixedDeltaTime);
            float alignmentVelocity = Mathf.Clamp((ladder.transform.position.x - body.position.x) / Time.fixedDeltaTime, -moveSpeed, moveSpeed);
            body.linearVelocity = new Vector2(alignmentVelocity, climbVelocity);
        }
        else
            body.linearVelocity = new Vector2(horizontal * moveSpeed,
                jumpRequested && IsGrounded ? jumpSpeed : body.linearVelocity.y);
        jumpRequested = false;
    }
}
