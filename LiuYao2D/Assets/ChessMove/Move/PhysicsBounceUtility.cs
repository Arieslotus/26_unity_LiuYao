using UnityEngine;

/// <summary>
/// 单步物理反弹结果（包含命中信息）
/// </summary>
public struct BounceResult
{
    public Vector2 newPos;
    public Vector2 newDir;
    public float traveledDistance;
    public bool hit;

    public Vector2 normal;

    public Collider2D collider;   // 新增：命中的对象
    public Vector2 hitPoint;      // 新增：命中点
}

public static class PhysicsBounceUtility
{
    public static BounceResult SimulateStep(
        Vector2 pos,
        Vector2 dir,
        float maxDistance,
        MovementConfig config,
        Collider2D ignoreCollider = null
    )
    {
        BounceResult result = new BounceResult();

        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            pos,
            config.radius,
            dir,
            maxDistance
        );

        RaycastHit2D validHit = default;
        bool foundValidHit = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];

            if (hit.collider == null)
                continue;

            // 忽略自己
            if (ignoreCollider != null && hit.collider == ignoreCollider)
                continue;

            // 忽略0距离命中（通常是起始重叠或贴脸命中）
            if (hit.distance <= 0.0001f)
                continue;

            validHit = hit;
            foundValidHit = true;
            break;
        }

        // ===== 命中 =====
        if (foundValidHit)
        {
            float dist = validHit.distance;

            result.hit = true;
            result.traveledDistance = dist;
            result.normal = validHit.normal;
            result.collider = validHit.collider;
            result.hitPoint = validHit.point;

            // 反弹方向
            result.newDir = Vector2.Reflect(dir, validHit.normal).normalized;

            // CircleCast 命中时，centroid 更适合作为“圆心落点”
            float skinWidth = Mathf.Max(0.001f, config.radius * 0.01f);
            Vector2 safePos = validHit.centroid + validHit.normal * skinWidth;

            result.newPos = safePos;
        }
        else
        {
            // ===== 未命中 =====
            result.hit = false;
            result.traveledDistance = maxDistance;
            result.newPos = pos + dir * maxDistance;
            result.newDir = dir;
            result.collider = null;
            result.hitPoint = Vector2.zero;
            result.normal = Vector2.zero;
        }

        return result;
    }
}