/// <summary>
/// 实现功能：定义内嵌技能效果“创建持续伤害圈”，由区域预制体碰撞体决定生效范围。
/// </summary>
using System;
using UnityEngine;

[Serializable]
public sealed class CreateDamageZoneEffectConfig : CollisionSkillEffectConfig
{
    [Header("生成位置")]
    [SerializeField] private DamageZoneSpawnMode spawnMode = DamageZoneSpawnMode.CollisionPosition;

    [Header("区域预制体")]
    [SerializeField] private GameObject zonePrefab;
    [Min(0.01f)]
    [SerializeField] private float prefabScale = 1f;

    [Header("伤害")]
    [SerializeField] private SkillDamageSource damageSource = SkillDamageSource.ActiveCoinAttack;
    [Min(0f)]
    [SerializeField] private float damagePercent = 50f;
    [Min(0)]
    [SerializeField] private int fixedDamage;
    [Min(1)]
    [SerializeField] private int tickCount = 3;

    public override string DisplayName => "创建持续伤害圈";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly CreateDamageZoneEffectConfig config;

        public Controller(CreateDamageZoneEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[CreateDamageZoneEffectConfig] 缺少 CoinRoundEffectManager，无法创建伤害圈。");
                return;
            }

            int damage = CollisionSkillDamageUtility.CalculateDamage(
                context,
                config.damageSource,
                config.damagePercent,
                config.fixedDamage);

            if (damage <= 0)
                return;

            if (config.spawnMode == DamageZoneSpawnMode.LastDamagedEnemies)
            {
                if (context == null || context.lastDamagedEnemies.Count == 0)
                    return;

                for (int i = 0; i < context.lastDamagedEnemies.Count; i++)
                {
                    EnemyStats enemy = context.lastDamagedEnemies[i];
                    if (enemy == null)
                        continue;

                    CoinRoundEffectManager.Instance.AddColliderDamageZone(
                        enemy.transform.position,
                        config.zonePrefab,
                        config.prefabScale,
                        damage,
                        config.tickCount,
                        context != null && context.skill != null ? context.skill.SkillName : nameof(CreateDamageZoneEffectConfig));
                }
                return;
            }

            if (context != null)
            {
                CoinRoundEffectManager.Instance.AddColliderDamageZone(
                    context.collisionPosition,
                    config.zonePrefab,
                    config.prefabScale,
                    damage,
                    config.tickCount,
                    context.skill != null ? context.skill.SkillName : nameof(CreateDamageZoneEffectConfig));
            }
        }
    }
}
