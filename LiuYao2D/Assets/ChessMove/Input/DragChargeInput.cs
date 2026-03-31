using UnityEngine;

/// <summary>
/// 拖拽蓄力输入系统（V2）
/// 1. 只能在棋子周围指定半径内按下才会激活
/// 2. 拖拽方向为发射方向的反向
/// 3. 阶段1：拖拽距离蓄力
/// 4. 阶段2：达到阶段1上限后，按住时间继续蓄力
/// 5. 实时更新预测轨迹
/// 6. 支持最小发射阈值
/// 7. 支持右键取消蓄力
/// 8. 棋子移动时锁定输入
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
    private ChargeStage currentStage = ChargeStage.None;

    private Vector2 pieceCenter;
    private Vector2 currentMouseWorld;
    private Vector2 currentDirection = Vector2.zero;

    private float currentPower = 0f;
    private float holdTimer = 0f;

    private float currentScaledDragDistance = 0f;

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

        // 棋子移动中：锁定输入，并确保轨迹清空
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
        ResetChargeState();
        ClearTrajectory();
    }

    /// <summary>
    /// 鼠标输入流程
    /// </summary>
    private void HandleMouseInput()
    {
        currentMouseWorld = GetMouseWorldPosition();

        // 右键取消蓄力
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

    /// <summary>
    /// 尝试开始蓄力
    /// </summary>
    private void TryStartCharge(Vector2 mouseWorldPos)
    {
        if (piece.IsMoving)
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

        if (debugLog)
        {
            Debug.Log("[DragChargeInput] 开始蓄力");
        }
    }

    /// <summary>
    /// 更新蓄力状态
    /// </summary>
    private void UpdateCharge(Vector2 mouseWorldPos)
    {
        pieceCenter = piece.transform.position;

        // 拖拽方向 = 发射方向
        // 因为玩家拖拽是朝“发射反方向”拉，所以发射方向 = 棋子中心 - 鼠标位置
        Vector2 dragVector = pieceCenter - mouseWorldPos;
        float rawDragDistance = dragVector.magnitude;

        // 实际拖拽距离先缩放，得到“有效拖拽距离”
        float scaledDragDistance = rawDragDistance * chargeConfig.dragDistanceScale;
        currentScaledDragDistance = scaledDragDistance;

        if (dragVector.sqrMagnitude > 0.0001f)
        {
            currentDirection = dragVector.normalized;
        }

        // ===== 阶段1：拖拽距离蓄力 =====
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
            // ===== 阶段2：按住时间蓄力 =====
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

        if (debugLog)
        {
            Debug.Log(
                $"[DragChargeInput] 阶段:{currentStage} | " +
                $"实际拖拽距离:{rawDragDistance:F2} | " +
                $"有效拖拽距离:{scaledDragDistance:F2} | " +
                $"方向:{currentDirection} | " +
                $"Power:{currentPower:F2} | " +
                $"计时:{holdTimer:F2}"
            );
        }
    }

    /// <summary>
    /// 实时更新轨迹预览
    /// </summary>
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
            currentPower
        );
    }

    /// <summary>
    /// 松手发射
    /// </summary>
    private void ReleaseCharge()
    {
        if (currentDirection.sqrMagnitude <= 0.0001f)
        {
            if (debugLog)
            {
                Debug.LogWarning("[DragChargeInput] 松手失败：当前方向无效");
            }

            ResetChargeState();
            ClearTrajectory();
            return;
        }

        if (currentPower < chargeConfig.minFirePower)
        {
            if (debugLog)
            {
                Debug.LogWarning($"[DragChargeInput] 松手未发射：力度过小 | 当前:{currentPower:F2} | 阈值:{chargeConfig.minFirePower:F2}");
            }

            ResetChargeState();
            ClearTrajectory();
            return;
        }

        if (debugLog)
        {
            Debug.Log($"[DragChargeInput] 发射 | 方向:{currentDirection} | Power:{currentPower:F2}");
        }

        piece.Fire(currentDirection, currentPower);

        if (turnController != null)
        {
            turnController.NotifyPieceFired();
        }

        ResetChargeState();
        ClearTrajectory();
    }

    /// <summary>
    /// 取消蓄力
    /// </summary>
    private void CancelChargeInternal(string logMessage)
    {
        if (debugLog)
        {
            Debug.Log(logMessage);
        }

        ResetChargeState();
        ClearTrajectory();
    }

    /// <summary>
    /// 重置蓄力状态
    /// </summary>
    private void ResetChargeState()
    {
        isCharging = false;
        currentStage = ChargeStage.None;
        currentPower = 0f;
        holdTimer = 0f;
        currentDirection = Vector2.zero;
        currentScaledDragDistance = 0f;
    }

    private void ClearTrajectory()
    {
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.Clear();
        }
    }

    /// <summary>
    /// 获取鼠标世界坐标
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        return new Vector2(mouseWorldPos.x, mouseWorldPos.y);
    }

    /// <summary>
    /// 切换当前棋子
    /// </summary>
    public void SetControlledPiece(ChessPiece newPiece)
    {
        piece = newPiece;

        if (debugLog)
        {
            Debug.Log($"[DragChargeInput] 切换控制棋子: {piece.name}");
        }

        // 切换时清空状态（防止残留输入）
        ResetChargeState();
        ClearTrajectory();
    }

    /// <summary>
    /// Debug绘制
    /// </summary>
    private void DrawDebugInfo()
    {
        Vector2 center = piece.transform.position;

        // 输入检测圆
        DrawCircle(center, chargeConfig.inputRadius, Color.yellow);

        // 当前发射方向
        if (currentDirection.sqrMagnitude > 0.0001f)
        {
            Debug.DrawLine(center, center + currentDirection * debugArrowLength, Color.cyan);
        }

        // 当前鼠标位置到棋子中心的连线
        Debug.DrawLine(center, currentMouseWorld, Color.magenta);
    }

    /// <summary>
    /// 用 Debug.DrawLine 画一个圆
    /// </summary>
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