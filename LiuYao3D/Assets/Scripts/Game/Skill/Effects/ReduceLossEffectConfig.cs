/// <summary>
/// 实现功能：定义内嵌技能效果“恢复损耗”，自动忽略没有损耗的硬币。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ReduceLossEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector();

    [Header("恢复")]
    [Min(0)]
    [SerializeField] private int reduceLoss = 1;

    [Header("可选特效")]
    [SerializeField] private SkillEffectVfxData vfx;

    public override string DisplayName => "恢复损耗";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly ReduceLossEffectConfig config;

        public Controller(ReduceLossEffectConfig config)
        {
            this.config = config;
        }

        public CollisionSkillEffectExecutionResult Execute(CollisionSkillContext context)
        {
            List<CoinStats> targets = config.targetSelector.Resolve(context, true);

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].ReduceLoss(config.reduceLoss);
            }

            SkillEffectVfxPlayer.PlayForCoins(config.vfx, targets);
            return CollisionSkillEffectExecutionResult.Continue;
        }
    }
}
