/// <summary>
/// 实现功能：封装棋子的主动发射、被碰撞启动逻辑，并管理己方互撞冷却状态。
/// 挂在每个棋子（硬币）上
/// </summary>
using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("该棋子的移动参数")]
    [SerializeField] private MovementConfig movementConfig;

    private MovementController movement;

    // 互撞冷却时间戳
    private float lastCoinCollisionTime = -999f;

    private void Awake()
    {
        movement = GetComponent<MovementController>();
    }

    /// <summary>
    /// 发射棋子（玩家主动发射）
    /// </summary>
    public void Fire(Vector2 direction, float power = 1f)
    {
        if (movementConfig == null)
        {
            Debug.LogError($"[ChessPiece] {name} 的 MovementConfig 未设置！");
            return;
        }

        ShotContext context = new ShotContext
        {
            isPlayerShot = true,
            isFullCharge = power >= 1f,
            power = power,
            sourceType = ShotSourceType.PlayerInput
        };

        movement.Init(direction, movementConfig, context);
    }

    /// <summary>
    /// 被己方硬币碰撞后启动移动
    /// </summary>
    public void ActivateByCollision(Vector2 direction, float startDistance, float speedScale = 1f)
    {
        if (movementConfig == null)
        {
            Debug.LogError($"[ChessPiece] {name} 的 MovementConfig 未设置，无法执行碰撞启动！");
            return;
        }

        float normalizedPower = movementConfig.totalDistance > 0.0001f
            ? startDistance / movementConfig.totalDistance
            : 0f;

        ShotContext context = new ShotContext
        {
            isPlayerShot = false,
            isFullCharge = false,
            power = normalizedPower,
            sourceType = ShotSourceType.CoinCollision
        };

        movement.InitByCollision(direction, movementConfig, context, startDistance, speedScale);

        Debug.Log($"[ChessPiece] {name} 被碰撞启动 | direction:{direction} | startDistance:{startDistance:F2} | speedScale:{speedScale:F2}");
    }

    /// <summary>
    /// 当前是否允许参与己方硬币互撞
    /// </summary>
    public bool CanTriggerCoinCollision(float cooldown)
    {
        return Time.time >= lastCoinCollisionTime + cooldown;
    }

    /// <summary>
    /// 标记己方硬币互撞已触发，进入短暂无敌
    /// </summary>
    public void MarkCoinCollisionTriggered()
    {
        lastCoinCollisionTime = Time.time;
    }

    public bool IsMoving => movement != null && movement.IsMoving;
}