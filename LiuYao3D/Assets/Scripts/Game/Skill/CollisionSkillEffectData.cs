/// <summary>
/// 实现功能：定义碰撞技能的内嵌效果配置，供技能资产直接组合伤害、恢复、增伤、损耗、破盾、伤害圈与翻面条件。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class CollisionSkillEffectData : ScriptableObject
{
    public abstract ICollisionSkillEffectController CreateController();
}

[Serializable]
public abstract class CollisionSkillEffectConfig
{
    public abstract string DisplayName { get; }
    public abstract ICollisionSkillEffectController CreateController();
}

public enum BreakShieldTrigramMode
{
    CollisionParticipants,
    SpecifiedTrigrams
}

public enum DamageZoneSpawnMode
{
    CollisionPosition,
    LastDamagedEnemies
}

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

[Serializable]
public sealed class ReduceLossEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector();

    [Header("恢复")]
    [Min(0)]
    [SerializeField] private int reduceLoss = 1;

    [Header("可选特效")]
    [SerializeField] private SkillEffectVfxData vfx;

    public override string DisplayName => "恢复损耗";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly ReduceLossEffectConfig config;

        public Controller(ReduceLossEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            List<CoinStats> targets = config.targetSelector.Resolve(context, true);

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].ReduceLoss(config.reduceLoss);
            }

            SkillEffectVfxPlayer.PlayForCoins(config.vfx, targets);
        }
    }
}

[Serializable]
public sealed class AddCoinLossEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector();

    [Header("损耗")]
    [Min(0)]
    [SerializeField] private int loss = 1;

    public override string DisplayName => "增加己方损耗";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly AddCoinLossEffectConfig config;

        public Controller(AddCoinLossEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            List<CoinStats> targets = config.targetSelector.Resolve(context);

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].AddLoss(config.loss, CoinLossCause.Skill);
            }
        }
    }
}

[Serializable]
public sealed class AddDamageModifierEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector();

    [Header("增伤")]
    [SerializeField] private float addDamagePercent = 30f;
    [SerializeField] private int durationRounds = -1;
    [Min(0)]
    [SerializeField] private int activateAfterRounds;
    [SerializeField] private bool stackable = true;
    [SerializeField] private string modifierId;

    [Header("可选特效")]
    [SerializeField] private SkillEffectVfxData vfx;

    public override string DisplayName => "添加增伤";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly AddDamageModifierEffectConfig config;

        public Controller(AddDamageModifierEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[AddDamageModifierEffectConfig] 缺少 CoinRoundEffectManager，无法添加增伤。");
                return;
            }

            string sourceId = string.IsNullOrWhiteSpace(config.modifierId)
                ? (context != null && context.skill != null ? context.skill.SkillName : nameof(AddDamageModifierEffectConfig))
                : config.modifierId.Trim();

            List<CoinStats> targets = config.targetSelector.Resolve(context);

            int runtimeModifierId = CoinRoundEffectManager.Instance.AddDamageModifier(
                sourceId,
                config.addDamagePercent / 100f,
                config.durationRounds,
                config.activateAfterRounds,
                config.stackable,
                targets);

            SkillEffectVfxPlayer.PlayForDamageModifierTargets(config.vfx, targets, runtimeModifierId, config.activateAfterRounds);
        }
    }
}

[Serializable]
public sealed class ScheduleCoinLossEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector(CoinSkillTargetType.ActiveCoin);

    [Header("延迟损耗")]
    [Min(0)]
    [SerializeField] private int loss = 1;
    [Min(0)]
    [SerializeField] private int delayRounds = 1;
    [SerializeField] private TrigramType requiredCurrentTrigram = TrigramType.None;

    public override string DisplayName => "延迟增加损耗";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly ScheduleCoinLossEffectConfig config;

        public Controller(ScheduleCoinLossEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[ScheduleCoinLossEffectConfig] 缺少 CoinRoundEffectManager，无法安排延迟损耗。");
                return;
            }

            List<CoinStats> targets = config.targetSelector.Resolve(context);
            string sourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(ScheduleCoinLossEffectConfig);

            for (int i = 0; i < targets.Count; i++)
            {
                CoinRoundEffectManager.Instance.ScheduleCoinLoss(
                    targets[i],
                    config.loss,
                    config.delayRounds,
                    config.requiredCurrentTrigram,
                    sourceId);
            }
        }
    }
}

[Serializable]
public sealed class GrantCoinProtectionEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector(CoinSkillTargetType.HighestLossAlly);

    [Header("保护")]
    [Min(1)]
    [SerializeField] private int durationRounds = 2;
    [Min(1)]
    [SerializeField] private int blockCount = 1;

    [Header("可选特效")]
    [SerializeField] private SkillEffectVfxData vfx;

    public override string DisplayName => "添加保护";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly GrantCoinProtectionEffectConfig config;

        public Controller(GrantCoinProtectionEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[GrantCoinProtectionEffectConfig] 缺少 CoinRoundEffectManager，无法添加保护。");
                return;
            }

            List<CoinStats> targets = config.targetSelector.Resolve(context);
            string sourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(GrantCoinProtectionEffectConfig);

            for (int i = 0; i < targets.Count; i++)
            {
                int protectionId = CoinRoundEffectManager.Instance.GrantCoinProtection(
                    targets[i],
                    config.durationRounds,
                    config.blockCount,
                    sourceId);

                SkillEffectVfxPlayer.PlayForProtection(config.vfx, targets[i], protectionId);
            }
        }
    }
}

[Serializable]
public sealed class BreakEnemyShieldEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private EnemySkillTargetSelector targetSelector = new EnemySkillTargetSelector();

    [Header("可破护盾卦象")]
    [SerializeField] private BreakShieldTrigramMode trigramMode = BreakShieldTrigramMode.CollisionParticipants;
    [SerializeField] private List<TrigramType> specifiedTrigrams = new List<TrigramType>();

    public override string DisplayName => "破除敌方护盾";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly BreakEnemyShieldEffectConfig config;

        public Controller(BreakEnemyShieldEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            List<EnemyStats> targets = config.targetSelector.Resolve(context);

            string sourceName = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(BreakEnemyShieldEffectConfig);

            for (int i = 0; i < targets.Count; i++)
            {
                EnemyShieldController shield = targets[i].GetComponent<EnemyShieldController>();
                if (shield == null || !shield.HasShield)
                    continue;

                if (!config.CanBreakShield(context, shield.CurrentShieldType))
                    continue;

                shield.TryBreakShield(shield.CurrentShieldType, sourceName);
            }
        }
    }

    private bool CanBreakShield(CollisionSkillContext context, TrigramType shieldType)
    {
        if (shieldType == TrigramType.None)
            return false;

        if (trigramMode == BreakShieldTrigramMode.SpecifiedTrigrams)
            return specifiedTrigrams != null && specifiedTrigrams.Contains(shieldType);

        if (context == null)
            return false;

        return shieldType == context.activeTrigram || shieldType == context.passiveTrigram;
    }
}

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

[Serializable]
public sealed class CoinFlipConditionEffectConfig : CollisionSkillEffectConfig
{
    [Header("监听目标")]
    [SerializeField] private CoinSkillTargetSelector watchedTargetSelector = new CoinSkillTargetSelector();

    [Header("检查")]
    [Tooltip("1 表示本大回合 RoundEnded 检查；2 表示下一个大回合 RoundEnded 检查。")]
    [Min(1)]
    [SerializeField] private int roundEndChecks = 1;
    [SerializeField] private bool requireNoFlip = true;

    [Header("满足条件时")]
    [SerializeField] private CoinSkillOutcomeConfig successOutcome = new CoinSkillOutcomeConfig();

    [Header("不满足条件时")]
    [SerializeField] private CoinSkillOutcomeConfig failureOutcome = new CoinSkillOutcomeConfig();

    public override string DisplayName => "翻面条件";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly CoinFlipConditionEffectConfig config;

        public Controller(CoinFlipConditionEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[CoinFlipConditionEffectConfig] 缺少 CoinRoundEffectManager，无法监听翻面条件。");
                return;
            }

            List<CoinStats> watchedTargets = config.watchedTargetSelector.Resolve(context);

            string sourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(CoinFlipConditionEffectConfig);

            CoinRoundEffectManager.Instance.ScheduleFlipCondition(
                context,
                sourceId,
                watchedTargets,
                config.roundEndChecks,
                config.requireNoFlip,
                config.successOutcome,
                config.failureOutcome);
        }
    }
}

[Serializable]
public sealed class UntilFlipDamageStackEffectConfig : CollisionSkillEffectConfig
{
    [Header("监听目标")]
    [SerializeField] private CoinSkillTargetSelector watchedTargetSelector = new CoinSkillTargetSelector();

    [Header("叠层")]
    [Min(1)]
    [SerializeField] private int maxStacks = 3;
    [SerializeField] private CoinSkillOutcomeConfig stackOutcome = new CoinSkillOutcomeConfig();

    public override string DisplayName => "直到翻面停止叠增伤";

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly UntilFlipDamageStackEffectConfig config;

        public Controller(UntilFlipDamageStackEffectConfig config)
        {
            this.config = config;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[UntilFlipDamageStackEffectConfig] 缺少 CoinRoundEffectManager，无法添加直到翻面的叠层效果。");
                return;
            }

            List<CoinStats> watchedTargets = config.watchedTargetSelector.Resolve(context);

            string sourceId = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(UntilFlipDamageStackEffectConfig);

            CoinRoundEffectManager.Instance.StartUntilFlipDamageStacks(
                context,
                sourceId,
                watchedTargets,
                config.maxStacks,
                config.stackOutcome);
        }
    }
}

public static class CollisionSkillDamageUtility
{
    public static int CalculateDamage(
        CollisionSkillContext context,
        SkillDamageSource damageSource,
        float damagePercent,
        int fixedDamage)
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
