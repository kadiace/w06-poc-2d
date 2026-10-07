using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class RestorablePlant : MonoBehaviour
{
    public static readonly Color DeadColor = new Color(0.08f, 0.25f, 0.12f);
    public static readonly Color AliveColor = new Color(0.5f, 0.95f, 0.45f);
    public bool IsAlive { get; private set; }
    public bool IsGrowing { get; private set; }

    private Renderer platformVisual;
    private Transform sprouts;
    private float growthStarted;
    private const float GrowthDuration = 0.35f;

    private void Awake()
    {
        platformVisual = transform.Find("Surface").GetComponent<Renderer>();
        platformVisual.sharedMaterial.color = DeadColor;
        sprouts = new GameObject("Plant sprouts").transform;
        sprouts.SetParent(transform, false);
        sprouts.localPosition = new Vector3(0f, GetComponent<BoxCollider2D>().size.y * 0.5f, 0f);
        PrototypeScene.Shape("Stem", sprouts, PrimitiveType.Cube,
            new Vector3(0f, 0.25f, -0.1f), new Vector3(0.08f, 0.5f, 0.08f), AliveColor);
        PrototypeScene.Shape("Left leaf", sprouts, PrimitiveType.Sphere,
            new Vector3(-0.15f, 0.38f, -0.12f), new Vector3(0.28f, 0.14f, 0.08f), AliveColor);
        PrototypeScene.Shape("Right leaf", sprouts, PrimitiveType.Sphere,
            new Vector3(0.15f, 0.31f, -0.12f), new Vector3(0.28f, 0.14f, 0.08f), AliveColor);
        sprouts.localScale = new Vector3(1f, 0.001f, 1f);
        sprouts.gameObject.SetActive(false);
    }

    public void Restore()
    {
        if (IsAlive) return;
        IsAlive = true;
        platformVisual.sharedMaterial.color = AliveColor;
        sprouts.gameObject.SetActive(true);
        growthStarted = Time.time;
        IsGrowing = true;
    }

    private void Update()
    {
        if (!IsGrowing) return;
        float progress = Mathf.Clamp01((Time.time - growthStarted) / GrowthDuration);
        sprouts.localScale = new Vector3(1f, Mathf.Lerp(0.001f, 1f, progress), 1f);
        if (progress >= 1f) IsGrowing = false;
    }
}
