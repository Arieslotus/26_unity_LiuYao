/// <summary>
/// 实现功能：定义内嵌技能效果“停止敌方护盾生成”，在指定轮数内阻止敌方生成新护盾。
/// </summary>
using System;
using UnityEngine;

[Serializable]
public sealed class BlockEnemyShieldGenerationEffectConfig : CollisionSkillEffectConfig
{
    [Header("持续")]
    [Min(1)]
    [SerializeField] private int roundCount = 1;

    public override string DisplayName => "停止敌方护盾生成";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly BlockEnemyShieldGenerationEffectConfig config;

        public Controller(BlockEnemyShieldGenerationEffectConfig config)
        {
            this.config = config;
        }

        public CollisionSkillEffectExecutionResult Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[BlockEnemyShieldGenerationEffectConfig] 缺少 CoinRoundEffectManager，无法停止敌方护盾生成。");
                return CollisionSkillEffectExecutionResult.Continue;
            }

            string runtimeSourceId = context != null
                ? context.GetRuntimeSourceId(nameof(BlockEnemyShieldGenerationEffectConfig))
                : nameof(BlockEnemyShieldGenerationEffectConfig);

            CoinRoundEffectManager.Instance.BlockEnemyShieldGeneration(
                config.roundCount,
                runtimeSourceId,
                context != null ? context.skill : null);

            return CollisionSkillEffectExecutionResult.Continue;
        }
    }
}
