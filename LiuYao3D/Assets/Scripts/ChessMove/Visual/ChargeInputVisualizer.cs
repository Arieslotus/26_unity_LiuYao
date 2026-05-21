/// <summary>
/// 实现功能：显示蓄力输入可视化，包括阶段1最大有效拖拽圆，以及当前有效拖拽距离线。
/// 适配 3D 项目，逻辑绘制平面为 XZ，Y 轴仅作为显示高度偏移。（会随着相机视角倾斜而倾斜）
/// </summary>
using UnityEngine;

public class ChargeInputVisualizer : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private DragChargeInput input;

    [Header("线Renderer")]
    [SerializeField] private LineRenderer circleRenderer;
    [SerializeField] private LineRenderer dragLineRenderer;

    [Header("圆参数")]
    [SerializeField] private int circleSegments = 48;

    [Header("显示参数")]
    [Tooltip("可视化线条离地高度，避免与地面重叠闪烁")]
    [SerializeField] private float visualHeight = 0.05f;

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

        SetupRenderer(circleRenderer);
        SetupRenderer(dragLineRenderer);
    }

    private void LateUpdate()
    {
        if (input == null)
        {
            HideAll();
            return;
        }

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

        Vector3 center = currentPiece.transform.position;
        center.y += visualHeight;

        float visualLength = Mathf.Clamp(input.CurrentScaledDragDistance, 0f, config.stage1MaxDistance);

        DrawCircle(center, visualLength);
        DrawDragLine(center, input.CurrentDirection, visualLength);
    }

    private void SetupRenderer(LineRenderer lineRenderer)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.useWorldSpace = true;
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

    private void DrawCircle(Vector3 center, float radius)
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

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            Vector3 point = center + new Vector3(x, 0f, z);
            circleRenderer.SetPosition(i, point);
        }
    }

    private void DrawDragLine(Vector3 center, Vector3 direction, float lineLength)
    {
        if (dragLineRenderer == null)
            return;

        Vector3 flatDirection = direction;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            dragLineRenderer.positionCount = 0;
            return;
        }

        // 显示“拖拽方向”，因此取发射方向的反向
        Vector3 dragDir = -flatDirection.normalized;

        dragLineRenderer.positionCount = 2;
        dragLineRenderer.SetPosition(0, center);
        dragLineRenderer.SetPosition(1, center + dragDir * lineLength);
    }
}
