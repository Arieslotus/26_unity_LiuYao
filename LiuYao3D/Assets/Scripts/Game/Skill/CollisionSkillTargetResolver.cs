/// <summary>
/// 实现功能：根据碰撞技能配置统一筛选敌方单位或己方硬币，支持范围、最近、随机、行动顺序与当前卦象过滤。
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using System;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public enum CollisionSkillTargetType
{
    AllEnemies,
    EnemiesInCollisionRadius,
    NearestEnemy,
    RandomEnemies,
    AllAllies,
    HighestLossAlly,
    RandomAllies,
    ActiveCoin,
    PassiveCoin
}

[Serializable]
public sealed class EnemySkillTargetSelector
{
    [SerializeField] private EnemySkillTargetType targetType = EnemySkillTargetType.AllEnemies;
    [Min(0f)]
    [SerializeField] private float radius = 3f;
    [Min(1)]
    [SerializeField] private int targetCount = 1;

    public EnemySkillTargetType TargetType => targetType;
    public float Radius => radius;
    public int TargetCount => targetCount;

    public List<EnemyStats> Resolve(CollisionSkillContext context)
    {
        int count = targetType == EnemySkillTargetType.RandomEnemies
            ? Mathf.Max(1, targetCount)
            : 0;

        return CollisionSkillTargetResolver.ResolveEnemies(
            context,
            ToLegacyTargetType(targetType),
            radius,
            count);
    }

    private static CollisionSkillTargetType ToLegacyTargetType(EnemySkillTargetType type)
    {
        switch (type)
        {
            case EnemySkillTargetType.EnemiesInCollisionRadius:
                return CollisionSkillTargetType.EnemiesInCollisionRadius;
            case EnemySkillTargetType.NearestEnemy:
                return CollisionSkillTargetType.NearestEnemy;
            case EnemySkillTargetType.RandomEnemies:
                return CollisionSkillTargetType.RandomEnemies;
            default:
                return CollisionSkillTargetType.AllEnemies;
        }
    }
}

[Serializable]
public sealed class CoinSkillTargetSelector
{
    [SerializeField] private CoinSkillTargetType targetType = CoinSkillTargetType.AllAllies;
    [Min(1)]
    [SerializeField] private int targetCount = 1;
    [SerializeField] private bool sortByActionOrder = true;
    [SerializeField] private List<TrigramType> currentTrigramFilter = new List<TrigramType>();

    public CoinSkillTargetType TargetType => targetType;
    public int TargetCount => targetCount;
    public bool SortByActionOrder => sortByActionOrder;
    public IReadOnlyList<TrigramType> CurrentTrigramFilter => currentTrigramFilter;

    public CoinSkillTargetSelector()
    {
    }

    public CoinSkillTargetSelector(CoinSkillTargetType defaultTargetType)
    {
        targetType = defaultTargetType;
    }

    public List<CoinStats> Resolve(CollisionSkillContext context, bool requireCurrentLoss = false)
    {
        CollisionSkillTargetType legacyType = ToLegacyTargetType(targetType);
        int count = NeedsCount(targetType) ? Mathf.Max(1, targetCount) : 0;
        IReadOnlyList<TrigramType> filter = UsesCurrentTrigramFilter(targetType)
            ? currentTrigramFilter
            : null;

        bool shouldSortByActionOrder = sortByActionOrder &&
            targetType != CoinSkillTargetType.RandomAllies &&
            targetType != CoinSkillTargetType.RandomAlliesWithCurrentTrigrams;

        if (shouldSortByActionOrder)
        {
            return CollisionSkillTargetResolver.ResolveCoinsByActionOrder(
                context,
                legacyType,
                count,
                requireCurrentLoss,
                filter);
        }

        return CollisionSkillTargetResolver.ResolveCoins(
            context,
            legacyType,
            count,
            requireCurrentLoss,
            filter);
    }

    public static bool NeedsCount(CoinSkillTargetType type)
    {
        return type == CoinSkillTargetType.RandomAllies ||
            type == CoinSkillTargetType.RandomAlliesWithCurrentTrigrams ||
            type == CoinSkillTargetType.HighestLossAlly;
    }

    public static bool UsesCurrentTrigramFilter(CoinSkillTargetType type)
    {
        return type == CoinSkillTargetType.AlliesWithCurrentTrigrams ||
            type == CoinSkillTargetType.RandomAlliesWithCurrentTrigrams;
    }

    private static CollisionSkillTargetType ToLegacyTargetType(CoinSkillTargetType type)
    {
        switch (type)
        {
            case CoinSkillTargetType.RandomAllies:
            case CoinSkillTargetType.RandomAlliesWithCurrentTrigrams:
                return CollisionSkillTargetType.RandomAllies;
            case CoinSkillTargetType.HighestLossAlly:
                return CollisionSkillTargetType.HighestLossAlly;
            case CoinSkillTargetType.ActiveCoin:
                return CollisionSkillTargetType.ActiveCoin;
            case CoinSkillTargetType.PassiveCoin:
                return CollisionSkillTargetType.PassiveCoin;
            default:
                return CollisionSkillTargetType.AllAllies;
        }
    }
}

public enum EnemySkillTargetType
{
    AllEnemies,
    EnemiesInCollisionRadius,
    NearestEnemy,
    RandomEnemies
}

public enum CoinSkillTargetType
{
    AllAllies,
    AlliesWithCurrentTrigrams,
    RandomAllies,
    RandomAlliesWithCurrentTrigrams,
    HighestLossAlly,
    ActiveCoin,
    PassiveCoin
}

public static class CollisionSkillTargetResolver
{
    public static List<EnemyStats> ResolveEnemies(
        CollisionSkillContext context,
        CollisionSkillTargetType targetType,
        float radius = 0f,
        int maxCount = 0)
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

            case CollisionSkillTargetType.RandomEnemies:
                AddAliveEnemies(result, enemies);
                break;
        }

        if (targetType == CollisionSkillTargetType.RandomEnemies)
        {
            Shuffle(result);
        }

        Trim(result, maxCount);
        return result;
    }

    public static List<CoinStats> ResolveCoins(
        CollisionSkillContext context,
        CollisionSkillTargetType targetType,
        int maxCount = 0,
        bool requireCurrentLoss = false,
        IReadOnlyList<TrigramType> currentTrigramFilter = null)
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
                AddHighestLossCoins(result, requireCurrentLoss);
                break;

            case CollisionSkillTargetType.RandomAllies:
                AddAvailableCoins(result, Object.FindObjectsOfType<CoinStats>());
                break;

            case CollisionSkillTargetType.ActiveCoin:
                AddCoinIfAvailable(result, context.activeStats);
                break;

            case CollisionSkillTargetType.PassiveCoin:
                AddCoinIfAvailable(result, context.passiveStats);
                break;
        }

        FilterCoins(result, requireCurrentLoss, currentTrigramFilter);
        if (targetType == CollisionSkillTargetType.RandomAllies)
        {
            Shuffle(result);
        }

        Trim(result, maxCount);
        return result;
    }

    public static List<CoinStats> ResolveCoinsByActionOrder(
        CollisionSkillContext context,
        CollisionSkillTargetType targetType,
        int maxCount,
        bool requireCurrentLoss = false,
        IReadOnlyList<TrigramType> currentTrigramFilter = null)
    {
        List<CoinStats> result = ResolveCoins(
            context,
            targetType,
            0,
            requireCurrentLoss,
            currentTrigramFilter);

        SortCoinsByActionOrder(result);
        Trim(result, maxCount);
        return result;
    }

    public static void SortCoinsByActionOrder(List<CoinStats> coins)
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

    private static void AddHighestLossCoins(List<CoinStats> result, bool requireCurrentLoss)
    {
        CoinStats[] coins = Object.FindObjectsOfType<CoinStats>();
        int highestLoss = int.MinValue;

        for (int i = 0; i < coins.Length; i++)
        {
            CoinStats coin = coins[i];
            if (coin == null || coin.IsBroken)
                continue;

            if (requireCurrentLoss && coin.CurrentLoss <= 0)
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
    }

    private static void FilterCoins(
        List<CoinStats> coins,
        bool requireCurrentLoss,
        IReadOnlyList<TrigramType> currentTrigramFilter)
    {
        for (int i = coins.Count - 1; i >= 0; i--)
        {
            CoinStats coin = coins[i];
            if (coin == null || coin.IsBroken)
            {
                coins.RemoveAt(i);
                continue;
            }

            if (requireCurrentLoss && coin.CurrentLoss <= 0)
            {
                coins.RemoveAt(i);
                continue;
            }

            if (currentTrigramFilter == null || currentTrigramFilter.Count == 0)
                continue;

            ChessPiece piece = coin.GetComponent<ChessPiece>();
            if (piece == null || !ContainsTrigram(currentTrigramFilter, piece.CurrentTrigram))
            {
                coins.RemoveAt(i);
            }
        }
    }

    private static bool ContainsTrigram(IReadOnlyList<TrigramType> trigrams, TrigramType target)
    {
        if (target == TrigramType.None)
            return false;

        for (int i = 0; i < trigrams.Count; i++)
        {
            if (trigrams[i] == target)
                return true;
        }

        return false;
    }

    private static void Trim<T>(List<T> list, int maxCount)
    {
        int validMaxCount = Mathf.Max(0, maxCount);
        if (validMaxCount > 0 && list.Count > validMaxCount)
        {
            list.RemoveRange(validMaxCount, list.Count - validMaxCount);
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
