/// <summary>
/// 实现功能：提供可复用 UI 弹窗基类，统一处理打开、关闭、进退场动画与关闭后销毁。
/// </summary>
using UnityEngine;

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
    [Tooltip("关闭后是否销毁弹窗物体。关闭则只隐藏物体。")]
    [SerializeField] private bool destroyOnClose = true;

    private UIPopupManager owner;
    private bool isOpen;
    private bool isClosing;

    public bool UseMask => useMask;
    public bool CloseOnMaskClick => closeOnMaskClick;
    public bool IsOpen => isOpen;

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

    private void HideImmediate()
    {
        gameObject.SetActive(false);
    }
}
