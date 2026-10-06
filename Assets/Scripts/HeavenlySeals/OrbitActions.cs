using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(10)]
[RequireComponent(typeof(PlatformPlayer))]
public sealed class OrbitActions : MonoBehaviour
{
    public const float OrbitRadius = 0.5f;
    public const float CircleDiameter = 0.16f;
    public const float AngularSpeed = 4f * Mathf.PI / 3f;
    public bool CanAct { get; private set; }

    private PlatformPlayer player;
    private readonly Transform[] circles = new Transform[3];
    private Transform guide;
    private Material guideMaterial;
    private Transform parry;
    private Transform attackArea;
    private Transform swing;
    private Transform interaction;
    private float phase;
    private float parryStarted = -10f;
    private float attackStarted = -10f;
    private float interactionStarted = -10f;
    private const float ParryDuration = 0.18f;
    private const float AttackDuration = 0.22f;
    private const float InteractionDuration = 0.3f;

    private void Awake()
    {
        player = GetComponent<PlatformPlayer>();
        for (int i = 0; i < circles.Length; i++)
            circles[i] = Circle("Orbit circle " + (i + 1), CircleDiameter, new Color(1f, 0.76f, 0.3f), -0.2f);
        guide = Circle("Forward guide", CircleDiameter, new Color(0.3f, 1f, 0.65f, 0.35f), -0.15f);
        guideMaterial = guide.GetComponent<Renderer>().sharedMaterial;
        parry = Circle("Parry flash", 0.65f, new Color(0.3f, 0.9f, 1f, 0.6f), -0.3f);
        attackArea = PrototypeScene.Shape("Attack area (visual only)", transform, PrimitiveType.Cube,
            Vector3.zero, new Vector3(1.1f, 0.8f, 0.02f), new Color(1f, 0.4f, 0.2f, 0.22f));
        swing = PrototypeScene.Shape("Swing", transform, PrimitiveType.Cube,
            Vector3.zero, new Vector3(0.85f, 0.07f, 0.02f), new Color(1f, 0.85f, 0.5f));
        interaction = Circle("Interaction pulse", 1f, new Color(1f, 1f, 0.7f, 0.45f), -0.4f);
        parry.gameObject.SetActive(false);
        attackArea.gameObject.SetActive(false);
        swing.gameObject.SetActive(false);
        interaction.gameObject.SetActive(false);
    }

    private Transform Circle(string objectName, float diameter, Color color, float depth)
    {
        return PrototypeScene.Shape(objectName, transform, PrimitiveType.Sphere,
            new Vector3(0f, 0f, depth), new Vector3(diameter, diameter, 0.025f), color);
    }

    private void Update()
    {
        phase = Mathf.Repeat(phase + AngularSpeed * Time.deltaTime, 2f * Mathf.PI);
        Vector2 guidePosition = Vector2.right * (player.Facing * OrbitRadius);
        guide.localPosition = new Vector3(guidePosition.x, 0f, -0.15f);
        CanAct = false;
        for (int i = 0; i < circles.Length; i++)
        {
            float angle = phase + i * 2f * Mathf.PI / 3f;
            Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * OrbitRadius;
            circles[i].localPosition = new Vector3(position.x, position.y, -0.2f);
            // Two equal circles overlap when their center distance is at most their diameter.
            CanAct |= (position - guidePosition).sqrMagnitude <= CircleDiameter * CircleDiameter;
        }
        guideMaterial.color = CanAct ? new Color(0.3f, 1f, 0.65f, 0.9f) : new Color(0.3f, 1f, 0.65f, 0.35f);

        Keyboard keyboard = Keyboard.current;
        if (CanAct && keyboard != null)
        {
            if (keyboard.aKey.wasPressedThisFrame) parryStarted = Time.time;
            if (keyboard.sKey.wasPressedThisFrame) attackStarted = Time.time;
            if (keyboard.dKey.wasPressedThisFrame) interactionStarted = Time.time;
        }
        UpdateFeedback();
    }

    private void UpdateFeedback()
    {
        float parryProgress = (Time.time - parryStarted) / ParryDuration;
        parry.gameObject.SetActive(parryProgress < 1f);
        parry.localPosition = new Vector3(player.Facing * 0.55f, 0f, -0.3f);
        parry.localScale = new Vector3(0.25f, 0.65f, 0.025f) * (1f + parryProgress * 0.3f);
        SetAlpha(parry, 0.7f * (1f - parryProgress));

        float attackProgress = (Time.time - attackStarted) / AttackDuration;
        attackArea.gameObject.SetActive(attackProgress < 1f);
        swing.gameObject.SetActive(attackProgress < 1f);
        attackArea.localPosition = new Vector3(player.Facing * 0.75f, 0f, -0.3f);
        float angle = Mathf.Lerp(70f, -70f, Mathf.Clamp01(attackProgress)) * Mathf.Deg2Rad;
        swing.localPosition = new Vector3(player.Facing * Mathf.Cos(angle) * 0.65f, Mathf.Sin(angle) * 0.65f, -0.35f);
        swing.localRotation = Quaternion.Euler(0f, 0f, player.Facing * angle * Mathf.Rad2Deg);
        SetAlpha(attackArea, 0.25f * (1f - attackProgress));
        SetAlpha(swing, 1f - attackProgress);

        float interactionProgress = (Time.time - interactionStarted) / InteractionDuration;
        interaction.gameObject.SetActive(interactionProgress < 1f);
        float diameter = Mathf.Lerp(0.4f, 2.5f, Mathf.Clamp01(interactionProgress));
        interaction.localScale = new Vector3(diameter, diameter, 0.025f);
        SetAlpha(interaction, 0.55f * (1f - interactionProgress));
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
        GUI.Label(new Rect(16f, 36f, 550f, 24f), "On overlap only: A Parry    S Attack    D Interact    |    Beat: 0.5 s");
        GUI.color = CanAct ? new Color(0.3f, 1f, 0.65f) : Color.gray;
        GUI.Label(new Rect(16f, 60f, 350f, 24f), CanAct ? "TIMING OPEN" : "Wait for a circle to overlap the green guide");
        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
    }
}
