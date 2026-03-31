using UnityEngine;

/// <summary>
/// 单步物理反弹模拟（统一逻辑源）
/// </summary>
public struct BounceResult
{
    public Vector2 newPos;
    public Vector2 newDir;
    public float traveledDistance;
    public bool hit;
    public Vector2 normal;
}

public static class PhysicsBounceUtility
{
    public static BounceResult SimulateStep(
        Vector2 pos,
        Vector2 dir,
        float maxDistance,
        MovementConfig config
    )
    {
        BounceResult result = new BounceResult();

        RaycastHit2D hit = Physics2D.CircleCast(
            pos,
            config.radius,
            dir,
            maxDistance
        );

        // ===== 命中 =====
        if (hit.collider != null && hit.distance > 0.0001f)
        {
            float dist = hit.distance;

            Vector2 hitPoint = pos + dir * dist;

            result.hit = true;
            result.traveledDistance = dist;
            result.normal = hit.normal;

            // 反弹
            result.newDir = Vector2.Reflect(dir, hit.normal).normalized;

            // CircleCast 命中时，centroid 更适合作为“圆心落点”
            float skinWidth = Mathf.Max(0.001f, config.radius * 0.01f);
            Vector2 safePos = hit.centroid + hit.normal * skinWidth;

            result.newPos = safePos;
        }
        else
        {
            // ===== 未命中 =====
            result.hit = false;
            result.traveledDistance = maxDistance;
            result.newPos = pos + dir * maxDistance;
            result.newDir = dir;
        }

        return result;
    }
}