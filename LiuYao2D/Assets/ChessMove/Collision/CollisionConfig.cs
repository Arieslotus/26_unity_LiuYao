/// <summary>
/// 实现功能：配置不同碰撞对象的路径保留比例、己方硬币互撞参数，并补充翻面机制与位置分离修正所需配置。
/// </summary>
using UnityEngine;

/// <summary>
/// 碰撞参数配置（控制不同对象的动能损耗、翻面规则与位置修正预留参数）
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

    [Header("翻面机制")]

    [Tooltip("是否启用翻面机制")]
    public bool enableFlip = true;

    [Tooltip("达到该力度阈值后视为满蓄力")]
    [Range(0f, 1f)]
    public float fullChargeThreshold = 0.999f;

    [Tooltip("翻面后是否限制为同一次发射只触发一次")]
    public bool flipOnlyOncePerShot = true;

    [Tooltip("撞敌人时是否允许触发翻面")]
    public bool flipOnEnemy = true;

    [Tooltip("撞己方硬币时是否允许触发翻面")]
    public bool flipOnPlayerCoin = true;

    [Tooltip("撞障碍物时是否允许触发翻面（当前设计应为 false）")]
    public bool flipOnObstacle = false;

    [Tooltip("翻面后的固定返回距离")]
    [Min(0f)]
    public float flipReturnDistance = 0.6f;

    [Tooltip("翻面返回阶段的速度倍率（相对 baseSpeed）")]
    [Range(0.05f, 2f)]
    public float flipReturnSpeedScale = 0.35f;

    [Header("位置分离修正（预留）")]

    [Tooltip("翻面后是否允许请求位置分离修正")]
    public bool enableSeparationAfterFlip = true;

    [Tooltip("位置分离修正后额外保留的安全间隙")]
    [Min(0f)]
    public float separationSkin = 0.02f;

    [Tooltip("位置分离修正最多允许的迭代次数")]
    [Range(1, 10)]
    public int maxSeparationIterations = 3;
}