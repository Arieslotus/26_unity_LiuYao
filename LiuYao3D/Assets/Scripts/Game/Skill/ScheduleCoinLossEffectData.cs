/// <summary>
/// 实现功能：配置碰撞技能在指定回合为目标硬币增加损耗，并可在结算时检查目标当前卦象。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Effect_ScheduleCoinLoss_", menuName = "Config/Collision Skill Effects/Schedule Coin Loss")]
public class ScheduleCoinLossEffectData : CollisionSkillEffectData
{
    [Header("目标")]
    [SerializeField] private CollisionSkillTargetType targetType = CollisionSkillTargetType.ActiveCoin;

    [Header("延迟损耗")]
    [Min(0)]
    [SerializeField] private int loss = 1;

    [Min(0)]
    [SerializeField] private int delayRounds = 1;

    [Tooltip("非 None 时，延迟结算时仅对当前仍为该卦象的硬币生效。")]
    [SerializeField] private TrigramType requiredCurrentTrigram = TrigramType.None;

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly ScheduleCoinLossEffectData data;

        public Controller(ScheduleCoinLossEffectData data)
        {
            this.data = data;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[ScheduleCoinLossEffectData] 缺少 CoinRoundEffectManager，无法安排延迟损耗。");
                return;
            }

            List<CoinStats> targets = CollisionSkillTargetResolver.ResolveCoins(context, data.targetType);
            string sourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : data.name;

            for (int i = 0; i < targets.Count; i++)
            {
                CoinRoundEffectManager.Instance.ScheduleCoinLoss(
                    targets[i],
                    data.loss,
                    data.delayRounds,
                    data.requiredCurrentTrigram,
                    sourceId);
            }
        }
    }
}
