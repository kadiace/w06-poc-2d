using UnityEngine;

public sealed class TigerBossView : MonoBehaviour
{
    private static readonly Color TigerOrange = new Color(1f, 0.52f, 0.12f);
    private static readonly Color StripeColor = new Color(0.16f, 0.07f, 0.025f);
    private static readonly Color LandingColor = new Color(0.68f, 0.3f, 1f, 0.45f);
    private static readonly Color DashColor = new Color(1f, 0.68f, 0.16f, 0.45f);
    private static readonly Color SpawnColor = new Color(0.2f, 0.9f, 1f, 0.35f);
    private static readonly Color ProjectileColor = new Color(0.2f, 0.9f, 1f, 0.42f);

    private const float TelegraphDepth = -0.3f;
    private const float TelegraphThickness = 0.02f;

    private Transform creatureVisual;
    private Transform head;
    private Transform nearEar;
    private Transform farEar;
    private Transform eye;
    private Transform directionMarker;
    private Material bodyMaterial;
    private Material headMaterial;
    private Material nearEarMaterial;
    private Material farEarMaterial;

    private Transform attackIndicator;
    private Transform warningIndicator;
    private Transform landingIndicator;
    private Transform dashPath;
    private Transform dashDirectionMarker;
    private Transform spawnIndicator;
    private Transform projectilePath;

    public void Initialize()
    {
        if (creatureVisual != null)
            return;

        creatureVisual = new GameObject("Creature visual").transform;
        creatureVisual.SetParent(transform, false);

        Transform body = PrototypeScene.Shape("Body", creatureVisual, PrimitiveType.Cube,
            Vector3.zero, new Vector3(1.1f, 0.65f, 0.18f), TigerOrange);
        bodyMaterial = body.GetComponent<Renderer>().sharedMaterial;

        head = PrototypeScene.Shape("Head", creatureVisual, PrimitiveType.Sphere,
            new Vector3(0.55f, 0.25f, -0.01f), new Vector3(0.5f, 0.5f, 0.18f), TigerOrange);
        headMaterial = head.GetComponent<Renderer>().sharedMaterial;

        nearEar = PrototypeScene.Shape("Near ear", creatureVisual, PrimitiveType.Sphere,
            new Vector3(0.66f, 0.51f, -0.005f), new Vector3(0.18f, 0.22f, 0.11f), TigerOrange);
        farEar = PrototypeScene.Shape("Far ear", creatureVisual, PrimitiveType.Sphere,
            new Vector3(0.43f, 0.51f, 0.01f), new Vector3(0.18f, 0.22f, 0.11f), TigerOrange);
        nearEarMaterial = nearEar.GetComponent<Renderer>().sharedMaterial;
        farEarMaterial = farEar.GetComponent<Renderer>().sharedMaterial;

        Transform leftStripe = PrototypeScene.Shape("Stripe 1", creatureVisual, PrimitiveType.Cube,
            new Vector3(-0.32f, 0.08f, -0.105f), new Vector3(0.09f, 0.31f, 0.025f), StripeColor);
        leftStripe.localRotation = Quaternion.Euler(0f, 0f, -14f);
        PrototypeScene.Shape("Stripe 2", creatureVisual, PrimitiveType.Cube,
            new Vector3(0f, 0.11f, -0.105f), new Vector3(0.09f, 0.34f, 0.025f), StripeColor);
        Transform rightStripe = PrototypeScene.Shape("Stripe 3", creatureVisual, PrimitiveType.Cube,
            new Vector3(0.31f, 0.08f, -0.105f), new Vector3(0.09f, 0.29f, 0.025f), StripeColor);
        rightStripe.localRotation = Quaternion.Euler(0f, 0f, 14f);

        eye = PrototypeScene.Shape("Eye", creatureVisual, PrimitiveType.Sphere,
            new Vector3(0.68f, 0.31f, -0.11f), new Vector3(0.11f, 0.11f, 0.025f), Color.white);
        directionMarker = PrototypeScene.Shape("Direction marker", creatureVisual, PrimitiveType.Cube,
            new Vector3(0.82f, 0.21f, -0.12f), new Vector3(0.14f, 0.1f, 0.03f), StripeColor);

        attackIndicator = CreateTelegraph("Attack indicator", PrimitiveType.Cube, Color.clear);
        warningIndicator = CreateTelegraph("Warning indicator", PrimitiveType.Cube, Color.clear);
        landingIndicator = CreateTelegraph("Landing indicator", PrimitiveType.Cube, LandingColor);
        dashPath = CreateTelegraph("Dash path", PrimitiveType.Cube, DashColor);
        dashDirectionMarker = CreateTelegraph("Dash direction marker", PrimitiveType.Cube, DashColor);
        spawnIndicator = CreateTelegraph("Spawn indicator", PrimitiveType.Cube, SpawnColor);
        projectilePath = CreateTelegraph("Tracking projectile aim", PrimitiveType.Cube, ProjectileColor);

        HideTelegraphs();
        SetAppearance(1, TigerOrange, true);
    }

    public void SetAppearance(int facing, Color stateColor, bool visible)
    {
        int direction = facing < 0 ? -1 : 1;
        bodyMaterial.color = stateColor;
        headMaterial.color = stateColor;
        nearEarMaterial.color = stateColor;
        farEarMaterial.color = stateColor;

        head.localPosition = new Vector3(direction * 0.55f, 0.25f, -0.01f);
        nearEar.localPosition = new Vector3(direction * 0.66f, 0.51f, -0.005f);
        farEar.localPosition = new Vector3(direction * 0.43f, 0.51f, 0.01f);
        eye.localPosition = new Vector3(direction * 0.68f, 0.31f, -0.11f);
        directionMarker.localPosition = new Vector3(direction * 0.82f, 0.21f, -0.12f);
        creatureVisual.gameObject.SetActive(visible);
    }

    public void ShowAttack(Rect bounds, Color color, bool active)
    {
        SetWorldCenter(attackIndicator, bounds.center);
        attackIndicator.localScale = new Vector3(bounds.width, bounds.height, TelegraphThickness);
        attackIndicator.GetComponent<Renderer>().sharedMaterial.color = color;
        attackIndicator.gameObject.SetActive(active);
    }

    public void ShowLanding(Vector2 center, float width, bool active)
    {
        SetWorldCenter(landingIndicator, center);
        landingIndicator.localScale = new Vector3(width, 0.12f, TelegraphThickness);
        landingIndicator.gameObject.SetActive(active);
    }

    public void ShowWarning(Rect bounds, Color color, bool active)
    {
        SetWorldCenter(warningIndicator, bounds.center);
        warningIndicator.localScale = new Vector3(bounds.width, bounds.height, TelegraphThickness);
        warningIndicator.GetComponent<Renderer>().sharedMaterial.color = color;
        warningIndicator.gameObject.SetActive(active);
    }

    public void ShowDash(Vector2 from, Vector2 to, bool active)
    {
        Vector2 displacement = to - from;
        float angle = Mathf.Atan2(displacement.y, displacement.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        SetWorldCenter(dashPath, (from + to) * 0.5f);
        dashPath.localScale = new Vector3(displacement.magnitude, 0.2f, TelegraphThickness);
        dashPath.localRotation = rotation;

        SetWorldCenter(dashDirectionMarker, to);
        dashDirectionMarker.localScale = new Vector3(0.3f, 0.13f, TelegraphThickness);
        dashDirectionMarker.localRotation = rotation;

        dashPath.gameObject.SetActive(active);
        dashDirectionMarker.gameObject.SetActive(active);
    }

    public void ShowSpawn(Vector2 position, bool active)
    {
        SetWorldCenter(spawnIndicator, position);
        spawnIndicator.localScale = new Vector3(1.1f, 0.9f, TelegraphThickness);
        spawnIndicator.gameObject.SetActive(active);
    }

    public void ShowProjectilePath(Vector2 from, Vector2 to, bool active)
    {
        Vector2 direction = to - from;
        SetWorldCenter(projectilePath, (from + to) * 0.5f);
        projectilePath.localScale = new Vector3(direction.magnitude, 0.06f, TelegraphThickness);
        projectilePath.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        projectilePath.gameObject.SetActive(active);
    }

    public void HideTelegraphs()
    {
        attackIndicator.gameObject.SetActive(false);
        warningIndicator.gameObject.SetActive(false);
        landingIndicator.gameObject.SetActive(false);
        dashPath.gameObject.SetActive(false);
        dashDirectionMarker.gameObject.SetActive(false);
        spawnIndicator.gameObject.SetActive(false);
        projectilePath.gameObject.SetActive(false);
    }

    private Transform CreateTelegraph(string objectName, PrimitiveType primitive, Color color)
    {
        return PrototypeScene.Shape(objectName, transform, primitive,
            new Vector3(0f, 0f, TelegraphDepth), new Vector3(1f, 1f, TelegraphThickness), color);
    }

    private void SetWorldCenter(Transform visual, Vector2 worldCenter)
    {
        Vector3 rootPosition = transform.position;
        visual.localPosition = new Vector3(worldCenter.x - rootPosition.x,
            worldCenter.y - rootPosition.y, TelegraphDepth - rootPosition.z);
    }
}
