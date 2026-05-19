/// <summary>
/// 实现功能：管理战斗镜头反馈，包括碰撞预慢动作期间的聚焦，以及蓄力阶段的缓慢放大与可选位移。
/// </summary>
using Cinemachine;
using UnityEngine;

public class CameraFeedbackController : MonoBehaviour
{
    public static CameraFeedbackController Instance { get; private set; }

    [Header("引用")]
    [Tooltip("用于战斗表现的 Cinemachine 虚拟相机，推荐绑定 CM_GameCamera")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    [Tooltip("用于计算屏幕位置的真实相机。为空时自动使用 Camera.main")]
    [SerializeField] private Camera renderCamera;

    [Header("碰撞镜头反馈")]
    [Tooltip("是否启用碰撞预慢动作期间的镜头反馈")]
    [SerializeField] private bool enableImpactFeedback = true;

    [Tooltip("碰撞镜头反馈是否启用位移")]
    [SerializeField] private bool enableImpactOffset = true;

    [Tooltip("碰撞镜头反馈的最大放大倍率")]
    [Min(1f)]
    [SerializeField] private float impactMaxZoomMultiplier = 1.12f;

    [Tooltip("碰撞镜头反馈的最大位移距离")]
    [Min(0f)]
    [SerializeField] private float impactMaxOffsetDistance = 1.2f;

    [Tooltip("碰撞镜头跟随目标的速度，数值越大越快靠近目标")]
    [Min(0f)]
    [SerializeField] private float impactFollowSpeed = 8f;

    [Tooltip("碰撞镜头反馈结束后恢复默认状态的持续时间")]
    [Min(0.001f)]
    [SerializeField] private float impactReturnDuration = 0.18f;

    [Header("蓄力镜头反馈")]
    [Tooltip("是否启用蓄力阶段镜头反馈")]
    [SerializeField] private bool enableChargeFeedback = true;

    [Tooltip("蓄力阶段是否启用位移")]
    [SerializeField] private bool enableChargeOffset = true;

    [Tooltip("蓄力阶段最大放大倍率")]
    [Min(1f)]
    [SerializeField] private float chargeMaxZoomMultiplier = 1.08f;

    [Tooltip("蓄力阶段最大位移距离")]
    [Min(0f)]
    [SerializeField] private float chargeMaxOffsetDistance = 0.8f;

    [Tooltip("蓄力阶段放大速度，数值越大越快达到最大放大")]
    [Min(0f)]
    [SerializeField] private float chargeZoomSpeed = 0.8f;

    [Tooltip("蓄力阶段位移速度，数值越大越快达到最大位移")]
    [Min(0f)]
    [SerializeField] private float chargeOffsetSpeed = 0.8f;

    [Tooltip("蓄力结束后镜头恢复默认状态的持续时间")]
    [Min(0.001f)]
    [SerializeField] private float chargeReturnDuration = 0.2f;

    [Header("调试")]
    [SerializeField] private bool debugLog = false;

    private Camera targetCamera;
    private Transform cameraTransform;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float baseFieldOfView;

    private Vector3 currentOffset;
    private float currentZoomMultiplier = 1f;

    private Vector3 impactTargetOffset;
    private float impactTargetZoomMultiplier = 1f;
    private float impactTimer;
    private float impactDuration;
    private bool isImpactFeedbackActive;
    private bool isReturningFromImpact;

    private bool isChargeFeedbackActive;
    private Vector3 chargeFocusPoint;
    private float chargeZoomWeight;
    private float chargeOffsetWeight;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[CameraFeedbackController] 场景中存在多个镜头反馈控制器，当前对象:{name}。");
        }

        Instance = this;

        if (renderCamera == null)
        {
            renderCamera = Camera.main;
        }

        ResolveCameraReferences();
        CaptureBaseState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        UpdateImpactState();
        UpdateChargeState();
        ApplyFeedbackState();
    }

    public void PlayImpactFocus(Vector3 worldPoint, float duration)
    {
        if (!enableImpactFeedback || cameraTransform == null)
            return;

        impactDuration = Mathf.Max(0.001f, duration);
        impactTimer = impactDuration;
        isImpactFeedbackActive = impactTimer > 0f;
        isReturningFromImpact = false;
        impactTargetZoomMultiplier = impactMaxZoomMultiplier;
        impactTargetOffset = enableImpactOffset
            ? CalculateScreenCenteringOffset(worldPoint, impactMaxOffsetDistance)
            : Vector3.zero;

        if (debugLog)
        {
            Debug.Log(
                $"[CameraFeedbackController] 碰撞镜头反馈 | point:{worldPoint} | " +
                $"duration:{duration:F3} | offset:{impactTargetOffset}"
            );
        }
    }

    public void BeginChargeFocus(Vector3 focusPoint)
    {
        if (!enableChargeFeedback)
            return;

        isReturningFromImpact = false;
        isChargeFeedbackActive = true;
        chargeFocusPoint = focusPoint;
    }

    public void UpdateChargeFocus(Vector3 focusPoint)
    {
        if (!enableChargeFeedback || !isChargeFeedbackActive)
            return;

        chargeFocusPoint = focusPoint;
    }

    public void EndChargeFocus()
    {
        isChargeFeedbackActive = false;
    }

    private void ResolveCameraReferences()
    {
        if (virtualCamera != null)
        {
            cameraTransform = virtualCamera.transform;
            return;
        }

        targetCamera = renderCamera != null ? renderCamera : Camera.main;
        if (targetCamera != null)
        {
            cameraTransform = targetCamera.transform;
        }
    }

    private void CaptureBaseState()
    {
        if (cameraTransform == null)
        {
            Debug.LogError("[CameraFeedbackController] 未绑定 CinemachineVirtualCamera，也未找到 Camera.main。");
            return;
        }

        basePosition = cameraTransform.position;
        baseRotation = cameraTransform.rotation;
        baseFieldOfView = GetFieldOfView();
    }

    private void UpdateImpactState()
    {
        if (!isImpactFeedbackActive)
            return;

        impactTimer -= Time.unscaledDeltaTime;
        if (impactTimer <= 0f)
        {
            impactTimer = 0f;
            isImpactFeedbackActive = false;
            isReturningFromImpact = true;
        }
    }

    private void UpdateChargeState()
    {
        if (isImpactFeedbackActive)
            return;

        if (isChargeFeedbackActive)
        {
            chargeZoomWeight = Mathf.MoveTowards(chargeZoomWeight, 1f, chargeZoomSpeed * Time.unscaledDeltaTime);
            chargeOffsetWeight = Mathf.MoveTowards(chargeOffsetWeight, 1f, chargeOffsetSpeed * Time.unscaledDeltaTime);
            return;
        }

        float returnSpeed = 1f / Mathf.Max(0.001f, chargeReturnDuration);
        chargeZoomWeight = Mathf.MoveTowards(chargeZoomWeight, 0f, returnSpeed * Time.unscaledDeltaTime);
        chargeOffsetWeight = Mathf.MoveTowards(chargeOffsetWeight, 0f, returnSpeed * Time.unscaledDeltaTime);
    }

    private void ApplyFeedbackState()
    {
        Vector3 targetOffset = Vector3.zero;
        float targetZoomMultiplier = 1f;
        float returnDuration = chargeReturnDuration;
        float blendSpeed = 1f / Mathf.Max(0.001f, returnDuration);

        if (isImpactFeedbackActive)
        {
            targetOffset = impactTargetOffset;
            targetZoomMultiplier = impactTargetZoomMultiplier;
            blendSpeed = impactFollowSpeed;
        }
        else if (chargeZoomWeight > 0f || chargeOffsetWeight > 0f)
        {
            targetZoomMultiplier = Mathf.Lerp(1f, chargeMaxZoomMultiplier, chargeZoomWeight);
            targetOffset = enableChargeOffset
                ? CalculateScreenCenteringOffset(chargeFocusPoint, chargeMaxOffsetDistance) * chargeOffsetWeight
                : Vector3.zero;
            returnDuration = chargeReturnDuration;
            blendSpeed = 1f / Mathf.Max(0.001f, returnDuration);
        }
        else
        {
            returnDuration = isReturningFromImpact ? impactReturnDuration : chargeReturnDuration;
            blendSpeed = 1f / Mathf.Max(0.001f, returnDuration);
        }

        currentOffset = Vector3.Lerp(currentOffset, targetOffset, blendSpeed * Time.unscaledDeltaTime);
        currentZoomMultiplier = Mathf.Lerp(currentZoomMultiplier, targetZoomMultiplier, blendSpeed * Time.unscaledDeltaTime);

        if (isReturningFromImpact && currentOffset.sqrMagnitude <= 0.0001f && Mathf.Abs(currentZoomMultiplier - 1f) <= 0.001f)
        {
            isReturningFromImpact = false;
        }

        cameraTransform.position = basePosition + currentOffset;
        cameraTransform.rotation = baseRotation;
        SetFieldOfView(baseFieldOfView / Mathf.Max(1f, currentZoomMultiplier));
    }

    private Vector3 CalculateScreenCenteringOffset(Vector3 worldPoint, float maxDistance)
    {
        if (renderCamera == null || maxDistance <= 0f)
            return Vector3.zero;

        Vector3 viewportPoint = renderCamera.WorldToViewportPoint(worldPoint);
        if (viewportPoint.z <= 0f)
            return Vector3.zero;

        Vector2 centerDelta = new Vector2(
            viewportPoint.x - 0.5f,
            viewportPoint.y - 0.5f
        );

        centerDelta = Vector2.ClampMagnitude(centerDelta * 2f, 1f);

        Vector3 offset =
            cameraTransform.right * centerDelta.x +
            cameraTransform.up * centerDelta.y;

        return Vector3.ClampMagnitude(offset, 1f) * maxDistance;
    }

    private float GetFieldOfView()
    {
        if (virtualCamera != null)
            return virtualCamera.m_Lens.FieldOfView;

        if (targetCamera != null)
            return targetCamera.fieldOfView;

        return 60f;
    }

    private void SetFieldOfView(float fieldOfView)
    {
        if (virtualCamera != null)
        {
            LensSettings lens = virtualCamera.m_Lens;
            lens.FieldOfView = fieldOfView;
            virtualCamera.m_Lens = lens;
            return;
        }

        if (targetCamera != null)
        {
            targetCamera.fieldOfView = fieldOfView;
        }
    }
}
