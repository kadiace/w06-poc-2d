using UnityEngine;

public sealed class TigerProjectile : MonoBehaviour
{
    public const float Radius = 0.16f;
    public const double MinimumFlightBeats = 1d;
    private TigerBoss owner;
    private Vector2 origin;
    private double spawnedBeat;
    private bool retired;
    private Material visualMaterial;
    private Transform path;
    private Transform targetMarker;
    private Material pathMaterial;
    private Material targetMaterial;

    public double SpawnedBeat => spawnedBeat;
    public double ExpiresBeat { get; private set; }
    public bool IsRetired => retired;
    public Vector2 LockedTarget { get; private set; }
    public Vector2 Origin => origin;

    public void Initialize(TigerBoss boss, Vector2 position, Vector2 target, double beat, double hitBeat)
    {
        owner = boss;
        origin = position;
        LockedTarget = target;
        spawnedBeat = beat;
        ExpiresBeat = hitBeat;
        transform.position = new Vector3(position.x, position.y, -0.2f);
        Transform visual = PrototypeScene.Shape("Aimed shot", transform, PrimitiveType.Sphere, Vector3.zero,
            new Vector3(Radius * 2f, Radius * 2f, 0.08f), new Color(0.25f, 0.9f, 1f));
        visualMaterial = visual.GetComponent<Renderer>().sharedMaterial;
        path = PrototypeScene.Shape("Locked shot path", transform, PrimitiveType.Cube, Vector3.zero,
            new Vector3(Vector2.Distance(origin, LockedTarget), 0.035f, 0.02f), new Color(0.65f, 1f, 1f, 0.3f));
        Vector2 direction = LockedTarget - origin;
        path.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        targetMarker = PrototypeScene.Shape("Locked aim point", transform, PrimitiveType.Sphere, Vector3.zero,
            new Vector3(0.2f, 0.2f, 0.02f), new Color(0.65f, 1f, 1f, 0.45f));
        pathMaterial = path.GetComponent<Renderer>().sharedMaterial;
        targetMaterial = targetMarker.GetComponent<Renderer>().sharedMaterial;
        PositionPath(position);
    }

    public bool AdvanceTo(double beat)
    {
        if (retired) return true;
        float progress = Mathf.Clamp01((float)((beat - spawnedBeat) / (ExpiresBeat - spawnedBeat)));
        Vector2 position = Vector2.Lerp(origin, LockedTarget, progress);
        transform.position = new Vector3(position.x, position.y, -0.2f);
        PositionPath(position);
        return retired;
    }

    public void Impact()
    {
        if (retired) return;
        retired = true;
        owner.ResolveProjectileImpact(ExpiresBeat, LockedTarget, Radius);
    }

    private void PositionPath(Vector2 position)
    {
        Vector2 center = (origin + LockedTarget) * 0.5f - position;
        path.localPosition = new Vector3(center.x, center.y, 0.1f);
        Vector2 markerPosition = LockedTarget - position;
        targetMarker.localPosition = new Vector3(markerPosition.x, markerPosition.y, 0.08f);
    }

    public void Retire()
    {
        retired = true;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (visualMaterial != null) Destroy(visualMaterial);
        if (pathMaterial != null) Destroy(pathMaterial);
        if (targetMaterial != null) Destroy(targetMaterial);
    }
}
