/// <summary>
/// 负责：能不能动、怎么发射、怎么被撞动、回合控制
/// 实现功能：封装棋子的主动发射、被碰撞启动、回合控制与碰撞反馈请求；硬币定义、正反面和卦象状态交由 CoinRuntimeData 管理。
/// </summary>
using System;
using UnityEngine;

[RequireComponent(typeof(CoinStats))]
public class ChessPiece : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("该棋子的移动参数")]
    [SerializeField] private MovementConfig movementConfig;

    [Header("回合控制")]
    [Tooltip("当前是否允许该棋子在玩家回合中被操作")]
    [SerializeField] private bool canBeControlledThisTurn = true;

    private MovementController movement;
    private CoinRuntimeData coinData;
    private CoinStats coinStats;
    private CoinVisualController visualController;

    private float lastCoinCollisionTime = -999f;

    public event Action<ChessPiece, CollisionType, bool, float, Vector3> ImpactFeedbackRequested;
    public event Action<ChessPiece, CollisionType, float, Vector3> PreImpactFeedbackRequested;

    private void Awake()
    {
        movement = GetComponent<MovementController>();
        coinData = GetComponent<CoinRuntimeData>();
        coinStats = GetComponent<CoinStats>();
        visualController = GetComponentInChildren<CoinVisualController>();

        if (movement == null)
        {
            Debug.LogError($"[ChessPiece] {name} 缺少 MovementController。");
        }

        if (coinData == null)
        {
            Debug.LogError($"[ChessPiece] {name} 缺少 CoinRuntimeData。请把硬币定义、正反面、属性配置迁移到 CoinRuntimeData。");
        }

        if (coinStats == null)
        {
            Debug.LogError($"[ChessPiece] {name} 缺少 CoinStats，无法提供硬币攻击力和生命值。");
        }
    }

    public void Fire(Vector3 direction, float power = 1f)
    {
        if (movementConfig == null)
        {
            Debug.LogError($"[ChessPiece] {name} 的 MovementConfig 未设置！");
            return;
        }

        if (movement == null)
        {
            Debug.LogError($"[ChessPiece] {name} 缺少 MovementController，无法发射。");
            return;
        }

        float fullChargeThreshold = FullChargeThreshold;

        ShotContext context = new ShotContext
        {
            isPlayerShot = true,
            isFullCharge = power >= fullChargeThreshold,
            power = power,
            sourceType = ShotSourceType.PlayerInput
        };

        movement.Init(direction, movementConfig, context);
    }

    public void ActivateByCollision(Vector3 direction, float startDistance, float speedScale = 1f)
    {
        if (movementConfig == null)
        {
            Debug.LogError($"[ChessPiece] {name} 的 MovementConfig 未设置，无法执行碰撞启动！");
            return;
        }

        if (movement == null)
        {
            Debug.LogError($"[ChessPiece] {name} 缺少 MovementController，无法被碰撞启动。");
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

        Debug.Log(
            $"[ChessPiece] 被碰撞启动 | 物体:{name} | " +
            $"direction:{direction} | startDistance:{startDistance:F2} | speedScale:{speedScale:F2}"
        );
    }

    public void PlayChargeFlip(Action onComplete)
    {
        if (coinData == null)
        {
            Debug.LogError($"[ChessPiece] {name} 缺少 CoinRuntimeData，无法执行蓄力翻面。");
            onComplete?.Invoke();
            return;
        }

        coinData.PlayChargeFlip(onComplete);
    }

    public void RestoreFaceImmediate(bool targetFrontSide)
    {
        if (coinData == null)
        {
            Debug.LogError($"[ChessPiece] {name} 缺少 CoinRuntimeData，无法恢复正反面。");
            return;
        }

        coinData.RestoreFaceImmediate(targetFrontSide);
    }

    public void SetFace(bool frontSide, bool playAnimation)
    {
        if (coinData == null)
        {
            Debug.LogError($"[ChessPiece] {name} 缺少 CoinRuntimeData，无法设置正反面。");
            return;
        }

        coinData.SetFace(frontSide, playAnimation);
    }

    public void SetCoinDefinition(CoinDefinition definition, bool refreshVisual = true)
    {
        if (coinData == null)
        {
            Debug.LogError($"[ChessPiece] {name} 缺少 CoinRuntimeData，无法设置 CoinDefinition。");
            return;
        }

        coinData.SetCoinDefinition(definition, refreshVisual);
    }

    public void RequestImpactFeedback(CollisionType targetType, bool isFlip, float strength, Vector3 hitPoint)
    {
        ImpactFeedbackRequested?.Invoke(this, targetType, isFlip, strength, hitPoint);

        Debug.Log(
            $"[ChessPiece] 反馈请求 | 物体:{name} | targetType:{targetType} | " +
            $"isFlip:{isFlip} | strength:{strength:F2} | point:{hitPoint}"
        );
    }

    public void RequestPreImpactFeedback(CollisionType targetType, float predictedTime, Vector3 hitPoint)
    {
        PreImpactFeedbackRequested?.Invoke(this, targetType, predictedTime, hitPoint);

        Debug.Log(
            $"[ChessPiece] 预碰撞反馈请求 | 物体:{name} | targetType:{targetType} | " +
            $"predictedTime:{predictedTime:F3} | point:{hitPoint}"
        );
    }

    public void SetTurnHighlight(bool highlighted)
    {
        if (visualController != null)
        {
            visualController.SetTurnHighlight(highlighted);
        }
    }

    public bool CanTriggerCoinCollision(float cooldown)
    {
        return Time.time >= lastCoinCollisionTime + cooldown;
    }

    public void MarkCoinCollisionTriggered()
    {
        lastCoinCollisionTime = Time.time;
    }

    public void SetCanBeControlledThisTurn(bool canControl)
    {
        canBeControlledThisTurn = canControl;
    }

    public bool IsMoving => movement != null && movement.IsMoving;
    public bool CanBeControlledThisTurn => canBeControlledThisTurn;

    public float FullChargeThreshold => movement != null && movement.CollisionConfig != null
        ? movement.CollisionConfig.fullChargeThreshold
        : 0.999f;

    public CoinDefinition CoinDefinition => coinData != null ? coinData.CoinDefinition : null;

    public bool IsFrontSide => coinData != null && coinData.IsFrontSide;

    public TrigramType CurrentTrigram
    {
        get
        {
            if (coinData == null)
            {
                Debug.LogWarning($"[ChessPiece] {name} 缺少 CoinRuntimeData，返回默认卦象 Qian。");
                return TrigramType.Qian;
            }

            return coinData.CurrentTrigram;
        }
    }

    public TrigramType OppositeTrigram
    {
        get
        {
            if (coinData == null)
            {
                Debug.LogWarning($"[ChessPiece] {name} 缺少 CoinRuntimeData，返回默认卦象 Qian。");
                return TrigramType.Qian;
            }

            return coinData.OppositeTrigram;
        }
    }
}
