/// <summary>
/// 实现功能：负责硬币的正反面 Sprite 显示、翻面旋转动画与当前回合高亮显示。
/// 挂在每个硬币上
/// </summary>
using System;
using System.Collections;
using UnityEngine;

public class CoinVisualController : MonoBehaviour
{
    [Header("显示引用")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("翻面动画")]
    [Tooltip("整段翻面动画时长")]
    [SerializeField] private float flipDuration = 0.16f;

    [Tooltip("翻面时 Y 轴最大旋转角度，通常为 180")]
    [SerializeField] private float flipAngleY = 180f;

    [Header("当前回合高亮")]
    [Tooltip("当前回合棋子的整体高亮缩放倍率")]
    [SerializeField] private float activeScaleMultiplier = 1.12f;

    private bool isFrontSide = true;
    private bool isHighlighted = false;

    private Vector3 baseScale;
    private Coroutine flipCoroutine;
    private Action pendingFlipComplete;
    private CoinDefinition currentDefinition;

    public bool IsFlipAnimating => flipCoroutine != null;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        baseScale = transform.localScale;
        RefreshSprite();
        RefreshTransformVisual();
    }

    public void SetFaceImmediate(bool isFront, CoinDefinition definition)
    {
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        currentDefinition = definition;

        ApplyFinalRotation();
        RefreshSprite();
        RefreshTransformVisual();
    }

    public void PlayFlipToFace(bool isFront, CoinDefinition definition, Action onComplete = null)
    {
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        currentDefinition = definition;
        pendingFlipComplete = onComplete;
        flipCoroutine = StartCoroutine(CoPlayFlip());
    }

    public void CancelFlipAndSetFace(bool isFront, CoinDefinition definition)
    {
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        currentDefinition = definition;

        ApplyFinalRotation();
        RefreshSprite();
        RefreshTransformVisual();
    }

    public void SetTurnHighlight(bool highlighted)
    {
        isHighlighted = highlighted;
        RefreshTransformVisual();
    }

    private IEnumerator CoPlayFlip()
    {
        float halfDuration = Mathf.Max(0.001f, flipDuration * 0.5f);

        float timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);

            float currentY = Mathf.Lerp(0f, flipAngleY * 0.5f, t);
            ApplyRotation(currentY);
            RefreshTransformVisual();
            yield return null;
        }

        ApplyRotation(flipAngleY * 0.5f);
        RefreshSprite();
        RefreshTransformVisual();

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);

            float currentY = Mathf.Lerp(flipAngleY * 0.5f, flipAngleY, t);
            ApplyRotation(currentY);
            RefreshTransformVisual();
            yield return null;
        }

        ApplyFinalRotation();
        RefreshTransformVisual();

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

    private void RefreshSprite()
    {
        if (targetRenderer == null)
            return;

        if (currentDefinition == null)
        {
            targetRenderer.sprite = null;
            Debug.LogWarning($"[CoinVisualController] {name} 未绑定 CoinDefinition，无法显示正反面 Sprite。");
            return;
        }

        targetRenderer.sprite = isFrontSide
            ? currentDefinition.frontSprite
            : currentDefinition.backSprite;
    }

    private void RefreshTransformVisual()
    {
        float highlightScale = isHighlighted ? activeScaleMultiplier : 1f;
        transform.localScale = baseScale * highlightScale;
    }

    private void ApplyRotation(float yAngle)
    {
        transform.localRotation = Quaternion.Euler(0f, yAngle, 0f);
    }

    private void ApplyFinalRotation()
    {
        transform.localRotation = Quaternion.identity;
    }
}