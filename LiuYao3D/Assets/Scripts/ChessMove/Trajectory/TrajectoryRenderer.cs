/// <summary>
/// 实现功能：根据预测路径绘制3D轨迹（XZ平面），使用 LineRenderer 显示；碰撞体中心与半径统一读取 MovementController。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryRenderer : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private MovementConfig config;
    [SerializeField] private MovementController movement;

    [Header("显示")]
    [SerializeField] private float lineHeight = 0.05f;

    [Tooltip("是否使用固定世界 Y 高度显示轨迹线，避免硬币翻面或碰撞体高度变化影响轨迹高度。")]
    [SerializeField] private bool useFixedWorldY = true;

    [Tooltip("轨迹线固定显示的世界 Y 坐标。")]
    [SerializeField] private float fixedWorldY = 0.08f;

    [Tooltip("完整显示的最大碰撞点数量。当前需求为最多显示两个碰撞点。")]
    [Min(1)]
    [SerializeField] private int maxVisibleCollisionPointCount = 2;

    [Tooltip("当轨迹包含下一个碰撞点时，保留最后一个可见碰撞点到下一个碰撞点之间的百分比。")]
    [Range(0f, 1f)]
    [SerializeField] private float overflowSegmentPercent = 0.35f;

    //[SerializeField] private int cornerVertices = 6;
    //[SerializeField] private int capVertices = 4;
    //[SerializeField] private LineAlignment lineAlignment = LineAlignment.View;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        Clear();

        if (movement == null)
        {
            movement = GetComponent<MovementController>();
        }

        if (movement == null)
        {
            Debug.LogError($"[TrajectoryRenderer] {name} 缺少 MovementController，轨迹预测无法统一碰撞体。");
        }

        line.numCornerVertices = 6;
        line.numCapVertices = 4;
        line.alignment = LineAlignment.View;
    }

    public void UpdateTrajectory(Vector3 direction, float power)
    {
        if (!EnsureLineRenderer())
            return;

        if (config == null || movement == null || movement.CollisionConfig == null)
        {
            Clear();
            return;
        }

        Vector3 startCenter = movement.GetCollisionCenter();
        float collisionRadius = movement.GetCollisionRadius();
        Collider selfCollider = movement.selfCollider;

        List<TrajectoryPathPoint> rawPoints = TrajectoryPredictor.CalculatePathWithCollisionInfo(
            startCenter,
            direction,
            config,
            movement.CollisionConfig,
            selfCollider,
            collisionRadius,
            power,
            20,
            movement
        );

        List<Vector3> points = BuildVisiblePoints(rawPoints);

        if (points == null || points.Count == 0)
        {
            Clear();
            return;
        }

        line.enabled = true;
        line.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p = points[i];
            p.y = useFixedWorldY ? fixedWorldY : p.y + lineHeight;
            line.SetPosition(i, p);
        }
    }

    private List<Vector3> BuildVisiblePoints(List<TrajectoryPathPoint> rawPoints)
    {
        List<Vector3> visiblePoints = new List<Vector3>();

        if (rawPoints == null || rawPoints.Count == 0)
            return visiblePoints;

        int collisionCount = 0;
        int lastVisibleCollisionIndex = -1;

        for (int i = 0; i < rawPoints.Count; i++)
        {
            TrajectoryPathPoint point = rawPoints[i];

            if (!point.IsCollisionPoint)
            {
                visiblePoints.Add(point.Position);
                continue;
            }

            collisionCount++;

            if (collisionCount <= Mathf.Max(1, maxVisibleCollisionPointCount))
            {
                visiblePoints.Add(point.Position);
                lastVisibleCollisionIndex = i;
                continue;
            }

            if (lastVisibleCollisionIndex >= 0)
            {
                Vector3 from = rawPoints[lastVisibleCollisionIndex].Position;
                Vector3 to = point.Position;
                Vector3 partialPoint = Vector3.Lerp(from, to, overflowSegmentPercent);
                visiblePoints.Add(partialPoint);
            }

            break;
        }

        return visiblePoints;
    }

    public void Clear()
    {
        if (!EnsureLineRenderer())
            return;

        line.positionCount = 0;
        line.enabled = false;
    }

    private bool EnsureLineRenderer()
    {
        if (line != null)
            return true;

        if (!this)
            return false;

        line = GetComponent<LineRenderer>();
        return line != null;
    }
}
