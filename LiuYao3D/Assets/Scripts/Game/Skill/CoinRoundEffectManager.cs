/// <summary>
/// 实现功能：统一管理硬币碰撞技能产生的永久增伤、限时增伤、延迟损耗、临时护盾与持续伤害区域。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public class CoinRoundEffectManager : MonoBehaviour
{
    private sealed class DamageModifier
    {
        public string sourceId;
        public float addPercent;
        public int activateRound;
        public int remainingRounds;
    }

    private sealed class PendingCoinLoss
    {
        public CoinStats target;
        public int loss;
        public int executeRound;
        public TrigramType requiredCurrentTrigram;
    }

    private sealed class CoinProtection
    {
        public CoinStats target;
        public int remainingRounds;
        public int remainingBlockCount;
    }

    private sealed class RoundDamageZone
    {
        public Vector3 center;
        public float radius;
        public int damage;
        public int remainingRounds;
    }

    public static CoinRoundEffectManager Instance { get; private set; }

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private readonly List<DamageModifier> damageModifiers = new List<DamageModifier>();
    private readonly List<PendingCoinLoss> pendingCoinLosses = new List<PendingCoinLoss>();
    private readonly List<CoinProtection> protections = new List<CoinProtection>();
    private readonly List<RoundDamageZone> damageZones = new List<RoundDamageZone>();

    private TurnManager subscribedTurnManager;
    private int currentRound;

    public int CurrentRound => currentRound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null)
            return;

        CoinRoundEffectManager existing = FindObjectOfType<CoinRoundEffectManager>();
        if (existing != null)
            return;

        GameObject root = new GameObject(nameof(CoinRoundEffectManager));
        root.AddComponent<CoinRoundEffectManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"[CoinRoundEffectManager] 场景中存在多个实例 | object:{name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SubscribeTurnManager();
    }

    private void OnEnable()
    {
        SubscribeTurnManager();
    }

    private void Update()
    {
        if (subscribedTurnManager == null && TurnManager.Instance != null)
        {
            SubscribeTurnManager();
        }
    }

    private void OnDisable()
    {
        UnsubscribeTurnManager();
    }

    private void OnDestroy()
    {
        UnsubscribeTurnManager();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public float GetDamageAddPercent(CoinStats attacker)
    {
        if (attacker == null)
            return 0f;

        float total = 0f;

        for (int i = 0; i < damageModifiers.Count; i++)
        {
            DamageModifier modifier = damageModifiers[i];
            if (modifier == null || modifier.activateRound > currentRound)
                continue;

            total += modifier.addPercent;
        }

        return total;
    }

    public void AddDamageModifier(
        string sourceId,
        float addPercent,
        int durationRounds = -1,
        int activateAfterRounds = 0,
        bool stackable = true)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || Mathf.Approximately(addPercent, 0f))
            return;

        if (!stackable)
        {
            RemoveDamageModifiers(sourceId);
        }

        damageModifiers.Add(new DamageModifier
        {
            sourceId = sourceId,
            addPercent = addPercent,
            activateRound = currentRound + Mathf.Max(0, activateAfterRounds),
            remainingRounds = durationRounds
        });

        if (debugLog)
        {
            Debug.Log(
                $"[CoinRoundEffectManager] 添加伤害修正 | source:{sourceId} | add:{addPercent:P0} | " +
                $"duration:{durationRounds} | activateRound:{currentRound + Mathf.Max(0, activateAfterRounds)}"
            );
        }
    }

    public void RemoveDamageModifiers(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        damageModifiers.RemoveAll(modifier => modifier != null && modifier.sourceId == sourceId);
    }

    public void ScheduleCoinLoss(
        CoinStats target,
        int loss,
        int delayRounds,
        TrigramType requiredCurrentTrigram = TrigramType.None)
    {
        if (target == null || loss <= 0)
            return;

        pendingCoinLosses.Add(new PendingCoinLoss
        {
            target = target,
            loss = loss,
            executeRound = currentRound + Mathf.Max(0, delayRounds),
            requiredCurrentTrigram = requiredCurrentTrigram
        });
    }

    public void GrantCoinProtection(CoinStats target, int durationRounds, int blockCount = 1)
    {
        if (target == null || durationRounds <= 0 || blockCount <= 0)
            return;

        protections.Add(new CoinProtection
        {
            target = target,
            remainingRounds = durationRounds,
            remainingBlockCount = blockCount
        });
    }

    public bool TryBlockCoinLoss(CoinStats target, int loss, CoinLossCause cause)
    {
        if (target == null || loss <= 0 || cause != CoinLossCause.EnemyAttack)
            return false;

        for (int i = protections.Count - 1; i >= 0; i--)
        {
            CoinProtection protection = protections[i];
            if (protection == null || protection.target != target)
                continue;

            protection.remainingBlockCount--;

            if (debugLog)
            {
                Debug.Log(
                    $"[CoinRoundEffectManager] 护盾抵挡损耗 | coin:{target.name} | loss:{loss} | " +
                    $"remainingBlockCount:{protection.remainingBlockCount}"
                );
            }

            if (protection.remainingBlockCount <= 0)
            {
                protections.RemoveAt(i);
            }

            return true;
        }

        return false;
    }

    public void AddRoundDamageZone(Vector3 center, float radius, int damage, int durationRounds)
    {
        if (radius <= 0f || damage <= 0 || durationRounds <= 0)
            return;

        damageZones.Add(new RoundDamageZone
        {
            center = center,
            radius = radius,
            damage = damage,
            remainingRounds = durationRounds
        });
    }

    public CoinStats FindHighestLossCoin()
    {
        CoinStats[] coins = FindObjectsOfType<CoinStats>();
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

    private void SubscribeTurnManager()
    {
        if (subscribedTurnManager == TurnManager.Instance)
            return;

        UnsubscribeTurnManager();
        subscribedTurnManager = TurnManager.Instance;

        if (subscribedTurnManager == null)
            return;

        currentRound = subscribedTurnManager.RoundIndex;
        subscribedTurnManager.RoundStarted += OnRoundStarted;
        subscribedTurnManager.RoundEnded += OnRoundEnded;
    }

    private void UnsubscribeTurnManager()
    {
        if (subscribedTurnManager == null)
            return;

        subscribedTurnManager.RoundStarted -= OnRoundStarted;
        subscribedTurnManager.RoundEnded -= OnRoundEnded;
        subscribedTurnManager = null;
    }

    private void OnRoundStarted(int roundIndex)
    {
        currentRound = roundIndex;
        ExecutePendingCoinLosses(roundIndex);
        TickDamageZones();
        CleanupDestroyedReferences();
    }

    private void OnRoundEnded(int roundIndex)
    {
        TickDamageModifierDurations(roundIndex);
        TickProtectionDurations();
        CleanupDestroyedReferences();
    }

    private void ExecutePendingCoinLosses(int roundIndex)
    {
        for (int i = pendingCoinLosses.Count - 1; i >= 0; i--)
        {
            PendingCoinLoss pending = pendingCoinLosses[i];
            if (pending == null || pending.executeRound > roundIndex)
                continue;

            pendingCoinLosses.RemoveAt(i);

            if (pending.target == null || pending.target.IsBroken)
                continue;

            if (pending.requiredCurrentTrigram != TrigramType.None)
            {
                ChessPiece piece = pending.target.GetComponent<ChessPiece>();
                if (piece == null || piece.CurrentTrigram != pending.requiredCurrentTrigram)
                    continue;
            }

            pending.target.AddLoss(pending.loss, CoinLossCause.Skill);
        }
    }

    private void TickDamageModifierDurations(int roundIndex)
    {
        for (int i = damageModifiers.Count - 1; i >= 0; i--)
        {
            DamageModifier modifier = damageModifiers[i];
            if (modifier == null)
            {
                damageModifiers.RemoveAt(i);
                continue;
            }

            if (modifier.remainingRounds < 0 || modifier.activateRound > roundIndex)
                continue;

            modifier.remainingRounds--;
            if (modifier.remainingRounds <= 0)
            {
                damageModifiers.RemoveAt(i);
            }
        }
    }

    private void TickProtectionDurations()
    {
        for (int i = protections.Count - 1; i >= 0; i--)
        {
            CoinProtection protection = protections[i];
            if (protection == null)
            {
                protections.RemoveAt(i);
                continue;
            }

            protection.remainingRounds--;
            if (protection.remainingRounds <= 0)
            {
                protections.RemoveAt(i);
            }
        }
    }

    private void TickDamageZones()
    {
        for (int i = damageZones.Count - 1; i >= 0; i--)
        {
            RoundDamageZone zone = damageZones[i];
            if (zone == null)
            {
                damageZones.RemoveAt(i);
                continue;
            }

            EnemyStats[] enemies = FindObjectsOfType<EnemyStats>();
            for (int j = 0; j < enemies.Length; j++)
            {
                EnemyStats enemy = enemies[j];
                if (enemy == null || enemy.IsDead)
                    continue;

                Vector3 delta = enemy.transform.position - zone.center;
                delta.y = 0f;

                if (delta.sqrMagnitude <= zone.radius * zone.radius)
                {
                    enemy.TakeDamage(zone.damage);
                }
            }

            zone.remainingRounds--;
            if (zone.remainingRounds <= 0)
            {
                damageZones.RemoveAt(i);
            }
        }
    }

    private void CleanupDestroyedReferences()
    {
        pendingCoinLosses.RemoveAll(pending => pending == null || pending.target == null);
        protections.RemoveAll(protection => protection == null || protection.target == null);
    }
}
