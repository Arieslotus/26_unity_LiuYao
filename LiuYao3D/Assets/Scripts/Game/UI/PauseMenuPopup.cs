/// <summary>
/// 实现功能：显示游戏暂停弹窗，锁定游戏输入，并提供返回主菜单与取消暂停按钮。
/// </summary>
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuPopup : UIPopupBase
{
    [Header("按钮")]
    [Tooltip("返回主菜单按钮。")]
    [SerializeField] private Button exitButton;

    [Tooltip("取消暂停并返回游戏按钮。")]
    [SerializeField] private Button cancelButton;

    [Header("场景")]
    [Tooltip("主菜单场景名。需要确保该场景已加入 Build Settings。")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private bool hasLockedGameplayInput;

    private void Awake()
    {
        BindButtons();
    }

    private void OnDestroy()
    {
        UnlockGameplayInput("PauseMenuPopup.OnDestroy");
    }

    private void BindButtons()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ReturnToMainMenu);
            exitButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelPause);
            cancelButton.onClick.AddListener(CancelPause);
        }
    }

    protected override void OnOpen()
    {
        LockGameplayInput();
    }

    protected override void OnClose()
    {
        UnlockGameplayInput("PauseMenuPopup.OnClose");
    }

    public void CancelPause()
    {
        Close();
    }

    public void ReturnToMainMenu()
    {
        UnlockGameplayInput("PauseMenuPopup.ReturnToMainMenu");

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning($"[PauseMenuPopup] 返回主菜单失败：未配置主菜单场景名 | popup:{name}");
            return;
        }

        if (debugLog)
        {
            Debug.Log($"[PauseMenuPopup] 返回主菜单 | popup:{name} | scene:{mainMenuSceneName}");
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void LockGameplayInput()
    {
        if (hasLockedGameplayInput)
            return;

        GameFlowController flowController = GameFlowController.Instance;
        if (flowController == null)
        {
            flowController = FindObjectOfType<GameFlowController>();
        }

        if (flowController == null)
        {
            Debug.LogWarning($"[PauseMenuPopup] 打开暂停界面但未找到 GameFlowController，无法锁定游戏输入 | popup:{name}");
            return;
        }

        flowController.LockGameplayInput("PauseMenuPopup");
        hasLockedGameplayInput = true;
    }

    private void UnlockGameplayInput(string reason)
    {
        if (!hasLockedGameplayInput)
            return;

        GameFlowController flowController = GameFlowController.Instance;
        if (flowController == null)
        {
            flowController = FindObjectOfType<GameFlowController>();
        }

        if (flowController != null)
        {
            flowController.UnlockGameplayInput(reason);
        }

        hasLockedGameplayInput = false;
    }
}
