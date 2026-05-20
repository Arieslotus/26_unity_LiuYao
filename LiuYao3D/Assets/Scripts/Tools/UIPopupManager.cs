/// <summary>
/// 实现功能：统一管理 UI 弹窗的创建、关闭、弹窗栈与遮罩显示。
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPopupManager : MonoBehaviour
{
    public static UIPopupManager Instance { get; private set; }

    [Header("根节点")]
    [Tooltip("弹窗实例挂载位置。为空时使用当前物体。")]
    [SerializeField] private Transform popupRoot;

    [Header("遮罩")]
    [Tooltip("全屏遮罩 Image。可为空。")]
    [SerializeField] private Image maskImage;

    [Tooltip("遮罩按钮。用于点击遮罩关闭顶部弹窗，可为空。")]
    [SerializeField] private Button maskButton;

    [Tooltip("没有弹窗时是否禁用遮罩物体。关闭则只禁用 Image 和 Button。")]
    [SerializeField] private bool deactivateMaskObjectWhenHidden = true;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private readonly List<UIPopupBase> popupStack = new List<UIPopupBase>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"[UIPopupManager] 场景中存在多个 UIPopupManager，销毁重复对象:{name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (popupRoot == null)
        {
            popupRoot = transform;
        }

        BindMaskButton();
        RefreshMask();
    }

    public UIPopupBase Open(UIPopupBase popupPrefab)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning($"[UIPopupManager] {name} 打开弹窗失败：popupPrefab 为空。");
            return null;
        }

        UIPopupBase popup = Instantiate(popupPrefab, popupRoot);
        popup.Initialize(this);
        popupStack.Add(popup);
        popup.Open();

        RefreshMask();

        if (debugLog)
        {
            Debug.Log($"[UIPopupManager] 打开弹窗 | manager:{name} | popup:{popup.name} | count:{popupStack.Count}");
        }

        return popup;
    }

    public T Open<T>(T popupPrefab) where T : UIPopupBase
    {
        return Open((UIPopupBase)popupPrefab) as T;
    }

    public void CloseTop()
    {
        UIPopupBase popup = GetTopPopup();
        if (popup == null)
            return;

        popup.Close();
    }

    public void CloseAll()
    {
        List<UIPopupBase> closingPopups = new List<UIPopupBase>(popupStack);
        popupStack.Clear();

        for (int i = closingPopups.Count - 1; i >= 0; i--)
        {
            UIPopupBase popup = closingPopups[i];
            if (popup == null)
                continue;

            popup.CloseImmediate();
        }

        RefreshMask();
    }

    public void NotifyPopupClosed(UIPopupBase popup)
    {
        if (popup == null)
            return;

        popupStack.Remove(popup);
        RefreshMask();

        if (debugLog)
        {
            Debug.Log($"[UIPopupManager] 弹窗关闭 | manager:{name} | popup:{popup.name} | count:{popupStack.Count}");
        }
    }

    private void BindMaskButton()
    {
        if (maskButton == null)
            return;

        maskButton.onClick.RemoveListener(OnMaskClicked);
        maskButton.onClick.AddListener(OnMaskClicked);
    }

    private void OnMaskClicked()
    {
        UIPopupBase popup = GetTopPopup();
        if (popup == null)
            return;

        if (!popup.CloseOnMaskClick)
            return;

        popup.Close();
    }

    private UIPopupBase GetTopPopup()
    {
        for (int i = popupStack.Count - 1; i >= 0; i--)
        {
            UIPopupBase popup = popupStack[i];
            if (popup != null)
                return popup;

            popupStack.RemoveAt(i);
        }

        return null;
    }

    private void RefreshMask()
    {
        bool shouldShowMask = false;
        UIPopupBase topPopup = GetTopPopup();

        if (topPopup != null && topPopup.UseMask)
        {
            shouldShowMask = true;
        }

        if (maskImage != null)
        {
            maskImage.enabled = shouldShowMask;

            if (deactivateMaskObjectWhenHidden)
            {
                maskImage.gameObject.SetActive(shouldShowMask);
            }
        }

        if (maskButton != null)
        {
            maskButton.interactable = shouldShowMask;
            maskButton.enabled = shouldShowMask;
        }
    }
}
