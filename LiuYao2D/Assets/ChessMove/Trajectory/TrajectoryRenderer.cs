using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryRenderer : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private MovementConfig config;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    public void UpdateTrajectory(Vector2 startPos, Vector2 direction, float power)
    {
        if (config == null)
        {
            Clear();
            return;
        }

        List<Vector2> points = TrajectoryPredictor.CalculatePath(
            startPos,
            direction,
            config,
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