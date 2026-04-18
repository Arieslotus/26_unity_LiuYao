using UnityEngine;

/// <summary>
/// 蓄力输入可视化
/// 1. 显示阶段1最大有效拖拽距离圆
/// 2. 显示当前有效拖拽距离线
/// </summary>
public class ChargeInputVisualizer : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private DragChargeInput input;

    [Header("线Renderer")]
    [SerializeField] private LineRenderer circleRenderer;
    [SerializeField] private LineRenderer dragLineRenderer;

    [Header("圆参数")]
    [SerializeField] private int circleSegments = 48;

    private void Awake()
    {
        if (input == null)
        {
            Debug.LogError("[ChargeInputVisualizer] 未绑定 DragChargeInput。");
        }

        if (circleRenderer == null)
        {
            Debug.LogError("[ChargeInputVisualizer] 未绑定 circleRenderer。");
        }

        if (dragLineRenderer == null)
        {
            Debug.LogError("[ChargeInputVisualizer] 未绑定 dragLineRenderer。");
        }
    }

    private void LateUpdate()
    {
        if (input == null)
            return;

        ChessPiece currentPiece = input.CurrentPiece;
        if (currentPiece == null)
        {
            HideAll();
            return;
        }


        ChargeInputConfig config = input.ChargeConfig;
        if (config == null)
        {
            HideAll();
            return;
        }

        if (!input.IsCharging)
        {
            HideAll();
            return;
        }

        Vector2 center = currentPiece.transform.position;

        DrawCircle(center, config.stage1MaxDistance);
        DrawDragLine(center, input.CurrentDirection, input.CurrentScaledDragDistance, config.stage1MaxDistance);
    }

    private void HideAll()
    {
        if (circleRenderer != null)
        {
            circleRenderer.positionCount = 0;
        }

        if (dragLineRenderer != null)
        {
            dragLineRenderer.positionCount = 0;
        }
    }

    private void DrawCircle(Vector2 center, float radius)
    {
        if (circleRenderer == null)
            return;

        if (radius <= 0f)
        {
            circleRenderer.positionCount = 0;
            return;
        }

        int segments = Mathf.Max(8, circleSegments);
        circleRenderer.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 2f;

            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            circleRenderer.SetPosition(i, point);
        }
    }

    private void DrawDragLine(Vector2 center, Vector2 direction, float scaledDistance, float maxDistance)
    {
        if (dragLineRenderer == null)
            return;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            dragLineRenderer.positionCount = 0;
            return;
        }

        float lineLength = Mathf.Clamp(scaledDistance, 0f, maxDistance);

        Vector2 dragDir = -direction; // 改为拖拽方向

        dragLineRenderer.positionCount = 2;
        dragLineRenderer.SetPosition(0, center);
        dragLineRenderer.SetPosition(1, center + dragDir.normalized * lineLength);
    }
}