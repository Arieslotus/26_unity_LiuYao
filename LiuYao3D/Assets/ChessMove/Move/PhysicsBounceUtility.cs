/// <summary>
/// 实现功能：3D 平面（XZ）下的球形投射反弹计算
/// </summary>
using UnityEngine;

public struct BounceResult
{
    public Vector3 newPos;
    public Vector3 newDir;
    public float traveledDistance;
    public bool hit;

    public Vector3 normal;

    public Collider collider;
    public Vector3 hitPoint;
}

public static class PhysicsBounceUtility
{
    public static BounceResult SimulateStep(
        Vector3 pos,
        Vector3 dir,
        float maxDistance,
        MovementConfig config,
        Collider ignoreCollider = null
    )
    {
        BounceResult result = new BounceResult();

        RaycastHit[] hits = Physics.SphereCastAll(
            pos,
            config.radius,
            dir,
            maxDistance
        );

        RaycastHit validHit = default;
        bool foundValidHit = false;

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];

            if (hit.collider == null)
                continue;

            if (ignoreCollider != null && hit.collider == ignoreCollider)
                continue;

            if (hit.distance <= 0.0001f)
                continue;

            validHit = hit;
            foundValidHit = true;
            break;
        }

        if (foundValidHit)
        {
            float dist = validHit.distance;

            result.hit = true;
            result.traveledDistance = dist;
            result.normal = validHit.normal;
            result.collider = validHit.collider;
            result.hitPoint = validHit.point;

            // ⚠️ 关键：限制反射在XZ平面
            Vector3 reflect = Vector3.Reflect(dir, validHit.normal);
            reflect.y = 0;
            result.newDir = reflect.normalized;

            float skinWidth = Mathf.Max(0.001f, config.radius * 0.01f);

            // SphereCast 的 hit.point 更接近接触点，不是球心位置
            // 所以球心安全落点应当沿法线退回“半径 + skin”
            Vector3 safePos = validHit.point + validHit.normal * (config.radius + skinWidth);

            // 锁定在逻辑平面高度
            safePos.y = pos.y;

            result.newPos = safePos;
        }
        else
        {
            result.hit = false;
            result.traveledDistance = maxDistance;
            result.newPos = pos + dir * maxDistance;
            result.newDir = dir;
            result.collider = null;
            result.hitPoint = Vector3.zero;
            result.normal = Vector3.zero;
        }

        return result;
    }
}