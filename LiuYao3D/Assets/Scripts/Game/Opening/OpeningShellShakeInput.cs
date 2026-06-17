/// <summary>
/// 实现功能：负责开局摇卦输入判定，根据鼠标有效晃动累计时间，并驱动龟壳轻微跟随鼠标。
/// </summary>
using System.Collections;
using UnityEngine;

public class OpeningShellShakeInput : MonoBehaviour
{
    [Header("摇卦目标")]
    [Tooltip("用于表现鼠标晃动的龟壳节点。建议绑定不被 Animator 写入 Transform 的父节点或独立表现节点。")]
    [SerializeField] private Transform shakeTarget;

    [Header("判定")]
    [Tooltip("有效摇动累计时间达到该值后，视为摇卦成功。")]
    [Min(0.01f)]
    [SerializeField] private float requiredShakeDuration = 1.2f;

    [Tooltip("单帧鼠标移动距离超过该值时，才计入有效摇动。")]
    [Min(0f)]
    [SerializeField] private float minMouseDelta = 6f;

    [Tooltip("停止有效摇动后，累计进度每秒衰减多少。")]
    [Min(0f)]
    [SerializeField] private float idleDecayPerSecond = 0.35f;

    [Header("跟随表现")]
    [Tooltip("鼠标晃动时龟壳最大局部位移。")]
    [SerializeField] private Vector3 maxLocalOffset = new Vector3(0.18f, 0f, 0.12f);

    [Tooltip("鼠标晃动时龟壳最大局部旋转角度。")]
    [SerializeField] private Vector3 maxLocalEuler = new Vector3(8f, 0f, 10f);

    [Tooltip("龟壳跟随鼠标的平滑速度。")]
    [Min(0.01f)]
    [SerializeField] private float followSpeed = 12f;

    [Tooltip("摇卦结束后龟壳回到初始局部姿态的速度。")]
    [Min(0.01f)]
    [SerializeField] private float returnSpeed = 14f;

    [Header("时间")]
    [Tooltip("是否使用不受 Time.timeScale 影响的时间。")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("调试")]
    [Tooltip("是否输出摇卦输入日志。")]
    [SerializeField] private bool debugLog = true;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation = Quaternion.identity;
    private Vector3 previousMousePosition;
    private float shakeProgress;
    private bool isShaking;
    private bool stopRequested;

    public float ShakeProgress => shakeProgress;
    public float Progress01 => requiredShakeDuration > 0f ? Mathf.Clamp01(shakeProgress / requiredShakeDuration) : 1f;
    public bool IsShaking => isShaking;

    private void Awake()
    {
        if (shakeTarget == null)
        {
            shakeTarget = transform;
        }

        CacheBasePose();
        WarnIfAnimatorWritesTarget();
    }

    public IEnumerator WaitForShakeSuccess(int roundIndex)
    {
        if (shakeTarget == null)
        {
            Debug.LogWarning($"[OpeningShellShakeInput] 摇卦目标为空，直接视为成功 | object:{name} | round:{roundIndex}");
            yield break;
        }

        CacheBasePose();
        previousMousePosition = Input.mousePosition;
        shakeProgress = 0f;
        stopRequested = false;
        isShaking = true;

        if (debugLog)
        {
            Debug.Log($"[OpeningShellShakeInput] 开始摇卦输入 | object:{name} | round:{roundIndex} | required:{requiredShakeDuration:F2}");
        }

        while (!stopRequested && shakeProgress < requiredShakeDuration)
        {
            float deltaTime = GetDeltaTime();
            Vector3 mousePosition = Input.mousePosition;
            Vector3 mouseDelta = mousePosition - previousMousePosition;
            previousMousePosition = mousePosition;

            bool validShake = mouseDelta.magnitude >= minMouseDelta;
            if (validShake)
            {
                shakeProgress += deltaTime;
            }
            else if (idleDecayPerSecond > 0f)
            {
                shakeProgress = Mathf.Max(0f, shakeProgress - idleDecayPerSecond * deltaTime);
            }

            UpdateShakePose(mouseDelta, validShake, deltaTime);
            yield return null;
        }

        bool success = !stopRequested && shakeProgress >= requiredShakeDuration;
        isShaking = false;

        yield return ReturnToBasePose();

        if (debugLog)
        {
            Debug.Log($"[OpeningShellShakeInput] 摇卦输入结束 | object:{name} | round:{roundIndex} | success:{success} | progress:{shakeProgress:F2}");
        }
    }

    public void StopShake()
    {
        stopRequested = true;
        isShaking = false;
    }

    public void ResetPoseImmediate()
    {
        if (shakeTarget == null)
            return;

        shakeTarget.localPosition = baseLocalPosition;
        shakeTarget.localRotation = baseLocalRotation;
    }

    private void CacheBasePose()
    {
        if (shakeTarget == null)
            return;

        baseLocalPosition = shakeTarget.localPosition;
        baseLocalRotation = shakeTarget.localRotation;
    }

    private void UpdateShakePose(Vector3 mouseDelta, bool validShake, float deltaTime)
    {
        if (shakeTarget == null)
            return;

        Vector3 normalizedDelta = Vector3.zero;
        if (validShake)
        {
            normalizedDelta = new Vector3(
                Mathf.Clamp(mouseDelta.x / 80f, -1f, 1f),
                0f,
                Mathf.Clamp(mouseDelta.y / 80f, -1f, 1f)
            );
        }

        Vector3 targetOffset = new Vector3(
            normalizedDelta.x * maxLocalOffset.x,
            maxLocalOffset.y,
            normalizedDelta.z * maxLocalOffset.z
        );

        Vector3 targetEuler = new Vector3(
            -normalizedDelta.z * maxLocalEuler.x,
            normalizedDelta.x * maxLocalEuler.y,
            -normalizedDelta.x * maxLocalEuler.z
        );

        float lerp = Mathf.Clamp01(followSpeed * deltaTime);
        shakeTarget.localPosition = Vector3.Lerp(shakeTarget.localPosition, baseLocalPosition + targetOffset, lerp);
        shakeTarget.localRotation = Quaternion.Slerp(shakeTarget.localRotation, baseLocalRotation * Quaternion.Euler(targetEuler), lerp);
    }

    private IEnumerator ReturnToBasePose()
    {
        if (shakeTarget == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            float deltaTime = GetDeltaTime();
            float lerp = Mathf.Clamp01(returnSpeed * deltaTime);
            shakeTarget.localPosition = Vector3.Lerp(shakeTarget.localPosition, baseLocalPosition, lerp);
            shakeTarget.localRotation = Quaternion.Slerp(shakeTarget.localRotation, baseLocalRotation, lerp);

            if (Vector3.Distance(shakeTarget.localPosition, baseLocalPosition) <= 0.001f &&
                Quaternion.Angle(shakeTarget.localRotation, baseLocalRotation) <= 0.1f)
            {
                break;
            }

            elapsed += deltaTime;
            yield return null;
        }

        ResetPoseImmediate();
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void WarnIfAnimatorWritesTarget()
    {
        if (!debugLog || shakeTarget == null)
            return;

        Animator animator = shakeTarget.GetComponent<Animator>();
        if (animator != null)
        {
            Debug.LogWarning($"[OpeningShellShakeInput] shakeTarget 上存在 Animator，可能覆盖摇卦位移。建议绑定 Animator 外层父节点 | object:{name} | target:{shakeTarget.name}");
        }
    }
}
