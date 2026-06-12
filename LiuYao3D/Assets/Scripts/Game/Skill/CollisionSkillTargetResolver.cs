/// <summary>
/// 实现功能：根据碰撞技能配置统一筛选敌方单位或己方硬币，供不同效果模块复用。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public enum CollisionSkillTargetType
{
    AllEnemies,
    EnemiesInCollisionRadius,
    NearestEnemy,
    AllAllies,                  //场上所有己方硬币。
    HighestLossAlly,            //场上当前损耗值最高的一个己方硬币。若多个硬币损耗相同，目前选中系统最先找到的一个。
    ActiveCoin,
    PassiveCoin
}

public static class CollisionSkillTargetResolver
{
    public static List<EnemyStats> ResolveEnemies(
        CollisionSkillContext context,
        CollisionSkillTargetType targetType,
        float radius = 0f)
    {
        List<EnemyStats> result = new List<EnemyStats>();
        if (context == null)
            return result;

        EnemyStats[] enemies = Object.FindObjectsOfType<EnemyStats>();

        switch (targetType)
        {
            case CollisionSkillTargetType.AllEnemies:
                AddAliveEnemies(result, enemies);
                break;

            case CollisionSkillTargetType.EnemiesInCollisionRadius:
                AddEnemiesInRadius(result, enemies, context.collisionPosition, radius);
                break;

            case CollisionSkillTargetType.NearestEnemy:
                EnemyStats nearest = FindNearestEnemy(enemies, context.collisionPosition);
                if (nearest != null)
                {
                    result.Add(nearest);
                }
                break;
        }

        return result;
    }

    public static List<CoinStats> ResolveCoins(
        CollisionSkillContext context,
        CollisionSkillTargetType targetType)
    {
        List<CoinStats> result = new List<CoinStats>();
        if (context == null)
            return result;

        switch (targetType)
        {
            case CollisionSkillTargetType.AllAllies:
                AddAvailableCoins(result, Object.FindObjectsOfType<CoinStats>());
                break;

            case CollisionSkillTargetType.HighestLossAlly:
                CoinStats highestLossCoin = CoinRoundEffectManager.Instance != null
                    ? CoinRoundEffectManager.Instance.FindHighestLossCoin()
                    : FindHighestLossCoin();

                AddCoinIfAvailable(result, highestLossCoin);
                break;

            case CollisionSkillTargetType.ActiveCoin:
                AddCoinIfAvailable(result, context.activeStats);
                break;

            case CollisionSkillTargetType.PassiveCoin:
                AddCoinIfAvailable(result, context.passiveStats);
                break;
        }

        return result;
    }

    public static List<CoinStats> ResolveCoinsByActionOrder(
        CollisionSkillContext context,
        CollisionSkillTargetType targetType,
        int maxCount)
    {
        List<CoinStats> result = targetType == CollisionSkillTargetType.HighestLossAlly
            ? ResolveHighestLossCoins()
            : ResolveCoins(context, targetType);

        SortCoinsByActionOrder(result);

        int validMaxCount = Mathf.Max(0, maxCount);
        if (validMaxCount > 0 && result.Count > validMaxCount)
        {
            result.RemoveRange(validMaxCount, result.Count - validMaxCount);
        }

        return result;
    }

    private static void AddAliveEnemies(List<EnemyStats> result, EnemyStats[] enemies)
    {
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyStats enemy = enemies[i];
            if (enemy != null && !enemy.IsDead)
            {
                result.Add(enemy);
            }
        }
    }

    private static void AddEnemiesInRadius(
        List<EnemyStats> result,
        EnemyStats[] enemies,
        Vector3 center,
        float radius)
    {
        float validRadius = Mathf.Max(0f, radius);
        float radiusSquared = validRadius * validRadius;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyStats enemy = enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            Vector3 offset = enemy.transform.position - center;
            offset.y = 0f;

            if (offset.sqrMagnitude <= radiusSquared)
            {
                result.Add(enemy);
            }
        }
    }

    private static EnemyStats FindNearestEnemy(EnemyStats[] enemies, Vector3 center)
    {
        EnemyStats result = null;
        float nearestDistanceSquared = float.MaxValue;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyStats enemy = enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            Vector3 offset = enemy.transform.position - center;
            offset.y = 0f;

            if (offset.sqrMagnitude >= nearestDistanceSquared)
                continue;

            result = enemy;
            nearestDistanceSquared = offset.sqrMagnitude;
        }

        return result;
    }

    private static void AddAvailableCoins(List<CoinStats> result, CoinStats[] coins)
    {
        for (int i = 0; i < coins.Length; i++)
        {
            AddCoinIfAvailable(result, coins[i]);
        }
    }

    private static void AddCoinIfAvailable(List<CoinStats> result, CoinStats coin)
    {
        if (coin != null && !coin.IsBroken)
        {
            result.Add(coin);
        }
    }

    private static CoinStats FindHighestLossCoin()
    {
        CoinStats[] coins = Object.FindObjectsOfType<CoinStats>();
        CoinStats result = null;

        for (int i = 0; i < coins.Length; i++)
        {
            CoinStats coin = coins[i];
            if (coin == null || coin.IsBroken)
                continue;

            if (result == null || coin.CurrentLoss > result.CurrentLoss)
            {
                result = coin;
            }
        }

        return result;
    }

    private static List<CoinStats> ResolveHighestLossCoins()
    {
        List<CoinStats> result = new List<CoinStats>();
        CoinStats[] coins = Object.FindObjectsOfType<CoinStats>();
        int highestLoss = int.MinValue;

        for (int i = 0; i < coins.Length; i++)
        {
            CoinStats coin = coins[i];
            if (coin == null || coin.IsBroken)
                continue;

            if (coin.CurrentLoss > highestLoss)
            {
                highestLoss = coin.CurrentLoss;
                result.Clear();
                result.Add(coin);
                continue;
            }

            if (coin.CurrentLoss == highestLoss)
            {
                result.Add(coin);
            }
        }

        return result;
    }

    private static void SortCoinsByActionOrder(List<CoinStats> coins)
    {
        if (coins == null || coins.Count <= 1)
            return;

        ChessTurnController turnController = Object.FindObjectOfType<ChessTurnController>();
        if (turnController == null || turnController.Pieces == null)
            return;

        List<CoinStats> ordered = new List<CoinStats>();
        IReadOnlyList<ChessPiece> pieces = turnController.Pieces;

        for (int i = 0; i < pieces.Count; i++)
        {
            ChessPiece piece = pieces[i];
            if (piece == null)
                continue;

            CoinStats stats = piece.GetComponent<CoinStats>();
            if (stats != null && coins.Contains(stats) && !ordered.Contains(stats))
            {
                ordered.Add(stats);
            }
        }

        for (int i = 0; i < coins.Count; i++)
        {
            CoinStats coin = coins[i];
            if (coin != null && !ordered.Contains(coin))
            {
                ordered.Add(coin);
            }
        }

        coins.Clear();
        coins.AddRange(ordered);
    }
}
