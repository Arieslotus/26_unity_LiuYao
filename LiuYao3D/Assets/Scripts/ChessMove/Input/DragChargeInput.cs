/// <summary>
/// 实现功能：处理当前棋子的拖拽蓄力输入，支持两段式蓄力、满蓄力翻面、松手发射、右键取消。
/// 适配 3D 项目，逻辑使用 XZ 平面，Y 轴仅作为表现层高度。
/// 轨迹预测功能当前暂时关闭，后续再接回。
/// </summary>
using UnityEngine;

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
    [SerializeField] private ChargeInputConfig chargeConfig;
    [SerializeField] private ChessTurnController turnController;

    [Header("射线平面")]
    [Tooltip("输入投射所使用的逻辑平面高度")]
    [SerializeField] private float inputPlaneY = 0f;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool debugDraw = true;
    [SerializeField] private float debugArrowLength = 2.0f;

    private bool isCharging = false;
    private bool isWaitingDelayedFire = false;
    private ChargeStage currentStage = ChargeStage.None;

    private Vector3 pieceCenter;
    private Vector3 currentMouseWorld;
    private Vector3 currentDirection = Vector3.zero;

    private float currentPower = 0f;
    private float holdTimer = 0f;
    private float currentScaledDragDistance = 0f;

    private bool hasTriggeredChargeFlip = false;
    private bool isChargeFlipAnimating = false;
    private bool pendingFireAfterFlip = false;
    private bool chargeStartFaceState = true;

    private Vector3 queuedFireDirection = Vector3.zero;
    private float queuedFirePower = 0f;
    private ChessPiece queuedFirePiece;

    private TrajectoryRenderer currentTrajectoryRenderer;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (piece == null && debugLog)
        {
            Debug.Log("[DragChargeInput] 启动时未绑定 ChessPiece，将等待回合系统动态指定当前棋子。");
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

        if (!CanAcceptGameplayInput())
        {
            if (isCharging || isWaitingDelayedFire)
            {
                CancelChargeInternal($"[DragChargeInput] 全局游戏流程禁止输入，取消蓄力 | input:{name}");
            }

            ClearTrajectory();
            return;
        }

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
        UnsubscribeGameFlow();
        ResetAllChargeState();
        ClearTrajectory();
        CameraFeedbackController.Instance?.EndChargeFocus();
    }

    private void OnEnable()
    {
        SubscribeGameFlow();
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

    private void TryStartCharge(Vector3 mouseWorldPos)
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
        pieceCenter.y = inputPlaneY;

        Vector3 flatOffset = mouseWorldPos - pieceCenter;
        flatOffset.y = 0f;

        float distanceToCenter = flatOffset.magnitude;
        if (distanceToCenter > chargeConfig.inputRadius)
        {
            if (debugLog)
            {
                Debug.Log($"[DragChargeInput] 按下无效：超出输入半径 | 距离:{distanceToCenter:F2} | 半径:{chargeConfig.inputRadius:F2}");
            }
            return;
        }

        isCharging = true;
        CameraFeedbackController.Instance?.BeginChargeFocus(piece.transform.position);
        currentStage = ChargeStage.Distance;
        currentPower = 0f;
        holdTimer = 0f;
        currentDirection = Vector3.zero;
        currentScaledDragDistance = 0f;

        hasTriggeredChargeFlip = false;
        isChargeFlipAnimating = false;
        pendingFireAfterFlip = false;
        queuedFireDirection = Vector3.zero;
        queuedFirePower = 0f;
        queuedFirePiece = null;

        chargeStartFaceState = piece.IsFrontSide;

        if (debugLog)
        {
            Debug.Log($"[DragChargeInput] 开始蓄力 | 初始面:{(chargeStartFaceState ? "正面" : "反面")}");
        }
    }

    private void UpdateCharge(Vector3 mouseWorldPos)
    {
        pieceCenter = piece.transform.position;
        pieceCenter.y = inputPlaneY;

        Vector3 dragVector = pieceCenter - mouseWorldPos;
        dragVector.y = 0f;

        float rawDragDistance = dragVector.magnitude;
        float scaledDragDistance = rawDragDistance * chargeConfig.dragDistanceScale;
        currentScaledDragDistance = scaledDragDistance;

        if (dragVector.sqrMagnitude > 0.0001f)
        {
            currentDirection = dragVector.normalized;
            currentDirection.y = 0f;
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
        CameraFeedbackController.Instance?.UpdateChargeFocus(piece.transform.position);

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
            ChessPiece firePiece = queuedFirePiece;
            Vector3 fireDirection = queuedFireDirection;
            float firePower = queuedFirePower;

            ClearQueuedDelayedFire();
            ExecuteFire(firePiece, fireDirection, firePower);
        }
    }

    private void UpdateTrajectoryPreview()
    {
        if (currentTrajectoryRenderer == null || piece == null)
            return;

        Vector3 dir = currentDirection;
        dir.y = 0;

        if (dir.sqrMagnitude <= 0.0001f)
        {
            currentTrajectoryRenderer.Clear();
            return;
        }

        currentTrajectoryRenderer.UpdateTrajectory(dir.normalized, currentPower);
    }

    private void ReleaseCharge()
    {
        Vector3 flatDir = currentDirection;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude <= 0.0001f)
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
            queuedFirePiece = piece;
            queuedFireDirection = flatDir.normalized;
            queuedFirePower = currentPower;

            if (debugLog)
            {
                Debug.Log($"[DragChargeInput] 翻面动画中松手，等待动画完成后发射 | piece:{queuedFirePiece.name} | 方向:{queuedFireDirection} | Power:{queuedFirePower:F2}");
            }

            ResetChargeStateKeepFace();
            ClearTrajectory();
            CameraFeedbackController.Instance?.EndChargeFocus();
            return;
        }

        ExecuteFire(piece, flatDir.normalized, currentPower);
    }

    private void ExecuteFire(ChessPiece firePiece, Vector3 fireDirection, float firePower)
    {
        if (firePiece == null)
            return;

        Vector3 flatDir = fireDirection;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude <= 0.0001f)
        {
            if (debugLog)
            {
                Debug.LogWarning("[DragChargeInput] ExecuteFire 失败：fireDirection 无效");
            }
            return;
        }

        flatDir.Normalize();

        if (debugLog)
        {
            Debug.Log($"[DragChargeInput] 发射 | 方向:{flatDir} | Power:{firePower:F2} | 已翻面:{hasTriggeredChargeFlip}");
        }

        firePiece.Fire(flatDir, firePower);

        if (turnController != null)
        {
            turnController.NotifyPieceFired();
        }

        ResetAllChargeState();
        ClearTrajectory();
        isWaitingDelayedFire = false;
        CameraFeedbackController.Instance?.EndChargeFocus();
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
        CameraFeedbackController.Instance?.EndChargeFocus();
    }

    private void ResetChargeStateKeepFace()
    {
        isCharging = false;
        currentStage = ChargeStage.None;
        currentPower = 0f;
        holdTimer = 0f;
        currentDirection = Vector3.zero;
        currentScaledDragDistance = 0f;
    }

    private void ResetAllChargeState()
    {
        ResetChargeStateKeepFace();

        hasTriggeredChargeFlip = false;
        isChargeFlipAnimating = false;
        pendingFireAfterFlip = false;
        queuedFireDirection = Vector3.zero;
        queuedFirePower = 0f;
        queuedFirePiece = null;
        isWaitingDelayedFire = false;
        chargeStartFaceState = piece != null ? piece.IsFrontSide : true;
    }

    private void ClearQueuedDelayedFire()
    {
        pendingFireAfterFlip = false;
        queuedFireDirection = Vector3.zero;
        queuedFirePower = 0f;
        queuedFirePiece = null;
        isWaitingDelayedFire = false;
    }

    private void ClearTrajectory()
    {
        if (currentTrajectoryRenderer == null)
            return;

        currentTrajectoryRenderer.Clear();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, inputPlaneY, 0f));

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            hitPoint.y = inputPlaneY;
            return hitPoint;
        }

        return Vector3.zero;
    }

    public void SetControlledPiece(ChessPiece newPiece)
    {
        // 清掉旧轨迹
        if (currentTrajectoryRenderer != null)
        {
            currentTrajectoryRenderer.Clear();
        }

        piece = newPiece;

        //关键：从当前棋子获取 TrajectoryRenderer
        currentTrajectoryRenderer = piece != null
            ? piece.GetComponent<TrajectoryRenderer>()
            : null;

        if (debugLog)
        {
            Debug.Log(piece != null
                ? $"[DragChargeInput] 切换控制棋子: {piece.name}"
                : "[DragChargeInput] 当前不控制任何棋子");
        }

        ResetAllChargeState();
        CameraFeedbackController.Instance?.EndChargeFocus();
    }

    private void DrawDebugInfo()
    {
        if (piece == null)
            return;

        Vector3 center = piece.transform.position;
        center.y = inputPlaneY;

        DrawCircle(center, chargeConfig.inputRadius, Color.yellow);

        Vector3 flatDir = currentDirection;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Debug.DrawLine(center, center + flatDir.normalized * debugArrowLength, Color.cyan);
        }

        Debug.DrawLine(center, currentMouseWorld, Color.magenta);
    }

    private void DrawCircle(Vector3 center, float radius, Color color, int segments = 32)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Debug.DrawLine(prevPoint, nextPoint, color);
            prevPoint = nextPoint;
        }
    }

    private void SubscribeGameFlow()
    {
        if (GameFlowController.Instance == null)
            return;

        GameFlowController.Instance.StateChanged -= OnGameFlowStateChanged;
        GameFlowController.Instance.StateChanged += OnGameFlowStateChanged;
    }

    private void UnsubscribeGameFlow()
    {
        if (GameFlowController.Instance == null)
            return;

        GameFlowController.Instance.StateChanged -= OnGameFlowStateChanged;
    }

    private void OnGameFlowStateChanged(GameFlowState state)
    {
        if (state == GameFlowState.Playing)
            return;

        if (isCharging || isWaitingDelayedFire)
        {
            CancelChargeInternal($"[DragChargeInput] 游戏状态切换为 {state}，取消当前蓄力 | input:{name}");
            return;
        }

        ClearTrajectory();
        CameraFeedbackController.Instance?.EndChargeFocus();
    }

    private bool CanAcceptGameplayInput()
    {
        return GameFlowController.Instance == null || GameFlowController.Instance.CanAcceptGameplayInput;
    }

    public bool IsCharging => isCharging;
    public Vector3 CurrentDirection => currentDirection;
    public float CurrentPower => currentPower;
    public float CurrentScaledDragDistance => currentScaledDragDistance;
    public ChargeInputConfig ChargeConfig => chargeConfig;
    public ChessPiece CurrentPiece => piece;
}
