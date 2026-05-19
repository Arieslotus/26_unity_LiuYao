/// <summary>
/// 实现功能：存储命中反馈系统使用的慢动作/短暂停顿参数，供 HitFeedbackController 读取。
/// </summary>
using UnityEngine;

[CreateAssetMenu(fileName = "HitFeedbackConfig", menuName = "Config/Hit Feedback Config")]
public class HitFeedbackConfig : ScriptableObject
{
    [Header("总开关")]
    [Tooltip("是否启用慢动作/短暂停顿效果")]
    public bool enableHitPause = true;

    [Header("翻面反馈")]
    [Tooltip("翻面时的时间缩放倍率")]
    [Range(0.01f, 1f)]
    public float flipHitPauseTimeScale = 0.08f;

    [Tooltip("翻面时的停顿持续时间（真实时间）")]
    [Min(0f)]
    public float flipHitPauseDuration = 0.06f;

    [Header("撞敌人反馈")]
    [Tooltip("是否启用撞敌人前的预慢动作")]
    public bool enableEnemyPreImpactSlowMotion = true;

    [Tooltip("预计多少秒内撞到敌人时开始预慢动作")]
    [Min(0f)]
    public float enemyPreImpactLookAheadTime = 0.14f;

    [Tooltip("撞敌人前预慢动作的时间缩放倍率")]
    [Range(0.01f, 1f)]
    public float enemyPreImpactTimeScale = 0.4f;

    [Tooltip("普通撞敌人时最轻的停顿时长")]
    [Min(0f)]
    public float enemyHitPauseMinDuration = 0.015f;

    [Tooltip("普通撞敌人时最重的停顿时长")]
    [Min(0f)]
    public float enemyHitPauseMaxDuration = 0.05f;

    [Tooltip("普通撞敌人时的时间缩放倍率")]
    [Range(0.01f, 1f)]
    public float enemyHitPauseTimeScale = 0.18f;

    [Header("己方互撞反馈")]
    [Tooltip("是否启用撞己方硬币前的预慢动作")]
    public bool enableCoinPreImpactSlowMotion = true;

    [Tooltip("预计多少秒内撞到己方硬币时开始预慢动作")]
    [Min(0f)]
    public float coinPreImpactLookAheadTime = 0.1f;

    [Tooltip("撞己方硬币前预慢动作的时间缩放倍率")]
    [Range(0.01f, 1f)]
    public float coinPreImpactTimeScale = 0.5f;

    [Tooltip("是否启用普通己方互撞命中停顿")]
    public bool enableCoinHitPause = true;

    [Tooltip("普通己方互撞时的时间缩放倍率")]
    [Range(0.01f, 1f)]
    public float coinHitPauseTimeScale = 0.25f;

    [Tooltip("普通己方互撞时的停顿持续时间")]
    [Min(0f)]
    public float coinHitPauseDuration = 0.02f;
}
