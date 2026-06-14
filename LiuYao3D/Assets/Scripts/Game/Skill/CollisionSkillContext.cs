/// <summary>
/// 实现功能：保存一次硬币碰撞技能触发时的完整快照，供即时与延迟效果稳定读取。
/// </summary>
using UnityEngine;
using System.Collections.Generic;

public sealed class CollisionSkillContext
{
    public TrigramCollisionSkillSO skill;
    public string runtimeEffectId;
    public ChessPiece activePiece;
    public ChessPiece passivePiece;
    public CoinStats activeStats;
    public CoinStats passiveStats;
    public TrigramType activeTrigram;
    public TrigramType passiveTrigram;
    public int activeAttackSnapshot;
    public int passiveAttackSnapshot;
    public Vector3 collisionPosition;
    public int triggeredRound;
    public readonly List<EnemyStats> lastDamagedEnemies = new List<EnemyStats>();

    public string GetRuntimeSourceId(string fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(runtimeEffectId))
            return runtimeEffectId;

        if (skill != null && !string.IsNullOrWhiteSpace(skill.SkillName))
            return skill.SkillName;

        return string.IsNullOrWhiteSpace(fallbackName) ? "CollisionSkillEffect" : fallbackName;
    }
}
