using UnityEngine;

public sealed class PrototypeScene : MonoBehaviour
{
    private void Awake()
    {
        Camera camera = Camera.main;
        camera.transform.position = new Vector3(0f, 1.3f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 4.6f;
        camera.backgroundColor = new Color(0.055f, 0.07f, 0.11f);

        Platform("Floor", new Vector2(0f, -1.4f), new Vector2(24f, 0.6f));
        Platform("Low platform", new Vector2(-3.5f, 0f), new Vector2(2.4f, 0.3f)).AddComponent<RestorablePlant>();
        Platform("Middle platform", new Vector2(0f, 1.2f), new Vector2(2f, 0.3f)).AddComponent<RestorablePlant>();
        Platform("High platform", new Vector2(3.2f, 2.5f), new Vector2(2.2f, 0.3f)).AddComponent<RestorablePlant>();
        Platform("Left step", new Vector2(-6f, 1.5f), new Vector2(1.6f, 0.3f));
        Platform("Right step", new Vector2(6f, 0.5f), new Vector2(1.8f, 0.3f));
        Platform("Left wall", new Vector2(-8f, 1f), new Vector2(0.4f, 4.2f));
        Platform("Right wall", new Vector2(8f, 2f), new Vector2(0.4f, 6.2f));
        Platform("Vine exit", new Vector2(8.7f, 5f), new Vector2(1.8f, 0.2f));
        Platform("Boss landing", new Vector2(15.4f, -0.95f), new Vector2(14f, 0.3f));
        Platform("Boss right wall", new Vector2(22.6f, 2f), new Vector2(0.4f, 5.6f));

        GameObject vine = new GameObject("Vine ladder");
        vine.transform.SetParent(transform, false);
        vine.transform.localPosition = new Vector3(7.5f, -1.1f, 0f);
        vine.AddComponent<VineLadder>().Initialize(5.55f);

        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(-1.7f, -0.65f, 0f);
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.44f, 0.8f);
        PhysicsMaterial2D frictionless = new PhysicsMaterial2D("Frictionless");
        frictionless.friction = 0f;
        frictionless.bounciness = 0f;
        collider.sharedMaterial = frictionless;
        PlatformPlayer controller = player.AddComponent<PlatformPlayer>();
        player.AddComponent<OrbitActions>();
        PlayerCombat combat = player.AddComponent<PlayerCombat>();
        StageCamera stageCamera = camera.gameObject.AddComponent<StageCamera>();
        stageCamera.Initialize(player.transform);
        GameObject bossArea = new GameObject("Boss Area");
        bossArea.transform.SetParent(transform, false);
        bossArea.transform.localPosition = new Vector3(15.4f, -0.25f, 0f);
        BossArea gate = bossArea.AddComponent<BossArea>();
        gate.Initialize(controller, new Vector2(10.6f, 1.5f));
        Rect arena = new Rect(8.4f, -0.8f, 14f, 6.7f);
        combat.InitializeArena(new Rect(10.1f, -0.8f, 10.6f, 6.7f), gate);
        stageCamera.ConfigureArena(arena, gate);
        Shape("Arena left limit", transform, PrimitiveType.Cube,
            new Vector3(10.1f, -0.7f, -0.1f), new Vector3(0.04f, 0.2f, 0.03f), new Color(1f, 0.76f, 0.3f));
        Shape("Arena right limit", transform, PrimitiveType.Cube,
            new Vector3(20.7f, -0.7f, -0.1f), new Vector3(0.04f, 0.2f, 0.03f), new Color(1f, 0.76f, 0.3f));
        GameObject tiger = new GameObject("Rhythm tiger boss");
        tiger.transform.SetParent(transform, false);
        tiger.transform.localPosition = new Vector3(18f, -0.35f, 0f);
        TigerBoss boss = tiger.AddComponent<TigerBoss>();
        boss.Initialize(combat, gate, arena);
        tiger.AddComponent<TigerBossHud>().Initialize(boss, combat);
    }

    private GameObject Platform(string objectName, Vector2 position, Vector2 size)
    {
        GameObject platform = new GameObject(objectName);
        platform.transform.SetParent(transform, false);
        platform.transform.localPosition = position;
        platform.layer = 6;
        platform.AddComponent<BoxCollider2D>().size = size;
        Shape("Surface", platform.transform, PrimitiveType.Cube,
            new Vector3(0f, 0f, 0.25f), new Vector3(size.x, size.y, 0.2f),
            new Color(0.27f, 0.34f, 0.43f));
        return platform;
    }

    public static Transform Shape(string objectName, Transform parent, PrimitiveType primitive,
        Vector3 position, Vector3 scale, Color color)
    {
        GameObject shape = GameObject.CreatePrimitive(primitive);
        shape.name = objectName;
        Object.Destroy(shape.GetComponent<Collider>());
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = position;
        shape.transform.localScale = scale;
        Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.color = color;
        shape.GetComponent<Renderer>().sharedMaterial = material;
        return shape.transform;
    }
}
