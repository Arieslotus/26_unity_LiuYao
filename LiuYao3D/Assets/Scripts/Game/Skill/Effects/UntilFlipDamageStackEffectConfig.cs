/// <summary>
/// 实现功能：定义内嵌技能效果“直到翻面停止叠增伤”，每回合叠加并在目标翻面后停止。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class UntilFlipDamageStackEffectConfig : CollisionSkillEffectConfig
{
    [Header("监听目标")]
    [SerializeField] private CoinSkillTargetSelector watchedTargetSelector = new CoinSkillTargetSelector();

    [Header("叠层")]
    [Min(1)]
    [SerializeField] private int maxStacks = 3;
    [SerializeField] private CoinSkillOutcomeConfig stackOutcome = new CoinSkillOutcomeConfig();

    public override string DisplayName => "直到翻面停止叠增伤";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly UntilFlipDamageStackEffectConfig config;

        public Controller(UntilFlipDamageStackEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[UntilFlipDamageStackEffectConfig] 缺少 CoinRoundEffectManager，无法添加直到翻面的叠层效果。");
                return;
            }

            List<CoinStats> watchedTargets = config.watchedTargetSelector.Resolve(context);

            string sourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(UntilFlipDamageStackEffectConfig);

            CoinRoundEffectManager.Instance.StartUntilFlipDamageStacks(
                context,
                sourceId,
                watchedTargets,
                config.maxStacks,
                config.stackOutcome);
        }
    }
}
