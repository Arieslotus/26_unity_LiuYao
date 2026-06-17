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

    //[Header("当前回合高亮")]
    //[Tooltip("保留旧字段兼容 Prefab。当前控制提示已迁移到 UI，不再放大 3D 硬币。")]
    //[SerializeField] private float activeScaleMultiplier = 1.12f;

    [Header("硬币材质")]
    [Tooltip("硬币模型的 Renderer，用于根据 CoinDefinition 切换材质")]
    [SerializeField] private Renderer coinRenderer;


    private bool isFrontSide = true;

    private Vector3 baseScale;
    private Quaternion baseRotation;

    private Coroutine flipCoroutine;
    private Action pendingFlipComplete;
    private CoinDefinition currentDefinition;
    private bool hasInitializedBaseTransform;

    public bool IsFlipAnimating => flipCoroutine != null;

    private void Awake()
    {
        EnsureBaseTransformInitialized();

        RefreshTransformVisual();
        ApplyFaceRotationImmediate();

        //Debug.Log($"[CoinVisual] 初始化 | 物体:{name} | baseScale:{baseScale} | baseRotation:{baseRotation.eulerAngles}");
    }

    public void SetFaceImmediate(bool isFront, CoinDefinition definition)
    {
        EnsureBaseTransformInitialized();
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        currentDefinition = definition;

        ApplyDefinitionVisual();
        ApplyFaceRotationImmediate();
        RefreshTransformVisual();
    }

    public void PlayFlipToFace(bool isFront, CoinDefinition definition, Action onComplete = null)
    {
        EnsureBaseTransformInitialized();
        StopFlipAnimationInternal(false);

        bool previousFace = isFrontSide;

        isFrontSide = isFront;
        currentDefinition = definition;
        pendingFlipComplete = onComplete;

        ApplyDefinitionVisual();
        PlayFlipSFX(previousFace, isFrontSide);

        flipCoroutine = StartCoroutine(CoPlayFlip(previousFace, isFrontSide));
    }

    public void CancelFlipAndSetFace(bool isFront, CoinDefinition definition)
    {
        EnsureBaseTransformInitialized();
        StopFlipAnimationInternal(false);

        isFrontSide = isFront;
        currentDefinition = definition;

        ApplyDefinitionVisual();
        ApplyFaceRotationImmediate();
        RefreshTransformVisual();
    }

    public void SetTurnHighlight(bool highlighted)
    {
        //if (highlighted && activeScaleMultiplier <= 0f)
        //{
        //    Debug.LogWarning($"[CoinVisual] {name} 的 activeScaleMultiplier 配置非法，但当前 3D 高亮缩放已停用。");
        //}

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
        EnsureBaseTransformInitialized();
        transform.localScale = baseScale;
    }

    private void ApplyFaceRotationImmediate()
    {
        EnsureBaseTransformInitialized();
        transform.localRotation = GetFaceRotation(isFrontSide);
    }

    private void EnsureBaseTransformInitialized()
    {
        if (hasInitializedBaseTransform && baseScale.sqrMagnitude > 0.0001f)
            return;

        baseScale = transform.localScale;
        if (baseScale.sqrMagnitude <= 0.0001f)
        {
            Debug.LogWarning($"[CoinVisual] {name} 的 Visual 缩放为 0，已自动恢复为 Vector3.one。请检查 Prefab 的 Visual 节点缩放。");
            baseScale = Vector3.one;
            transform.localScale = baseScale;
        }

        baseRotation = transform.localRotation;
        hasInitializedBaseTransform = true;
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

    private void ApplyDefinitionVisual()
    {
        if (coinRenderer == null)
        {
            Debug.LogWarning($"[CoinVisual] {name} 未绑定 coinRenderer，无法应用硬币材质。");
            return;
        }

        if (currentDefinition == null)
        {
            Debug.LogWarning($"[CoinVisual] {name} 当前 CoinDefinition 为空，跳过材质设置。");
            return;
        }

        if (currentDefinition.coinMaterial == null)
        {
            Debug.LogWarning($"[CoinVisual] {name} 的 CoinDefinition:{currentDefinition.coinName} 未配置 coinMaterial。");
            return;
        }

        coinRenderer.sharedMaterial = currentDefinition.coinMaterial;
    }

    private void PlayFlipSFX(bool previousFace, bool nextFace)
    {
        if (previousFace == nextFace || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFX(SFXType.Game_CoinFlip);
    }

}
