using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 实现功能：使用 DOTween 通过 CanvasGroup 控制 UI 淡入与淡出特效。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class UIFadeEffect : MonoBehaviour
{
    [Serializable]
    private class FadeEffectSettings
    {
        [Tooltip("是否启用该段透明度动画。关闭后会直接设置为结束透明度。")]
        public bool enableEffect = true;

        [Tooltip("特效持续时间，单位：秒。")]
        [Min(0f)]
        public float duration = 0.3f;

        [Tooltip("动画起始透明度。")]
        [Range(0f, 1f)]
        public float fromAlpha = 0f;

        [Tooltip("动画结束透明度。")]
        [Range(0f, 1f)]
        public float toAlpha = 1f;

        [Tooltip("DOTween 缓动类型。淡入淡出常用 Linear、InOutQuad。")]
        public Ease ease = Ease.Linear;

        [Tooltip("启用后使用自定义曲线，忽略 Ease 设置。")]
        public bool useCustomEase = false;

        [Tooltip("自定义透明度曲线。横轴为时间进度，纵轴为透明度进度。")]
        public AnimationCurve customEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    [Header("进场设置")]
    [SerializeField] private FadeEffectSettings enterSettings = new FadeEffectSettings();

    [Tooltip("物体启用时是否自动播放进场。适合弹窗、技能图标等 SetActive(true) 后自动出现的 UI。")]
    [SerializeField] private bool playEnterOnEnable = false;

    [Header("退场设置")]
    [SerializeField] private FadeEffectSettings exitSettings = new FadeEffectSettings
    {
        fromAlpha = 1f,
        toAlpha = 0f
    };

    [Tooltip("退场动画结束后是否禁用当前物体。")]
    [SerializeField] private bool deactivateOnExitComplete = false;

    [Header("通用设置")]
    [Tooltip("是否使用不受 Time.timeScale 影响的时间。暂停菜单 UI 建议开启。")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("透明度为 0 时是否禁止交互。")]
    [SerializeField] private bool disableInteractionWhenHidden = true;

    private CanvasGroup canvasGroup;
    private Tween fadeTween;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
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
        StopFade();
    }

    /// <summary>
    /// 播放淡入动画。
    /// </summary>
    public void PlayEnter()
    {
        gameObject.SetActive(true);
        PlayFade(enterSettings, false);
    }

    /// <summary>
    /// 播放淡出动画。
    /// </summary>
    public void PlayExit()
    {
        PlayFade(exitSettings, deactivateOnExitComplete);
    }

    /// <summary>
    /// 按进场配置立即设置为显示状态。
    /// </summary>
    public void SetVisibleImmediately()
    {
        StopFade();
        SetAlpha(enterSettings.toAlpha);
    }

    /// <summary>
    /// 按退场配置立即设置为隐藏状态。
    /// </summary>
    public void SetHiddenImmediately()
    {
        StopFade();
        SetAlpha(exitSettings.toAlpha);
    }

    private void PlayFade(FadeEffectSettings settings, bool deactivateWhenComplete)
    {
        StopFade();
        SetAlpha(settings.fromAlpha);

        if (!settings.enableEffect || settings.duration <= 0f)
        {
            SetAlpha(settings.toAlpha);

            if (deactivateWhenComplete)
            {
                gameObject.SetActive(false);
            }

            return;
        }

        fadeTween = canvasGroup
            .DOFade(settings.toAlpha, settings.duration)
            .SetUpdate(useUnscaledTime)
            .OnUpdate(() => UpdateInteractionState())
            .OnKill(() => fadeTween = null);

        ApplyEase(fadeTween, settings);

        if (deactivateWhenComplete)
        {
            fadeTween.OnComplete(() => gameObject.SetActive(false));
        }
    }

    private void ApplyEase(Tween tween, FadeEffectSettings settings)
    {
        if (settings.useCustomEase && settings.customEase != null)
        {
            tween.SetEase(settings.customEase);
            return;
        }

        tween.SetEase(settings.ease);
    }

    private void SetAlpha(float alpha)
    {
        canvasGroup.alpha = Mathf.Clamp01(alpha);
        UpdateInteractionState();
    }

    private void UpdateInteractionState()
    {
        if (!disableInteractionWhenHidden)
        {
            return;
        }

        bool canInteract = canvasGroup.alpha > 0.001f;
        canvasGroup.interactable = canInteract;
        canvasGroup.blocksRaycasts = canInteract;
    }

    private void StopFade()
    {
        if (fadeTween == null || !fadeTween.IsActive())
        {
            fadeTween = null;
            return;
        }

        fadeTween.Kill();
    }
}
