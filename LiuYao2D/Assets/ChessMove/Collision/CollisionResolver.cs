/// <summary>
/// 实现功能：作为碰撞规则统一入口，处理障碍物、敌人与己方硬币互撞的普通结算逻辑。
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

        Debug.Log($"[CollisionResolver] 普通碰撞类型: {ctx.target.type}");
        return result;
    }

    private static CollisionResult CreateDefaultResult(
        CollisionContext ctx,
        MovementConfig movementConfig
    )
    {
        return new CollisionResult
        {
            newDirection = Vector2.Reflect(ctx.incomingDir, ctx.normal).normalized,
            remainingDistanceMultiplier = movementConfig.bounceDamping,
            stopImmediately = false,

            triggerOtherCoinMove = false,
            otherCoin = null,
            otherCoinDirection = Vector2.zero,
            otherCoinStartDistance = 0f,
            otherCoinSpeedScale = 1f,

            triggerHitTarget = false,
            hitDirection = Vector2.zero,
            impactStrength = 0f,
            collider = ctx.hitCollider
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

        Vector2 hitDir = GetCenterLineDirection(ctx);
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
            return;
        }

        if (selfPiece == otherPiece)
        {
            return;
        }

        if (otherPiece.IsMoving)
        {
            return;
        }

        if (!selfPiece.CanTriggerCoinCollision(collisionConfig.coinCollisionCooldown) ||
            !otherPiece.CanTriggerCoinCollision(collisionConfig.coinCollisionCooldown))
        {
            return;
        }

        Vector2 pushDirection = (otherPiece.transform.position - selfPiece.transform.position).normalized;

        if (pushDirection.sqrMagnitude < 0.0001f)
        {
            pushDirection = ctx.incomingDir.normalized;
        }

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

        selfPiece.MarkCoinCollisionTriggered();
        otherPiece.MarkCoinCollisionTriggered();

        Debug.Log(
            $"[CollisionResolver] 己方互撞触发 | 发起:{selfPiece.name} | 被撞:{otherPiece.name} | " +
            $"pushDir:{pushDirection} | transferredDistance:{transferredDistance:F2}"
        );
    }

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