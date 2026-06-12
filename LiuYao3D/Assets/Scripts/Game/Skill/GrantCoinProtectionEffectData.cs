/// <summary>
/// 实现功能：配置碰撞技能为指定己方硬币添加临时伤害抵挡效果。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Effect_GrantCoinProtection_", menuName = "Config/Collision Skill Effects/Grant Coin Protection")]
public class GrantCoinProtectionEffectData : CollisionSkillEffectData
{
    [Header("目标")]
    [SerializeField] private CollisionSkillTargetType targetType = CollisionSkillTargetType.HighestLossAlly;

    [Header("保护")]
    [Tooltip("本效果最多给多少枚满足条件的己方硬币添加护盾。多个候选同时满足时，按行动顺序选择。")]
    [Min(1)]
    [SerializeField] private int protectionTargetCount = 1;

    [Min(1)]
    [SerializeField] private int durationRounds = 2;

    [Tooltip("持续期间最多抵挡多少次敌方攻击。")]
    [Min(1)]
    [SerializeField] private int blockCount = 1;

    [Header("可选特效")]
    [SerializeField] private SkillEffectVfxData vfx;

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly GrantCoinProtectionEffectData data;

        public Controller(GrantCoinProtectionEffectData data)
        {
            this.data = data;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[GrantCoinProtectionEffectData] 缺少 CoinRoundEffectManager，无法添加保护效果。");
                return;
            }

            List<CoinStats> targets = CollisionSkillTargetResolver.ResolveCoinsByActionOrder(
                context,
                data.targetType,
                data.protectionTargetCount);
            string sourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : data.name;

            for (int i = 0; i < targets.Count; i++)
            {
                int protectionId = CoinRoundEffectManager.Instance.GrantCoinProtection(
                    targets[i],
                    data.durationRounds,
                    data.blockCount,
                    sourceId);

                SkillEffectVfxPlayer.PlayForProtection(data.vfx, targets[i], protectionId);
            }
        }
    }
}
