/// <summary>
/// 实现功能：统一配置敌人出生点的候选点生成、地面检测与阻挡检测参数。
/// </summary>
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnPointConfig_", menuName = "Config/Enemy Spawn Point Config")]
public class EnemySpawnPointConfigSO : ScriptableObject
{
    [Header("候选点")]
    [Min(0.1f)]
    [Tooltip("出生点周围用于重排单位的最大半径。")]
    public float spawnRadius = 3f;

    [Min(0.1f)]
    [Tooltip("环形候选点之间的大致间距。")]
    public float pointSpacing = 1f;

    [Header("地面检测")]
    [Tooltip("八边形地面所在 Layer。生成与重排位置必须完整落在该地面 Collider 上。")]
    public LayerMask groundMask;

    [Tooltip("地面检测射线从候选点上方多高开始。")]
    public float groundProbeHeight = 5f;

    [Tooltip("地面检测射线从候选点上方向下额外检测多远。")]
    public float groundProbeDistance = 10f;

    [Range(6, 24)]
    [Tooltip("检测单位圆形占位是否完整落在地面上时，圆周采样数量。")]
    public int groundProbeSegments = 12;

    [Header("阻挡检测")]
    [Tooltip("是否把 Trigger Collider 也视为出生/重排阻挡。")]
    public bool includeTriggerColliders;

    [Header("调试")]
    [Tooltip("选中出生点时是否绘制候选点 Gizmos。")]
    public bool drawGizmos = true;
}
