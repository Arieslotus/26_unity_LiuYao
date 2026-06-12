/// <summary>
/// 实现功能：定义内嵌技能效果“翻面条件”，在延迟回合检查硬币是否翻面并执行对应结果。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CoinFlipConditionEffectConfig : CollisionSkillEffectConfig
{
    [Header("监听目标")]
    [SerializeField] private CoinSkillTargetSelector watchedTargetSelector = new CoinSkillTargetSelector();

    [Header("检查")]
    [Tooltip("1 表示本大回合 RoundEnded 检查；2 表示下一个大回合 RoundEnded 检查。")]
    [Min(1)]
    [SerializeField] private int roundEndChecks = 1;
    [SerializeField] private bool requireNoFlip = true;

    [Header("满足条件时")]
    [SerializeField] private CoinSkillOutcomeConfig successOutcome = new CoinSkillOutcomeConfig();

    [Header("不满足条件时")]
    [SerializeField] private CoinSkillOutcomeConfig failureOutcome = new CoinSkillOutcomeConfig();

    public override string DisplayName => "翻面条件";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly CoinFlipConditionEffectConfig config;

        public Controller(CoinFlipConditionEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[CoinFlipConditionEffectConfig] 缺少 CoinRoundEffectManager，无法监听翻面条件。");
                return;
            }

            List<CoinStats> watchedTargets = config.watchedTargetSelector.Resolve(context);

            string sourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(CoinFlipConditionEffectConfig);

            CoinRoundEffectManager.Instance.ScheduleFlipCondition(
                context,
                sourceId,
                watchedTargets,
                config.roundEndChecks,
                config.requireNoFlip,
                config.successOutcome,
                config.failureOutcome);
        }
    }
}
