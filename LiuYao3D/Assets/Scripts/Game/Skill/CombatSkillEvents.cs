/// <summary>
/// 实现功能：提供战斗技能表现事件，解耦碰撞逻辑、UI、音效与特效等表现系统。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public static class CombatSkillEvents
{
    public static event Action<TrigramCollisionSkillSO, float> SkillTriggerFeedbackRequested;
    public static event Action<TrigramType> SkillImpactWaveRequested;

    public static void RequestSkillTriggerFeedback(TrigramCollisionSkillSO skill, float duration)
    {
        if (skill == null || duration <= 0f)
            return;

        SkillTriggerFeedbackRequested?.Invoke(skill, duration);
    }

    public static void RequestSkillImpactWave(TrigramType trigram)
    {
        if (trigram == TrigramType.None)
            return;

        SkillImpactWaveRequested?.Invoke(trigram);
    }
}

public static class CombatVfxEvents
{
    public static event Action<ChessPiece, ChessPiece, Vector3> CoinCollisionRequested;
    public static event Action<ChessPiece, EnemyStats, Vector3> CoinEnemyCollisionRequested;
    public static event Action<EnemyStats, int, Vector3> EnemyDamagedRequested;
    public static event Action<CoinStats, int, CoinLossCause, Vector3> CoinDamagedRequested;
    public static event Action<CoinStats, int, Vector3> CoinHealedRequested;
    public static event Action<int, IReadOnlyList<CoinStats>, int> DamageModifierAddedRequested;

    public static void RequestCoinCollision(ChessPiece activePiece, ChessPiece passivePiece, Vector3 hitPoint)
    {
        if (activePiece == null || passivePiece == null)
            return;

        CoinCollisionRequested?.Invoke(activePiece, passivePiece, hitPoint);
    }

    public static void RequestCoinEnemyCollision(ChessPiece activePiece, EnemyStats enemy, Vector3 hitPoint)
    {
        if (activePiece == null || enemy == null)
            return;

        CoinEnemyCollisionRequested?.Invoke(activePiece, enemy, hitPoint);
    }

    public static void RequestEnemyDamaged(EnemyStats enemy, int damage, Vector3 hitPoint)
    {
        if (enemy == null || damage <= 0)
            return;

        EnemyDamagedRequested?.Invoke(enemy, damage, hitPoint);
    }

    public static void RequestCoinDamaged(CoinStats coin, int loss, CoinLossCause cause, Vector3 hitPoint)
    {
        if (coin == null || loss <= 0)
            return;

        CoinDamagedRequested?.Invoke(coin, loss, cause, hitPoint);
    }

    public static void RequestCoinHealed(CoinStats coin, int reduceLoss, Vector3 hitPoint)
    {
        if (coin == null || reduceLoss <= 0)
            return;

        CoinHealedRequested?.Invoke(coin, reduceLoss, hitPoint);
    }

    public static void RequestDamageModifierAdded(int modifierId, IReadOnlyList<CoinStats> targets, int activateAfterRounds)
    {
        if (modifierId <= 0 || targets == null || targets.Count <= 0)
            return;

        DamageModifierAddedRequested?.Invoke(modifierId, targets, activateAfterRounds);
    }
}
