/// <summary>
/// 实现功能：作为碰撞规则统一入口，处理 3D（XZ平面）下障碍物、敌人与己方硬币互撞的普通结算逻辑。
/// 所有方向计算统一限制在 XZ 平面，Y 轴仅作为表现层高度，不参与逻辑结算。
/// </summary>
using UnityEngine;

public static class CollisionResolver
{
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

        if (!ctx.suppressSideEffects)
        {
            Debug.Log($"[CollisionResolver] 普通碰撞类型: {ctx.target.type}");
        }

        return result;
    }

    private static CollisionResult CreateDefaultResult(
        CollisionContext ctx,
        MovementConfig movementConfig
    )
    {
        Vector3 reflectedDir = Vector3.Reflect(ctx.incomingDir, ctx.normal);
        reflectedDir.y = 0f;

        if (reflectedDir.sqrMagnitude <= 0.0001f)
        {
            reflectedDir = ctx.incomingDir;
            reflectedDir.y = 0f;
        }

        reflectedDir = reflectedDir.normalized;

        return new CollisionResult
        {
            newDirection = reflectedDir,
            remainingDistanceMultiplier = movementConfig.bounceDamping,
            stopImmediately = false,

            triggerOtherCoinMove = false,
            otherCoin = null,
            otherCoinDirection = Vector3.zero,
            otherCoinStartDistance = 0f,
            otherCoinSpeedScale = 1f,

            triggerHitTarget = false,
            hitDirection = Vector3.zero,
            impactStrength = 0f,
            collider = ctx.hitCollider,
            hitPoint = ctx.hitPoint
        };
    }

    private static void ResolveEnemyCollision(
        CollisionContext ctx,
        MovementConfig movementConfig,
        CollisionConfig collisionConfig,
        ref CollisionResult result
    )
    {
        result.remainingDistanceMultiplier = collisionConfig.enemyBounceMultiplier;

        Vector3 hitDir = GetCenterLineDirection(ctx);
        float strength = GetImpactStrength(ctx, movementConfig);

        result.triggerHitTarget = true;
        result.hitDirection = hitDir;
        result.impactStrength = strength;
    }

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
            LogCoinCollisionBlocked(
                collisionConfig,
                "缺少 ChessPiece",
                selfPiece,
                otherPiece,
                ctx,
                0f,
                Vector3.zero
            );
            return;
        }

        if (selfPiece == otherPiece)
        {
            LogCoinCollisionBlocked(
                collisionConfig,
                "命中自身",
                selfPiece,
                otherPiece,
                ctx,
                0f,
                Vector3.zero
            );
            return;
        }

        if (otherPiece.IsMoving)
        {
            LogCoinCollisionBlocked(
                collisionConfig,
                "被动币正在移动",
                selfPiece,
                otherPiece,
                ctx,
                0f,
                Vector3.zero
            );
            return;
        }

        if (!selfPiece.CanTriggerCoinCollision(collisionConfig.coinCollisionCooldown) ||
            !otherPiece.CanTriggerCoinCollision(collisionConfig.coinCollisionCooldown))
        {
            LogCoinCollisionBlocked(
                collisionConfig,
                "冷却中",
                selfPiece,
                otherPiece,
                ctx,
                0f,
                Vector3.zero
            );
            return;
        }

        Vector3 pushDirection = otherPiece.transform.position - selfPiece.transform.position;
        pushDirection.y = 0f;

        if (pushDirection.sqrMagnitude < 0.0001f)
        {
            pushDirection = ctx.incomingDir;
            pushDirection.y = 0f;
        }

        if (pushDirection.sqrMagnitude < 0.0001f)
        {
            LogCoinCollisionBlocked(
                collisionConfig,
                "推力方向无效",
                selfPiece,
                otherPiece,
                ctx,
                0f,
                pushDirection
            );
            return;
        }

        pushDirection = pushDirection.normalized;

        float transferredDistance = GetRemainingDistance(ctx) * collisionConfig.coinTransferDistanceRatio;
        transferredDistance = Mathf.Max(0f, transferredDistance);

        if (transferredDistance <= 0.0001f)
        {
            LogCoinCollisionBlocked(
                collisionConfig,
                "传递距离过小",
                selfPiece,
                otherPiece,
                ctx,
                transferredDistance,
                pushDirection
            );
            return;
        }

        result.triggerOtherCoinMove = true;
        result.otherCoin = otherPiece;
        result.otherCoinDirection = pushDirection;
        result.otherCoinStartDistance = transferredDistance;
        result.otherCoinSpeedScale = collisionConfig.coinTransferSpeedRatio;

        if (!ctx.suppressSideEffects)
        {
            selfPiece.MarkCoinCollisionTriggered();
            otherPiece.MarkCoinCollisionTriggered();
        }

        if (!ctx.suppressSideEffects)
        {
            Debug.Log(
                $"[CollisionResolver] 己方互撞触发 | 发起:{selfPiece.name} | 被撞:{otherPiece.name} | " +
                $"pushDir:{pushDirection} | transferredDistance:{transferredDistance:F2}"
            );
        }
    }

    private static void LogCoinCollisionBlocked(
        CollisionConfig collisionConfig,
        string reason,
        ChessPiece selfPiece,
        ChessPiece otherPiece,
        CollisionContext ctx,
        float transferredDistance,
        Vector3 pushDirection
    )
    {
        if (ctx.suppressSideEffects || collisionConfig == null || !collisionConfig.debugCoinCollisionResolve)
            return;

        string selfName = selfPiece != null ? selfPiece.name : "空";
        string otherName = otherPiece != null ? otherPiece.name : "空";
        bool selfCanTrigger = selfPiece != null && selfPiece.CanTriggerCoinCollision(collisionConfig.coinCollisionCooldown);
        bool otherCanTrigger = otherPiece != null && otherPiece.CanTriggerCoinCollision(collisionConfig.coinCollisionCooldown);

        Debug.LogWarning(
            $"[CollisionResolver] 己方互撞未触发 | 原因:{reason} | " +
            $"发起:{selfName} | 被撞:{otherName} | " +
            $"selfMoving:{(selfPiece != null && selfPiece.IsMoving)} | otherMoving:{(otherPiece != null && otherPiece.IsMoving)} | " +
            $"selfCanTrigger:{selfCanTrigger} | otherCanTrigger:{otherCanTrigger} | cooldown:{collisionConfig.coinCollisionCooldown:F3} | " +
            $"selfPos:{(selfPiece != null ? selfPiece.transform.position.ToString() : "空")} | " +
            $"otherPos:{(otherPiece != null ? otherPiece.transform.position.ToString() : "空")} | " +
            $"incomingDir:{ctx.incomingDir} | normal:{ctx.normal} | hitPoint:{ctx.hitPoint} | " +
            $"remainingDistance:{GetRemainingDistance(ctx):F4} | " +
            $"transferredDistance:{transferredDistance:F4} | pushDirection:{pushDirection}"
        );
    }

    private static Vector3 GetCenterLineDirection(CollisionContext ctx)
    {
        Vector3 selfPos = ctx.self.transform.position;
        Vector3 targetPos = ctx.target.transform.position;

        Vector3 dir = targetPos - selfPos;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = ctx.incomingDir;
            dir.y = 0f;
        }

        if (dir.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        return dir.normalized;
    }

    private static float GetImpactStrength(
        CollisionContext ctx,
        MovementConfig movementConfig
    )
    {
        if (movementConfig == null || movementConfig.totalDistance <= 0.0001f)
            return 0f;

        return GetRemainingDistance(ctx) / movementConfig.totalDistance;
    }

    private static float GetRemainingDistance(CollisionContext ctx)
    {
        if (ctx.useRemainingDistanceOverride)
            return Mathf.Max(0f, ctx.remainingDistanceOverride);

        return ctx.self != null ? Mathf.Max(0f, ctx.self.RemainingDistance) : 0f;
    }
}
