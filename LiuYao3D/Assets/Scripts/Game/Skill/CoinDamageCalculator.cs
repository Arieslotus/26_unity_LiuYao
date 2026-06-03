/// <summary>
/// 实现功能：为普通碰撞和技能伤害提供统一的硬币伤害计算入口。
/// </summary>
using UnityEngine;

public static class CoinDamageCalculator
{
    public static int Calculate(CoinStats attacker, float skillMultiplier = 1f)
    {
        if (attacker == null || attacker.IsBroken)
            return 0;

        float damageAddPercent = CoinRoundEffectManager.Instance != null
            ? CoinRoundEffectManager.Instance.GetDamageAddPercent(attacker)
            : 0f;

        float value = attacker.Attack * Mathf.Max(0f, 1f + damageAddPercent) * Mathf.Max(0f, skillMultiplier);
        return Mathf.Max(0, Mathf.CeilToInt(value));
    }

    public static int CalculateFromSnapshot(int attackSnapshot, float skillMultiplier = 1f, float damageAddPercent = 0f)
    {
        float value = Mathf.Max(0, attackSnapshot) *
            Mathf.Max(0f, 1f + damageAddPercent) *
            Mathf.Max(0f, skillMultiplier);

        return Mathf.Max(0, Mathf.CeilToInt(value));
    }
}
