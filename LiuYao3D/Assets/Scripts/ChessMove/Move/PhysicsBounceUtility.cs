/// <summary>
/// 实现功能：3D 平面（XZ）下的球形投射反弹计算。
/// 碰撞半径由外部传入，通常读取棋子自身 SphereCollider，而不是 MovementConfig。
/// </summary>
using UnityEngine;

public struct BounceResult
{
    public Vector3 newPos; // 这里表示“碰撞体中心”的新位置
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
        Collider ignoreCollider,
        float radius
    )
    {
        BounceResult result = new BounceResult();

        radius = Mathf.Max(0.001f, radius);

        RaycastHit[] hits = Physics.SphereCastAll(
            pos,
            radius,
            dir,
            maxDistance
        );

        RaycastHit validHit = default;
        bool foundValidHit = false;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null)
                continue;

            if (ignoreCollider != null && hit.collider == ignoreCollider)
                continue;

            if (hit.distance <= 0.0001f)
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                validHit = hit;
                foundValidHit = true;
            }
        }

        if (foundValidHit)
        {
            result.hit = true;
            result.traveledDistance = validHit.distance;
            result.normal = validHit.normal;
            result.collider = validHit.collider;
            result.hitPoint = validHit.point;

            Vector3 reflect = Vector3.Reflect(dir, validHit.normal);
            reflect.y = 0f;
            result.newDir = reflect.sqrMagnitude > 0.0001f ? reflect.normalized : dir;

            float skinWidth = Mathf.Max(0.001f, radius * 0.01f);

            Vector3 safePos = validHit.point + validHit.normal * (radius + skinWidth);
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