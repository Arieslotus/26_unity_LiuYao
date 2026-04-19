/// <summary>
/// 实现功能：负责硬币模型的翻面旋转动画与当前回合高亮显示。
/// 挂在每个硬币上。
/// 适配 3D 项目：通过旋转模型节点实现正反面切换，不再依赖 Sprite 切换。
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

    [Header("显示引用")]
    [Tooltip("实际执行翻面旋转的模型节点。若不指定，默认使用当前物体自身。")]
    [SerializeField] private Transform visualRoot;

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
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        baseScale = visualRoot.localScale;
        baseRotation = visualRoot.localRotation;

        RefreshTransformVisual();
        ApplyFaceRotationImmediate();
    }

    /// <summary>
    /// 立即设置当前正反面，不播放动画
    /// </summary>
    public void SetFaceImmediate(bool isFront, CoinDefinition definition)
    {
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        currentDefinition = definition;

        ApplyFaceRotationImmediate();
        RefreshTransformVisual();
    }

    /// <summary>
    /// 播放翻面到目标面的动画
    /// </summary>
    public void PlayFlipToFace(bool isFront, CoinDefinition definition, Action onComplete = null)
    {
        StopFlipAnimationInternal(false);

        bool previousFace = isFrontSide;

        isFrontSide = isFront;
        currentDefinition = definition;
        pendingFlipComplete = onComplete;

        flipCoroutine = StartCoroutine(CoPlayFlip(previousFace, isFrontSide));
    }

    /// <summary>
    /// 取消当前翻面，并立即设置到指定面
    /// </summary>
    public void CancelFlipAndSetFace(bool isFront, CoinDefinition definition)
    {
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        currentDefinition = definition;

        ApplyFaceRotationImmediate();
        RefreshTransformVisual();
    }

    /// <summary>
    /// 设置当前回合高亮
    /// </summary>
    public void SetTurnHighlight(bool highlighted)
    {
        isHighlighted = highlighted;
        RefreshTransformVisual();
    }

    private IEnumerator CoPlayFlip(bool fromFront, bool toFront)
    {
        if (visualRoot == null)
        {
            flipCoroutine = null;
            Action callbackFallback = pendingFlipComplete;
            pendingFlipComplete = null;
            callbackFallback?.Invoke();
            yield break;
        }

        float duration = Mathf.Max(0.001f, flipDuration);

        Quaternion startRotation = GetFaceRotation(fromFront);
        Quaternion endRotation = GetFaceRotation(toFront);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            visualRoot.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            RefreshTransformVisual();
            yield return null;
        }

        visualRoot.localRotation = endRotation;
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
        if (visualRoot == null)
            return;

        float highlightScale = isHighlighted ? activeScaleMultiplier : 1f;
        visualRoot.localScale = baseScale * highlightScale;
    }

    private void ApplyFaceRotationImmediate()
    {
        if (visualRoot == null)
            return;

        visualRoot.localRotation = GetFaceRotation(isFrontSide);
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