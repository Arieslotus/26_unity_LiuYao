
    /// <summary>
/// 实现功能：固定相机宽高比，在不同屏幕下自动添加黑边（Letterbox/Pillarbox）。
/// </summary>

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraLetterbox : MonoBehaviour
{
    [Header("目标宽高比")]
    [Tooltip("默认16:9")]
    [SerializeField]
    private Vector2 targetAspect = new Vector2(16, 9);

    private Camera cam;

    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        UpdateViewport();
    }

    private void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            UpdateViewport();
        }
    }

    private void UpdateViewport()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float target = targetAspect.x / targetAspect.y;
        float current = (float)Screen.width / Screen.height;

        Rect rect = new Rect();

        if (current > target)
        {
            // 屏幕更宽：左右黑边
            float width = target / current;

            rect.width = width;
            rect.height = 1f;
            rect.x = (1f - width) * 0.5f;
            rect.y = 0f;
        }
        else
        {
            // 屏幕更高：上下黑边
            float height = current / target;

            rect.width = 1f;
            rect.height = height;
            rect.x = 0f;
            rect.y = (1f - height) * 0.5f;
        }

        cam.rect = rect;
    }
}
