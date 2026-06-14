/// <summary>
/// 实现功能：定义内嵌技能效果“临时碰撞物理修正”，按倍率调整硬币碰撞传递或自身剩余路径。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CoinPhysicsModifierEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector();

    [Header("物理修正")]
    [SerializeField] private CoinPhysicsModifierType modifierType = CoinPhysicsModifierType.CoinTransferDistance;
    [Min(0f)]
    [SerializeField] private float multiplier = 1.3f;

    public override string DisplayName => "临时碰撞物理修正";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly CoinPhysicsModifierEffectConfig config;

        public Controller(CoinPhysicsModifierEffectConfig config)
        {
            this.config = config;
        }

        public CollisionSkillEffectExecutionResult Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[CoinPhysicsModifierEffectConfig] 缺少 CoinRoundEffectManager，无法添加临时物理修正。");
                return CollisionSkillEffectExecutionResult.Continue;
            }

            string sourceId = context != null
                ? context.GetRuntimeSourceId(nameof(CoinPhysicsModifierEffectConfig))
                : nameof(CoinPhysicsModifierEffectConfig);

            List<CoinStats> targets = config.targetSelector.Resolve(context);
            CoinRoundEffectManager.Instance.AddPhysicsModifier(
                sourceId,
                config.modifierType,
                config.multiplier,
                targets);

            return CollisionSkillEffectExecutionResult.Continue;
        }
    }
}
