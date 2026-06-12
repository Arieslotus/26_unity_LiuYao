/// <summary>
/// 实现功能：定义内嵌技能效果“添加保护”，为选中硬币添加可抵挡损耗的持续保护。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GrantCoinProtectionEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector(CoinSkillTargetType.HighestLossAlly);

    [Header("保护")]
    [Min(1)]
    [SerializeField] private int durationRounds = 2;
    [Min(1)]
    [SerializeField] private int blockCount = 1;

    [Header("可选特效")]
    [SerializeField] private SkillEffectVfxData vfx;

    public override string DisplayName => "添加保护";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly GrantCoinProtectionEffectConfig config;

        public Controller(GrantCoinProtectionEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[GrantCoinProtectionEffectConfig] 缺少 CoinRoundEffectManager，无法添加保护。");
                return;
            }

            List<CoinStats> targets = config.targetSelector.Resolve(context);
            string sourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(GrantCoinProtectionEffectConfig);

            for (int i = 0; i < targets.Count; i++)
            {
                int protectionId = CoinRoundEffectManager.Instance.GrantCoinProtection(
                    targets[i],
                    config.durationRounds,
                    config.blockCount,
                    sourceId);

                SkillEffectVfxPlayer.PlayForProtection(config.vfx, targets[i], protectionId);
            }
        }
    }
}
