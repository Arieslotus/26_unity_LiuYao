/// <summary>
/// 实现功能：控制开局流程中的 Cinemachine 镜头优先级切换，并等待正式游戏镜头混合完成。
/// </summary>
using System.Collections;
using Cinemachine;
using UnityEngine;

public class OpeningCameraController : MonoBehaviour
{
    [Header("镜头引用")]
    [Tooltip("正式游戏使用的 Cinemachine 虚拟相机，通常是 Cinemachine1。")]
    [SerializeField] private CinemachineVirtualCamera gameplayCamera;

    [Tooltip("开场流程使用的 Cinemachine 虚拟相机，通常是 Cinemachine2。")]
    [SerializeField] private CinemachineVirtualCamera openingCamera;

    [Tooltip("主相机上的 CinemachineBrain。为空时自动从 Camera.main 查找。")]
    [SerializeField] private CinemachineBrain cinemachineBrain;

    [Header("优先级")]
    [Tooltip("当前激活虚拟相机的优先级。")]
    [SerializeField] private int activeCameraPriority = 20;

    [Tooltip("非当前虚拟相机的优先级。")]
    [SerializeField] private int inactiveCameraPriority = 10;

    [Header("等待")]
    [Tooltip("切回正式游戏镜头时，是否等待 Cinemachine 混合完成。")]
    [SerializeField] private bool waitForCameraBlend = true;

    [Tooltip("等待镜头混合完成的最长时间，避免配置异常导致流程卡死。")]
    [Min(0.1f)]
    [SerializeField] private float maxCameraBlendWait = 3f;

    [Tooltip("等待时是否使用不受 Time.timeScale 影响的时间。")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("调试")]
    [Tooltip("是否输出镜头切换日志。")]
    [SerializeField] private bool debugLog = true;

    private void Awake()
    {
        ResolveCinemachineBrain();
    }

    public void ActivateOpeningCamera()
    {
        SetCameraPriority(openingCamera, activeCameraPriority);
        SetCameraPriority(gameplayCamera, inactiveCameraPriority);

        if (debugLog)
        {
            Debug.Log(
                $"[OpeningCameraController] 切换到开场镜头 | object:{name} | " +
                $"opening:{GetCameraName(openingCamera)}({GetCameraPriority(openingCamera)}) | " +
                $"gameplay:{GetCameraName(gameplayCamera)}({GetCameraPriority(gameplayCamera)})"
            );
        }
    }

    public void ActivateGameplayCamera()
    {
        SetCameraPriority(gameplayCamera, activeCameraPriority);
        SetCameraPriority(openingCamera, inactiveCameraPriority);

        if (debugLog)
        {
            Debug.Log(
                $"[OpeningCameraController] 切换到正式游戏镜头 | object:{name} | " +
                $"gameplay:{GetCameraName(gameplayCamera)}({GetCameraPriority(gameplayCamera)}) | " +
                $"opening:{GetCameraName(openingCamera)}({GetCameraPriority(openingCamera)})"
            );
        }
    }

    public IEnumerator WaitForGameplayCameraBlend()
    {
        if (!waitForCameraBlend)
        {
            yield break;
        }

        ResolveCinemachineBrain();
        if (cinemachineBrain == null)
        {
            Debug.LogWarning($"[OpeningCameraController] 未找到 CinemachineBrain，无法等待镜头混合完成 | object:{name}");
            yield break;
        }

        yield return null;

        float elapsed = 0f;
        while (cinemachineBrain != null && cinemachineBrain.IsBlending && elapsed < maxCameraBlendWait)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        if (cinemachineBrain != null && cinemachineBrain.IsBlending)
        {
            Debug.LogWarning($"[OpeningCameraController] 等待镜头混合超时，继续开局流程 | object:{name} | wait:{elapsed:F2}");
        }
        else if (debugLog)
        {
            Debug.Log($"[OpeningCameraController] 镜头混合完成 | object:{name} | wait:{elapsed:F2}");
        }
    }

    private void SetCameraPriority(CinemachineVirtualCamera virtualCamera, int priority)
    {
        if (virtualCamera == null)
            return;

        if (!virtualCamera.gameObject.activeSelf)
        {
            virtualCamera.gameObject.SetActive(true);
        }

        virtualCamera.Priority = priority;
    }

    private int GetCameraPriority(CinemachineVirtualCamera virtualCamera)
    {
        return virtualCamera != null ? virtualCamera.Priority : -1;
    }

    private static string GetCameraName(CinemachineVirtualCamera virtualCamera)
    {
        return virtualCamera != null ? virtualCamera.name : "None";
    }

    private void ResolveCinemachineBrain()
    {
        if (cinemachineBrain != null)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
        }

        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        }
    }
}
