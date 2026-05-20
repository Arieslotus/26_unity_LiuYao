/// <summary>
/// 实现功能：让 UI 按钮通过 Inspector 配置直接打开指定弹窗。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIPopupOpenButton : MonoBehaviour
{
    [Header("弹窗")]
    [SerializeField] private UIPopupBase popupPrefab;

    [Tooltip("指定弹窗管理器。为空时使用 UIPopupManager.Instance。")]
    [SerializeField] private UIPopupManager popupManager;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OpenPopup);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OpenPopup);
        }
    }

    public void OpenPopup()
    {
        UIPopupManager manager = popupManager != null ? popupManager : UIPopupManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[UIPopupOpenButton] {name} 打开弹窗失败：场景中没有 UIPopupManager。");
            return;
        }

        if (popupPrefab == null)
        {
            Debug.LogWarning($"[UIPopupOpenButton] {name} 打开弹窗失败：popupPrefab 为空。");
            return;
        }

        manager.Open(popupPrefab);
    }
}
