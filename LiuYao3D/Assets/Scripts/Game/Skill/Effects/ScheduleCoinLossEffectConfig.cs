/// <summary>
/// 实现功能：定义内嵌技能效果“延迟增加损耗”，可按目标当前卦象二次校验。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ScheduleCoinLossEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector(CoinSkillTargetType.ActiveCoin);

    [Header("延迟损耗")]
    [Min(0)]
    [SerializeField] private int loss = 1;
    [Min(0)]
    [SerializeField] private int delayRounds = 1;
    [SerializeField] private TrigramType requiredCurrentTrigram = TrigramType.None;

    public override string DisplayName => "延迟增加损耗";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly ScheduleCoinLossEffectConfig config;

        public Controller(ScheduleCoinLossEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[ScheduleCoinLossEffectConfig] 缺少 CoinRoundEffectManager，无法安排延迟损耗。");
                return;
            }

            List<CoinStats> targets = config.targetSelector.Resolve(context);
            string sourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(ScheduleCoinLossEffectConfig);

            for (int i = 0; i < targets.Count; i++)
            {
                CoinRoundEffectManager.Instance.ScheduleCoinLoss(
                    targets[i],
                    config.loss,
                    config.delayRounds,
                    config.requiredCurrentTrigram,
                    sourceId);
            }
        }
    }
}
