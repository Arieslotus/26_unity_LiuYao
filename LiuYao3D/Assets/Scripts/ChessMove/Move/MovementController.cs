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

    public Collider selfCollider;
    public SphereCollider selfSphereCollider;

    private float speedScaleMultiplier = 1f;

    [Header("碰撞配置")]
    [SerializeField] private CollisionConfig collisionConfig;

    [Header("卦象技能配置")]
    [SerializeField] private TrigramSkillDatabase skillDatabase;

    [Header("调试")]
    [Tooltip("是否输出 SphereCast 命中候选日志，用于排查隐形碰撞体")]
    [SerializeField] private bool debugPhysicsBounceHits = false;

    private ChessPiece chessPiece;
    private Collider lastPreImpactCollider;
    private bool hasSettledOperationLoss;

    public bool IsMoving => isMoving;
    public float RemainingDistance => remainingDistance;
    public Vector3 CurrentDirection => direction;
    public CollisionConfig CollisionConfig => collisionConfig;

    private void Awake()
    {
        chessPiece = GetComponentInParent<ChessPiece>();
        selfCollider = selfSphereCollider;

        if (selfCollider == null)
        {
            Debug.LogError($"[Movement] {name} 缺少 3D Collider。");
        }

        if (selfSphereCollider == null)
        {
            Debug.LogWarning($"[Movement] {name} 未使用 SphereCollider，将回退使用 MovementConfig.radius。");
        }
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
        lastPreImpactCollider = null;
        hasSettledOperationLoss = false;

        //Debug.Log($"[Movement] 开始移动 | 物体:{name} | 来源:{shotContext.sourceType} | 方向:{direction} | 总路径:{remainingDistance:F2}");
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
        lastPreImpactCollider = null;
        hasSettledOperationLoss = true;

        //Debug.Log($"[Movement] 碰撞启动移动 | 物体:{name} | 来源:{shotContext.sourceType} | 方向:{direction} | 路径:{remainingDistance:F2} | 初速度:{currentSpeed:F2}");
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

        float collisionRadius = GetCollisionRadius();
        float maxStep = collisionRadius * 0.5f;
        float remainingMoveThisFrame = currentSpeed * Time.deltaTime;
        remainingMoveThisFrame = Mathf.Min(remainingMoveThisFrame, remainingDistance);
        remainingMoveThisFrame = Mathf.Min(remainingMoveThisFrame, maxStep);

        TryRequestPreImpactFeedback(collisionRadius);

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
            PhysicsBounceUtility.DebugLogHits = debugPhysicsBounceHits;

            Vector3 currentCenter = GetCollisionCenter();

            var result = PhysicsBounceUtility.SimulateStep(
                currentCenter,
                direction,
                remainingMoveThisFrame,
                config,
                selfCollider,
                collisionRadius,
                name
            );

            float traveled = result.traveledDistance;

            if (traveled <= 0.0001f)
            {
                //Debug.LogWarning(
                //    $"[Movement] traveled 过小 | 物体:{name} | hit:{result.hit} | " +
                //    $"startedOverlapping:{result.startedOverlapping} | penetration:{result.penetrationDistance:F6} | " +
                //    $"collider:{result.collider?.name} | pos:{currentCenter} | dir:{direction} | " +
                //    $"normal:{result.normal} | hitPoint:{result.hitPoint} | traveled:{traveled:F6} | " +
                //    $"remainingMoveThisFrame:{remainingMoveThisFrame:F6} | remainingDistance:{remainingDistance:F4}"
                //);

                if (result.hit)
                {
                    MoveTransformByCenterDelta(currentCenter, result.newPos);
                    ResolveBounceHit(result, direction);

                    if (!isMoving)
                        return;
                }

                break;
            }

            MoveTransformByCenterDelta(currentCenter, result.newPos);

            if (debugPhysicsBounceHits)
            {
                //Debug.Log(
                //    $"[Movement] 本步移动 | 物体:{name} | from:{currentCenter} | to:{result.newPos} | " +
                //    $"hit:{result.hit} | collider:{result.collider?.name} | traveled:{traveled:F6} | " +
                //    $"dirBefore:{direction} | newDir:{result.newDir}"
                //);
            }

            remainingDistance -= traveled;
            remainingDistance = Mathf.Max(remainingDistance, 0f);

            remainingMoveThisFrame -= traveled;

            if (result.hit)
            {
                ResolveBounceHit(result, direction);

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

    private void ResolveBounceHit(BounceResult result, Vector3 incomingDirection)
    {
        //Debug.Log(
        //    $"[Movement] 碰撞 | 物体:{name} | 剩余路径:{remainingDistance:F2} | " +
        //    $"startedOverlapping:{result.startedOverlapping} | collider:{result.collider?.name}"
        //);

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
            incomingDir = incomingDirection,
            shotContext = shotContext
        };

        CollisionResult collisionResult = CollisionResolver.Resolve(ctx, config, collisionConfig);
        ApplyCollisionResult(collisionResult);
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

        TryTriggerTrigramSkill(result);
        RequestCoinImpactFeedback(result);

        if (result.triggerOtherCoinMove && result.otherCoin != null)
        {
            result.otherCoin.ActivateByCollision(
                result.otherCoinDirection,
                result.otherCoinStartDistance,
                result.otherCoinSpeedScale
            );

            //Debug.Log($"[Movement] 触发其他硬币移动 | 发起者:{name} | 目标:{result.otherCoin.name} | startDistance:{result.otherCoinStartDistance:F2}");
        }

        if (result.stopImmediately)
        {
            Stop();
            return;
        }

        //Debug.Log($"[BounceDebug] 物体:{name} | dir:{direction} | remainingDistance:{remainingDistance:F4}");
    }

    private void TryRequestPreImpactFeedback(float collisionRadius)
    {
        if (chessPiece == null || HitFeedbackController.Instance == null)
            return;

        if (direction.sqrMagnitude <= 0.0001f || currentSpeed <= 0.0001f)
            return;

        Vector3 currentCenter = GetCollisionCenter();
        Collider hitCollider = null;
        CollisionTarget target = null;
        Vector3 hitPoint = Vector3.zero;
        float hitDistance = 0f;

        if (!TryFindNearestPreImpactTarget(
            currentCenter,
            collisionRadius,
            out hitCollider,
            out target,
            out hitPoint,
            out hitDistance))
        {
            lastPreImpactCollider = null;
            return;
        }

        if (hitCollider == lastPreImpactCollider)
            return;

        lastPreImpactCollider = hitCollider;
        float predictedTime = hitDistance / currentSpeed;
        chessPiece.RequestPreImpactFeedback(target.type, predictedTime, hitPoint);
        TryRequestSkillTriggerFeedback(target);
    }

    private bool TryFindNearestPreImpactTarget(
        Vector3 currentCenter,
        float collisionRadius,
        out Collider hitCollider,
        out CollisionTarget target,
        out Vector3 hitPoint,
        out float hitDistance)
    {
        hitCollider = null;
        target = null;
        hitPoint = Vector3.zero;
        hitDistance = 0f;

        float maxLookAheadTime = GetMaxPreImpactLookAheadTime();
        if (maxLookAheadTime <= 0f)
            return false;

        float castDistance = Mathf.Min(currentSpeed * maxLookAheadTime, remainingDistance);
        if (castDistance <= 0.0001f)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            currentCenter,
            Mathf.Max(0.001f, collisionRadius),
            direction,
            castDistance
        );

        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (!IsValidPreImpactHit(hit, out CollisionTarget hitTarget, out float lookAheadTime))
                continue;

            if (hit.distance / currentSpeed > lookAheadTime)
                continue;

            if (hit.distance >= nearestDistance)
                continue;

            nearestDistance = hit.distance;
            hitCollider = hit.collider;
            target = hitTarget;
            hitPoint = hit.point;
            hitDistance = hit.distance;
        }

        return hitCollider != null && target != null;
    }

    private bool IsValidPreImpactHit(RaycastHit hit, out CollisionTarget target, out float lookAheadTime)
    {
        target = null;
        lookAheadTime = 0f;

        if (hit.collider == null)
            return false;

        if (PhysicsBounceUtility.ShouldIgnoreCollider(hit.collider, selfCollider))
            return false;

        if (hit.distance <= 0.0001f)
            return false;

        target = hit.collider.GetComponentInParent<CollisionTarget>();
        if (target == null)
            return false;

        if (target.type != CollisionType.Enemy && target.type != CollisionType.PlayerCoin)
            return false;

        if (target.type == CollisionType.PlayerCoin)
        {
            ChessPiece targetPiece = target.GetComponentInParent<ChessPiece>();
            if (targetPiece == null || targetPiece == chessPiece)
                return false;
        }

        return HitFeedbackController.Instance.TryGetPreImpactLookAheadTime(target.type, out lookAheadTime);
    }

    private float GetMaxPreImpactLookAheadTime()
    {
        float maxTime = 0f;

        if (HitFeedbackController.Instance.TryGetPreImpactLookAheadTime(CollisionType.Enemy, out float enemyTime))
        {
            maxTime = Mathf.Max(maxTime, enemyTime);
        }

        if (HitFeedbackController.Instance.TryGetPreImpactLookAheadTime(CollisionType.PlayerCoin, out float coinTime))
        {
            maxTime = Mathf.Max(maxTime, coinTime);
        }

        return maxTime;
    }

    private void RequestCoinImpactFeedback(CollisionResult result)
    {
        if (result.collider == null || chessPiece == null)
            return;

        CollisionTarget target = result.collider.GetComponentInParent<CollisionTarget>();
        if (target == null || target.type != CollisionType.PlayerCoin)
            return;

        float strength = config != null && config.totalDistance > 0.0001f
            ? remainingDistance / config.totalDistance
            : 0f;

        chessPiece.RequestImpactFeedback(CollisionType.PlayerCoin, false, Mathf.Clamp01(strength), result.collider.transform.position);
    }

    private void TryRequestSkillTriggerFeedback(CollisionTarget target)
    {
        if (target == null || target.type != CollisionType.PlayerCoin)
            return;

        TrigramCollisionSkillSO skill = GetCollisionSkill(target.GetComponentInParent<ChessPiece>());
        if (skill == null)
            return;

        float duration = HitFeedbackController.Instance != null
            ? HitFeedbackController.Instance.GetPreImpactFeedbackDuration(CollisionType.PlayerCoin)
            : 0f;

        CombatSkillEvents.RequestSkillTriggerFeedback(skill, duration);
    }

    private TrigramCollisionSkillSO GetCollisionSkill(ChessPiece passivePiece)
    {
        if (skillDatabase == null || passivePiece == null)
            return null;

        CoinRuntimeData activeCoin = GetComponentInParent<CoinRuntimeData>();
        CoinRuntimeData passiveCoin = passivePiece.GetComponentInParent<CoinRuntimeData>();

        if (activeCoin == null || passiveCoin == null)
            return null;

        return skillDatabase.GetSkill(activeCoin.CurrentTrigram, passiveCoin.CurrentTrigram);
    }

    private void TryTriggerTrigramSkill(CollisionResult result)
    {
        if (!result.triggerOtherCoinMove || result.otherCoin == null)
            return;

        if (skillDatabase == null)
        {
            Debug.LogWarning($"[卦象技能] {name} 未配置 TrigramSkillDatabase。");
            return;
        }

        CoinRuntimeData activeCoin = GetComponentInParent<CoinRuntimeData>();
        CoinRuntimeData passiveCoin = result.otherCoin.GetComponentInParent<CoinRuntimeData>();

        if (activeCoin == null || passiveCoin == null)
        {
            Debug.LogWarning(
                $"[卦象技能] 碰撞双方缺少 CoinRuntimeData | " +
                $"主动:{name} | 被动:{result.otherCoin.name}"
            );
            return;
        }

        TrigramCollisionSkillSO skill = GetCollisionSkill(result.otherCoin);

        if (skill == null)
        {
            Debug.Log(
                $"[卦象技能] 未找到技能 | " +
                $"主动币:{activeCoin.name} | 主动卦:{activeCoin.CurrentTrigram} | " +
                $"被动币:{passiveCoin.name} | 被动卦:{passiveCoin.CurrentTrigram}"
            );
            return;
        }

        Debug.Log(
            $"[卦象技能] 触发技能:{skill.SkillName} | " +
            $"主动币:{activeCoin.name} | 主动卦:{activeCoin.CurrentTrigram} | " +
            $"被动币:{passiveCoin.name} | 被动卦:{passiveCoin.CurrentTrigram} | " +
            $"效果:{skill.EffectText}"
        );

        CoinStats activeStats = GetComponentInParent<CoinStats>();
        CoinStats passiveStats = result.otherCoin.GetComponentInParent<CoinStats>();
        CollisionSkillExecutor.Execute(
            skill,
            new CollisionSkillContext
            {
                skill = skill,
                activePiece = chessPiece,
                passivePiece = result.otherCoin,
                activeStats = activeStats,
                passiveStats = passiveStats,
                activeTrigram = activeCoin.CurrentTrigram,
                passiveTrigram = passiveCoin.CurrentTrigram,
                activeAttackSnapshot = activeStats != null ? activeStats.Attack : 0,
                passiveAttackSnapshot = passiveStats != null ? passiveStats.Attack : 0,
                collisionPosition = result.hitPoint,
                triggeredRound = TurnManager.Instance != null ? TurnManager.Instance.RoundIndex : 0
            }
        );

    }

    private void ApplyHitTarget(CollisionResult result)
    {
        if (result.collider == null)
            return;

        IHittable hittable = result.collider.GetComponentInParent<IHittable>();
        if (hittable != null)
        {
            hittable.OnHit(result.hitDirection, result.impactStrength);
        }

        CoinStats attackerStats = GetComponentInParent<CoinStats>();
        ChessPiece attackerPiece = GetComponentInParent<ChessPiece>();
        IDamageable damageable = result.collider.GetComponentInParent<IDamageable>();
        int damage = CoinDamageCalculator.Calculate(attackerStats);

        EnemyShieldController shieldController = result.collider.GetComponentInParent<EnemyShieldController>();
        if (shieldController != null && attackerPiece != null)
        {
            shieldController.TryBreakShield(attackerPiece.CurrentTrigram, attackerPiece.name);
        }

        if (damageable != null && damage > 0)
        {
            damageable.TakeDamage(damage);
        }
        else
        {
            Debug.LogWarning(
                $"[Movement] 命中敌人但未造成伤害 | 发起者:{name} | 目标:{result.collider.name} | " +
                $"attackerStats:{(attackerStats != null ? attackerStats.name : "空")} | damage:{damage} | " +
                $"damageable:{(damageable != null ? damageable.ToString() : "空")}"
            );
        }

        if (attackerPiece != null)
        {
            attackerPiece.RequestImpactFeedback(CollisionType.Enemy, false, result.impactStrength, transform.position);
        }

        Debug.Log(
            $"[Movement] 命中敌人 | 发起者:{name} | 目标:{result.collider.name} | " +
            $"dir:{result.hitDirection} | strength:{result.impactStrength:F2} | damage:{damage}"
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

    public float GetCollisionRadius()
    {
        if (selfSphereCollider == null)
            return config != null ? config.radius : 0.5f;

        Vector3 scale = selfSphereCollider.transform.lossyScale;
        float maxScale = Mathf.Max(scale.x, scale.y, scale.z);

        return selfSphereCollider.radius * maxScale;
    }

    public Vector3 GetCollisionCenter()
    {
        // 视觉节点会执行翻面动画。逻辑圆心固定使用棋子根节点，避免子级 Collider 跟随旋转后产生漂移。
        return transform.position;
    }

    private void MoveTransformByCenterDelta(Vector3 oldCenter, Vector3 newCenter)
    {
        Vector3 delta = newCenter - oldCenter;
        delta.y = 0f;

        transform.position += delta;
    }

    private void OnDrawGizmos()
    {
        if (config == null)
            return;

        Gizmos.color = Color.green;
        float radius = Application.isPlaying ? GetCollisionRadius() : config.radius;
        Vector3 center = Application.isPlaying ? GetCollisionCenter() : transform.position;
        Gizmos.DrawWireSphere(center, radius);
    }

    private void Stop()
    {
        isMoving = false;
        currentSpeed = 0f;
        lastPreImpactCollider = null;

        if (!hasSettledOperationLoss && shotContext.isPlayerShot)
        {
            hasSettledOperationLoss = true;

            CoinStats stats = GetComponentInParent<CoinStats>();
            if (stats != null)
            {
                stats.AddOperationLoss();
            }
            else
            {
                Debug.LogWarning($"[Movement] 主动操作结束但未找到 CoinStats | 物体:{name}");
            }
        }

        Debug.Log($"[Movement] 停止移动 | 物体:{name}");
    }
}
