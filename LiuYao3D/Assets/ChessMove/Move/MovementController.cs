/// <summary>
/// 实现功能：负责棋子的路径主导移动、碰撞结算应用，并支持己方硬币互撞后的带动启动。
/// 挂在每个棋子（硬币）上
/// </summary>
using UnityEngine;

public class MovementController : MonoBehaviour
{
    private MovementConfig config;
    private Vector3 direction;
    private float remainingDistance;
    private float currentSpeed;
    private bool isMoving = false;
    private ShotContext shotContext;
    private Collider selfCollider;

    private float speedScaleMultiplier = 1f;

    [Header("碰撞配置")]
    [SerializeField] private CollisionConfig collisionConfig;

    public bool IsMoving => isMoving;
    public float RemainingDistance => remainingDistance;
    public Vector3 CurrentDirection => direction;
    public CollisionConfig CollisionConfig => collisionConfig;

    private void Awake()
    {
        selfCollider = GetComponent<Collider>();
    }

    public void Init(Vector3 dir, MovementConfig movementConfig, ShotContext context)
    {
        config = movementConfig;
        shotContext = context;
        speedScaleMultiplier = 1f;

        direction = new Vector3(dir.x, 0, dir.z).normalized;
        remainingDistance = config.totalDistance * context.power;
        currentSpeed = config.baseSpeed;

        isMoving = true;

        Debug.Log($"[Movement] 开始移动 | 物体:{name} | 来源:{shotContext.sourceType} | 方向:{direction} | 总路径:{remainingDistance:F2}");
    }

    public void InitByCollision(Vector3 dir, MovementConfig movementConfig, ShotContext context, float startDistance, float speedScale)
    {
        config = movementConfig;
        shotContext = context;
        speedScaleMultiplier = Mathf.Max(0f, speedScale);

        direction = dir.normalized;
        remainingDistance = Mathf.Max(0f, startDistance);
        currentSpeed = config.baseSpeed * speedScaleMultiplier;

        isMoving = true;

        Debug.Log($"[Movement] 碰撞启动移动 | 物体:{name} | 来源:{shotContext.sourceType} | 方向:{direction} | 路径:{remainingDistance:F2} | 初速度:{currentSpeed:F2}");
    }

    private void Update()
    {
        if (!isMoving || config == null)
            return;

        if (remainingDistance <= 0.0001f)
        {
            remainingDistance = 0f;
            FinishCurrentMove();
            return;
        }

        if (currentSpeed <= 0.0001f)
        {
            remainingDistance = 0f;
            FinishCurrentMove();
            return;
        }

        float maxStep = config.radius * 0.5f;
        float remainingMoveThisFrame = currentSpeed * Time.deltaTime;
        remainingMoveThisFrame = Mathf.Min(remainingMoveThisFrame, remainingDistance);
        remainingMoveThisFrame = Mathf.Min(remainingMoveThisFrame, maxStep);

        if (remainingMoveThisFrame <= 0.0001f)
        {
            remainingDistance = 0f;
            FinishCurrentMove();
            return;
        }

        int loopCount = 0;
        int maxBouncePerFrame = 8;

        while (remainingMoveThisFrame > 0.0001f && loopCount < maxBouncePerFrame)
        {
            loopCount++;

            Vector3 currentPos = transform.position;

            var result = PhysicsBounceUtility.SimulateStep(
                currentPos,
                direction,
                remainingMoveThisFrame,
                config,
                selfCollider
            );

            float traveled = result.traveledDistance;

            if (traveled <= 0.0001f)
            {
                Debug.Log($"[Movement] traveled 过小 | 物体:{name} | hit:{result.hit} | collider:{result.collider?.name} | traveled:{traveled:F6}");
                Move(direction * 0.02f);
                continue;
            }

            transform.position = result.newPos;

            remainingDistance -= traveled;
            remainingDistance = Mathf.Max(remainingDistance, 0f);

            remainingMoveThisFrame -= traveled;

            if (result.hit)
            {
                Debug.Log($"[Movement] 碰撞 | 物体:{name} | 剩余路径:{remainingDistance:F2}");

                CollisionTarget target = null;
                if (result.collider != null)
                    target = result.collider.GetComponentInParent<CollisionTarget>();

                CollisionContext ctx = new CollisionContext
                {
                    self = this,
                    target = target,
                    selfCollider = selfCollider,
                    hitCollider = result.collider,
                    hitPoint = result.hitPoint,
                    normal = result.normal,
                    incomingDir = direction,
                    shotContext = shotContext
                };

                CollisionResult collisionResult = CollisionResolver.Resolve(ctx, config, collisionConfig);
                ApplyCollisionResult(collisionResult);

                if (!isMoving)
                    return;
            }
            else
            {
                break;
            }
        }

        UpdateSpeed();

        direction.y = 0;
    }

    private void ApplyCollisionResult(CollisionResult result)
    {
        if (result.triggerHitTarget)
        {
            ApplyHitTarget(result);
        }

        direction = result.newDirection;
        remainingDistance *= result.remainingDistanceMultiplier;
        remainingDistance = Mathf.Max(remainingDistance, 0f);

        if (result.triggerOtherCoinMove && result.otherCoin != null)
        {
            result.otherCoin.ActivateByCollision(
                result.otherCoinDirection,
                result.otherCoinStartDistance,
                result.otherCoinSpeedScale
            );

            Debug.Log($"[Movement] 触发其他硬币移动 | 发起者:{name} | 目标:{result.otherCoin.name} | startDistance:{result.otherCoinStartDistance:F2}");
        }

        if (result.stopImmediately)
        {
            Stop();
            return;
        }

        Debug.Log($"[BounceDebug] 物体:{name} | dir:{direction} | remainingDistance:{remainingDistance:F4}");
    }

    private void ApplyHitTarget(CollisionResult result)
    {
        if (result.collider == null)
            return;

        IHittable hittable = result.collider.GetComponentInParent<IHittable>();
        if (hittable == null)
            return;

        hittable.OnHit(result.hitDirection, result.impactStrength);

        ChessPiece piece = GetComponentInParent<ChessPiece>();
        if (piece != null)
        {
            piece.RequestImpactFeedback(CollisionType.Enemy, false, result.impactStrength, transform.position);
        }

        Debug.Log(
            $"[Movement] 命中敌人 | 发起者:{name} | 目标:{result.collider.name} | " +
            $"dir:{result.hitDirection} | strength:{result.impactStrength:F2}"
        );
    }

    private void UpdateSpeed()
    {
        if (!isMoving || config == null)
            return;

        float t = Mathf.Clamp01(remainingDistance / config.totalDistance);

        float scaledBaseSpeed = config.baseSpeed * speedScaleMultiplier;
        float scaledMinSpeed = config.minSpeed * speedScaleMultiplier;

        float targetSpeed = scaledBaseSpeed * (1f - Mathf.Pow(1f - t, config.speedDecayPower));
        currentSpeed = Mathf.Max(targetSpeed, scaledMinSpeed);

        if (config.debugDraw)
        {
            Debug.Log($"[Movement] 物体:{name} | Speed:{currentSpeed:F2} | 剩余:{remainingDistance:F2}");
        }
    }

    private void FinishCurrentMove()
    {
        Stop();
    }

    private void Move(Vector3 delta)
    {
        Vector3 pos = transform.position;
        pos += delta;
        pos.y = transform.position.y;
        transform.position = pos;
    }

    private void OnDrawGizmos()
    {
        if (config == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, config.radius);
    }

    private void Stop()
    {
        isMoving = false;
        currentSpeed = 0f;

        Debug.Log($"[Movement] 停止移动 | 物体:{name}");
    }
}