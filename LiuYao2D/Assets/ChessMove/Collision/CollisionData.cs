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

    public Collider2D selfCollider;
    public Collider2D hitCollider;

    public Vector2 hitPoint;
    public Vector2 normal;
    public Vector2 incomingDir;

    public ShotContext shotContext;
}

public struct CollisionResult
{
    // 通用移动结果
    public Vector2 newDirection;
    public float remainingDistanceMultiplier;
    public bool stopImmediately;

    // 己方硬币互撞：启动另一枚硬币
    public bool triggerOtherCoinMove;
    public ChessPiece otherCoin;
    public Vector2 otherCoinDirection;
    public float otherCoinStartDistance;
    public float otherCoinSpeedScale;

    // 敌人受击
    public bool triggerHitTarget;
    public Vector2 hitDirection;
    public float impactStrength;
    public Collider2D collider;
}