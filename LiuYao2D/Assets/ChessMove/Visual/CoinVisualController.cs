/// <summary>
/// 实现功能：负责硬币的正反面显示、翻面动画与当前回合高亮显示。
/// 挂在每个硬币上
/// </summary>
using System;
using System.Collections;
using UnityEngine;

public class CoinVisualController : MonoBehaviour
{
    [Header("显示引用")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("正反面颜色")]
    [SerializeField] private Color frontColor = Color.white;
    [SerializeField] private Color backColor = Color.black;

    [Header("翻面动画")]
    [SerializeField] private float flipDuration = 0.12f;
    [SerializeField] private float flipMinScaleX = 0.15f;

    [Header("当前回合高亮")]
    [SerializeField] private float activeScaleMultiplier = 1.12f;

    private bool isFrontSide = true;
    private bool isHighlighted = false;

    private Vector3 baseScale;
    private float flipScaleX = 1f;
    private Coroutine flipCoroutine;
    private Action pendingFlipComplete;

    public bool IsFlipAnimating => flipCoroutine != null;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        baseScale = transform.localScale;
        RefreshColor();
        RefreshScale();
    }

    public void SetFaceImmediate(bool isFront)
    {
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        flipScaleX = 1f;

        RefreshColor();
        RefreshScale();
    }

    public void PlayFlipToFace(bool isFront, Action onComplete = null)
    {
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        pendingFlipComplete = onComplete;
        flipCoroutine = StartCoroutine(CoPlayFlip());
    }

    public void CancelFlipAndSetFace(bool isFront)
    {
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        flipScaleX = 1f;

        RefreshColor();
        RefreshScale();
    }

    public void SetTurnHighlight(bool highlighted)
    {
        isHighlighted = highlighted;
        RefreshScale();
    }

    private IEnumerator CoPlayFlip()
    {
        float halfDuration = Mathf.Max(0.001f, flipDuration * 0.5f);

        float timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);
            flipScaleX = Mathf.Lerp(1f, flipMinScaleX, t);
            RefreshScale();
            yield return null;
        }

        flipScaleX = flipMinScaleX;
        RefreshScale();

        RefreshColor();

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);
            flipScaleX = Mathf.Lerp(flipMinScaleX, 1f, t);
            RefreshScale();
            yield return null;
        }

        flipScaleX = 1f;
        RefreshScale();

        flipCoroutine = null;

        Action callback = pendingFlipComplete;
        pendingFlipComplete = null;
        callback?.Invoke();
    }

    private void StopFlipAnimationInternal(bool invokeCallback)
    {
        if (flipCoroutine != null)
        {
            StopCoroutine(flipCoroutine);
            flipCoroutine = null;
        }

        Action callback = pendingFlipComplete;
        pendingFlipComplete = null;

        if (invokeCallback)
        {
            callback?.Invoke();
        }
    }

    private void RefreshColor()
    {
        if (targetRenderer == null)
            return;

        targetRenderer.color = isFrontSide ? frontColor : backColor;
    }

    private void RefreshScale()
    {
        float highlightScale = isHighlighted ? activeScaleMultiplier : 1f;

        transform.localScale = new Vector3(
            baseScale.x * flipScaleX * highlightScale,
            baseScale.y * highlightScale,
            baseScale.z
        );
    }
}