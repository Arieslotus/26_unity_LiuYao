/// <summary>
/// 实现功能：定义碰撞技能效果的基础配置类型、公共枚举与通用伤害计算工具。
/// </summary>
using System;
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

public enum SkillDamageSource
{
    ActiveCoinAttack,
    PassiveCoinAttack,
    FixedValue
}

public enum CoinSkillScheduleTiming
{
    RoundStarted,
    RoundEnded
}

public enum TurnTriggerCountMode
{
    OncePerRoundOverLimit,
    EveryTimeOverLimit
}

public enum CoinPhysicsModifierType
{
    CoinTransferDistance,
    CoinTransferSpeed,
    SelfRemainingDistance
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
