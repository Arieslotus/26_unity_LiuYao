/// <summary>
/// 实现功能：配置碰撞技能为指定己方硬币降低损耗值。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Effect_ReduceLoss_", menuName = "Config/Collision Skill Effects/Reduce Loss")]
public class ReduceLossEffectData : CollisionSkillEffectData
{
    [Header("目标")]
    [SerializeField] private CollisionSkillTargetType targetType = CollisionSkillTargetType.AllAllies;

    [Header("恢复")]
    [Min(0)]
    [SerializeField] private int reduceLoss = 1;

    [Header("可选特效")]
    [SerializeField] private SkillEffectVfxData vfx;

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly ReduceLossEffectData data;

        public Controller(ReduceLossEffectData data)
        {
            this.data = data;
        }

        public void Execute(CollisionSkillContext context)
        {
            List<CoinStats> targets = CollisionSkillTargetResolver.ResolveCoins(context, data.targetType);

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].ReduceLoss(data.reduceLoss);
            }

            SkillEffectVfxPlayer.PlayForCoins(data.vfx, targets);
        }
    }
}
