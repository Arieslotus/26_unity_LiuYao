using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 轨迹预测（复用统一物理逻辑）
/// </summary>
public static class TrajectoryPredictor
{
    public static List<Vector2> CalculatePath(
        Vector2 startPos,
        Vector2 direction,
        MovementConfig config,
        float power,
        int maxBounceCount = 20
    )
    {
        List<Vector2> points = new List<Vector2>();

        if (config == null)
            return points;

        if (direction.sqrMagnitude <= 0.0001f)
            return points;

        if (power <= 0.0001f)
            return points;

        Vector2 pos = startPos;
        Vector2 dir = direction.normalized;

        float accumulatedDistance = 0f;
        float remainingDistance = config.totalDistance * power;
        float maxDisplayDistance = config.maxDisplayDistance;

        points.Add(pos);

        for (int i = 0; i < maxBounceCount; i++)
        {
            if (remainingDistance <= 0.0001f)
                break;

            float remainingDisplay = maxDisplayDistance - accumulatedDistance;
            if (remainingDisplay <= 0f)
                break;

            float maxStep = Mathf.Min(remainingDistance, remainingDisplay);

            BounceResult result = PhysicsBounceUtility.SimulateStep(
                pos,
                dir,
                maxStep,
                config
            );

            float traveled = result.traveledDistance;

            // 防止极端情况下死循环
            if (traveled <= 0.0001f)
            {
                break;
            }

            // 这里用于画线的点，表示“这一段的终点 / 反弹折点”
            Vector2 pathPoint = pos + dir * traveled;
            points.Add(pathPoint);

            accumulatedDistance += traveled;
            remainingDistance -= traveled;

            if (result.hit)
            {
                // 下一次模拟从安全位置继续
                pos = result.newPos;
                dir = result.newDir;

                // 和真实移动一致：碰撞后路径衰减
                remainingDistance *= config.bounceDamping;
            }
            else
            {
                break;
            }
        }

        return points;
    }
}