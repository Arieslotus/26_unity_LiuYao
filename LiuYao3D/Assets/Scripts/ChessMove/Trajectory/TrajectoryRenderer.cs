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

        List<Vector3> points = TrajectoryPredictor.CalculatePath(
            startCenter,
            direction,
            config,
            movement.CollisionConfig,
            selfCollider,
            collisionRadius,
            power
        );

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
            p.y += lineHeight;
            line.SetPosition(i, p);
        }
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
