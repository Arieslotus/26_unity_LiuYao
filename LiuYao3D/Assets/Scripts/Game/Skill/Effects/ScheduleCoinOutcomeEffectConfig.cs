/// <summary>
/// 实现功能：定义内嵌技能效果“延迟执行结果”，可在指定回合开始或结束时执行通用技能结果。
/// </summary>
using System;
using UnityEngine;

[Serializable]
public sealed class ScheduleCoinOutcomeEffectConfig : CollisionSkillEffectConfig
{
    [Header("延迟")]
    [Min(0)]
    [SerializeField] private int delayRounds = 1;
    [SerializeField] private CoinSkillScheduleTiming timing = CoinSkillScheduleTiming.RoundEnded;

    [Header("执行结果")]
    [SerializeField] private CoinSkillOutcomeConfig outcome = new CoinSkillOutcomeConfig();

    public override string DisplayName => "延迟执行结果";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly ScheduleCoinOutcomeEffectConfig config;

        public Controller(ScheduleCoinOutcomeEffectConfig config)
        {
            this.config = config;
        }

        public CollisionSkillEffectExecutionResult Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[ScheduleCoinOutcomeEffectConfig] 缺少 CoinRoundEffectManager，无法安排延迟结果。");
                return CollisionSkillEffectExecutionResult.Continue;
            }

            string runtimeSourceId = context != null
                ? context.GetRuntimeSourceId(nameof(ScheduleCoinOutcomeEffectConfig))
                : nameof(ScheduleCoinOutcomeEffectConfig);

            CoinRoundEffectManager.Instance.ScheduleOutcome(
                context,
                runtimeSourceId,
                config.delayRounds,
                config.timing,
                config.outcome);

            return CollisionSkillEffectExecutionResult.Continue;
        }
    }
}
