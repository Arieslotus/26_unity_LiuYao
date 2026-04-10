/// <summary>
/// 实现功能：定义碰撞系统使用的上下文数据，包括发射来源、碰撞输入与碰撞输出结构。
/// </summary>
using UnityEngine;

/// <summary>
/// 发射来源类型
/// </summary>
public enum ShotSourceType
{
    PlayerInput,
    CoinCollision
}

/// <summary>
/// 一次发射的上下文信息（用于判断翻面、来源等规则）
/// </summary>
public struct ShotContext
{
    public bool isPlayerShot;
    public bool isFullCharge;
    public float power;
    public ShotSourceType sourceType;
}

/// <summary>
/// 碰撞输入信息（提供给碰撞规则系统）
/// </summary>
public struct CollisionContext
{
    public MovementController self;
    public CollisionTarget target;

    public Vector2 hitPoint;
    public Vector2 normal;
    public Vector2 incomingDir;

    public ShotContext shotContext;
}

/// <summary>
/// 碰撞结算结果（MovementController 只负责应用）
/// </summary>
public struct CollisionResult
{
    public Vector2 newDirection;
    public float remainingDistanceMultiplier;
    public bool stopImmediately;

    // 预留：翻面
    public bool triggerFlip;

    // 己方硬币互撞：启动另一枚硬币
    public bool triggerOtherCoinMove;
    public ChessPiece otherCoin;
    public Vector2 otherCoinDirection;

    // 被撞硬币获得的“实际起始剩余路程”
    public float otherCoinStartDistance;

    // 被撞硬币启动时的速度倍率
    public float otherCoinSpeedScale;

    // 是否命中敌人
    public bool triggerHitTarget;

    // 命中信息
    public Vector2 hitDirection;
    public float impactStrength;
    public Collider2D collider;
}