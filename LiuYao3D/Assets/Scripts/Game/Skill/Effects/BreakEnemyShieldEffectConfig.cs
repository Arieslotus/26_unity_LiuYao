/// <summary>
/// 实现功能：定义内嵌技能效果“破除敌方护盾”，支持碰撞参与卦象或指定卦象破盾。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BreakEnemyShieldEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private EnemySkillTargetSelector targetSelector = new EnemySkillTargetSelector();

    [Header("可破护盾卦象")]
    [SerializeField] private BreakShieldTrigramMode trigramMode = BreakShieldTrigramMode.CollisionParticipants;
    [SerializeField] private List<TrigramType> specifiedTrigrams = new List<TrigramType>();

    public override string DisplayName => "破除敌方护盾";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly BreakEnemyShieldEffectConfig config;

        public Controller(BreakEnemyShieldEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            List<EnemyStats> targets = config.targetSelector.Resolve(context);

            string sourceName = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(BreakEnemyShieldEffectConfig);

            for (int i = 0; i < targets.Count; i++)
            {
                EnemyShieldController shield = targets[i].GetComponent<EnemyShieldController>();
                if (shield == null || !shield.HasShield)
                    continue;

                if (!config.CanBreakShield(context, shield.CurrentShieldType))
                    continue;

                shield.TryBreakShield(shield.CurrentShieldType, sourceName);
            }
        }
    }

    private bool CanBreakShield(CollisionSkillContext context, TrigramType shieldType)
    {
        if (shieldType == TrigramType.None)
            return false;

        if (trigramMode == BreakShieldTrigramMode.SpecifiedTrigrams)
            return specifiedTrigrams != null && specifiedTrigrams.Contains(shieldType);

        if (context == null)
            return false;

        return shieldType == context.activeTrigram || shieldType == context.passiveTrigram;
    }
}
