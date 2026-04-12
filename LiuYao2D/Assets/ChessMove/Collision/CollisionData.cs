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

    public Collider2D selfCollider;
    public Collider2D hitCollider;

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
    // ===== 通用移动结果 =====
    public Vector2 newDirection;
    public float remainingDistanceMultiplier;
    public bool stopImmediately;

    // ===== 翻面结果 =====
    public bool triggerFlip;

    [Tooltip("翻面后的小位移方向")]
    public Vector2 flipMoveDirection;

    [Tooltip("翻面后的小位移固定距离")]
    public float flipMoveDistance;

    [Tooltip("翻面后是否终止主运动并切入 FlipReturn 阶段")]
    public bool flipShouldStopMainMove;

    [Tooltip("本次翻面命中的目标")]
    public CollisionTarget flipTarget;

    // ===== 己方硬币互撞：启动另一枚硬币 =====
    public bool triggerOtherCoinMove;
    public ChessPiece otherCoin;
    public Vector2 otherCoinDirection;

    [Tooltip("被撞硬币获得的实际起始剩余路程")]
    public float otherCoinStartDistance;

    [Tooltip("被撞硬币启动时的速度倍率")]
    public float otherCoinSpeedScale;

    // ===== 敌人受击 =====
    public bool triggerHitTarget;

    [Tooltip("命中方向（给敌人使用）")]
    public Vector2 hitDirection;

    [Tooltip("冲击强度")]
    public float impactStrength;

    [Tooltip("命中的具体碰撞体")]
    public Collider2D collider;

    // ===== 位置分离修正预留 =====
    [Tooltip("是否请求在强制小位移后做位置分离修正")]
    public bool requestSeparationResolve;

    [Tooltip("本次位置修正的主要目标")]
    public Transform separationTarget;

    [Tooltip("主要目标的碰撞类型")]
    public CollisionType separationTargetType;

    // ===== 表现层预留 =====
    [Tooltip("是否触发表现层的翻面反馈")]
    public bool triggerFlipFeedback;

    [Tooltip("反馈位置（一般取碰撞点）")]
    public Vector2 feedbackPoint;
}