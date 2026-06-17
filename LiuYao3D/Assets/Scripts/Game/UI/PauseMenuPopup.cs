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

    protected override bool ShouldLockGameplayInputOnOpen => true;

    private void Awake()
    {
        BindButtons();
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

    public void CancelPause()
    {
        Close();
    }

    public void ReturnToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning($"[PauseMenuPopup] 返回主菜单失败：未配置主菜单场景名 | popup:{name}");
            return;
        }

        if (debugLog)
        {
            Debug.Log($"[PauseMenuPopup] 返回主菜单 | popup:{name} | scene:{mainMenuSceneName}");
        }

        CloseImmediate();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
