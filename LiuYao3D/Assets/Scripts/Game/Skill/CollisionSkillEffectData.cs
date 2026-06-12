/// <summary>
/// 实现功能：定义硬币碰撞技能的单项效果配置基类，具体技能效果通过派生 Data 提供可调参数。
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

[Serializable]
public sealed class DealDamageEffectConfig : CollisionSkillEffectConfig
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
            int damage = config.CalculateDamage(context);
            if (damage <= 0)
                return;

            List<EnemyStats> targets = CollisionSkillTargetResolver.ResolveEnemies(context, config.targetType, config.radius);

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].TakeDamage(damage);
            }

            SkillEffectVfxPlayer.PlayForEnemies(config.vfx, targets);
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

        return CoinDamageCalculator.CalculateFromSnapshot(attackSnapshot, damagePercent / 100f, addPercent);
    }
}

[Serializable]
public sealed class ReduceLossEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CollisionSkillTargetType targetType = CollisionSkillTargetType.AllAllies;

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
            List<CoinStats> targets = CollisionSkillTargetResolver.ResolveCoins(context, config.targetType);

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].ReduceLoss(config.reduceLoss);
            }

            SkillEffectVfxPlayer.PlayForCoins(config.vfx, targets);
        }
    }
}

[Serializable]
public sealed class AddDamageModifierEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CollisionSkillTargetType targetType = CollisionSkillTargetType.AllAllies;

    [Header("增伤")]
    [Tooltip("伤害增加比例。例如 30 表示伤害增加 30%。")]
    [SerializeField] private float addDamagePercent = 30f;

    [Tooltip("-1 表示永久有效；正数表示持续对应数量的回合。")]
    [SerializeField] private int durationRounds = -1;

    [Tooltip("0 表示立即生效；1 表示下一回合开始生效。")]
    [Min(0)]
    [SerializeField] private int activateAfterRounds;

    [Tooltip("开启后，同一个效果可重复叠加。")]
    [SerializeField] private bool stackable = true;

    [Tooltip("非叠加效果使用该标识覆盖旧效果。留空时使用当前技能名。")]
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
                Debug.LogWarning("[AddDamageModifierEffectConfig] 缺少 CoinRoundEffectManager，无法添加增伤效果。");
                return;
            }

            string sourceId = string.IsNullOrWhiteSpace(config.modifierId)
                ? (context != null && context.skill != null ? context.skill.SkillName : nameof(AddDamageModifierEffectConfig))
                : config.modifierId.Trim();

            List<CoinStats> targets = CollisionSkillTargetResolver.ResolveCoins(context, config.targetType);

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
    [SerializeField] private CollisionSkillTargetType targetType = CollisionSkillTargetType.ActiveCoin;

    [Header("延迟损耗")]
    [Min(0)]
    [SerializeField] private int loss = 1;

    [Min(0)]
    [SerializeField] private int delayRounds = 1;

    [Tooltip("非 None 时，延迟结算时仅对当前仍为该卦象的硬币生效。")]
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

            List<CoinStats> targets = CollisionSkillTargetResolver.ResolveCoins(context, config.targetType);

            for (int i = 0; i < targets.Count; i++)
            {
                CoinRoundEffectManager.Instance.ScheduleCoinLoss(
                    targets[i],
                    config.loss,
                    config.delayRounds,
                    config.requiredCurrentTrigram);
            }
        }
    }
}

[Serializable]
public sealed class GrantCoinProtectionEffectConfig : CollisionSkillEffectConfig
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
                Debug.LogWarning("[GrantCoinProtectionEffectConfig] 缺少 CoinRoundEffectManager，无法添加保护效果。");
                return;
            }

            List<CoinStats> targets = CollisionSkillTargetResolver.ResolveCoinsByActionOrder(
                context,
                config.targetType,
                config.protectionTargetCount);

            for (int i = 0; i < targets.Count; i++)
            {
                int protectionId = CoinRoundEffectManager.Instance.GrantCoinProtection(
                    targets[i],
                    config.durationRounds,
                    config.blockCount);

                SkillEffectVfxPlayer.PlayForProtection(config.vfx, targets[i], protectionId);
            }
        }
    }
}

[Serializable]
public sealed class BreakEnemyShieldEffectConfig : CollisionSkillEffectConfig
{
    [Header("目标")]
    [SerializeField] private CollisionSkillTargetType targetType = CollisionSkillTargetType.AllEnemies;

    [Tooltip("目标为碰撞范围内敌人时使用。")]
    [Min(0f)]
    [SerializeField] private float radius = 3f;

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
            List<EnemyStats> targets = CollisionSkillTargetResolver.ResolveEnemies(context, config.targetType, config.radius);

            string sourceName = context != null && context.skill != null
                ? context.skill.SkillName
                : nameof(BreakEnemyShieldEffectConfig);

            for (int i = 0; i < targets.Count; i++)
            {
                EnemyShieldController shield = targets[i].GetComponent<EnemyShieldController>();
                if (shield == null || !shield.HasShield)
                    continue;

                if (!CanBreakShield(context, shield.CurrentShieldType))
                    continue;

                shield.TryBreakShield(shield.CurrentShieldType, sourceName);
            }
        }

        private static bool CanBreakShield(CollisionSkillContext context, TrigramType shieldType)
        {
            if (context == null || shieldType == TrigramType.None)
                return false;

            return shieldType == context.activeTrigram ||
                shieldType == context.passiveTrigram;
        }
    }
}
