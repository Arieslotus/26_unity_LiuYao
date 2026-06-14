/// <summary>
/// 实现功能：定义内嵌技能效果“本回合触发次数限制”，超过阈值后执行通用技能结果。
/// </summary>
using System;
using UnityEngine;

[Serializable]
public sealed class TurnTriggerCountEffectConfig : CollisionSkillEffectConfig
{
    [Header("计数")]
    [Tooltip("为空时默认使用技能名。多个技能填同一个 ID 时会共享本回合计数。")]
    [SerializeField] private string counterId;
    [Min(0)]
    [SerializeField] private int triggerLimit = 2;
    [SerializeField] private TurnTriggerCountMode triggerMode = TurnTriggerCountMode.OncePerRoundOverLimit;

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

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[TurnTriggerCountEffectConfig] 缺少 CoinRoundEffectManager，无法记录本回合触发次数。");
                return;
            }

            string fallbackSourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(TurnTriggerCountEffectConfig);

            string runtimeCounterId = string.IsNullOrWhiteSpace(config.counterId)
                ? fallbackSourceId
                : config.counterId.Trim();

            CoinRoundEffectManager.Instance.RecordTurnTrigger(
                context,
                runtimeCounterId,
                fallbackSourceId,
                config.triggerLimit,
                config.triggerMode,
                config.overLimitOutcome);
        }
    }
}
