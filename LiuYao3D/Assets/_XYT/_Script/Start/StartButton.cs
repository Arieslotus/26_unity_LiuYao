using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Scene")]
    public string sceneName;

    [Header("Idle Animation")]
    public float pulseScale = 1.05f;
    public float pulseSpeed = 2f;

    [Header("Hover")]
    public float hoverScale = 1.2f;
    public float scaleLerpSpeed = 10f;

    private RectTransform rectTransform;

    private Vector3 baseScale;
    private Vector3 targetScale;

    private bool isHover;
    private bool isLoading;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        baseScale = rectTransform.localScale;
        targetScale = baseScale;
    }

    void Update()
    {
        if (isHover)
        {
            targetScale = baseScale * hoverScale;
        }
        else
        {
            float pulse =
                1f +
                Mathf.Sin(Time.unscaledTime * pulseSpeed) *
                (pulseScale - 1f);

            targetScale = baseScale * pulse;
        }

        rectTransform.localScale =
            Vector3.Lerp(
                rectTransform.localScale,
                targetScale,
                Time.unscaledDeltaTime * scaleLerpSpeed
            );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHover = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLoading)
            return;

        isLoading = true;

        SceneManager.LoadScene(sceneName);
    }
}