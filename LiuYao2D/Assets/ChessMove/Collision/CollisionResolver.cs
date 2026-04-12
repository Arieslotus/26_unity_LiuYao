/// <summary>
/// 实现功能：作为碰撞规则统一入口，处理障碍物、敌人与己方硬币互撞的结算逻辑，并支持满蓄力时的翻面优先分支。
/// </summary>
using UnityEngine;

public static class CollisionResolver
{
    /// <summary>
    /// 碰撞规则统一入口
    /// </summary>
    public static CollisionResult Resolve(
        CollisionContext ctx,
        MovementConfig movementConfig,
        CollisionConfig collisionConfig
    )
    {
        CollisionResult result = CreateDefaultResult(ctx, movementConfig);

        if (ctx.target == null)
        {
            return result;
        }

        // ===== 翻面优先级覆盖 =====
        if (ShouldTriggerFlip(ctx, collisionConfig))
        {
            return ResolveFlipCollision(ctx, movementConfig, collisionConfig);
        }

        switch (ctx.target.type)
        {
            case CollisionType.Obstacle:
                result.remainingDistanceMultiplier = collisionConfig.obstacleBounceMultiplier;
                break;

            case CollisionType.Enemy:
                ResolveEnemyCollision(ctx, movementConfig, collisionConfig, ref result);
                break;

            case CollisionType.PlayerCoin:
                ResolvePlayerCoinCollision(ctx, collisionConfig, ref result);
                break;
        }

        Debug.Log($"[CollisionResolver] 普通碰撞类型: {ctx.target.type}");
        return result;
    }

    /// <summary>
    /// 创建默认碰撞结果（普通反弹）
    /// </summary>
    private static CollisionResult CreateDefaultResult(
        CollisionContext ctx,
        MovementConfig movementConfig
    )
    {
        return new CollisionResult
        {
            // ===== 通用移动结果 =====
            newDirection = Vector2.Reflect(ctx.incomingDir, ctx.normal).normalized,
            remainingDistanceMultiplier = movementConfig.bounceDamping,
            stopImmediately = false,

            // ===== 翻面结果 =====
            triggerFlip = false,
            flipMoveDirection = Vector2.zero,
            flipMoveDistance = 0f,
            flipShouldStopMainMove = false,
            flipTarget = null,

            // ===== 己方互撞 =====
            triggerOtherCoinMove = false,
            otherCoin = null,
            otherCoinDirection = Vector2.zero,
            otherCoinStartDistance = 0f,
            otherCoinSpeedScale = 1f,

            // ===== 敌人受击 =====
            triggerHitTarget = false,
            hitDirection = Vector2.zero,
            impactStrength = 0f,
            collider = ctx.hitCollider,

            // ===== 位置分离修正预留 =====
            requestSeparationResolve = false,
            separationTarget = null,
            separationTargetType = CollisionType.Obstacle,

            // ===== 表现层预留 =====
            triggerFlipFeedback = false,
            feedbackPoint = ctx.hitPoint
        };
    }

    /// <summary>
    /// 是否满足翻面触发条件
    /// </summary>
    private static bool ShouldTriggerFlip(
        CollisionContext ctx,
        CollisionConfig collisionConfig
    )
    {
        if (!collisionConfig.enableFlip)
            return false;

        if (!ctx.shotContext.isPlayerShot)
            return false;

        if (!ctx.shotContext.isFullCharge)
            return false;

        if (ctx.self == null)
            return false;

        if (collisionConfig.flipOnlyOncePerShot && ctx.self.HasTriggeredFlipThisShot)
            return false;

        if (ctx.target == null)
            return false;

        switch (ctx.target.type)
        {
            case CollisionType.Enemy:
                return collisionConfig.flipOnEnemy;

            case CollisionType.PlayerCoin:
                return collisionConfig.flipOnPlayerCoin;

            case CollisionType.Obstacle:
                return collisionConfig.flipOnObstacle;

            default:
                return false;
        }
    }

    /// <summary>
    /// 处理翻面碰撞（高优先级覆盖普通碰撞规则）
    /// </summary>
    private static CollisionResult ResolveFlipCollision(
        CollisionContext ctx,
        MovementConfig movementConfig,
        CollisionConfig collisionConfig
    )
    {
        CollisionResult result = CreateDefaultResult(ctx, movementConfig);

        result.triggerFlip = true;
        result.flipMoveDirection = -ctx.incomingDir.normalized; // 原路返回
        result.flipMoveDistance = collisionConfig.flipReturnDistance;
        result.flipShouldStopMainMove = true;
        result.flipTarget = ctx.target;

        result.requestSeparationResolve = collisionConfig.enableSeparationAfterFlip;
        result.separationTarget = ctx.target.transform;
        result.separationTargetType = ctx.target.type;

        result.triggerFlipFeedback = true;
        result.feedbackPoint = ctx.hitPoint;

        // ===== 敌人翻面时，仍然保留受击信息 =====
        if (ctx.target.type == CollisionType.Enemy)
        {
            Vector2 hitDir = GetCenterLineDirection(ctx);
            float strength = GetImpactStrength(ctx, movementConfig);

            result.triggerHitTarget = true;
            result.hitDirection = hitDir;
            result.impactStrength = strength;
        }

        Debug.Log(
            $"[CollisionResolver] 触发翻面 | 发起者:{ctx.self.name} | 目标:{ctx.target.name} | " +
            $"returnDir:{result.flipMoveDirection} | returnDistance:{result.flipMoveDistance:F2}"
        );

        return result;
    }

    /// <summary>
    /// 普通敌人碰撞结算
    /// </summary>
    private static void ResolveEnemyCollision(
        CollisionContext ctx,
        MovementConfig movementConfig,
        CollisionConfig collisionConfig,
        ref CollisionResult result
    )
    {
        result.remainingDistanceMultiplier = collisionConfig.enemyBounceMultiplier;

        Vector2 hitDir = GetCenterLineDirection(ctx);
        float strength = GetImpactStrength(ctx, movementConfig);

        result.triggerHitTarget = true;
        result.hitDirection = hitDir;
        result.impactStrength = strength;
    }

    /// <summary>
    /// 处理己方硬币互撞
    /// </summary>
    private static void ResolvePlayerCoinCollision(
        CollisionContext ctx,
        CollisionConfig collisionConfig,
        ref CollisionResult result
    )
    {
        result.remainingDistanceMultiplier = collisionConfig.coinBounceMultiplier;

        ChessPiece selfPiece = ctx.self.GetComponentInParent<ChessPiece>();
        ChessPiece otherPiece = ctx.target.GetComponentInParent<ChessPiece>();

        if (selfPiece == null || otherPiece == null)
        {
            return;
        }

        // 防止撞到自己
        if (selfPiece == otherPiece)
        {
            return;
        }

        // 只处理“撞到静止己方硬币”
        if (otherPiece.IsMoving)
        {
            return;
        }

        // 冷却保护，防止短时间重复触发
        if (!selfPiece.CanTriggerCoinCollision(collisionConfig.coinCollisionCooldown) ||
            !otherPiece.CanTriggerCoinCollision(collisionConfig.coinCollisionCooldown))
        {
            return;
        }

        // B 的方向 = A -> B 连线方向
        Vector2 pushDirection = (otherPiece.transform.position - selfPiece.transform.position).normalized;

        // 极端情况下防止零向量
        if (pushDirection.sqrMagnitude < 0.0001f)
        {
            pushDirection = ctx.incomingDir.normalized;
        }

        // 被撞硬币获得的实际剩余路程
        float transferredDistance = ctx.self.RemainingDistance * collisionConfig.coinTransferDistanceRatio;
        transferredDistance = Mathf.Max(0f, transferredDistance);

        if (transferredDistance <= 0.0001f)
        {
            return;
        }

        result.triggerOtherCoinMove = true;
        result.otherCoin = otherPiece;
        result.otherCoinDirection = pushDirection;
        result.otherCoinStartDistance = transferredDistance;
        result.otherCoinSpeedScale = collisionConfig.coinTransferSpeedRatio;

        // 双方进入短暂无敌，防止重复触发
        selfPiece.MarkCoinCollisionTriggered();
        otherPiece.MarkCoinCollisionTriggered();

        Debug.Log(
            $"[CollisionResolver] 己方互撞触发 | 发起:{selfPiece.name} | 被撞:{otherPiece.name} | " +
            $"pushDir:{pushDirection} | transferredDistance:{transferredDistance:F2}"
        );
    }

    /// <summary>
    /// 计算命中方向（中心点连线方向 A → B）
    /// </summary>
    private static Vector2 GetCenterLineDirection(CollisionContext ctx)
    {
        Vector2 selfPos = ctx.self.transform.position;
        Vector2 targetPos = ctx.target.transform.position;

        Vector2 dir = (targetPos - selfPos).normalized;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = ctx.incomingDir.normalized;
        }

        return dir;
    }

    /// <summary>
    /// 计算冲击强度（先按剩余路程比例）
    /// </summary>
    private static float GetImpactStrength(
        CollisionContext ctx,
        MovementConfig movementConfig
    )
    {
        if (movementConfig.totalDistance <= 0.0001f)
            return 0f;

        return ctx.self.RemainingDistance / movementConfig.totalDistance;
    }
}