/// <summary>
/// 实现功能：定义内嵌技能效果“造成伤害”，支持目标选择、倍率/固定伤害与受击特效。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DealDamageEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private EnemySkillTargetSelector targetSelector = new EnemySkillTargetSelector();

    [Header("伤害")]
    [SerializeField] private SkillDamageSource damageSource = SkillDamageSource.ActiveCoinAttack;
    [Min(0f)]
    [SerializeField] private float damagePercent = 100f;
    [Min(0)]
    [SerializeField] private int fixedDamage;

    [Header("可选特效")]
    [SerializeField] private SkillEffectVfxData vfx;

    public override string DisplayName => "造成伤害";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly DealDamageEffectConfig config;

        public Controller(DealDamageEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            int damage = CollisionSkillDamageUtility.CalculateDamage(
                context,
                config.damageSource,
                config.damagePercent,
                config.fixedDamage);

            if (damage <= 0)
                return;

            List<EnemyStats> targets = config.targetSelector.Resolve(context);

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

            SkillEffectVfxPlayer.PlayForEnemies(config.vfx, targets);
        }
    }
}
