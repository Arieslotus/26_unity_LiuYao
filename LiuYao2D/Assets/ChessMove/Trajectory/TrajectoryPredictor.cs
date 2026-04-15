using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 轨迹预测（按当前真实碰撞规则预测“当前棋子自身”的路径）
/// 只预测当前棋子自身：
/// - 撞障碍物：继续反弹并按 obstacleBounceMultiplier 衰减
/// - 撞敌人：继续反弹并按 enemyBounceMultiplier 衰减
/// - 撞己方硬币：继续反弹并按 coinBounceMultiplier 衰减
/// 不预测被撞对象自身后续轨迹。
/// </summary>
public static class TrajectoryPredictor
{
    public static List<Vector2> CalculatePath(
        Vector2 startPos,
        Vector2 direction,
        MovementConfig movementConfig,
        CollisionConfig collisionConfig,
        Collider2D selfCollider,
        float power,
        int maxBounceCount = 20
    )
    {
        List<Vector2> points = new List<Vector2>();

        if (movementConfig == null || collisionConfig == null)
            return points;

        if (direction.sqrMagnitude <= 0.0001f)
            return points;

        if (power <= 0.0001f)
            return points;

        Vector2 pos = startPos;
        Vector2 dir = direction.normalized;

        float accumulatedDistance = 0f;
        float remainingDistance = movementConfig.totalDistance * power;
        float maxDisplayDistance = movementConfig.maxDisplayDistance;

        points.Add(pos);

        for (int i = 0; i < maxBounceCount; i++)
        {
            if (remainingDistance <= 0.0001f)
                break;

            float remainingDisplay = maxDisplayDistance - accumulatedDistance;
            if (remainingDisplay <= 0.0001f)
                break;

            float maxStep = Mathf.Min(remainingDistance, remainingDisplay);

            BounceResult bounceResult = PhysicsBounceUtility.SimulateStep(
                pos,
                dir,
                maxStep,
                movementConfig,
                selfCollider
            );

            float traveled = bounceResult.traveledDistance;

            if (traveled <= 0.0001f)
                break;

            // 这一段路径终点 / 折点
            Vector2 pathPoint = pos + dir * traveled;
            points.Add(pathPoint);

            accumulatedDistance += traveled;
            remainingDistance -= traveled;
            remainingDistance = Mathf.Max(remainingDistance, 0f);

            if (!bounceResult.hit)
                break;

            // ===== 命中后，按当前真实规则处理“当前棋子自身” =====
            CollisionTarget target = null;
            if (bounceResult.collider != null)
            {
                target = bounceResult.collider.GetComponentInParent<CollisionTarget>();
            }

            float distanceMultiplier = GetDistanceMultiplier(target, collisionConfig);

            // 方向与真实逻辑一致：仍按法线反射
            dir = bounceResult.newDir;

            // 路径衰减按碰撞对象类型决定
            remainingDistance *= distanceMultiplier;
            remainingDistance = Mathf.Max(remainingDistance, 0f);

            // 下一次模拟从安全位置继续
            pos = bounceResult.newPos;
        }

        return points;
    }

    private static float GetDistanceMultiplier(CollisionTarget target, CollisionConfig collisionConfig)
    {
        if (target == null)
            return collisionConfig.obstacleBounceMultiplier;

        switch (target.type)
        {
            case CollisionType.Obstacle:
                return collisionConfig.obstacleBounceMultiplier;

            case CollisionType.Enemy:
                return collisionConfig.enemyBounceMultiplier;

            case CollisionType.PlayerCoin:
                return collisionConfig.coinBounceMultiplier;

            default:
                return collisionConfig.obstacleBounceMultiplier;
        }
    }
}