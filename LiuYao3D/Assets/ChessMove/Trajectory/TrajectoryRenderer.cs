/// <summary>
/// 实现功能：根据预测路径绘制3D轨迹（XZ平面），使用LineRenderer显示。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryRenderer : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private MovementConfig config;
    [SerializeField] private CollisionConfig collisionConfig;

    [Header("显示")]
    [SerializeField] private float lineHeight = 0.05f; // 防止Z-Fighting

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
    }

    public void UpdateTrajectory(Vector3 startPos, Vector3 direction, float power, Collider selfCollider)
    {
        if (config == null || collisionConfig == null)
        {
            Clear();
            return;
        }
        float collisionRadius = GetColliderRadius(selfCollider);

        List<Vector3> points = TrajectoryPredictor.CalculatePath(
            startPos,
            direction,
            config,
            collisionConfig,
            selfCollider,
            collisionRadius,
            power
        );

        if (points == null || points.Count == 0)
        {
            Clear();
            return;
        }

        line.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p = points[i];
            p.y += lineHeight; // 防止贴地闪烁
            line.SetPosition(i, p);
        }
    }

    public void Clear()
    {
        line.positionCount = 0;
    }

    private float GetColliderRadius(Collider collider)
    {
        if (collider is SphereCollider sphere)
        {
            Vector3 scale = sphere.transform.lossyScale;
            float maxScale = Mathf.Max(scale.x, scale.y, scale.z);
            return sphere.radius * maxScale;
        }

        return config != null ? config.radius : 0.5f;
    }
}