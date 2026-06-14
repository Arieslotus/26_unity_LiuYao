/// <summary>
/// 实现功能：定义内嵌技能效果“增加己方损耗”，损耗可被硬币保护效果抵挡。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AddCoinLossEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector();

    [Header("损耗")]
    [Min(0)]
    [SerializeField] private int loss = 1;

    public override string DisplayName => "增加己方损耗";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly AddCoinLossEffectConfig config;

        public Controller(AddCoinLossEffectConfig config)
        {
            this.config = config;
        }

        public CollisionSkillEffectExecutionResult Execute(CollisionSkillContext context)
        {
            List<CoinStats> targets = config.targetSelector.Resolve(context);

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].AddLoss(config.loss, CoinLossCause.Skill);
            }

            return CollisionSkillEffectExecutionResult.Continue;
        }
    }
}
