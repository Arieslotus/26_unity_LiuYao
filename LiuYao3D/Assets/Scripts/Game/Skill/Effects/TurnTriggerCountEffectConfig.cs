/// <summary>
/// 实现功能：定义内嵌技能效果“本回合触发次数限制”，超过阈值后执行通用技能结果或停止技能。
/// </summary>
using System;
using UnityEngine;

[Serializable]
public sealed class TurnTriggerCountEffectConfig : CollisionSkillEffectConfig
{
    [Header("计数")]
    [Min(0)]
    [SerializeField] private int triggerLimit = 2;
    [SerializeField] private TurnTriggerCountMode triggerMode = TurnTriggerCountMode.OncePerRoundOverLimit;
    [SerializeField] private TurnTriggerOverLimitAction overLimitAction = TurnTriggerOverLimitAction.RunOutcomeAndContinue;

    [Header("超过限制时")]
    [SerializeField] private CoinSkillOutcomeConfig overLimitOutcome = new CoinSkillOutcomeConfig();

    public override string DisplayName => "本回合触发次数限制";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly TurnTriggerCountEffectConfig config;

        public Controller(TurnTriggerCountEffectConfig config)
        {
            this.config = config;
        }

        public CollisionSkillEffectExecutionResult Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[TurnTriggerCountEffectConfig] 缺少 CoinRoundEffectManager，无法记录本回合触发次数。");
                return CollisionSkillEffectExecutionResult.Continue;
            }

            string sourceId = context != null
                ? context.GetRuntimeSourceId(nameof(TurnTriggerCountEffectConfig))
                : nameof(TurnTriggerCountEffectConfig);

            bool isOverLimit = CoinRoundEffectManager.Instance.RecordTurnTrigger(
                context,
                sourceId,
                sourceId,
                config.triggerLimit,
                config.triggerMode,
                config.overLimitAction,
                config.overLimitOutcome);

            if (!isOverLimit)
                return CollisionSkillEffectExecutionResult.Continue;

            if (config.overLimitAction == TurnTriggerOverLimitAction.RunOutcomeAndStopSkill ||
                config.overLimitAction == TurnTriggerOverLimitAction.StopSkillOnly)
            {
                return CollisionSkillEffectExecutionResult.StopSkill;
            }

            return CollisionSkillEffectExecutionResult.Continue;
        }
    }
}
