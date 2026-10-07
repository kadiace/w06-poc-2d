using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class BossArea : MonoBehaviour
{
    private const float ArrivalFlashDuration = 0.8f;

    private readonly Color waitingColor = new Color(1f, 0.76f, 0.3f, 0.18f);
    private readonly Color arrivedColor = new Color(0.3f, 1f, 0.65f, 0.3f);
    private readonly Color flashColor = new Color(1f, 1f, 0.7f, 0.65f);

    public bool HasArrived { get; private set; }

    private PlatformPlayer player;
    private Rigidbody2D playerBody;
    private BoxCollider2D areaCollider;
    private Transform marker;
    private Transform arrivalFlash;
    private TextMesh label;
    private Vector2 areaSize;
    private float arrivedAt;

    public void Initialize(PlatformPlayer player, Vector2 size)
    {
        this.player = player;
        playerBody = player.GetComponent<Rigidbody2D>();
        areaSize = size;
        areaCollider = GetComponent<BoxCollider2D>();
        areaCollider.size = size;
        areaCollider.isTrigger = true;

        marker = PrototypeScene.Shape("Boss area marker", transform, PrimitiveType.Cube,
            new Vector3(0f, -size.y * 0.5f + 0.2f, 0.15f), new Vector3(size.x, 0.12f, 0.02f), waitingColor);
        arrivalFlash = PrototypeScene.Shape("Boss area arrival flash", transform, PrimitiveType.Cube,
            new Vector3(0f, -size.y * 0.5f + 0.2f, 0.1f), new Vector3(size.x, 0.18f, 0.02f), flashColor);
        arrivalFlash.gameObject.SetActive(false);

        GameObject labelObject = new GameObject("Boss Area label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, size.y * 0.5f + 0.45f, -0.1f);
        label = labelObject.AddComponent<TextMesh>();
        label.text = "Boss Area";
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 64;
        label.characterSize = 0.08f;
        label.fontStyle = FontStyle.Bold;
        label.color = new Color(1f, 0.85f, 0.5f);
        label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
    }

    private void Update()
    {
        if (!HasArrived)
        {
            Bounds bounds = areaCollider.bounds;
            Vector2 bodyPosition = playerBody.position;
            Vector3 playerCenter = new Vector3(bodyPosition.x, bodyPosition.y, bounds.center.z);
            if (player.IsGrounded && Mathf.Abs(playerBody.linearVelocity.y) < 0.1f && bounds.Contains(playerCenter))
                Arrive();
        }

        if (!HasArrived)
            return;

        float progress = (Time.time - arrivedAt) / ArrivalFlashDuration;
        bool isFlashing = progress < 1f;
        arrivalFlash.gameObject.SetActive(isFlashing);
        if (!isFlashing)
            return;

        progress = Mathf.Clamp01(progress);
        float scale = Mathf.Lerp(0.85f, 1.15f, progress);
        arrivalFlash.localScale = new Vector3(areaSize.x * scale, 0.18f * scale, 0.02f);
        SetAlpha(arrivalFlash, flashColor.a * (1f - progress));
    }

    private void Arrive()
    {
        HasArrived = true;
        arrivedAt = Time.time;
        marker.GetComponent<Renderer>().sharedMaterial.color = arrivedColor;
        label.color = new Color(0.3f, 1f, 0.65f);
        arrivalFlash.gameObject.SetActive(true);
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
        if (!HasArrived)
            return;

        GUI.matrix = Matrix4x4.Scale(Vector3.one * (Screen.height / 720f));
        float flash = 1f - Mathf.Clamp01((Time.time - arrivedAt) / ArrivalFlashDuration);
        GUI.color = Color.Lerp(new Color(0.3f, 1f, 0.65f), Color.white, flash);
        GUI.Label(new Rect(16f, 164f, 500f, 24f), "Boss arena entered - rhythm tiger encounter");
        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
    }
}
