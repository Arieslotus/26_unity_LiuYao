/// <summary>
/// 实现功能：根据敌人或单位的 Collider 参数计算 XZ 平面占位半径。
/// </summary>
using UnityEngine;

public static class EnemySpawnFootprintUtility
{
    private const float DefaultRadius = 0.5f;

    public static float GetHorizontalRadius(GameObject root)
    {
        if (root == null)
            return DefaultRadius;

        return GetHorizontalRadius(root.transform);
    }

    public static float GetHorizontalRadius(Transform root)
    {
        if (root == null)
            return DefaultRadius;

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        float radius = 0f;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled || collider.isTrigger)
                continue;

            radius = Mathf.Max(radius, GetColliderHorizontalRadius(root, collider));
        }

        return radius > 0f ? radius : DefaultRadius;
    }

    private static float GetColliderHorizontalRadius(Transform root, Collider collider)
    {
        return GetColliderHorizontalExtent(collider);
    }

    private static Vector3 GetWorldCenter(Collider collider)
    {
        if (collider is BoxCollider box)
            return box.transform.TransformPoint(box.center);

        if (collider is SphereCollider sphere)
            return sphere.transform.TransformPoint(sphere.center);

        if (collider is CapsuleCollider capsule)
            return capsule.transform.TransformPoint(capsule.center);

        return collider.transform.position;
    }

    private static float GetColliderHorizontalExtent(Collider collider)
    {
        Vector3 scale = collider.transform.lossyScale;

        if (collider is SphereCollider sphere)
        {
            return sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        if (collider is CapsuleCollider capsule)
        {
            float radius = capsule.radius;
            float halfHeight = Mathf.Max(capsule.height * 0.5f, radius);

            switch (capsule.direction)
            {
                case 0:
                    return Mathf.Max(
                        halfHeight * Mathf.Abs(scale.x),
                        radius * Mathf.Abs(scale.z)
                    );
                case 2:
                    return Mathf.Max(
                        radius * Mathf.Abs(scale.x),
                        halfHeight * Mathf.Abs(scale.z)
                    );
                default:
                    return radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            }
        }

        if (collider is BoxCollider box)
        {
            return Mathf.Max(
                box.size.x * Mathf.Abs(scale.x),
                box.size.z * Mathf.Abs(scale.z)
            ) * 0.5f;
        }

        Bounds bounds = collider.bounds;
        return Mathf.Max(bounds.extents.x, bounds.extents.z);
    }

    public static float GetFlatDistanceXZ(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
