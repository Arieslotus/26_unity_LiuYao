/// <summary>
/// 实现功能：作为碰撞规则统一入口，处理障碍物、敌人与己方硬币互撞的结算逻辑。
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
        CollisionResult result = new CollisionResult
        {
            // 默认：正常反弹
            newDirection = Vector2.Reflect(ctx.incomingDir, ctx.normal).normalized,
            remainingDistanceMultiplier = movementConfig.bounceDamping,
            stopImmediately = false,
            triggerFlip = false,
            triggerOtherCoinMove = false,
            otherCoin = null,
            otherCoinDirection = Vector2.zero,
            otherCoinStartDistance = 0f,
            otherCoinSpeedScale = 1f
        };

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
                {
                    result.remainingDistanceMultiplier = collisionConfig.enemyBounceMultiplier;

                    // ===== 计算命中方向（中心点连线方向（A → B））（给敌人用,指向即将被推/受力的方向）=====
                    Vector2 selfPos = ctx.self.transform.position;
                    Vector2 enemyPos = ctx.target.transform.position;

                    Vector2 hitDir = (enemyPos - selfPos).normalized;

                    // 防止极端重合
                    if (hitDir.sqrMagnitude < 0.0001f)
                    {
                        hitDir = ctx.incomingDir.normalized;
                    }

                    // ===== 冲击强度（先简单用剩余路程比例）=====
                    float strength = ctx.self.RemainingDistance / movementConfig.totalDistance;

                    result.triggerHitTarget = true;
                    result.hitDirection = hitDir;
                    result.impactStrength = strength;

                    break;
                }

            case CollisionType.PlayerCoin:
                ResolvePlayerCoinCollision(ctx, collisionConfig, ref result);
                break;
        }

        Debug.Log($"Type: {ctx.target?.type}");

        return result;
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
}