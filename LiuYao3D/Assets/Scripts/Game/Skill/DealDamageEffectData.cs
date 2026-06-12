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
            int damage = data.CalculateDamage(context);
            if (damage <= 0)
                return;

            List<EnemyStats> targets = CollisionSkillTargetResolver.ResolveEnemies(
                context,
                data.targetType,
                data.radius);

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].TakeDamage(damage);
            }

            SkillEffectVfxPlayer.PlayForEnemies(data.vfx, targets);
        }
    }

    private int CalculateDamage(CollisionSkillContext context)
    {
        if (context == null)
            return 0;

        if (damageSource == SkillDamageSource.FixedValue)
            return Mathf.Max(0, fixedDamage);

        CoinStats sourceStats = damageSource == SkillDamageSource.PassiveCoinAttack
            ? context.passiveStats
            : context.activeStats;

        int attackSnapshot = damageSource == SkillDamageSource.PassiveCoinAttack
            ? context.passiveAttackSnapshot
            : context.activeAttackSnapshot;

        float addPercent = CoinRoundEffectManager.Instance != null
            ? CoinRoundEffectManager.Instance.GetDamageAddPercent(sourceStats)
            : 0f;

        return CoinDamageCalculator.CalculateFromSnapshot(
            attackSnapshot,
            damagePercent / 100f,
            addPercent);
    }
}
