/// <summary>
/// 实现功能：封装棋子的主动发射、被碰撞启动、正反面状态切换，并持有硬币固定正反面定义，提供当前卦象与蓄力翻面接口。
/// </summary>
using System;
using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("该棋子的移动参数")]
    [SerializeField] private MovementConfig movementConfig;

    [Header("硬币定义")]
    [Tooltip("这枚硬币固定的正反面卦象与显示资源")]
    [SerializeField] private CoinDefinition coinDefinition;

    [Header("正反面")]
    [Tooltip("是否默认以正面开始")]
    [SerializeField] private bool startFrontSide = true;

    [Header("回合控制")]
    [Tooltip("当前是否允许该棋子在玩家回合中被操作")]
    [SerializeField] private bool canBeControlledThisTurn = true;

    private MovementController movement;
    private CoinVisualController visualController;

    private float lastCoinCollisionTime = -999f;
    private bool isFrontSide = true;

    public event Action<ChessPiece, CollisionType, bool, float, Vector2> ImpactFeedbackRequested;

    private void Awake()
    {
        if (coinDefinition == null)
        {
            Debug.LogWarning($"[ChessPiece] {name} 未配置 CoinDefinition。");
        }
        movement = GetComponent<MovementController>();
        visualController = GetComponent<CoinVisualController>();

        isFrontSide = startFrontSide;

        RefreshVisualImmediate();
    }

    public void Fire(Vector2 direction, float power = 1f)
    {
        if (movementConfig == null)
        {
            Debug.LogError($"[ChessPiece] {name} 的 MovementConfig 未设置！");
            return;
        }

        float fullChargeThreshold = 0.999f;

        if (movement != null && movement.CollisionConfig != null)
        {
            fullChargeThreshold = movement.CollisionConfig.fullChargeThreshold;
        }

        ShotContext context = new ShotContext
        {
            isPlayerShot = true,
            isFullCharge = power >= fullChargeThreshold,
            power = power,
            sourceType = ShotSourceType.PlayerInput
        };

        movement.Init(direction, movementConfig, context);
    }

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

    public void PlayChargeFlip(Action onComplete)
    {
        bool targetFace = !isFrontSide;
        isFrontSide = targetFace;

        if (visualController != null)
        {
            visualController.PlayFlipToFace(isFrontSide, coinDefinition, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }

        Debug.Log(
            $"[ChessPiece] 蓄力翻面 | 物体:{name} | 当前面:{(isFrontSide ? "正面" : "反面")} | 当前卦象:{CurrentTrigram}"
        );
    }

    public void RestoreFaceImmediate(bool targetFrontSide)
    {
        isFrontSide = targetFrontSide;

        if (visualController != null)
        {
            visualController.CancelFlipAndSetFace(isFrontSide, coinDefinition);
        }

        Debug.Log(
            $"[ChessPiece] 恢复初始面 | 物体:{name} | 当前面:{(isFrontSide ? "正面" : "反面")} | 当前卦象:{CurrentTrigram}"
        );
    }

    public void RequestImpactFeedback(CollisionType targetType, bool isFlip, float strength, Vector2 hitPoint)
    {
        ImpactFeedbackRequested?.Invoke(this, targetType, isFlip, strength, hitPoint);

        Debug.Log(
            $"[ChessPiece] 反馈请求 | 物体:{name} | targetType:{targetType} | " +
            $"isFlip:{isFlip} | strength:{strength:F2} | point:{hitPoint}"
        );
    }

    public void SetTurnHighlight(bool highlighted)
    {
        if (visualController != null)
        {
            visualController.SetTurnHighlight(highlighted);
        }
    }

    public void SetFace(bool frontSide, bool playAnimation)
    {
        isFrontSide = frontSide;

        if (visualController == null)
            return;

        if (playAnimation)
        {
            visualController.PlayFlipToFace(isFrontSide, coinDefinition);
        }
        else
        {
            visualController.SetFaceImmediate(isFrontSide, coinDefinition);
        }
    }

    public void SetCoinDefinition(CoinDefinition definition, bool refreshVisual = true)
    {
        coinDefinition = definition;

        if (refreshVisual)
        {
            RefreshVisualImmediate();
        }

        Debug.Log(
            definition != null
                ? $"[ChessPiece] 设置硬币定义 | 物体:{name} | coin:{definition.coinName} | 当前卦象:{CurrentTrigram}"
                : $"[ChessPiece] 清空硬币定义 | 物体:{name}"
        );
    }

    public bool CanTriggerCoinCollision(float cooldown)
    {
        return Time.time >= lastCoinCollisionTime + cooldown;
    }

    public void MarkCoinCollisionTriggered()
    {
        lastCoinCollisionTime = Time.time;
    }

    private void RefreshVisualImmediate()
    {
        if (visualController != null)
        {
            visualController.SetFaceImmediate(isFrontSide, coinDefinition);
        }
    }

    public bool IsMoving => movement != null && movement.IsMoving;
    public bool IsFrontSide => isFrontSide;
    public bool CanBeControlledThisTurn => canBeControlledThisTurn;
    public float FullChargeThreshold => movement != null && movement.CollisionConfig != null
        ? movement.CollisionConfig.fullChargeThreshold
        : 0.999f;

    public CoinDefinition CoinDefinition => coinDefinition;

    public TrigramType CurrentTrigram
    {
        get
        {
            if (coinDefinition == null)
            {
                Debug.LogWarning($"[ChessPiece] {name} 未配置 CoinDefinition，返回默认卦象 Qian。");
                return TrigramType.Qian;
            }

            return isFrontSide ? coinDefinition.frontTrigram : coinDefinition.backTrigram;
        }
    }

    public TrigramType OppositeTrigram
    {
        get
        {
            if (coinDefinition == null)
            {
                Debug.LogWarning($"[ChessPiece] {name} 未配置 CoinDefinition，返回默认卦象 Qian。");
                return TrigramType.Qian;
            }

            return isFrontSide ? coinDefinition.backTrigram : coinDefinition.frontTrigram;
        }
    }

    public void SetCanBeControlledThisTurn(bool canControl)
    {
        canBeControlledThisTurn = canControl;
    }
}