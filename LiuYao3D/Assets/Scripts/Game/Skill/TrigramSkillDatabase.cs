/// <summary>
/// 实现功能：管理所有卦象碰撞技能，并根据两枚硬币的卦象查找对应技能；可配置是否区分主动与被动。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrigramSkillDatabase", menuName = "Config/Trigram Skill Database")]
public class TrigramSkillDatabase : ScriptableObject
{
    [Header("匹配规则")]
    [Tooltip("是否区分主动卦象与被动卦象。关闭时，A 撞 B 与 B 撞 A 会共用同一个技能。")]
    [SerializeField] private bool distinguishActivePassive = false;

    [Header("所有卦象碰撞技能")]
    [SerializeField] private List<TrigramCollisionSkillSO> skills = new List<TrigramCollisionSkillSO>();

    public bool DistinguishActivePassive => distinguishActivePassive;

    public TrigramCollisionSkillSO GetSkill(TrigramType activeTrigram, TrigramType passiveTrigram)
    {
        foreach (var skill in skills)
        {
            if (skill == null)
                continue;

            if (skill.Match(activeTrigram, passiveTrigram, distinguishActivePassive))
                return skill;
        }

        return null;
    }
}
