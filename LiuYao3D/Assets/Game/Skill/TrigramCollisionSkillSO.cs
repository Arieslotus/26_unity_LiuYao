/// <summary>
/// 实现功能：定义两个硬币碰撞时，由主动卦象与被动卦象共同触发的组合技能配置。
/// </summary>
using UnityEngine;

[CreateAssetMenu(fileName = "Skill_", menuName = "Config/Trigram Collision Skill")]
public class TrigramCollisionSkillSO : ScriptableObject
{
    [Header("触发条件")]
    [SerializeField] private TrigramType activeTrigram;
    [SerializeField] private TrigramType passiveTrigram;

    [Header("技能信息")]
    [SerializeField] private string skillName;
    [SerializeField] private Sprite skillIcon;

    [TextArea]
    [SerializeField] private string effectText;

    public TrigramType ActiveTrigram => activeTrigram;
    public TrigramType PassiveTrigram => passiveTrigram;
    public string SkillName => skillName;
    public Sprite SkillIcon => skillIcon;
    public string EffectText => effectText;

    public bool Match(TrigramType active, TrigramType passive)
    {
        return activeTrigram == active && passiveTrigram == passive;
    }
}