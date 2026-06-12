/// <summary>
/// 实现功能：定义内嵌技能效果“添加增伤”，支持延迟生效、持续回合与叠层。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AddDamageModifierEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector();

    [Header("增伤")]
    [SerializeField] private float addDamagePercent = 30f;
    [SerializeField] private int durationRounds = -1;
    [Min(0)]
    [SerializeField] private int activateAfterRounds;
    [SerializeField] private bool stackable = true;
    [SerializeField] private string modifierId;

    [Header("可选特效")]
    [SerializeField] private SkillEffectVfxData vfx;

    public override string DisplayName => "添加增伤";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly AddDamageModifierEffectConfig config;

        public Controller(AddDamageModifierEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[AddDamageModifierEffectConfig] 缺少 CoinRoundEffectManager，无法添加增伤。");
                return;
            }

            string sourceId = string.IsNullOrWhiteSpace(config.modifierId)
                ? (context != null && context.skill != null ? context.skill.SkillName : nameof(AddDamageModifierEffectConfig))
                : config.modifierId.Trim();

            List<CoinStats> targets = config.targetSelector.Resolve(context);

            int runtimeModifierId = CoinRoundEffectManager.Instance.AddDamageModifier(
                sourceId,
                config.addDamagePercent / 100f,
                config.durationRounds,
                config.activateAfterRounds,
                config.stackable,
                targets);

            SkillEffectVfxPlayer.PlayForDamageModifierTargets(config.vfx, targets, runtimeModifierId, config.activateAfterRounds);
        }
    }
}
