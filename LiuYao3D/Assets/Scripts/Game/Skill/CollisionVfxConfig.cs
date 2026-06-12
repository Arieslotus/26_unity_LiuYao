/// <summary>
/// 实现功能：配置硬币碰撞、冲击波与通用受伤特效使用的预制体和播放参数。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CollisionVfxConfig", menuName = "Config/Collision VFX Config")]
public class CollisionVfxConfig : ScriptableObject
{
    [System.Serializable]
    public sealed class TrigramVfxEntry
    {
        public TrigramType trigram = TrigramType.None;
        public GameObject prefab;
    }

    [Header("基础碰撞特效")]
    [Tooltip("硬币与硬币碰撞时播放的通用碰撞特效。")]
    public GameObject coinCollisionPrefab;

    [Tooltip("硬币与敌人碰撞时播放的通用碰撞特效。为空时使用硬币碰撞特效。")]
    public GameObject coinEnemyCollisionPrefab;

    [Tooltip("基础碰撞特效自动销毁时间。小于等于 0 时不自动销毁。")]
    [Min(0f)]
    public float collisionLifetime = 2f;

    [Header("八卦冲击波")]
    [Tooltip("每个八卦对应的冲击波特效预制体。")]
    public List<TrigramVfxEntry> shockwavePrefabs = new List<TrigramVfxEntry>();

    [Tooltip("两道冲击波之间的播放间隔。")]
    [Min(0f)]
    public float shockwaveInterval = 0.1f;

    [Tooltip("冲击波特效自动销毁时间。小于等于 0 时不自动销毁。")]
    [Min(0f)]
    public float shockwaveLifetime = 2f;

    [Header("通用受伤特效")]
    [Tooltip("敌人受到有效伤害时播放。包括硬币碰撞伤害和技能伤害。")]
    public GameObject enemyDamagedPrefab;

    [Tooltip("硬币受到敌方攻击或技能损耗时播放。不包含玩家操作后的自然损耗。")]
    public GameObject coinDamagedPrefab;

    [Tooltip("受伤特效自动销毁时间。小于等于 0 时不自动销毁。")]
    [Min(0f)]
    public float damagedLifetime = 2f;

    [Header("生成参数")]
    [Tooltip("所有特效生成时追加的世界坐标偏移。")]
    public Vector3 worldOffset = Vector3.zero;

    [Tooltip("受伤特效生成时是否挂到受击目标下。")]
    public bool parentDamagedVfxToTarget;

    [Tooltip("生成特效后，自动将预制体内所有 ParticleSystem 设置为不受 Time.timeScale 影响。")]
    public bool useUnscaledTimeForParticles = true;

    public GameObject GetShockwavePrefab(TrigramType trigram)
    {
        for (int i = 0; i < shockwavePrefabs.Count; i++)
        {
            TrigramVfxEntry entry = shockwavePrefabs[i];
            if (entry != null && entry.trigram == trigram)
            {
                return entry.prefab;
            }
        }

        return null;
    }
}
