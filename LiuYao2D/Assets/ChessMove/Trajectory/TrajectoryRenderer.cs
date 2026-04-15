using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(Collider2D))]
public class TrajectoryRenderer : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private MovementConfig config;
    [SerializeField] private CollisionConfig collisionConfig;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    public void UpdateTrajectory(Vector2 startPos, Vector2 direction, float power, Collider2D selfCollider)
    {
        if (config == null || collisionConfig == null)
        {
            Clear();
            return;
        }

        List<Vector2> points = TrajectoryPredictor.CalculatePath(
            startPos,
            direction,
            config,
            collisionConfig,
            selfCollider,
            power
        );

        if (points == null || points.Count <= 0)
        {
            Clear();
            return;
        }

        line.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(i, points[i]);
        }
    }

    public void Clear()
    {
        line.positionCount = 0;
    }
}