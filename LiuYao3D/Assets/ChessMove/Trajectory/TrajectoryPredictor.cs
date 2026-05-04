/// <summary>
/// 静态工具类，不挂载到物体；由 TrajectoryRenderer / DragChargeInput 等脚本调用。
/// 实现功能：3D（XZ平面）轨迹预测，完全复用真实 Movement + Bounce 逻辑，保证预测路径与实际一致。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public static class TrajectoryPredictor
{
    public static List<Vector3> CalculatePath(
        Vector3 startPos,
        Vector3 direction,
        MovementConfig movementConfig,
        CollisionConfig collisionConfig,
        Collider selfCollider,
        float collisionRadius,
        float power,
        int maxBounceCount = 20
    )
    {
        List<Vector3> points = new List<Vector3>();

        if (movementConfig == null || collisionConfig == null)
            return points;

        if (direction.sqrMagnitude <= 0.0001f)
            return points;

        if (power <= 0.0001f)
            return points;

        Vector3 pos = startPos;
        pos.y = startPos.y;

        Vector3 dir = direction.normalized;
        dir.y = 0;

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
                selfCollider,
                collisionRadius
            );

            float traveled = bounceResult.traveledDistance;

            if (traveled <= 0.0001f)
                break;

            // ✅ 用真实安全位置（核心修复点）
            Vector3 pathPoint = bounceResult.newPos;
            points.Add(pathPoint);

            accumulatedDistance += traveled;
            remainingDistance -= traveled;
            remainingDistance = Mathf.Max(remainingDistance, 0f);

            if (!bounceResult.hit)
                break;

            // ===== 碰撞处理（完全复用真实规则） =====
            CollisionTarget target = null;
            if (bounceResult.collider != null)
            {
                target = bounceResult.collider.GetComponentInParent<CollisionTarget>();
            }

            float distanceMultiplier = GetDistanceMultiplier(target, collisionConfig);

            dir = bounceResult.newDir;
            dir.y = 0;

            remainingDistance *= distanceMultiplier;
            remainingDistance = Mathf.Max(remainingDistance, 0f);

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