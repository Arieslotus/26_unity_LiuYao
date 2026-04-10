/// <summary>
/// 实现功能：负责棋子的路径主导移动、碰撞结算应用，并支持己方硬币互撞后的带动启动。
/// 挂在每个棋子（硬币）上
/// </summary>
using UnityEngine;
public class MovementController : MonoBehaviour
{
    private MovementConfig config;

    private Vector2 direction;
    private float remainingDistance;
    private float currentSpeed;
    private bool isMoving = false;
    private ShotContext shotContext;
    private Collider2D selfCollider;

    // 持续生效的速度倍率（用于被撞启动）
    private float speedScaleMultiplier = 1f;

    public bool IsMoving => isMoving;

    /// <summary>
    /// 当前剩余路程（供互撞结算读取）
    /// </summary>
    public float RemainingDistance => remainingDistance;

    /// <summary>
    /// 当前运动方向（供互撞结算读取）
    /// </summary>
    public Vector2 CurrentDirection => direction;

    [Header("碰撞配置")]
    [SerializeField] private CollisionConfig collisionConfig;

    private void Awake()
    {
        selfCollider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// 初始化移动（玩家主动发射）
    /// </summary>
    public void Init(Vector2 dir, MovementConfig movementConfig, ShotContext context)
    {
        config = movementConfig;
        shotContext = context;
        speedScaleMultiplier = 1f;

        direction = dir.normalized;
        remainingDistance = config.totalDistance * context.power;
        currentSpeed = config.baseSpeed;

        isMoving = true;

        Debug.Log($"[Movement] 开始移动 | 物体:{name} | 来源:{shotContext.sourceType} | 方向:{direction} | 总路径:{remainingDistance:F2}");
    }

    /// <summary>
    /// 初始化移动（由其他硬币碰撞带动）
    /// </summary>
    public void InitByCollision(Vector2 dir, MovementConfig movementConfig, ShotContext context, float startDistance, float speedScale)
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
        if (!isMoving || config == null) return;

        // ===== 停止条件 =====
        if (remainingDistance <= 0)
        {
            Stop();
            return;
        }

        // 步长限制，防止高速穿透
        float maxStep = config.radius * 0.5f;
        float remainingMoveThisFrame = currentSpeed * Time.deltaTime;
        remainingMoveThisFrame = Mathf.Min(remainingMoveThisFrame, remainingDistance);
        remainingMoveThisFrame = Mathf.Min(remainingMoveThisFrame, maxStep);

        int loopCount = 0;
        int maxBouncePerFrame = 8;

        while (remainingMoveThisFrame > 0.0001f && loopCount < maxBouncePerFrame)
        {
            loopCount++;

            Vector2 currentPos = transform.position;

            var result = PhysicsBounceUtility.SimulateStep(
                currentPos,
                direction,
                remainingMoveThisFrame,
                config,
                selfCollider
            );

            float traveled = result.traveledDistance;

            // ===== 防止0距离卡死 =====
            if (traveled <= 0.0001f)
            {
                Debug.Log($"[Movement] traveled 过小 | 物体:{name} | hit:{result.hit} | collider:{result.collider?.name} | traveled:{traveled:F6}");
                Move(direction * 0.02f);
                continue;
            }

            // ===== 更新位置 =====
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
                    hitPoint = result.hitPoint,
                    normal = result.normal,
                    incomingDir = direction,
                    shotContext = shotContext
                };

                CollisionResult r = CollisionResolver.Resolve(ctx, config, collisionConfig);

                ApplyCollisionResult(r);

                if (!isMoving)
                    return;
            }
            else
            {
                // 没碰撞直接结束本轮 while
                break;
            }

        }

        // ===== 速度更新（按剩余路程比例衰减）=====
        float t = Mathf.Clamp01(remainingDistance / config.totalDistance);

        float scaledBaseSpeed = config.baseSpeed * speedScaleMultiplier;
        float scaledMinSpeed = config.minSpeed * speedScaleMultiplier;

        float targetSpeed = scaledBaseSpeed * (1f - Mathf.Pow(1f - t, config.speedDecayPower));

        // 到达最低速度后保持最低速度继续移动，直到走完剩余路径
        currentSpeed = Mathf.Max(targetSpeed, scaledMinSpeed);

        if (config.debugDraw)
        {
            Debug.Log($"[Movement] 物体:{name} | Speed:{currentSpeed:F2} | 剩余:{remainingDistance:F2}");
        }
    }

    /// <summary>
    /// 应用碰撞结算结果
    /// </summary>
    private void ApplyCollisionResult(CollisionResult result)
    {
        direction = result.newDirection;
        remainingDistance *= result.remainingDistanceMultiplier;
        remainingDistance = Mathf.Max(remainingDistance, 0f);

        // 互撞后启动另一枚硬币
        if (result.triggerOtherCoinMove && result.otherCoin != null)
        {
            result.otherCoin.ActivateByCollision(
                result.otherCoinDirection,
                result.otherCoinStartDistance,
                result.otherCoinSpeedScale
            );

            Debug.Log($"[Movement] 触发其他硬币移动 | 发起者:{name} | 目标:{result.otherCoin.name} | startDistance:{result.otherCoinStartDistance:F2}");
        }

        // ===== 敌人受击 =====
        if (result.triggerHitTarget && result.collider != null)
        {
            IHittable hittable = result.collider.GetComponentInParent<IHittable>();

            if (hittable != null)
            {
                hittable.OnHit(result.hitDirection, result.impactStrength);

                Debug.Log(
                    $"[Movement] 命中敌人 | 发起者:{name} | 目标:{result.collider.name} | " +
                    $"dir:{result.hitDirection} | strength:{result.impactStrength:F2}"
                );
            }
        }

        if (result.stopImmediately)
        {
            Stop();
            return;
        }

        Debug.Log(
            $"[BounceDebug] 物体:{name} | dir:{direction} | remainingDistance:{remainingDistance:F4}"
        );
    }

    /// <summary>
    /// 安全移动（统一处理 Vector2 → Vector3）
    /// </summary>
    private void Move(Vector2 delta)
    {
        Vector2 pos = transform.position;
        pos += delta;
        transform.position = pos;
    }

    private void OnDrawGizmos()
    {
        if (config == null) return;

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