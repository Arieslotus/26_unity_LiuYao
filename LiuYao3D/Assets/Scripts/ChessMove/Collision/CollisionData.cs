/// <summary>
/// 实现功能：定义碰撞系统使用的上下文数据，包括发射来源、碰撞输入与碰撞输出结构。
/// </summary>
using UnityEngine;

public enum ShotSourceType
{
    PlayerInput,
    CoinCollision
}

public struct ShotContext
{
    public bool isPlayerShot;
    public bool isFullCharge;
    public float power;
    public ShotSourceType sourceType;
}

public struct CollisionContext
{
    public MovementController self;
    public CollisionTarget target;

    public Collider selfCollider;
    public Collider hitCollider;

    public Vector3 hitPoint;
    public Vector3 normal;
    public Vector3 incomingDir;

    public ShotContext shotContext;

    // 预测轨迹会复用碰撞规则，但不能修改真实游戏状态。
    public bool suppressSideEffects;

    // 预测阶段的剩余距离不一定等于 MovementController 当前运行时剩余距离。
    public bool useRemainingDistanceOverride;
    public float remainingDistanceOverride;
}

public struct CollisionResult
{
    // 通用移动结果
    public Vector3 newDirection;
    public float remainingDistanceMultiplier;
    public bool stopImmediately;

    // 己方硬币互撞：启动另一枚硬币
    public bool triggerOtherCoinMove;
    public ChessPiece otherCoin;
    public Vector3 otherCoinDirection;
    public float otherCoinStartDistance;
    public float otherCoinSpeedScale;

    // 敌人受击
    public bool triggerHitTarget;
    public Vector3 hitDirection;
    public float impactStrength;
    public Collider collider;
}
