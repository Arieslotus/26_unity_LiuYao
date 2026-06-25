/// <summary>
/// 实现功能：UI透明度循环闪烁，支持淡入、淡出以及亮暗停留时间。
/// </summary>

using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIAlphaPulse : MonoBehaviour
{
    [Header("透明度范围")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 1f;

    [Header("变化时长")]
    [SerializeField] private float fadeInDuration = 0.3f;

    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("停留时间")]
    [SerializeField] private float stayBrightDuration = 0.5f;

    [SerializeField] private float stayDarkDuration = 0.5f;

    private CanvasGroup canvasGroup;

    private float timer;

    private enum PulseState
    {
        FadeIn,
        StayBright,
        FadeOut,
        StayDark
    }

    private PulseState currentState = PulseState.FadeIn;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = minAlpha;
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;

        switch (currentState)
        {
            case PulseState.FadeIn:
                UpdateFadeIn();
                break;

            case PulseState.StayBright:
                UpdateStayBright();
                break;

            case PulseState.FadeOut:
                UpdateFadeOut();
                break;

            case PulseState.StayDark:
                UpdateStayDark();
                break;
        }
    }

    private void UpdateFadeIn()
    {
        float t = Mathf.Clamp01(timer / fadeInDuration);

        canvasGroup.alpha =
            Mathf.Lerp(minAlpha, maxAlpha, t);

        if (t >= 1f)
        {
            EnterState(PulseState.StayBright);
        }
    }

    private void UpdateStayBright()
    {
        canvasGroup.alpha = maxAlpha;

        if (timer >= stayBrightDuration)
        {
            EnterState(PulseState.FadeOut);
        }
    }

    private void UpdateFadeOut()
    {
        float t = Mathf.Clamp01(timer / fadeOutDuration);

        canvasGroup.alpha =
            Mathf.Lerp(maxAlpha, minAlpha, t);

        if (t >= 1f)
        {
            EnterState(PulseState.StayDark);
        }
    }

    private void UpdateStayDark()
    {
        canvasGroup.alpha = minAlpha;

        if (timer >= stayDarkDuration)
        {
            EnterState(PulseState.FadeIn);
        }
    }

    private void EnterState(PulseState newState)
    {
        currentState = newState;
        timer = 0f;
    }

    private void OnDisable()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = maxAlpha;
        }
    }
}