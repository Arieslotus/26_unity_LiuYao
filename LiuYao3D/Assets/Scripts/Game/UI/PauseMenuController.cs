/// <summary>
/// 实现功能：监听 Esc 与暂停按钮请求，打开暂停弹窗。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("暂停弹窗预制体。")]
    [SerializeField] private PauseMenuPopup pausePopupPrefab;

    [Tooltip("暂停按钮。为空时只响应 Esc。")]
    [SerializeField] private Button pauseButton;

    [Tooltip("指定弹窗管理器。为空时使用 UIPopupManager.Instance。")]
    [SerializeField] private UIPopupManager popupManager;

    [Header("输入")]
    [Tooltip("是否允许按 Esc 打开暂停界面。")]
    [SerializeField] private bool enableEscapeKey = true;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private PauseMenuPopup currentPopup;

    private void Awake()
    {
        BindPauseButton();
    }

    private void OnDestroy()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(OpenPauseMenu);
        }
    }

    private void Update()
    {
        if (!enableEscapeKey)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        if (!CanOpenPauseMenu())
            return;

        UIPopupManager manager = popupManager != null ? popupManager : UIPopupManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[PauseMenuController] 打开暂停界面失败：场景中没有 UIPopupManager | object:{name}");
            return;
        }

        if (pausePopupPrefab == null)
        {
            Debug.LogWarning($"[PauseMenuController] 打开暂停界面失败：未绑定 PauseMenuPopup 预制体 | object:{name}");
            return;
        }

        currentPopup = manager.Open(pausePopupPrefab);

        if (debugLog && currentPopup != null)
        {
            Debug.Log($"[PauseMenuController] 打开暂停界面 | object:{name} | popup:{currentPopup.name}");
        }
    }

    private bool CanOpenPauseMenu()
    {
        if (currentPopup != null && currentPopup.IsOpen)
            return false;

        GameFlowController flowController = GameFlowController.Instance;
        if (flowController == null)
        {
            flowController = FindObjectOfType<GameFlowController>();
        }

        if (flowController == null)
            return true;

        return flowController.IsGameplayActive && !flowController.IsGameplayInputLocked;
    }

    private void BindPauseButton()
    {
        if (pauseButton == null)
            return;

        pauseButton.onClick.RemoveListener(OpenPauseMenu);
        pauseButton.onClick.AddListener(OpenPauseMenu);
    }
}
