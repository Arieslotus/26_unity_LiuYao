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
    public bool startedOverlapping;
    public float penetrationDistance;
}

public static class PhysicsBounceUtility
{
    public static bool DebugLogHits { get; set; }

    public static BounceResult SimulateStep(
        Vector3 pos,
        Vector3 dir,
        float maxDistance,
        MovementConfig config,
        Collider ignoreCollider,
        float radius,
        string debugOwnerName = null
    )
    {
        BounceResult result = new BounceResult();

        radius = Mathf.Max(0.001f, radius);

        if (TryResolveInitialOverlap(pos, dir, ignoreCollider, radius, debugOwnerName, out result))
        {
            return result;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            pos,
            radius,
            dir,
            maxDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        RaycastHit validHit = default;
        bool foundValidHit = false;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (DebugLogHits)
            {
                LogHitCandidate(debugOwnerName, hit, ignoreCollider, i);
            }

            if (hit.collider == null)
                continue;

            if (ShouldIgnoreCollider(hit.collider, ignoreCollider))
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
            if (DebugLogHits)
            {
                LogFinalHit(debugOwnerName, validHit, radius, maxDistance);
            }

            result.hit = true;
            result.traveledDistance = validHit.distance;
            result.normal = validHit.normal;
            result.collider = validHit.collider;
            result.hitPoint = validHit.point;
            result.startedOverlapping = false;
            result.penetrationDistance = 0f;

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
            if (DebugLogHits)
            {
                Debug.Log(
                    $"[PhysicsBounceUtility] 本步无有效命中 | owner:{debugOwnerName} | " +
                    $"pos:{pos} | dir:{dir} | maxDistance:{maxDistance:F6} | radius:{radius:F4} | " +
                    $"candidateCount:{hits.Length}"
                );
            }

            result.hit = false;
            result.traveledDistance = maxDistance;
            result.newPos = pos + dir * maxDistance;
            result.newDir = dir;
            result.collider = null;
            result.hitPoint = Vector3.zero;
            result.normal = Vector3.zero;
            result.startedOverlapping = false;
            result.penetrationDistance = 0f;
        }

        return result;
    }

    private static bool TryResolveInitialOverlap(
        Vector3 pos,
        Vector3 dir,
        Collider ignoreCollider,
        float radius,
        string debugOwnerName,
        out BounceResult result)
    {
        result = new BounceResult();

        Collider[] overlaps = Physics.OverlapSphere(
            pos,
            radius,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Collider bestCollider = null;
        Vector3 bestNormal = Vector3.zero;
        float bestPenetrationDistance = 0f;

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null)
                continue;

            if (ShouldIgnoreCollider(overlap, ignoreCollider))
                continue;

            Vector3 separationDirection;
            float separationDistance;
            bool hasPenetration = false;

            if (ignoreCollider != null)
            {
                Vector3 simulatedColliderPosition = GetSimulatedColliderPosition(ignoreCollider, pos);

                hasPenetration = Physics.ComputePenetration(
                    ignoreCollider,
                    simulatedColliderPosition,
                    ignoreCollider.transform.rotation,
                    overlap,
                    overlap.transform.position,
                    overlap.transform.rotation,
                    out separationDirection,
                    out separationDistance
                );
            }
            else
            {
                separationDirection = pos - overlap.bounds.center;
                separationDirection.y = 0f;
                separationDistance = radius;
                hasPenetration = separationDirection.sqrMagnitude > 0.0001f;
            }

            if (!hasPenetration)
                continue;

            CollisionTarget target = overlap.GetComponentInParent<CollisionTarget>();
            Vector3 horizontalSeparationDirection = separationDirection;
            horizontalSeparationDirection.y = 0f;

            if (horizontalSeparationDirection.sqrMagnitude <= 0.0001f && target == null)
                continue;

            if (horizontalSeparationDirection.sqrMagnitude <= 0.0001f)
            {
                horizontalSeparationDirection = -dir;
                horizontalSeparationDirection.y = 0f;
            }

            if (horizontalSeparationDirection.sqrMagnitude <= 0.0001f)
                continue;

            horizontalSeparationDirection.Normalize();

            if (separationDistance <= bestPenetrationDistance)
                continue;

            bestCollider = overlap;
            bestNormal = horizontalSeparationDirection;
            bestPenetrationDistance = separationDistance;
        }

        if (bestCollider == null)
            return false;

        float skinWidth = Mathf.Max(0.001f, radius * 0.01f);
        Vector3 safePos = pos + bestNormal * (bestPenetrationDistance + skinWidth);
        safePos.y = pos.y;

        Vector3 reflect = Vector3.Reflect(dir, bestNormal);
        reflect.y = 0f;

        result.hit = true;
        result.startedOverlapping = true;
        result.traveledDistance = 0f;
        result.normal = bestNormal;
        result.collider = bestCollider;
        result.hitPoint = pos - bestNormal * radius;
        result.newPos = safePos;
        result.newDir = reflect.sqrMagnitude > 0.0001f ? reflect.normalized : dir;
        result.penetrationDistance = bestPenetrationDistance;

        if (DebugLogHits)
        {
            CollisionTarget target = bestCollider.GetComponentInParent<CollisionTarget>();
            Debug.LogWarning(
                $"[PhysicsBounceUtility] 初始重叠命中 | owner:{debugOwnerName} | " +
                $"collider:{bestCollider.name} | object:{bestCollider.gameObject.name} | " +
                $"targetType:{(target != null ? target.type.ToString() : "无")} | " +
                $"pos:{pos} | normal:{bestNormal} | penetration:{bestPenetrationDistance:F6} | safePos:{safePos}"
            );
        }

        return true;
    }

    private static Vector3 GetSimulatedColliderPosition(Collider collider, Vector3 simulatedCenter)
    {
        Vector3 currentCenter = GetColliderWorldCenter(collider);
        return collider.transform.position + (simulatedCenter - currentCenter);
    }

    private static Vector3 GetColliderWorldCenter(Collider collider)
    {
        if (collider is SphereCollider sphereCollider)
            return sphereCollider.transform.TransformPoint(sphereCollider.center);

        if (collider is BoxCollider boxCollider)
            return boxCollider.transform.TransformPoint(boxCollider.center);

        if (collider is CapsuleCollider capsuleCollider)
            return capsuleCollider.transform.TransformPoint(capsuleCollider.center);

        return collider.bounds.center;
    }

    private static void LogHitCandidate(string ownerName, RaycastHit hit, Collider ignoreCollider, int index)
    {
        if (hit.collider == null)
        {
            Debug.Log($"[PhysicsBounceUtility] 候选命中为空 | owner:{ownerName} | index:{index}");
            return;
        }

        CollisionTarget target = hit.collider.GetComponentInParent<CollisionTarget>();
        string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
        bool isIgnoredSelf = ShouldIgnoreCollider(hit.collider, ignoreCollider);

        Debug.Log(
            $"[PhysicsBounceUtility] 候选命中 | owner:{ownerName} | index:{index} | " +
            $"collider:{hit.collider.name} | object:{hit.collider.gameObject.name} | " +
            $"layer:{hit.collider.gameObject.layer}({layerName}) | isTrigger:{hit.collider.isTrigger} | " +
            $"distance:{hit.distance:F6} | point:{hit.point} | normal:{hit.normal} | " +
            $"hasTarget:{target != null} | targetType:{(target != null ? target.type.ToString() : "无")} | " +
            $"ignoredSelf:{isIgnoredSelf}"
        );
    }

    private static void LogFinalHit(string ownerName, RaycastHit hit, float radius, float maxDistance)
    {
        CollisionTarget target = hit.collider != null
            ? hit.collider.GetComponentInParent<CollisionTarget>()
            : null;

        string layerName = hit.collider != null
            ? LayerMask.LayerToName(hit.collider.gameObject.layer)
            : "无";

        Debug.Log(
            $"[PhysicsBounceUtility] 最终有效命中 | owner:{ownerName} | " +
            $"collider:{(hit.collider != null ? hit.collider.name : "空")} | " +
            $"object:{(hit.collider != null ? hit.collider.gameObject.name : "空")} | " +
            $"layer:{(hit.collider != null ? hit.collider.gameObject.layer : -1)}({layerName}) | " +
            $"isTrigger:{(hit.collider != null && hit.collider.isTrigger)} | " +
            $"distance:{hit.distance:F6} | maxDistance:{maxDistance:F6} | radius:{radius:F4} | " +
            $"point:{hit.point} | normal:{hit.normal} | " +
            $"hasTarget:{target != null} | targetType:{(target != null ? target.type.ToString() : "无")}"
        );
    }

    public static bool ShouldIgnoreCollider(Collider candidate, Collider ignoreCollider)
    {
        if (candidate == null)
            return false;

        if (candidate.isTrigger)
            return true;

        if (ignoreCollider == null)
            return false;

        if (candidate == ignoreCollider)
            return true;

        Rigidbody candidateRigidbody = candidate.attachedRigidbody;
        Rigidbody ignoreRigidbody = ignoreCollider.attachedRigidbody;
        if (candidateRigidbody != null && candidateRigidbody == ignoreRigidbody)
            return true;

        MovementController candidateMovement = candidate.GetComponentInParent<MovementController>();
        MovementController ignoreMovement = ignoreCollider.GetComponentInParent<MovementController>();
        if (candidateMovement != null && candidateMovement == ignoreMovement)
            return true;

        ChessPiece candidatePiece = candidate.GetComponentInParent<ChessPiece>();
        ChessPiece ignorePiece = ignoreCollider.GetComponentInParent<ChessPiece>();
        return candidatePiece != null && candidatePiece == ignorePiece;
    }
}
