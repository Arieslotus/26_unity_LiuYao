/// <summary>
/// 实现功能：配置敌人护盾生成、减伤，以及被不同卦象硬币命中或技能影响时累计的破盾值。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyShieldBreakConfig_", menuName = "Config/Enemy Shield Break Config")]
public class EnemyShieldBreakConfigSO : ScriptableObject
{
    [Serializable]
    private class ShieldBreakOverride
    {
        [Tooltip("触发破盾累计的硬币或冲击波卦象。")]
        public TrigramType triggerTrigram = TrigramType.None;

        [Min(0)]
        [Tooltip("该卦象对当前护盾累计的破盾值。0 表示不会推进破盾。")]
        public int breakValue = 1;
    }

    [Serializable]
    private class ShieldBreakRule
    {
        [Tooltip("被攻击的护盾卦象。")]
        public TrigramType shieldType = TrigramType.None;

        [Tooltip("针对该护盾的特殊破盾值覆盖。未配置时使用默认规则。")]
        public List<ShieldBreakOverride> overrides = new List<ShieldBreakOverride>();

        public bool TryGetBreakValue(TrigramType triggerTrigram, out int breakValue)
        {
            breakValue = 0;

            if (triggerTrigram == TrigramType.None || overrides == null)
                return false;

            for (int i = 0; i < overrides.Count; i++)
            {
                ShieldBreakOverride entry = overrides[i];
                if (entry == null || entry.triggerTrigram != triggerTrigram)
                    continue;

                breakValue = Mathf.Max(0, entry.breakValue);
                return true;
            }

            return false;
        }
    }

    [Header("生成与减伤")]
    [Min(1)]
    [Tooltip("每经过多少个大回合尝试生成一次护盾。已有护盾时不会生成新护盾。")]
    [SerializeField] private int shieldIntervalRounds = 2;

    [Range(0f, 1f)]
    [Tooltip("持有护盾时受到的伤害减免比例。0 表示不减伤，1 表示完全免伤。")]
    [SerializeField] private float damageReductionPercent = 0.5f;

    [Tooltip("第一回合开始时是否立即生成一次护盾。之后再按护盾生成间隔计数。")]
    [SerializeField] private bool generateShieldOnFirstRound = true;

    [Header("默认破盾规则")]
    [Min(1)]
    [SerializeField] private int requiredBreakValue = 3;

    [Min(0)]
    [SerializeField] private int sameTrigramBreakValue = 3;

    [Min(0)]
    [SerializeField] private int differentTrigramBreakValue = 1;

    [Header("特殊规则")]
    [SerializeField] private List<ShieldBreakRule> shieldRules = new List<ShieldBreakRule>();

    public int ShieldIntervalRounds => Mathf.Max(1, shieldIntervalRounds);
    public float DamageReductionPercent => Mathf.Clamp01(damageReductionPercent);
    public bool GenerateShieldOnFirstRound => generateShieldOnFirstRound;
    public int RequiredBreakValue => Mathf.Max(1, requiredBreakValue);

    public int GetBreakValue(TrigramType shieldType, TrigramType triggerTrigram)
    {
        if (shieldType == TrigramType.None || triggerTrigram == TrigramType.None)
            return 0;

        if (TryGetOverrideValue(shieldType, triggerTrigram, out int overrideValue))
            return overrideValue;

        return triggerTrigram == shieldType
            ? Mathf.Max(0, sameTrigramBreakValue)
            : Mathf.Max(0, differentTrigramBreakValue);
    }

    private bool TryGetOverrideValue(TrigramType shieldType, TrigramType triggerTrigram, out int breakValue)
    {
        breakValue = 0;

        if (shieldRules == null)
            return false;

        for (int i = 0; i < shieldRules.Count; i++)
        {
            ShieldBreakRule rule = shieldRules[i];
            if (rule == null || rule.shieldType != shieldType)
                continue;

            return rule.TryGetBreakValue(triggerTrigram, out breakValue);
        }

        return false;
    }
}
