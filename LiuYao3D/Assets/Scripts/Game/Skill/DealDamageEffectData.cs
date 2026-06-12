/// <summary>
/// 实现功能：配置碰撞技能对敌方目标造成的即时伤害，支持全体、范围和最近单体目标。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public enum SkillDamageSource
{
    ActiveCoinAttack,
    PassiveCoinAttack,
    FixedValue
}

[CreateAssetMenu(fileName = "Effect_DealDamage_", menuName = "Config/Collision Skill Effects/Deal Damage")]
public class DealDamageEffectData : CollisionSkillEffectData
{
    [Header("目标")]
    [SerializeField] private CollisionSkillTargetType targetType = CollisionSkillTargetType.AllEnemies;

    [Tooltip("目标为碰撞范围内敌人时使用。")]
    [Min(0f)]
    [SerializeField] private float radius = 3f;

    [Tooltip("大于 0 时只取指定数量目标。随机目标请把目标类型设为 RandomEnemies。")]
    [Min(0)]
    [SerializeField] private int targetCount;

    [Header("伤害")]
    [SerializeField] private SkillDamageSource damageSource = SkillDamageSource.ActiveCoinAttack;

    [Tooltip("攻击力倍率。例如 130 表示造成攻击力 130% 的伤害。固定伤害模式下不使用。")]
    [Min(0f)]
    [SerializeField] private float damagePercent = 100f;

    [Tooltip("固定伤害模式下使用。")]
    [Min(0)]
    [SerializeField] private int fixedDamage;

    [Header("可选特效")]
    [SerializeField] private SkillEffectVfxData vfx;

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly DealDamageEffectData data;

        public Controller(DealDamageEffectData data)
        {
            this.data = data;
        }

        public void Execute(CollisionSkillContext context)
        {
            int damage = CollisionSkillDamageUtility.CalculateDamage(
                context,
                data.damageSource,
                data.damagePercent,
                data.fixedDamage);
            if (damage <= 0)
                return;

            List<EnemyStats> targets = CollisionSkillTargetResolver.ResolveEnemies(
                context,
                data.targetType,
                data.radius,
                data.targetCount);

            if (context != null)
            {
                context.lastDamagedEnemies.Clear();
            }

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].TakeDamage(damage);
                if (context != null && targets[i] != null && !context.lastDamagedEnemies.Contains(targets[i]))
                {
                    context.lastDamagedEnemies.Add(targets[i]);
                }
            }

            SkillEffectVfxPlayer.PlayForEnemies(data.vfx, targets);
        }
    }

}
