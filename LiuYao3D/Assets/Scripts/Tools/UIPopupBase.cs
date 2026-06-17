/// <summary>
/// 实现功能：提供可复用 UI 弹窗基类，统一处理打开、关闭、进退场动画与关闭后销毁。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class UIPopupBase : MonoBehaviour
{
    [Header("遮罩")]
    [Tooltip("打开该弹窗时是否显示遮罩。")]
    [SerializeField] private bool useMask = true;

    [Tooltip("点击遮罩时是否关闭该弹窗。")]
    [SerializeField] private bool closeOnMaskClick = true;

    [Header("动画")]
    [Tooltip("弹窗平移动画，可为空。")]
    [SerializeField] private UIPositionEffect positionEffect;

    [Tooltip("弹窗淡入淡出动画，可为空。")]
    [SerializeField] private UIFadeEffect fadeEffect;

    [Tooltip("关闭后等待多少秒再销毁。建议等于最长退场动画时长。")]
    [Min(0f)]
    [SerializeField] private float destroyDelayAfterClose = 0.3f;

    [Header("行为")]
    [Tooltip("可选：点击后关闭当前弹窗的按钮。")]
    [SerializeField] private Button closeButton;

    [Tooltip("关闭后是否销毁弹窗物体。关闭则只隐藏物体。")]
    [SerializeField] private bool destroyOnClose = true;

    [Tooltip("打开弹窗时是否锁定游戏内输入。适用于暂停、说明书、选择面板等阻塞式 UI。")]
    [SerializeField] private bool lockGameplayInputOnOpen = false;

    private UIPopupManager owner;
    private bool isOpen;
    private bool isClosing;
    private bool hasLockedGameplayInput;

    public bool UseMask => useMask;
    public bool CloseOnMaskClick => closeOnMaskClick;
    public bool IsOpen => isOpen;
    protected virtual bool ShouldLockGameplayInputOnOpen => lockGameplayInputOnOpen;

    private void Reset()
    {
        positionEffect = GetComponent<UIPositionEffect>();
        fadeEffect = GetComponent<UIFadeEffect>();
    }

    public void Initialize(UIPopupManager popupManager)
    {
        owner = popupManager;
    }

    public void Open()
    {
        if (isOpen && !isClosing)
            return;

        gameObject.SetActive(true);
        isOpen = true;
        isClosing = false;

        BindCloseButton();
        LockGameplayInputIfNeeded();
        OnOpen();

        positionEffect?.PlayEnter();
        fadeEffect?.PlayEnter();
    }

    public void Close()
    {
        if (isClosing)
            return;

        isClosing = true;
        isOpen = false;

        OnClose();
        UnlockGameplayInputIfNeeded($"{GetType().Name}.Close");

        positionEffect?.PlayExit();
        fadeEffect?.PlayExit();

        if (owner != null)
        {
            owner.NotifyPopupClosed(this);
        }

        if (destroyOnClose)
        {
            Destroy(gameObject, destroyDelayAfterClose);
        }
        else
        {
            Invoke(nameof(HideImmediate), destroyDelayAfterClose);
        }
    }

    public void CloseImmediate()
    {
        isOpen = false;
        isClosing = false;

        OnClose();
        UnlockGameplayInputIfNeeded($"{GetType().Name}.CloseImmediate");

        if (owner != null)
        {
            owner.NotifyPopupClosed(this);
        }

        if (destroyOnClose)
        {
            Destroy(gameObject);
        }
        else
        {
            HideImmediate();
        }
    }

    protected virtual void OnOpen()
    {
    }

    protected virtual void OnClose()
    {
    }

    protected virtual void OnDestroy()
    {
        UnbindCloseButton();
        UnlockGameplayInputIfNeeded($"{GetType().Name}.OnDestroy");
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);
    }

    private void UnbindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(Close);
    }

    private void LockGameplayInputIfNeeded()
    {
        if (!ShouldLockGameplayInputOnOpen || hasLockedGameplayInput)
            return;

        GameFlowController flowController = ResolveGameFlowController();
        if (flowController == null)
        {
            Debug.LogWarning($"[UIPopupBase] 打开弹窗但未找到 GameFlowController，无法锁定游戏输入 | popup:{name} | type:{GetType().Name}");
            return;
        }

        flowController.LockGameplayInput(GetType().Name);
        hasLockedGameplayInput = true;
    }

    private void UnlockGameplayInputIfNeeded(string reason)
    {
        if (!hasLockedGameplayInput)
            return;

        GameFlowController flowController = ResolveGameFlowController();
        if (flowController != null)
        {
            flowController.UnlockGameplayInput(reason);
        }

        hasLockedGameplayInput = false;
    }

    private GameFlowController ResolveGameFlowController()
    {
        GameFlowController flowController = GameFlowController.Instance;
        if (flowController == null)
        {
            flowController = FindObjectOfType<GameFlowController>();
        }

        return flowController;
    }

    private void HideImmediate()
    {
        gameObject.SetActive(false);
    }
}
