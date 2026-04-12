/// <summary>
/// 实现功能：负责棋子的路径主导移动、碰撞结算应用，并支持己方硬币互撞、翻面返回阶段与后续位置修正预留。
/// 挂在每个棋子（硬币）上
/// </summary>
using UnityEngine;

public class MovementController : MonoBehaviour
{
    private enum MovePhase
    {
        Normal,
        FlipReturn
    }

    private MovementConfig config;
    private Vector2 direction;
    private float remainingDistance;
    private float currentSpeed;
    private bool isMoving = false;
    private ShotContext shotContext;
    private Collider2D selfCollider;

    private float speedScaleMultiplier = 1f;
    private MovePhase currentPhase = MovePhase.Normal;
    private bool hasTriggeredFlipThisShot = false;
    private CollisionTarget lastFlipTarget;

    [Header("碰撞配置")]
    [SerializeField] private CollisionConfig collisionConfig;

    public bool IsMoving => isMoving;
    public float RemainingDistance => remainingDistance;
    public Vector2 CurrentDirection => direction;
    public bool HasTriggeredFlipThisShot => hasTriggeredFlipThisShot;
    public bool IsInFlipReturnPhase => currentPhase == MovePhase.FlipReturn;
    public CollisionConfig CollisionConfig => collisionConfig;

    private void Awake()
    {
        selfCollider = GetComponent<Collider2D>();
    }

    // 初始化移动（玩家主动发射）
    public void Init(Vector2 dir, MovementConfig movementConfig, ShotContext context)
    {
        config = movementConfig;
        shotContext = context;
        speedScaleMultiplier = 1f;

        currentPhase = MovePhase.Normal;
        hasTriggeredFlipThisShot = false;
        lastFlipTarget = null;

        direction = dir.normalized;
        remainingDistance = config.totalDistance * context.power;
        currentSpeed = config.baseSpeed;

        isMoving = true;

        Debug.Log($"[Movement] 开始移动 | 物体:{name} | 来源:{shotContext.sourceType} | 方向:{direction} | 总路径:{remainingDistance:F2}");
    }

    // 初始化移动（由其他硬币碰撞带动）
    public void InitByCollision(Vector2 dir, MovementConfig movementConfig, ShotContext context, float startDistance, float speedScale)
    {
        config = movementConfig;
        shotContext = context;
        speedScaleMultiplier = Mathf.Max(0f, speedScale);

        currentPhase = MovePhase.Normal;
        hasTriggeredFlipThisShot = false;
        lastFlipTarget = null;

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

        // ===== 停止条件 =====
        if (remainingDistance <= 0.0001f)
        {
            FinishCurrentMove();
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
                Debug.Log($"[Movement] 碰撞 | 物体:{name} | 阶段:{currentPhase} | 剩余路径:{remainingDistance:F2}");

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
                // 没碰撞直接结束本轮 while
                break;
            }
        }

        UpdateSpeed();
    }

    // 应用碰撞结算结果（所有情况）
    private void ApplyCollisionResult(CollisionResult result)
    {
        if (result.triggerHitTarget)
        {
            ApplyHitTarget(result);
        }

        if (result.triggerFlip)
        {
            ApplyFlipResult(result);
            return;
        }

        if (currentPhase == MovePhase.FlipReturn)
        {
            Debug.Log($"[Movement] FlipReturn 阶段再次碰撞，直接结束 | 物体:{name}");
            FinishCurrentMove();
            return;
        }

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

        if (result.stopImmediately)
        {
            Stop();
            return;
        }

        Debug.Log($"[BounceDebug] 物体:{name} | dir:{direction} | remainingDistance:{remainingDistance:F4}");
    }

    // 应用翻面结果
    private void ApplyFlipResult(CollisionResult result)
    {
        MarkFlipTriggered(result.flipTarget);

        currentPhase = MovePhase.FlipReturn;

        Vector2 flipDir = result.flipMoveDirection;
        if (flipDir.sqrMagnitude < 0.0001f)
            flipDir = -direction;

        direction = flipDir.normalized;
        remainingDistance = Mathf.Max(0f, result.flipMoveDistance);

        float speedScale = 1f;
        if (collisionConfig != null)
            speedScale = collisionConfig.flipReturnSpeedScale;

        currentSpeed = config.baseSpeed * Mathf.Max(0.01f, speedScale);

        ChessPiece piece = GetComponentInParent<ChessPiece>();
        if (piece != null)
        {
            piece.HandleFlipTriggered(result.flipTarget, result.feedbackPoint, direction);
        }

        Debug.Log(
            $"[Movement] 触发翻面 | 物体:{name} | 目标:{result.flipTarget?.name} | " +
            $"returnDir:{direction} | returnDistance:{remainingDistance:F2} | speed:{currentSpeed:F2}"
        );

        if (result.flipShouldStopMainMove && remainingDistance <= 0.0001f)
        {
            FinishCurrentMove();
        }
    }

    // 应用命中敌人结果
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

    // 速度更新（按剩余路程比例衰减）
    private void UpdateSpeed()
    {
        if (!isMoving || config == null)
            return;

        if (currentPhase == MovePhase.FlipReturn)
        {
            float speedScale = 1f;
            if (collisionConfig != null)
                speedScale = collisionConfig.flipReturnSpeedScale;

            currentSpeed = config.baseSpeed * Mathf.Max(0.01f, speedScale);
            return;
        }

        float t = Mathf.Clamp01(remainingDistance / config.totalDistance);

        float scaledBaseSpeed = config.baseSpeed * speedScaleMultiplier;
        float scaledMinSpeed = config.minSpeed * speedScaleMultiplier;

        float targetSpeed = scaledBaseSpeed * (1f - Mathf.Pow(1f - t, config.speedDecayPower));
        // 到达最低速度后保持最低速度继续移动，直到走完剩余路径
        currentSpeed = Mathf.Max(targetSpeed, scaledMinSpeed);

        if (config.debugDraw)
        {
            Debug.Log($"[Movement] 物体:{name} | Phase:{currentPhase} | Speed:{currentSpeed:F2} | 剩余:{remainingDistance:F2}");
        }
    }

    private void FinishCurrentMove()
    {
        if (currentPhase == MovePhase.FlipReturn)
        {
            TryResolvePostFlipOverlap();
        }

        Stop();
    }

    private void TryResolvePostFlipOverlap()
    {
        if (collisionConfig == null)
            return;

        if (!collisionConfig.enableSeparationAfterFlip)
            return;

        if (lastFlipTarget == null)
            return;

        Debug.Log($"[Movement] 预留：翻面结束后可在这里执行位置分离修正 | 物体:{name} | 目标:{lastFlipTarget.name}");
    }

    private void Move(Vector2 delta)
    {
        Vector2 pos = transform.position;
        pos += delta;
        transform.position = pos;
    }

    public void MarkFlipTriggered(CollisionTarget target)
    {
        hasTriggeredFlipThisShot = true;
        lastFlipTarget = target;
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
        currentPhase = MovePhase.Normal;

        Debug.Log($"[Movement] 停止移动 | 物体:{name}");
    }
}