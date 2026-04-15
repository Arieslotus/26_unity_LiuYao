using UnityEngine;

/// <summary>
/// 拖拽蓄力输入系统（V3）
/// 1. 只能在棋子周围指定半径内按下才会激活
/// 2. 拖拽方向为发射方向的反向
/// 3. 阶段1：拖拽距离蓄力
/// 4. 阶段2：达到阶段1上限后，按住时间继续蓄力
/// 5. 满蓄力后立刻原地翻面，松手后按普通规则发射
/// 6. 若在翻面动画中松手，则等待动画结束后再发射
/// 7. 右键取消时恢复到本次蓄力开始前的初始态
/// 8. 实时更新预测轨迹
/// </summary>
public class DragChargeInput : MonoBehaviour
{
    private enum ChargeStage
    {
        None,
        Distance,
        Time
    }

    [Header("引用")]
    [SerializeField] private ChessPiece piece;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TrajectoryRenderer trajectoryRenderer;
    [SerializeField] private ChargeInputConfig chargeConfig;
    [SerializeField] private ChessTurnController turnController;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool debugDraw = true;
    [SerializeField] private float debugArrowLength = 2.0f;

    private bool isCharging = false;
    private bool isWaitingDelayedFire = false;
    private ChargeStage currentStage = ChargeStage.None;

    private Vector2 pieceCenter;
    private Vector2 currentMouseWorld;
    private Vector2 currentDirection = Vector2.zero;

    private float currentPower = 0f;
    private float holdTimer = 0f;
    private float currentScaledDragDistance = 0f;

    private bool hasTriggeredChargeFlip = false;
    private bool isChargeFlipAnimating = false;
    private bool pendingFireAfterFlip = false;
    private bool chargeStartFaceState = true;

    private Vector2 queuedFireDirection = Vector2.zero;
    private float queuedFirePower = 0f;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (piece == null)
        {
            Debug.LogError("[DragChargeInput] 未绑定 ChessPiece。");
        }

        if (mainCamera == null)
        {
            Debug.LogError("[DragChargeInput] 未找到 Camera，请手动绑定 mainCamera。");
        }

        if (chargeConfig == null)
        {
            Debug.LogError("[DragChargeInput] 未绑定 ChargeInputConfig。");
        }
    }

    private void Update()
    {
        if (piece == null || mainCamera == null || chargeConfig == null)
            return;

        if (isWaitingDelayedFire)
            return;

        if (piece.IsMoving)
        {
            if (isCharging)
            {
                CancelChargeInternal("[DragChargeInput] 棋子已进入移动状态，自动取消蓄力");
            }
            else
            {
                ClearTrajectory();
            }

            return;
        }

        HandleMouseInput();

        if (isCharging && debugDraw)
        {
            DrawDebugInfo();
        }
    }

    private void OnDisable()
    {
        ResetAllChargeState();
        ClearTrajectory();
    }

    private void HandleMouseInput()
    {
        currentMouseWorld = GetMouseWorldPosition();

        if (isCharging && Input.GetMouseButtonDown(1))
        {
            CancelChargeInternal("[DragChargeInput] 右键取消蓄力");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryStartCharge(currentMouseWorld);
        }

        if (!isCharging)
            return;

        UpdateCharge(currentMouseWorld);
        UpdateTrajectoryPreview();

        if (Input.GetMouseButtonUp(0))
        {
            ReleaseCharge();
        }
    }

    private void TryStartCharge(Vector2 mouseWorldPos)
    {
        if (piece == null || piece.IsMoving)
        {
            if (debugLog)
            {
                Debug.Log("[DragChargeInput] 棋子移动中，无法开始蓄力");
            }
            return;
        }

        pieceCenter = piece.transform.position;

        float distanceToCenter = Vector2.Distance(mouseWorldPos, pieceCenter);
        if (distanceToCenter > chargeConfig.inputRadius)
        {
            if (debugLog)
            {
                Debug.Log($"[DragChargeInput] 按下无效：超出输入半径 | 距离:{distanceToCenter:F2} | 半径:{chargeConfig.inputRadius:F2}");
            }
            return;
        }

        isCharging = true;
        currentStage = ChargeStage.Distance;
        currentPower = 0f;
        holdTimer = 0f;
        currentDirection = Vector2.zero;
        currentScaledDragDistance = 0f;

        hasTriggeredChargeFlip = false;
        isChargeFlipAnimating = false;
        pendingFireAfterFlip = false;
        queuedFireDirection = Vector2.zero;
        queuedFirePower = 0f;

        chargeStartFaceState = piece.IsFrontSide;

        if (debugLog)
        {
            Debug.Log($"[DragChargeInput] 开始蓄力 | 初始面:{(chargeStartFaceState ? "正面" : "反面")}");
        }
    }

    private void UpdateCharge(Vector2 mouseWorldPos)
    {
        pieceCenter = piece.transform.position;

        Vector2 dragVector = pieceCenter - mouseWorldPos;
        float rawDragDistance = dragVector.magnitude;

        float scaledDragDistance = rawDragDistance * chargeConfig.dragDistanceScale;
        currentScaledDragDistance = scaledDragDistance;

        if (dragVector.sqrMagnitude > 0.0001f)
        {
            currentDirection = dragVector.normalized;
        }

        float normalizedDistance = chargeConfig.stage1MaxDistance <= 0.0001f
            ? 1f
            : Mathf.Clamp01(scaledDragDistance / chargeConfig.stage1MaxDistance);

        float distancePower = normalizedDistance * chargeConfig.stage1MaxPower;

        if (distancePower < chargeConfig.stage1MaxPower - 0.0001f)
        {
            currentStage = ChargeStage.Distance;
            holdTimer = 0f;
            currentPower = distancePower;
        }
        else
        {
            if (currentStage != ChargeStage.Time)
            {
                currentStage = ChargeStage.Time;
                holdTimer = 0f;

                if (debugLog)
                {
                    Debug.Log("[DragChargeInput] 进入阶段2：按住时间蓄力");
                }
            }

            holdTimer += Time.deltaTime;

            float timePercent = chargeConfig.maxHoldTime <= 0.0001f
                ? 1f
                : Mathf.Clamp01(holdTimer / chargeConfig.maxHoldTime);

            float extraPower = timePercent * (1f - chargeConfig.stage1MaxPower);
            currentPower = chargeConfig.stage1MaxPower + extraPower;
        }

        currentPower = Mathf.Clamp01(currentPower);

        TryTriggerChargeFlip();

        if (debugLog)
        {
            Debug.Log(
                $"[DragChargeInput] 阶段:{currentStage} | " +
                $"实际拖拽距离:{rawDragDistance:F2} | " +
                $"有效拖拽距离:{scaledDragDistance:F2} | " +
                $"方向:{currentDirection} | " +
                $"Power:{currentPower:F2} | " +
                $"计时:{holdTimer:F2} | " +
                $"已翻面:{hasTriggeredChargeFlip} | 动画中:{isChargeFlipAnimating}"
            );
        }
    }

    private void TryTriggerChargeFlip()
    {
        if (piece == null)
            return;

        if (hasTriggeredChargeFlip)
            return;

        if (currentPower < piece.FullChargeThreshold)
            return;

        hasTriggeredChargeFlip = true;
        isChargeFlipAnimating = true;

        piece.PlayChargeFlip(OnChargeFlipAnimationComplete);

        if (debugLog)
        {
            Debug.Log("[DragChargeInput] 达到满蓄力，立即触发原地翻面");
        }
    }

    private void OnChargeFlipAnimationComplete()
    {
        isChargeFlipAnimating = false;

        if (debugLog)
        {
            Debug.Log($"[DragChargeInput] 翻面动画播放完成 | pendingFireAfterFlip:{pendingFireAfterFlip}");
        }

        if (pendingFireAfterFlip)
        {
            ExecuteFire(queuedFireDirection, queuedFirePower);
            pendingFireAfterFlip = false;
            queuedFireDirection = Vector2.zero;
            queuedFirePower = 0f;
        }
    }

    private void UpdateTrajectoryPreview()
    {
        if (trajectoryRenderer == null)
            return;

        if (currentDirection.sqrMagnitude <= 0.0001f || currentPower <= 0.0001f)
        {
            trajectoryRenderer.Clear();
            return;
        }

        trajectoryRenderer.UpdateTrajectory(
            piece.transform.position,
            currentDirection,
            currentPower,
            piece.GetComponent<Collider2D>()
        );
    }

    private void ReleaseCharge()
    {
        if (currentDirection.sqrMagnitude <= 0.0001f)
        {
            if (debugLog)
            {
                Debug.LogWarning("[DragChargeInput] 松手失败：当前方向无效");
            }

            CancelChargeInternal("[DragChargeInput] 方向无效，取消蓄力");
            return;
        }

        if (currentPower < chargeConfig.minFirePower)
        {
            if (debugLog)
            {
                Debug.LogWarning($"[DragChargeInput] 松手未发射：力度过小 | 当前:{currentPower:F2} | 阈值:{chargeConfig.minFirePower:F2}");
            }

            CancelChargeInternal("[DragChargeInput] 力度过小，取消蓄力");
            return;
        }

        if (hasTriggeredChargeFlip && isChargeFlipAnimating)
        {
            pendingFireAfterFlip = true;
            isWaitingDelayedFire = true;
            queuedFireDirection = currentDirection;
            queuedFirePower = currentPower;

            if (debugLog)
            {
                Debug.Log($"[DragChargeInput] 翻面动画中松手，等待动画完成后发射 | 方向:{queuedFireDirection} | Power:{queuedFirePower:F2}");
            }

            ResetChargeStateKeepFace();
            ClearTrajectory();
            return;
        }

        ExecuteFire(currentDirection, currentPower);
    }

    private void ExecuteFire(Vector2 fireDirection, float firePower)
    {
        if (piece == null)
            return;

        if (debugLog)
        {
            Debug.Log($"[DragChargeInput] 发射 | 方向:{fireDirection} | Power:{firePower:F2} | 已翻面:{hasTriggeredChargeFlip}");
        }

        piece.Fire(fireDirection, firePower);

        if (turnController != null)
        {
            turnController.NotifyPieceFired();
        }

        ResetAllChargeState();
        ClearTrajectory();
        isWaitingDelayedFire = false;
    }

    private void CancelChargeInternal(string logMessage)
    {
        if (debugLog)
        {
            Debug.Log(logMessage);
        }

        if (piece != null)
        {
            piece.RestoreFaceImmediate(chargeStartFaceState);
        }

        ResetAllChargeState();
        ClearTrajectory();
    }

    private void ResetChargeStateKeepFace()
    {
        isCharging = false;
        currentStage = ChargeStage.None;
        currentPower = 0f;
        holdTimer = 0f;
        currentDirection = Vector2.zero;
        currentScaledDragDistance = 0f;
    }

    private void ResetAllChargeState()
    {
        ResetChargeStateKeepFace();

        hasTriggeredChargeFlip = false;
        isChargeFlipAnimating = false;
        pendingFireAfterFlip = false;
        queuedFireDirection = Vector2.zero;
        queuedFirePower = 0f;
        isWaitingDelayedFire = false;
        chargeStartFaceState = piece != null ? piece.IsFrontSide : true;
    }

    private void ClearTrajectory()
    {
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.Clear();
        }
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        return new Vector2(mouseWorldPos.x, mouseWorldPos.y);
    }

    public void SetControlledPiece(ChessPiece newPiece)
    {
        piece = newPiece;

        if (debugLog)
        {
            Debug.Log(piece != null
                ? $"[DragChargeInput] 切换控制棋子: {piece.name}"
                : "[DragChargeInput] 当前不控制任何棋子");
        }

        ResetAllChargeState();
        ClearTrajectory();
    }

    private void DrawDebugInfo()
    {
        if (piece == null)
            return;

        Vector2 center = piece.transform.position;

        DrawCircle(center, chargeConfig.inputRadius, Color.yellow);

        if (currentDirection.sqrMagnitude > 0.0001f)
        {
            Debug.DrawLine(center, center + currentDirection * debugArrowLength, Color.cyan);
        }

        Debug.DrawLine(center, currentMouseWorld, Color.magenta);
    }

    private void DrawCircle(Vector2 center, float radius, Color color, int segments = 32)
    {
        float angleStep = 360f / segments;
        Vector2 prevPoint = center + new Vector2(radius, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector2 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Debug.DrawLine(prevPoint, nextPoint, color);
            prevPoint = nextPoint;
        }
    }

    public bool IsCharging => isCharging;
    public Vector2 CurrentDirection => currentDirection;
    public float CurrentPower => currentPower;
    public float CurrentScaledDragDistance => currentScaledDragDistance;
    public ChargeInputConfig ChargeConfig => chargeConfig;
    public ChessPiece CurrentPiece => piece;
}