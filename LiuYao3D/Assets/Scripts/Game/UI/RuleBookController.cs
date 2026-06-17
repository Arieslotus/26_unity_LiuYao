/// <summary>
/// 实现功能：监听规则说明按钮请求，打开规则说明书弹窗。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class RuleBookController : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("规则说明书弹窗预制体。")]
    [SerializeField] private RuleBookPopup ruleBookPopupPrefab;

    [Tooltip("规则说明按钮。")]
    [SerializeField] private Button ruleBookButton;

    [Tooltip("指定弹窗管理器。为空时使用 UIPopupManager.Instance。")]
    [SerializeField] private UIPopupManager popupManager;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private RuleBookPopup currentPopup;

    private void Awake()
    {
        BindRuleBookButton();
    }

    private void OnDestroy()
    {
        if (ruleBookButton != null)
        {
            ruleBookButton.onClick.RemoveListener(OpenRuleBook);
        }
    }

    public void OpenRuleBook()
    {
        if (currentPopup != null && currentPopup.IsOpen)
            return;

        UIPopupManager manager = popupManager != null ? popupManager : UIPopupManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[RuleBookController] 打开规则说明失败：场景中没有 UIPopupManager | object:{name}");
            return;
        }

        if (ruleBookPopupPrefab == null)
        {
            Debug.LogWarning($"[RuleBookController] 打开规则说明失败：未绑定 RuleBookPopup 预制体 | object:{name}");
            return;
        }

        currentPopup = manager.Open(ruleBookPopupPrefab);

        if (debugLog && currentPopup != null)
        {
            Debug.Log($"[RuleBookController] 打开规则说明 | object:{name} | popup:{currentPopup.name}");
        }
    }

    private void BindRuleBookButton()
    {
        if (ruleBookButton == null)
            return;

        ruleBookButton.onClick.RemoveListener(OpenRuleBook);
        ruleBookButton.onClick.AddListener(OpenRuleBook);
    }
}
