using UnityEngine;

/// <summary>
/// 标记碰撞对象类型（Enemy / PlayerCoin / Obstacle）
/// 挂在所有“可被碰撞的物体”上
/// </summary>
public enum CollisionType
{
    Obstacle,
    Enemy,
    PlayerCoin
}

public class CollisionTarget : MonoBehaviour
{
    public CollisionType type;
}