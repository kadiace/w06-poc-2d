using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class VineLadder : MonoBehaviour
{
    public bool IsGrowing { get; private set; }
    public bool IsGrown { get; private set; }
    public float BottomY => transform.position.y;
    public float TopY { get; private set; }
    public int GrowthStage { get; private set; }
    public bool IsClimbable => climbArea != null && climbArea.enabled;
    public float ClimbTopY => BottomY + stem.localScale.y;
    public const int MaxGrowthStage = 6;

    private Transform stem;
    private Transform[] rungs;
    private BoxCollider2D climbArea;
    private float growthStarted;
    private float growthFromHeight;
    private float growthStepHeight;
    private const float GrowthDuration = 0.35f;

    public void Initialize(float topY)
    {
        growthStepHeight = (topY - BottomY) / 5f;
        TopY = BottomY + growthStepHeight * MaxGrowthStage;
        CircleCollider2D root = GetComponent<CircleCollider2D>();
        root.isTrigger = true;
        root.radius = 0.25f;
        root.offset = new Vector2(0f, 0.25f);
        float height = TopY - BottomY;
        climbArea = gameObject.AddComponent<BoxCollider2D>();
        climbArea.isTrigger = true;
        climbArea.offset = new Vector2(0f, height * 0.5f + 0.2f);
        climbArea.size = new Vector2(0.6f, height + 0.4f);
        climbArea.enabled = false;
        stem = PrototypeScene.Shape("Vine stem", transform, PrimitiveType.Cube,
            new Vector3(0f, 0.25f, -0.1f), new Vector3(0.12f, 0.5f, 0.1f), RestorablePlant.DeadColor);
        rungs = new Transform[Mathf.CeilToInt(height / 0.35f)];
        for (int i = 0; i < rungs.Length; i++)
        {
            rungs[i] = PrototypeScene.Shape("Vine rung " + (i + 1), transform, PrimitiveType.Cube,
                new Vector3(0f, 0.25f + i * 0.35f, -0.12f), new Vector3(0.45f, 0.07f, 0.08f), RestorablePlant.DeadColor);
            rungs[i].gameObject.SetActive(i == 0);
        }
    }

    public void Restore()
    {
        if (GrowthStage >= MaxGrowthStage) return;
        GrowthStage++;
        growthFromHeight = stem.localScale.y;
        IsGrowing = true;
        growthStarted = Time.time;
        stem.GetComponent<Renderer>().sharedMaterial.color = RestorablePlant.AliveColor;
        foreach (Transform rung in rungs)
            rung.GetComponent<Renderer>().sharedMaterial.color = RestorablePlant.AliveColor;
    }

    private void Update()
    {
        if (!IsGrowing) return;
        float progress = Mathf.Clamp01((Time.time - growthStarted) / GrowthDuration);
        float height = Mathf.Lerp(growthFromHeight, growthStepHeight * GrowthStage, progress);
        stem.localPosition = new Vector3(0f, height * 0.5f, -0.1f);
        stem.localScale = new Vector3(0.12f, height, 0.1f);
        foreach (Transform rung in rungs)
            rung.gameObject.SetActive(rung.localPosition.y <= height);
        climbArea.offset = new Vector2(0f, height * 0.5f + 0.2f);
        climbArea.size = new Vector2(0.6f, height + 0.4f);
        if (progress >= 1f)
        {
            IsGrowing = false;
            IsGrown = GrowthStage == MaxGrowthStage;
            climbArea.enabled = true;
        }
    }

    public bool CanClimbAt(Bounds playerBounds)
    {
        return IsClimbable && climbArea.bounds.Intersects(playerBounds);
    }
}
