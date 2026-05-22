using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 实现功能：使用 DOTween 控制 UI 平移进场与退场，进场终点和退场起点始终以缓存的初始位置为准。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIPositionEffect : MonoBehaviour
{
    public enum MoveDirection
    {
        FromTopToBottom,
        FromBottomToTop,
        FromLeftToRight,
        FromRightToLeft
    }

    [Serializable]
    private class MoveEffectSettings
    {
        [Tooltip("是否启用该段平移动画。")]
        public bool enableEffect = true;

        [Tooltip("UI 移动方向。进场表示从该方向外侧移动到初始位置；退场表示从初始位置向该方向移动。")]
        public MoveDirection moveDirection = MoveDirection.FromBottomToTop;

        [Tooltip("特效持续时间，单位：秒。")]
        [Min(0f)]
        public float duration = 0.3f;

        [Tooltip("移动距离，单位：UI 像素。")]
        [Min(0f)]
        public float moveDistance = 100f;

        [Tooltip("DOTween 缓动类型。常用：OutQuad 平滑、OutBack 回弹、OutBounce 弹跳。")]
        public Ease ease = Ease.OutQuad;

        [Tooltip("启用后使用自定义曲线，忽略 Ease 设置。")]
        public bool useCustomEase = false;

        [Tooltip("自定义运动曲线。横轴为时间进度，纵轴为位移进度。")]
        public AnimationCurve customEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    [Header("进场设置")]
    [SerializeField] private MoveEffectSettings enterSettings = new MoveEffectSettings();

    [Tooltip("物体启用时是否自动播放进场。适合弹窗、技能图标等 SetActive(true) 后自动出现的 UI。")]
    [SerializeField] private bool playEnterOnEnable = false;

    [Header("退场设置")]
    [SerializeField] private MoveEffectSettings exitSettings = new MoveEffectSettings();

    [Tooltip("退场动画结束后是否禁用当前物体。")]
    [SerializeField] private bool deactivateOnExitComplete = false;

    [Header("通用设置")]
    [Tooltip("是否使用不受 Time.timeScale 影响的时间。暂停菜单 UI 建议开启。")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("是否启用像素吸附。像素风 UI 可开启，普通 UI 通常关闭。")]
    [SerializeField] private bool snapping = false;

    private RectTransform rectTransform;
    private Vector2 initialAnchoredPosition;
    private Tween moveTween;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        CacheInitialPosition();
    }

    private void OnEnable()
    {
        if (playEnterOnEnable)
        {
            PlayEnter();
        }
    }

    private void OnDisable()
    {
        StopMove();
    }

    /// <summary>
    /// 缓存当前 UI 位置作为进场终点和退场起点。
    /// 如果 UI 初始布局被代码或 LayoutGroup 改过，应在布局完成后手动调用。
    /// </summary>
    public void CacheInitialPosition()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        initialAnchoredPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// 播放进场平移动画。
    /// </summary>
    public void PlayEnter()
    {
        PlayEnter(null);
    }

    /// <summary>
    /// 播放进场平移动画，并在动画完成后回调。
    /// </summary>
    public void PlayEnter(Action onComplete)
    {
        gameObject.SetActive(true);

        if (!enterSettings.enableEffect)
        {
            StopMove();
            rectTransform.anchoredPosition = initialAnchoredPosition;
            onComplete?.Invoke();
            return;
        }

        Vector2 offset = GetDirectionOffset(enterSettings);
        PlayMove(initialAnchoredPosition - offset, initialAnchoredPosition, enterSettings, false, onComplete);
    }

    /// <summary>
    /// 播放退场平移动画。
    /// </summary>
    public void PlayExit()
    {
        PlayExit(null);
    }

    /// <summary>
    /// 播放退场平移动画，并在动画完成后回调。
    /// </summary>
    public void PlayExit(Action onComplete)
    {
        if (!exitSettings.enableEffect)
        {
            StopMove();
            rectTransform.anchoredPosition = initialAnchoredPosition;

            if (deactivateOnExitComplete)
            {
                gameObject.SetActive(false);
            }

            onComplete?.Invoke();
            return;
        }

        Vector2 offset = GetDirectionOffset(exitSettings);
        PlayMove(initialAnchoredPosition, initialAnchoredPosition + offset, exitSettings, deactivateOnExitComplete, onComplete);
    }

    /// <summary>
    /// 立即回到缓存的初始位置。
    /// </summary>
    public void ResetToInitialPosition()
    {
        StopMove();
        rectTransform.anchoredPosition = initialAnchoredPosition;
    }

    private void PlayMove(Vector2 startPosition, Vector2 endPosition, MoveEffectSettings settings, bool deactivateWhenComplete, Action onComplete)
    {
        StopMove();
        rectTransform.anchoredPosition = startPosition;

        if (settings.duration <= 0f)
        {
            rectTransform.anchoredPosition = endPosition;

            if (deactivateWhenComplete)
            {
                gameObject.SetActive(false);
            }

            onComplete?.Invoke();
            return;
        }

        moveTween = rectTransform
            .DOAnchorPos(endPosition, settings.duration, snapping)
            .SetUpdate(useUnscaledTime)
            .OnKill(() => moveTween = null);

        ApplyEase(moveTween, settings);

        moveTween.OnComplete(() =>
        {
            if (deactivateWhenComplete)
            {
                gameObject.SetActive(false);
            }

            onComplete?.Invoke();
        });

    }

    private void ApplyEase(Tween tween, MoveEffectSettings settings)
    {
        if (settings.useCustomEase && settings.customEase != null)
        {
            tween.SetEase(settings.customEase);
            return;
        }

        tween.SetEase(settings.ease);
    }

    private void StopMove()
    {
        if (moveTween == null || !moveTween.IsActive())
        {
            moveTween = null;
            return;
        }

        moveTween.Kill();
    }

    private Vector2 GetDirectionOffset(MoveEffectSettings settings)
    {
        switch (settings.moveDirection)
        {
            case MoveDirection.FromTopToBottom:
                return Vector2.down * settings.moveDistance;
            case MoveDirection.FromBottomToTop:
                return Vector2.up * settings.moveDistance;
            case MoveDirection.FromLeftToRight:
                return Vector2.right * settings.moveDistance;
            case MoveDirection.FromRightToLeft:
                return Vector2.left * settings.moveDistance;
            default:
                Debug.LogWarning($"[UIPositionEffect] 未识别的移动方向，对象：{name}，方向：{settings.moveDirection}");
                return Vector2.zero;
        }
    }
}
