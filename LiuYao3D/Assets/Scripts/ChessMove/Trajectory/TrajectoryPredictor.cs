/// <summary>
/// 实现功能：静态轨迹预测工具，复用真实碰撞检测与碰撞结算规则计算 XZ 平面预测路径。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public struct TrajectoryPathPoint
{
    public Vector3 Position { get; }
    public bool IsCollisionPoint { get; }

    public TrajectoryPathPoint(Vector3 position, bool isCollisionPoint)
    {
        Position = position;
        IsCollisionPoint = isCollisionPoint;
    }
}

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
        int maxBounceCount = 20,
        MovementController selfMovement = null
    )
    {
        List<TrajectoryPathPoint> pathPoints = CalculatePathWithCollisionInfo(
            startPos,
            direction,
            movementConfig,
            collisionConfig,
            selfCollider,
            collisionRadius,
            power,
            maxBounceCount,
            selfMovement
        );

        List<Vector3> points = new List<Vector3>(pathPoints.Count);
        for (int i = 0; i < pathPoints.Count; i++)
        {
            points.Add(pathPoints[i].Position);
        }

        return points;
    }

    public static List<TrajectoryPathPoint> CalculatePathWithCollisionInfo(
        Vector3 startPos,
        Vector3 direction,
        MovementConfig movementConfig,
        CollisionConfig collisionConfig,
        Collider selfCollider,
        float collisionRadius,
        float power,
        int maxBounceCount = 20,
        MovementController selfMovement = null
    )
    {
        List<TrajectoryPathPoint> points = new List<TrajectoryPathPoint>();

        if (movementConfig == null || collisionConfig == null)
            return points;

        if (direction.sqrMagnitude <= 0.0001f)
            return points;

        if (power <= 0.0001f)
            return points;

        Vector3 pos = startPos;
        pos.y = startPos.y;

        Vector3 dir = direction.normalized;
        dir.y = 0f;

        if (selfMovement == null && selfCollider != null)
        {
            selfMovement = selfCollider.GetComponentInParent<MovementController>();
        }

        float accumulatedDistance = 0f;
        float remainingDistance = movementConfig.totalDistance * power;
        float maxDisplayDistance = movementConfig.maxDisplayDistance;

        points.Add(new TrajectoryPathPoint(pos, false));

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
            {
                if (!bounceResult.hit || !bounceResult.startedOverlapping)
                    break;

                pos = bounceResult.newPos;
                points.Add(new TrajectoryPathPoint(pos, true));

                CollisionResult overlapResult = ResolveCollision(
                    selfMovement,
                    selfCollider,
                    bounceResult,
                    dir,
                    movementConfig,
                    collisionConfig,
                    power,
                    remainingDistance
                );

                dir = FlattenDirection(overlapResult.newDirection, dir);
                remainingDistance *= overlapResult.remainingDistanceMultiplier;
                remainingDistance = Mathf.Max(remainingDistance, 0f);
                continue;
            }

            Vector3 pathPoint = bounceResult.newPos;
            points.Add(new TrajectoryPathPoint(pathPoint, bounceResult.hit));

            accumulatedDistance += traveled;
            remainingDistance -= traveled;
            remainingDistance = Mathf.Max(remainingDistance, 0f);

            if (!bounceResult.hit)
                break;

            CollisionResult collisionResult = ResolveCollision(
                selfMovement,
                selfCollider,
                bounceResult,
                dir,
                movementConfig,
                collisionConfig,
                power,
                remainingDistance
            );

            dir = FlattenDirection(collisionResult.newDirection, dir);
            remainingDistance *= collisionResult.remainingDistanceMultiplier;
            remainingDistance = Mathf.Max(remainingDistance, 0f);

            pos = bounceResult.newPos;
        }

        return points;
    }

    private static CollisionResult ResolveCollision(
        MovementController selfMovement,
        Collider selfCollider,
        BounceResult bounceResult,
        Vector3 incomingDirection,
        MovementConfig movementConfig,
        CollisionConfig collisionConfig,
        float power,
        float remainingDistance
    )
    {
        CollisionTarget target = null;
        if (bounceResult.collider != null)
        {
            target = bounceResult.collider.GetComponentInParent<CollisionTarget>();
        }

        if (selfMovement == null)
        {
            return new CollisionResult
            {
                newDirection = bounceResult.newDir,
                remainingDistanceMultiplier = GetFallbackDistanceMultiplier(target, collisionConfig),
                stopImmediately = false,
                triggerOtherCoinMove = false,
                otherCoin = null,
                otherCoinDirection = Vector3.zero,
                otherCoinStartDistance = 0f,
                otherCoinSpeedScale = 1f,
                triggerHitTarget = false,
                hitDirection = Vector3.zero,
                impactStrength = 0f,
                collider = bounceResult.collider
            };
        }

        CollisionContext ctx = new CollisionContext
        {
            self = selfMovement,
            target = target,
            selfCollider = selfCollider,
            hitCollider = bounceResult.collider,
            hitPoint = bounceResult.hitPoint,
            normal = bounceResult.normal,
            incomingDir = incomingDirection,
            shotContext = new ShotContext
            {
                isPlayerShot = true,
                isFullCharge = false,
                power = power,
                sourceType = ShotSourceType.PlayerInput
            },
            suppressSideEffects = true,
            useRemainingDistanceOverride = true,
            remainingDistanceOverride = remainingDistance
        };

        return CollisionResolver.Resolve(ctx, movementConfig, collisionConfig);
    }

    private static Vector3 FlattenDirection(Vector3 direction, Vector3 fallback)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            return direction.normalized;

        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
    }

    private static float GetFallbackDistanceMultiplier(CollisionTarget target, CollisionConfig collisionConfig)
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
