/// <summary>
/// 实现功能：定义两个硬币碰撞时，由主动卦象与被动卦象共同触发的组合技能配置；字段保留主从含义，匹配时可由数据库决定是否区分主从。
/// </summary>
#if UNITY_EDITOR
using UnityEditor;
#endif
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
    [SerializeField] private string description;

    [TextArea]
    [SerializeField] private string effectText;

    public TrigramType ActiveTrigram => activeTrigram;
    public TrigramType PassiveTrigram => passiveTrigram;
    public string SkillName => skillName;
    public Sprite SkillIcon => skillIcon;
    public string Description => description;
    public string EffectText => effectText;

    public bool Match(TrigramType active, TrigramType passive)
    {
        return activeTrigram == active && passiveTrigram == passive;
    }

    public bool Match(TrigramType active, TrigramType passive, bool distinguishActivePassive)
    {
        if (distinguishActivePassive)
        {
            return Match(active, passive);
        }

        return (activeTrigram == active && passiveTrigram == passive) ||
            (activeTrigram == passive && passiveTrigram == active);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoFillSkillIcon();
    }

    private void AutoFillSkillIcon()
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return;

        string iconPath = $"Assets/Resources/UI/SkillPopup/{skillName.Trim()}.png";
        Sprite matchedIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

        if (matchedIcon == null)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(iconPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    matchedIcon = sprite;
                    break;
                }
            }
        }

        if (matchedIcon == null || skillIcon == matchedIcon)
            return;

        skillIcon = matchedIcon;
        EditorUtility.SetDirty(this);
    }
#endif
}
