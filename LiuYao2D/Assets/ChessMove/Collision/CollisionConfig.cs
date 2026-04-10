/// <summary>
/// 实现功能：配置不同碰撞对象的路径保留比例，并提供己方硬币互撞所需的传递参数。
/// </summary>
using UnityEngine;

/// <summary>
/// 碰撞参数配置（控制不同对象的动能损耗）
/// </summary>
[CreateAssetMenu(menuName = "Config/CollisionConfig")]
public class CollisionConfig : ScriptableObject
{
    [Header("不同碰撞对象的路径保留比例")]

    [Tooltip("撞障碍物后的路径保留比例")]
    public float obstacleBounceMultiplier = 0.8f;

    [Tooltip("撞敌人后的路径保留比例")]
    public float enemyBounceMultiplier = 0.6f;

    [Tooltip("撞己方硬币后的路径保留比例（主动撞击者自身）")]
    public float coinBounceMultiplier = 0.5f;

    [Header("己方硬币互撞")]

    [Tooltip("被撞硬币获得的剩余路程传递比例")]
    [Range(0f, 1f)]
    public float coinTransferDistanceRatio = 0.4f;

    [Tooltip("被撞硬币启动时的速度继承比例")]
    [Range(0f, 2f)]
    public float coinTransferSpeedRatio = 1f;

    [Tooltip("己方硬币互撞后的短暂无敌时间，防止重复触发")]
    [Min(0f)]
    public float coinCollisionCooldown = 0.1f;
}