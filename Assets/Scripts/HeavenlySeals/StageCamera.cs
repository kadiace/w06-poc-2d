using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class StageCamera : MonoBehaviour
{
    private const float OrthographicSize = 4.6f;
    private const float CameraDepth = -10f;
    private const float MinimumCameraY = 1.3f;
    private const float FollowSmoothTime = 0.2f;
    private const float ArenaCameraX = 15.4f;
    private const float ArenaCameraY = 2.1f;
    private const float ArenaHorizontalMargin = 1.4f;

    private Camera stageCamera;
    private Transform target;
    private Vector3 followVelocity;
    private Rect arena;
    private BossArea arenaGate;
    private bool hasEnteredArena;

    private void Awake()
    {
        stageCamera = GetComponent<Camera>();
    }

    public void Initialize(Transform target)
    {
        this.target = target;
        stageCamera.orthographic = true;
        stageCamera.orthographicSize = OrthographicSize;
        transform.position = new Vector3(transform.position.x,
            Mathf.Max(MinimumCameraY, transform.position.y), CameraDepth);
        followVelocity = Vector3.zero;
    }

    public void ConfigureArena(Rect arena, BossArea gate)
    {
        this.arena = arena;
        arenaGate = gate;
        hasEnteredArena = false;
    }

    private void LateUpdate()
    {
        if (arenaGate != null && arenaGate.HasArrived)
        {
            if (!hasEnteredArena)
            {
                hasEnteredArena = true;
                followVelocity = Vector3.zero;
            }

            transform.position = new Vector3(ArenaCameraX, ArenaCameraY, CameraDepth);
            stageCamera.orthographicSize = Mathf.Max(OrthographicSize,
                (arena.width + ArenaHorizontalMargin) / (2f * stageCamera.aspect));
            return;
        }

        if (target == null)
            return;

        Vector3 targetPosition = target.position;
        Vector3 desiredPosition = new Vector3(targetPosition.x,
            Mathf.Max(MinimumCameraY, targetPosition.y), CameraDepth);
        Vector3 position = Vector3.SmoothDamp(transform.position, desiredPosition,
            ref followVelocity, FollowSmoothTime);
        position.z = CameraDepth;
        transform.position = position;
        stageCamera.orthographicSize = OrthographicSize;
    }
}
