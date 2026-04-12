/// <summary>
/// 实现功能：封装棋子的主动发射、被碰撞启动、正反面状态切换，并预留翻面表现与命中反馈接口。
/// 挂在每个棋子（硬币）上
/// </summary>
using System;
using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("该棋子的移动参数")]
    [SerializeField] private MovementConfig movementConfig;
    [Header("回合控制")]
    [Tooltip("当前是否允许该棋子在玩家回合中被操作")]
    [SerializeField] private bool canBeControlledThisTurn = true;

    [Header("正反面")]
    [Tooltip("是否默认以正面开始")]
    [SerializeField] private bool startFrontSide = true;

    private MovementController movement;
    private ChessVisualController visualController;

    private float lastCoinCollisionTime = -999f;
    private bool isFrontSide = true;

    public event Action<ChessPiece, CollisionTarget, Vector2, Vector2> FlipTriggered;
    //慢动作 + 打击感系统:
    public event Action<ChessPiece, CollisionType, bool, float, Vector2> ImpactFeedbackRequested;

    public bool CanBeControlledThisTurn => canBeControlledThisTurn;

    public void SetCanBeControlledThisTurn(bool canControl)
    {
        canBeControlledThisTurn = canControl;
    }

    private void Awake()
    {
        movement = GetComponent<MovementController>();
        visualController = GetComponent<ChessVisualController>();

        isFrontSide = startFrontSide;

        if (visualController != null)
        {
            visualController.SetFaceImmediate(isFrontSide);
        }
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

    //翻面
    public void HandleFlipTriggered(CollisionTarget target, Vector2 hitPoint, Vector2 returnDirection)
    {
        ToggleFace(true);

        FlipTriggered?.Invoke(this, target, hitPoint, returnDirection);

        CollisionType targetType = target != null ? target.type : CollisionType.Obstacle;
        RequestImpactFeedback(targetType, true, 1f, hitPoint);

        Debug.Log(
            $"[ChessPiece] 翻面触发 | 物体:{name} | 当前面:{(isFrontSide ? "正面" : "反面")} | " +
            $"目标:{target?.name} | returnDir:{returnDirection}"
        );
    }

    //慢动作 + 打击感系统的标准入口
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
            visualController.PlayFlipToFace(isFrontSide);
        }
        else
        {
            visualController.SetFaceImmediate(isFrontSide);
        }
    }

    public void ToggleFace(bool playAnimation)
    {
        SetFace(!isFrontSide, playAnimation);
    }

    public bool CanTriggerCoinCollision(float cooldown)
    {
        return Time.time >= lastCoinCollisionTime + cooldown;
    }

    public void MarkCoinCollisionTriggered()
    {
        lastCoinCollisionTime = Time.time;
    }

    public bool IsMoving => movement != null && movement.IsMoving;
    public bool IsFrontSide => isFrontSide;
}