/// <summary>
/// 实现功能：将 Overlay Canvas 下的 UIRoot 限制到固定宽高比区域（默认16:9）。
/// </summary>

using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIAspectFitter : MonoBehaviour
{
    [Header("目标宽高比")]
    [SerializeField]
    private Vector2 targetAspect = new Vector2(16, 9);

    private RectTransform rectTransform;

    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        UpdateRect();
    }

    private void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            UpdateRect();
        }
    }

    private void UpdateRect()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float target = targetAspect.x / targetAspect.y;
        float current = (float)Screen.width / Screen.height;

        if (current > target)
        {
            // 左右黑边
            float width = target / current;

            rectTransform.anchorMin = new Vector2((1f - width) * 0.5f, 0f);
            rectTransform.anchorMax = new Vector2(1f - (1f - width) * 0.5f, 1f);
        }
        else
        {
            // 上下黑边
            float height = current / target;

            rectTransform.anchorMin = new Vector2(0f, (1f - height) * 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 1f - (1f - height) * 0.5f);
        }

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}