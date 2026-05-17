/// <summary>
/// 实现功能：管理所有卦象碰撞技能，并根据主动卦象与被动卦象查找对应技能。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrigramSkillDatabase", menuName = "Config/Trigram Skill Database")]
public class TrigramSkillDatabase : ScriptableObject
{
    [Header("所有卦象碰撞技能")]
    [SerializeField] private List<TrigramCollisionSkillSO> skills = new List<TrigramCollisionSkillSO>();

    public TrigramCollisionSkillSO GetSkill(TrigramType activeTrigram, TrigramType passiveTrigram)
    {
        foreach (var skill in skills)
        {
            if (skill == null)
                continue;

            if (skill.Match(activeTrigram, passiveTrigram))
                return skill;
        }

        return null;
    }
}