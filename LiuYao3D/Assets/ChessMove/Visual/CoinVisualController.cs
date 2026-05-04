/// <summary>
/// 实现功能：负责 VisualRoot 的硬币翻面旋转动画与当前回合高亮缩放。
/// 挂载位置：CoinRoot 下的 VisualRoot。
/// </summary>
using System;
using System.Collections;
using UnityEngine;

public class CoinVisualController : MonoBehaviour
{
    private enum FlipAxis
    {
        X,
        Y,
        Z
    }

    [Header("翻面动画")]
    [Tooltip("整段翻面动画时长")]
    [SerializeField] private float flipDuration = 0.16f;

    [Tooltip("翻面旋转轴。俯视/斜视硬币通常推荐 X 或 Z，不推荐 Y。")]
    [SerializeField] private FlipAxis flipAxis = FlipAxis.X;

    [Tooltip("翻面旋转角度，通常为 180")]
    [SerializeField] private float flipAngle = 180f;

    [Header("当前回合高亮")]
    [Tooltip("当前回合棋子的整体高亮缩放倍率")]
    [SerializeField] private float activeScaleMultiplier = 1.12f;

    private bool isFrontSide = true;
    private bool isHighlighted = false;

    private Vector3 baseScale;
    private Quaternion baseRotation;

    private Coroutine flipCoroutine;
    private Action pendingFlipComplete;
    private CoinDefinition currentDefinition;

    public bool IsFlipAnimating => flipCoroutine != null;

    private void Awake()
    {
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;

        RefreshTransformVisual();
        ApplyFaceRotationImmediate();

        Debug.Log($"[CoinVisual] 初始化 | 物体:{name} | baseScale:{baseScale} | baseRotation:{baseRotation.eulerAngles}");
    }

    public void SetFaceImmediate(bool isFront, CoinDefinition definition)
    {
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        currentDefinition = definition;

        ApplyFaceRotationImmediate();
        RefreshTransformVisual();
    }

    public void PlayFlipToFace(bool isFront, CoinDefinition definition, Action onComplete = null)
    {
        StopFlipAnimationInternal(false);

        bool previousFace = isFrontSide;

        isFrontSide = isFront;
        currentDefinition = definition;
        pendingFlipComplete = onComplete;

        flipCoroutine = StartCoroutine(CoPlayFlip(previousFace, isFrontSide));
    }

    public void CancelFlipAndSetFace(bool isFront, CoinDefinition definition)
    {
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        currentDefinition = definition;

        ApplyFaceRotationImmediate();
        RefreshTransformVisual();
    }

    public void SetTurnHighlight(bool highlighted)
    {
        isHighlighted = highlighted;
        RefreshTransformVisual();
    }

    private IEnumerator CoPlayFlip(bool fromFront, bool toFront)
    {
        float duration = Mathf.Max(0.001f, flipDuration);

        Quaternion startRotation = GetFaceRotation(fromFront);
        Quaternion endRotation = GetFaceRotation(toFront);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            transform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            RefreshTransformVisual();

            yield return null;
        }

        transform.localRotation = endRotation;
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

    private void RefreshTransformVisual()
    {
        float highlightScale = isHighlighted ? activeScaleMultiplier : 1f;
        transform.localScale = baseScale * highlightScale;
    }

    private void ApplyFaceRotationImmediate()
    {
        transform.localRotation = GetFaceRotation(isFrontSide);
    }

    private Quaternion GetFaceRotation(bool frontSide)
    {
        float angle = frontSide ? 0f : flipAngle;

        Quaternion offsetRotation = flipAxis switch
        {
            FlipAxis.X => Quaternion.Euler(angle, 0f, 0f),
            FlipAxis.Y => Quaternion.Euler(0f, angle, 0f),
            FlipAxis.Z => Quaternion.Euler(0f, 0f, angle),
            _ => Quaternion.identity
        };

        return baseRotation * offsetRotation;
    }
}