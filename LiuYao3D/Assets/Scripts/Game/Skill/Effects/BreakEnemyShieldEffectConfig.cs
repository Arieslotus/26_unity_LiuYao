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

        public CollisionSkillEffectExecutionResult Execute(CollisionSkillContext context)
        {
            List<EnemyStats> targets = config.targetSelector.Resolve(context);

            string sourceName = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(BreakEnemyShieldEffectConfig);

            for (int i = 0; i < targets.Count; i++)
            {
                EnemyStats target = targets[i];
                if (target == null)
                    continue;

                EnemyShieldController shield = target.GetComponentInChildren<EnemyShieldController>(true);
                if (shield == null)
                {
                    Debug.LogWarning($"[BreakEnemyShieldEffectConfig] 技能目标未找到护盾控制器 | skill:{sourceName} | enemy:{target.name}");
                    continue;
                }

                if (!shield.HasShield)
                    continue;

                config.ApplyBreakValue(context, shield, sourceName);
            }

            return CollisionSkillEffectExecutionResult.Continue;
        }
    }

    private void ApplyBreakValue(CollisionSkillContext context, EnemyShieldController shield, string sourceName)
    {
        if (shield == null || !shield.HasShield)
            return;

        if (trigramMode == BreakShieldTrigramMode.SpecifiedTrigrams)
        {
            ApplySpecifiedTrigrams(shield, sourceName);
            return;
        }

        if (context == null)
            return;

        ApplyCollisionParticipantTrigram(shield, context.activeTrigram, sourceName);

        if (context.passiveTrigram != context.activeTrigram)
        {
            ApplyCollisionParticipantTrigram(shield, context.passiveTrigram, sourceName);
        }
    }

    private void ApplySpecifiedTrigrams(EnemyShieldController shield, string sourceName)
    {
        if (specifiedTrigrams == null)
            return;

        for (int i = 0; i < specifiedTrigrams.Count; i++)
        {
            ApplyCollisionParticipantTrigram(shield, specifiedTrigrams[i], sourceName);

            if (shield == null || !shield.HasShield)
                return;
        }
    }

    private void ApplyCollisionParticipantTrigram(EnemyShieldController shield, TrigramType trigram, string sourceName)
    {
        if (shield == null || !shield.HasShield || trigram == TrigramType.None)
            return;

        shield.TryApplyShieldBreak(trigram, sourceName);
    }
}
