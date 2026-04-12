/// <summary>
/// 实现功能：表现层。负责硬币的正反面显示、翻面动画与当前回合高亮显示。
/// 挂在每个硬币上
/// </summary>
using System.Collections;
using UnityEngine;

public class ChessVisualController : MonoBehaviour
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
        isFrontSide = isFront;
        RefreshColor();
    }

    public void PlayFlipToFace(bool isFront)
    {
        isFrontSide = isFront;

        if (flipCoroutine != null)
        {
            StopCoroutine(flipCoroutine);
        }

        flipCoroutine = StartCoroutine(CoPlayFlip());
    }

    public void SetTurnHighlight(bool highlighted)
    {
        isHighlighted = highlighted;
        RefreshScale();
    }

    //翻面表现：先缩放X轴到flipMinScaleX，再切换颜色，最后缩放回正常
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