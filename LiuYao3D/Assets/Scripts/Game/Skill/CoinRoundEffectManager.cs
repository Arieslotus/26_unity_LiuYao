/// <summary>
/// 实现功能：统一管理碰撞技能产生的跨回合效果，包括增伤、延迟损耗、护盾、伤害区域与翻面条件。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public enum CoinSkillOutcomeType
{
    None,
    AddLoss,
    ReduceLoss,
    AddDamageModifier
}

public enum CoinSkillRuntimeEffectKind
{
    DamageModifier,
    PendingCoinLoss,
    CoinProtection,
    DamageZone,
    FlipCondition,
    UntilFlipDamageStack,
    ScheduledOutcome,
    TurnTriggerCounter,
    PhysicsModifier,
    EnemyShieldGenerationBlock
}

public readonly struct CoinSkillRuntimeEffectSnapshot
{
    public readonly int runtimeId;
    public readonly CoinSkillRuntimeEffectKind kind;
    public readonly string sourceId;
    public readonly TrigramCollisionSkillSO sourceSkill;
    public readonly TrigramType activeTrigram;
    public readonly TrigramType passiveTrigram;
    public readonly int remainingRounds;
    public readonly int stackCount;
    public readonly UnityEngine.Object target;
    public readonly IReadOnlyList<CoinStats> targets;
    public readonly float addDamagePercent;
    public readonly int loss;
    public readonly bool requireNoFlip;
    public readonly CoinSkillOutcomeConfig outcome;
    public readonly CoinSkillOutcomeConfig successOutcome;
    public readonly CoinSkillOutcomeConfig failureOutcome;
    public readonly CollisionSkillContext context;

    public CoinSkillRuntimeEffectSnapshot(
        int runtimeId,
        CoinSkillRuntimeEffectKind kind,
        string sourceId,
        TrigramCollisionSkillSO sourceSkill,
        int remainingRounds,
        int stackCount,
        UnityEngine.Object target,
        IReadOnlyList<CoinStats> targets = null,
        float addDamagePercent = 0f,
        int loss = 0,
        bool requireNoFlip = false,
        CoinSkillOutcomeConfig outcome = null,
        CoinSkillOutcomeConfig successOutcome = null,
        CoinSkillOutcomeConfig failureOutcome = null,
        CollisionSkillContext context = null)
    {
        this.runtimeId = runtimeId;
        this.kind = kind;
        this.sourceId = sourceId;
        this.sourceSkill = sourceSkill;
        this.activeTrigram = sourceSkill != null ? sourceSkill.ActiveTrigram : TrigramType.None;
        this.passiveTrigram = sourceSkill != null ? sourceSkill.PassiveTrigram : TrigramType.None;
        this.remainingRounds = remainingRounds;
        this.stackCount = stackCount;
        this.target = target;
        this.targets = targets;
        this.addDamagePercent = addDamagePercent;
        this.loss = loss;
        this.requireNoFlip = requireNoFlip;
        this.outcome = outcome;
        this.successOutcome = successOutcome;
        this.failureOutcome = failureOutcome;
        this.context = context;
    }
}

[Serializable]
public sealed class CoinSkillOutcomeConfig
{
    [Header("结果类型")]
    [SerializeField] private CoinSkillOutcomeType outcomeType = CoinSkillOutcomeType.None;

    [Header("目标")]
    [SerializeField] private CoinSkillTargetSelector targetSelector = new CoinSkillTargetSelector();

    [Header("损耗 / 恢复")]
    [Min(0)]
    [SerializeField] private int loss = 1;
    [Min(0)]
    [SerializeField] private int reduceLoss = 1;

    [Header("增伤")]
    [SerializeField] private float addDamagePercent = 30f;
    [SerializeField] private int durationRounds = -1;
    [Min(0)]
    [SerializeField] private int activateAfterRounds;
    [SerializeField] private bool stackable = true;

    public CoinSkillOutcomeType OutcomeType => outcomeType;
    public int Loss => loss;
    public int ReduceLoss => reduceLoss;
    public float AddDamagePercent => addDamagePercent;
    public int DurationRounds => durationRounds;
    public int ActivateAfterRounds => activateAfterRounds;

    public void Apply(CollisionSkillContext context, string fallbackSourceId)
    {
        if (outcomeType == CoinSkillOutcomeType.None)
            return;

        List<CoinStats> targets = targetSelector.Resolve(
            context,
            outcomeType == CoinSkillOutcomeType.ReduceLoss);

        if (outcomeType == CoinSkillOutcomeType.AddLoss)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].AddLoss(loss, CoinLossCause.Skill);
            }
            return;
        }

        if (outcomeType == CoinSkillOutcomeType.ReduceLoss)
        {
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (targets[i] == null || targets[i].CurrentLoss <= 0)
                {
                    targets.RemoveAt(i);
                }
            }

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].ReduceLoss(reduceLoss);
            }
            return;
        }

        if (outcomeType == CoinSkillOutcomeType.AddDamageModifier)
        {
            if (CoinRoundEffectManager.Instance == null)
                return;

            string sourceId = BuildChildSourceId(fallbackSourceId, "DamageModifier");

            CoinRoundEffectManager.Instance.AddDamageModifier(
                sourceId,
                addDamagePercent / 100f,
                durationRounds,
                activateAfterRounds,
                stackable,
                targets,
                context != null ? context.skill : null);
        }
    }

    public List<CoinStats> ResolveTargets(CollisionSkillContext context)
    {
        return targetSelector.Resolve(
            context,
            outcomeType == CoinSkillOutcomeType.ReduceLoss);
    }

    private static string BuildChildSourceId(string sourceId, string childName)
    {
        string root = string.IsNullOrWhiteSpace(sourceId) ? "CoinSkillEffect" : sourceId.Trim();
        string child = string.IsNullOrWhiteSpace(childName) ? "Child" : childName.Trim();
        return root + "/" + child;
    }

}

public class CoinRoundEffectManager : MonoBehaviour
{
    private sealed class DamageModifier
    {
        public int id;
        public string sourceId;
        public TrigramCollisionSkillSO sourceSkill;
        public float addPercent;
        public int activateRound;
        public int remainingRounds;
        public bool started;
        public List<CoinStats> targets;
        public int stackCount = 1;

        public bool ContainsTarget(CoinStats attacker)
        {
            if (targets == null)
                return true;

            return attacker != null && targets.Contains(attacker);
        }
    }

    private sealed class PendingCoinLoss
    {
        public int id;
        public string sourceId;
        public TrigramCollisionSkillSO sourceSkill;
        public CoinStats target;
        public int loss;
        public int executeRound;
        public TrigramType requiredCurrentTrigram;
    }

    private sealed class CoinProtection
    {
        public int id;
        public string sourceId;
        public CoinStats target;
        public int remainingRounds;
        public int remainingBlockCount;
    }

    private sealed class RoundDamageZone
    {
        public int id;
        public string sourceId;
        public GameObject instance;
        public Collider collider;
        public Vector3 fallbackCenter;
        public int damage;
        public int remainingTicks;
    }

    private sealed class FlipCondition
    {
        public int id;
        public CollisionSkillContext context;
        public string sourceId;
        public TrigramCollisionSkillSO sourceSkill;
        public List<CoinStats> watchedTargets;
        public Dictionary<CoinStats, int> startFlipVersions;
        public int checkRoundEnd;
        public bool requireNoFlip;
        public CoinSkillOutcomeConfig successOutcome;
        public CoinSkillOutcomeConfig failureOutcome;
    }

    private sealed class UntilFlipDamageStack
    {
        public int id;
        public CollisionSkillContext context;
        public string sourceId;
        public TrigramCollisionSkillSO sourceSkill;
        public List<CoinStats> watchedTargets;
        public Dictionary<CoinStats, int> startFlipVersions;
        public CoinSkillOutcomeConfig stackOutcome;
        public int maxStacks;
        public int appliedStacks;
        public int startRound;
    }

    private sealed class ScheduledOutcome
    {
        public int id;
        public CollisionSkillContext context;
        public string sourceId;
        public TrigramCollisionSkillSO sourceSkill;
        public int executeRound;
        public CoinSkillScheduleTiming timing;
        public CoinSkillOutcomeConfig outcome;
    }

    private sealed class TurnTriggerCounter
    {
        public int id;
        public string counterId;
        public string sourceId;
        public int round;
        public int count;
        public bool triggeredThisRound;
    }

    private sealed class PhysicsModifier
    {
        public int id;
        public string sourceId;
        public CoinPhysicsModifierType modifierType;
        public float multiplier;
        public List<CoinStats> targets;

        public bool ContainsTarget(CoinStats coin)
        {
            if (targets == null || targets.Count == 0)
                return true;

            return coin != null && targets.Contains(coin);
        }
    }

    private sealed class EnemyShieldGenerationBlock
    {
        public int id;
        public string sourceId;
        public TrigramCollisionSkillSO sourceSkill;
        public int remainingRounds;
    }

    public static CoinRoundEffectManager Instance { get; private set; }

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private readonly List<DamageModifier> damageModifiers = new List<DamageModifier>();
    private readonly List<PendingCoinLoss> pendingCoinLosses = new List<PendingCoinLoss>();
    private readonly List<CoinProtection> protections = new List<CoinProtection>();
    private readonly List<RoundDamageZone> damageZones = new List<RoundDamageZone>();
    private readonly List<FlipCondition> flipConditions = new List<FlipCondition>();
    private readonly List<UntilFlipDamageStack> untilFlipStacks = new List<UntilFlipDamageStack>();
    private readonly List<ScheduledOutcome> scheduledOutcomes = new List<ScheduledOutcome>();
    private readonly List<TurnTriggerCounter> turnTriggerCounters = new List<TurnTriggerCounter>();
    private readonly List<PhysicsModifier> physicsModifiers = new List<PhysicsModifier>();
    private readonly List<EnemyShieldGenerationBlock> enemyShieldGenerationBlocks = new List<EnemyShieldGenerationBlock>();
    private readonly Dictionary<CoinStats, int> coinFlipVersions = new Dictionary<CoinStats, int>();
    private readonly List<CoinRuntimeData> subscribedCoinData = new List<CoinRuntimeData>();

    private TurnManager subscribedTurnManager;
    private ChessTurnController subscribedChessTurnController;
    private int currentRound;
    private int nextRuntimeEffectId = 1;
    private int nextFlipVersion = 1;

    public int CurrentRound => currentRound;
    public event Action<int, string> DamageModifierStarted;
    public event Action<int, string> DamageModifierEnded;
    public event Action<int, CoinStats> CoinProtectionStarted;
    public event Action<int, CoinStats> CoinProtectionEnded;
    public event Action RuntimeEffectsChanged;

    private int AllocateRuntimeId()
    {
        return nextRuntimeEffectId++;
    }

    private void NotifyRuntimeEffectsChanged()
    {
        RuntimeEffectsChanged?.Invoke();
    }

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
        SubscribeChessTurnController();
        RefreshCoinSubscriptions();
    }

    private void OnEnable()
    {
        SubscribeTurnManager();
        SubscribeChessTurnController();
        RefreshCoinSubscriptions();
    }

    private void Update()
    {
        if (subscribedTurnManager == null && TurnManager.Instance != null)
        {
            SubscribeTurnManager();
        }

        if (subscribedChessTurnController == null)
        {
            SubscribeChessTurnController();
        }

        RefreshCoinSubscriptions();
    }

    private void OnDisable()
    {
        UnsubscribeTurnManager();
        UnsubscribeChessTurnController();
        UnsubscribeAllCoinData();
    }

    private void OnDestroy()
    {
        UnsubscribeTurnManager();
        UnsubscribeChessTurnController();
        UnsubscribeAllCoinData();

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
            if (modifier == null || modifier.activateRound > currentRound || !modifier.ContainsTarget(attacker))
                continue;

            total += modifier.addPercent;
        }

        return total;
    }

    public List<CoinSkillRuntimeEffectSnapshot> GetRuntimeEffectSnapshots()
    {
        List<CoinSkillRuntimeEffectSnapshot> result = new List<CoinSkillRuntimeEffectSnapshot>();

        for (int i = 0; i < damageModifiers.Count; i++)
        {
            DamageModifier modifier = damageModifiers[i];
            if (modifier == null)
                continue;

            result.Add(new CoinSkillRuntimeEffectSnapshot(
                modifier.id,
                CoinSkillRuntimeEffectKind.DamageModifier,
                modifier.sourceId,
                modifier.sourceSkill,
                modifier.remainingRounds,
                Mathf.Max(1, modifier.stackCount),
                null,
                modifier.targets,
                modifier.addPercent * 100f));
        }

        for (int i = 0; i < pendingCoinLosses.Count; i++)
        {
            PendingCoinLoss pending = pendingCoinLosses[i];
            if (pending == null || pending.target == null)
                continue;

            int remainingRounds = Mathf.Max(0, pending.executeRound - currentRound);
            result.Add(new CoinSkillRuntimeEffectSnapshot(
                pending.id,
                CoinSkillRuntimeEffectKind.PendingCoinLoss,
                pending.sourceId,
                pending.sourceSkill,
                remainingRounds,
                1,
                pending.target,
                new List<CoinStats> { pending.target },
                0f,
                pending.loss));
        }

        for (int i = 0; i < protections.Count; i++)
        {
            CoinProtection protection = protections[i];
            if (protection == null || protection.target == null)
                continue;

            result.Add(new CoinSkillRuntimeEffectSnapshot(
                protection.id,
                CoinSkillRuntimeEffectKind.CoinProtection,
                protection.sourceId,
                null,
                protection.remainingRounds,
                1,
                protection.target));
        }

        for (int i = 0; i < damageZones.Count; i++)
        {
            RoundDamageZone zone = damageZones[i];
            if (zone == null)
                continue;

            result.Add(new CoinSkillRuntimeEffectSnapshot(
                zone.id,
                CoinSkillRuntimeEffectKind.DamageZone,
                zone.sourceId,
                null,
                zone.remainingTicks,
                1,
                zone.instance));
        }

        for (int i = 0; i < flipConditions.Count; i++)
        {
            FlipCondition condition = flipConditions[i];
            if (condition == null)
                continue;

            int remainingRounds = Mathf.Max(0, condition.checkRoundEnd - currentRound + 1);
            result.Add(new CoinSkillRuntimeEffectSnapshot(
                condition.id,
                CoinSkillRuntimeEffectKind.FlipCondition,
                condition.sourceId,
                condition.sourceSkill,
                remainingRounds,
                1,
                null,
                condition.watchedTargets,
                0f,
                0,
                condition.requireNoFlip,
                null,
                condition.successOutcome,
                condition.failureOutcome,
                condition.context));
        }

        for (int i = 0; i < untilFlipStacks.Count; i++)
        {
            UntilFlipDamageStack stack = untilFlipStacks[i];
            if (stack == null)
                continue;

            result.Add(new CoinSkillRuntimeEffectSnapshot(
                stack.id,
                CoinSkillRuntimeEffectKind.UntilFlipDamageStack,
                stack.sourceId,
                stack.sourceSkill,
                Mathf.Max(0, stack.maxStacks - stack.appliedStacks),
                Mathf.Max(1, stack.appliedStacks),
                null,
                stack.watchedTargets,
                stack.stackOutcome != null && stack.stackOutcome.OutcomeType == CoinSkillOutcomeType.AddDamageModifier
                    ? stack.stackOutcome.AddDamagePercent
                    : 0f,
                0,
                true,
                stack.stackOutcome,
                null,
                null,
                stack.context));
        }

        for (int i = 0; i < scheduledOutcomes.Count; i++)
        {
            ScheduledOutcome scheduled = scheduledOutcomes[i];
            if (scheduled == null)
                continue;

            int remainingRounds = Mathf.Max(0, scheduled.executeRound - currentRound);
            result.Add(new CoinSkillRuntimeEffectSnapshot(
                scheduled.id,
                CoinSkillRuntimeEffectKind.ScheduledOutcome,
                scheduled.sourceId,
                scheduled.sourceSkill,
                remainingRounds,
                1,
                null,
                null,
                0f,
                0,
                false,
                scheduled.outcome,
                null,
                null,
                scheduled.context));
        }

        for (int i = 0; i < turnTriggerCounters.Count; i++)
        {
            TurnTriggerCounter counter = turnTriggerCounters[i];
            if (counter == null || counter.round != currentRound)
                continue;

            result.Add(new CoinSkillRuntimeEffectSnapshot(
                counter.id,
                CoinSkillRuntimeEffectKind.TurnTriggerCounter,
                counter.sourceId,
                null,
                0,
                Mathf.Max(1, counter.count),
                null));
        }

        for (int i = 0; i < physicsModifiers.Count; i++)
        {
            PhysicsModifier modifier = physicsModifiers[i];
            if (modifier == null)
                continue;

            result.Add(new CoinSkillRuntimeEffectSnapshot(
                modifier.id,
                CoinSkillRuntimeEffectKind.PhysicsModifier,
                modifier.sourceId,
                null,
                0,
                1,
                null));
        }

        for (int i = 0; i < enemyShieldGenerationBlocks.Count; i++)
        {
            EnemyShieldGenerationBlock block = enemyShieldGenerationBlocks[i];
            if (block == null)
                continue;

            result.Add(new CoinSkillRuntimeEffectSnapshot(
                block.id,
                CoinSkillRuntimeEffectKind.EnemyShieldGenerationBlock,
                block.sourceId,
                block.sourceSkill,
                block.remainingRounds,
                1,
                null));
        }

        return result;
    }

    public int AddDamageModifier(
        string sourceId,
        float addPercent,
        int durationRounds = -1,
        int activateAfterRounds = 0,
        bool stackable = true,
        IReadOnlyList<CoinStats> targets = null,
        TrigramCollisionSkillSO sourceSkill = null)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || Mathf.Approximately(addPercent, 0f))
            return 0;

        if (!stackable)
        {
            RemoveDamageModifiers(sourceId);
        }

        int activateRound = currentRound + Mathf.Max(0, activateAfterRounds);
        DamageModifier modifier = new DamageModifier
        {
            id = AllocateRuntimeId(),
            sourceId = sourceId,
            sourceSkill = sourceSkill,
            addPercent = addPercent,
            activateRound = activateRound,
            remainingRounds = durationRounds,
            started = false,
            targets = CopyTargets(targets)
        };

        damageModifiers.Add(modifier);
        RefreshDamageModifierStackCounts();
        TryStartDamageModifier(modifier);

        if (debugLog)
        {
            Debug.Log(
                $"[CoinRoundEffectManager] 添加伤害修正 | source:{sourceId} | add:{addPercent:P0} | " +
                $"duration:{durationRounds} | activateRound:{activateRound}"
            );
        }

        NotifyRuntimeEffectsChanged();
        return modifier.id;
    }

    public void RemoveDamageModifiers(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        for (int i = damageModifiers.Count - 1; i >= 0; i--)
        {
            DamageModifier modifier = damageModifiers[i];
            if (modifier == null || modifier.sourceId != sourceId)
                continue;

            damageModifiers.RemoveAt(i);
            NotifyDamageModifierEnded(modifier);
        }

        RefreshDamageModifierStackCounts();
        NotifyRuntimeEffectsChanged();
    }

    private void RefreshDamageModifierStackCounts()
    {
        for (int i = 0; i < damageModifiers.Count; i++)
        {
            DamageModifier modifier = damageModifiers[i];
            if (modifier == null)
                continue;

            int count = 0;
            for (int j = 0; j < damageModifiers.Count; j++)
            {
                DamageModifier other = damageModifiers[j];
                if (other == null)
                    continue;

                if (modifier.sourceId != other.sourceId)
                    continue;

                if (!HaveSameTargets(modifier.targets, other.targets))
                    continue;

                count++;
            }

            modifier.stackCount = Mathf.Max(1, count);
        }
    }

    private static bool HaveSameTargets(List<CoinStats> a, List<CoinStats> b)
    {
        if (a == null || a.Count == 0)
            return b == null || b.Count == 0;

        if (b == null || a.Count != b.Count)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            CoinStats target = a[i];
            if (target == null)
                continue;

            if (!b.Contains(target))
                return false;
        }

        return true;
    }

    public int ScheduleCoinLoss(
        CoinStats target,
        int loss,
        int delayRounds,
        TrigramType requiredCurrentTrigram = TrigramType.None,
        string sourceId = null,
        TrigramCollisionSkillSO sourceSkill = null)
    {
        if (target == null || loss <= 0)
            return 0;

        int runtimeId = AllocateRuntimeId();

        pendingCoinLosses.Add(new PendingCoinLoss
        {
            id = runtimeId,
            sourceId = sourceId,
            sourceSkill = sourceSkill,
            target = target,
            loss = loss,
            executeRound = currentRound + Mathf.Max(0, delayRounds),
            requiredCurrentTrigram = requiredCurrentTrigram
        });

        NotifyRuntimeEffectsChanged();
        return runtimeId;
    }

    public int GrantCoinProtection(CoinStats target, int durationRounds, int blockCount = 1, string sourceId = null)
    {
        if (target == null || durationRounds <= 0 || blockCount <= 0)
            return 0;

        int protectionId = AllocateRuntimeId();
        protections.Add(new CoinProtection
        {
            id = protectionId,
            sourceId = sourceId,
            target = target,
            remainingRounds = durationRounds,
            remainingBlockCount = blockCount
        });

        CoinProtectionStarted?.Invoke(protectionId, target);
        return protectionId;
    }

    public bool TryBlockCoinLoss(CoinStats target, int loss, CoinLossCause cause)
    {
        if (target == null || loss <= 0)
            return false;

        if (cause != CoinLossCause.EnemyAttack && cause != CoinLossCause.Skill)
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
                    $"[CoinRoundEffectManager] 护盾抵挡损耗 | coin:{target.name} | cause:{cause} | loss:{loss} | " +
                    $"remainingBlockCount:{protection.remainingBlockCount}"
                );
            }

            if (protection.remainingBlockCount <= 0)
            {
                protections.RemoveAt(i);
                NotifyProtectionEnded(protection);
            }

            return true;
        }

        return false;
    }

    public int AddRoundDamageZone(Vector3 center, float radius, int damage, int durationRounds, string sourceId = null)
    {
        if (radius <= 0f || damage <= 0 || durationRounds <= 0)
            return 0;

        GameObject root = new GameObject("SkillDamageZone_Runtime");
        root.transform.position = center;
        SphereCollider sphere = root.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = radius;

        return AddColliderDamageZone(root, sphere, center, damage, durationRounds, sourceId);
    }

    public int AddColliderDamageZone(Vector3 center, GameObject prefab, float scale, int damage, int durationTicks, string sourceId = null)
    {
        if (prefab == null || damage <= 0 || durationTicks <= 0)
            return 0;

        GameObject instance = Instantiate(prefab, center, prefab.transform.rotation);
        instance.transform.localScale *= Mathf.Max(0.01f, scale);

        Collider zoneCollider = instance.GetComponentInChildren<Collider>();
        if (zoneCollider == null)
        {
            Debug.LogWarning($"[CoinRoundEffectManager] 伤害圈预制体缺少 Collider | prefab:{prefab.name}");
            Destroy(instance);
            return 0;
        }

        return AddColliderDamageZone(instance, zoneCollider, center, damage, durationTicks, sourceId);
    }

    public int ScheduleFlipCondition(
        CollisionSkillContext context,
        string sourceId,
        IReadOnlyList<CoinStats> watchedTargets,
        int roundEndChecks,
        bool requireNoFlip,
        CoinSkillOutcomeConfig successOutcome,
        CoinSkillOutcomeConfig failureOutcome)
    {
        List<CoinStats> targets = CopyTargets(watchedTargets);
        if (targets == null || targets.Count == 0)
            return 0;

        int runtimeId = AllocateRuntimeId();

        flipConditions.Add(new FlipCondition
        {
            id = runtimeId,
            context = context,
            sourceId = sourceId,
            sourceSkill = context != null ? context.skill : null,
            watchedTargets = targets,
            startFlipVersions = CaptureFlipVersions(targets),
            checkRoundEnd = currentRound + Mathf.Max(0, roundEndChecks - 1),
            requireNoFlip = requireNoFlip,
            successOutcome = successOutcome,
            failureOutcome = failureOutcome
        });

        NotifyRuntimeEffectsChanged();
        return runtimeId;
    }

    public int StartUntilFlipDamageStacks(
        CollisionSkillContext context,
        string sourceId,
        IReadOnlyList<CoinStats> watchedTargets,
        int maxStacks,
        CoinSkillOutcomeConfig stackOutcome)
    {
        List<CoinStats> targets = CopyTargets(watchedTargets);
        if (targets == null || targets.Count == 0 || maxStacks <= 0 || stackOutcome == null)
            return 0;

        int runtimeId = AllocateRuntimeId();

        untilFlipStacks.Add(new UntilFlipDamageStack
        {
            id = runtimeId,
            context = context,
            sourceId = sourceId,
            sourceSkill = context != null ? context.skill : null,
            watchedTargets = targets,
            startFlipVersions = CaptureFlipVersions(targets),
            stackOutcome = stackOutcome,
            maxStacks = maxStacks,
            appliedStacks = 0,
            startRound = currentRound + 1
        });

        NotifyRuntimeEffectsChanged();
        return runtimeId;
    }

    public int ScheduleOutcome(
        CollisionSkillContext context,
        string sourceId,
        int delayRounds,
        CoinSkillScheduleTiming timing,
        CoinSkillOutcomeConfig outcome)
    {
        if (outcome == null || outcome.OutcomeType == CoinSkillOutcomeType.None)
            return 0;

        int safeDelay = Mathf.Max(0, delayRounds);
        if (safeDelay == 0 && timing == CoinSkillScheduleTiming.RoundStarted)
        {
            outcome.Apply(context, BuildChildSourceId(sourceId, "ImmediateOutcome"));
            return 0;
        }

        int runtimeId = AllocateRuntimeId();
        scheduledOutcomes.Add(new ScheduledOutcome
        {
            id = runtimeId,
            context = context,
            sourceId = sourceId,
            sourceSkill = context != null ? context.skill : null,
            executeRound = currentRound + safeDelay,
            timing = timing,
            outcome = outcome
        });

        NotifyRuntimeEffectsChanged();
        return runtimeId;
    }

    public bool RecordTurnTrigger(
        CollisionSkillContext context,
        string counterId,
        string sourceId,
        int triggerLimit,
        TurnTriggerCountMode triggerMode,
        TurnTriggerOverLimitAction overLimitAction,
        CoinSkillOutcomeConfig overLimitOutcome)
    {
        if (string.IsNullOrWhiteSpace(counterId))
            return false;

        TurnTriggerCounter counter = FindOrCreateTurnTriggerCounter(counterId.Trim(), sourceId);
        counter.count++;

        if (counter.count <= Mathf.Max(0, triggerLimit))
            return false;

        if (triggerMode == TurnTriggerCountMode.OncePerRoundOverLimit && counter.triggeredThisRound)
            return true;

        counter.triggeredThisRound = true;
        if (overLimitAction != TurnTriggerOverLimitAction.StopSkillOnly && overLimitOutcome != null)
        {
            overLimitOutcome.Apply(context, BuildChildSourceId(sourceId, "OverLimitOutcome"));
        }

        return true;
    }

    public int AddPhysicsModifier(
        string sourceId,
        CoinPhysicsModifierType modifierType,
        float multiplier,
        IReadOnlyList<CoinStats> targets = null)
    {
        if (multiplier < 0f)
            return 0;

        int runtimeId = AllocateRuntimeId();
        physicsModifiers.Add(new PhysicsModifier
        {
            id = runtimeId,
            sourceId = sourceId,
            modifierType = modifierType,
            multiplier = multiplier,
            targets = CopyTargets(targets)
        });

        return runtimeId;
    }

    public float GetPhysicsModifierMultiplier(CoinPhysicsModifierType modifierType, CoinStats coin)
    {
        float result = 1f;

        for (int i = 0; i < physicsModifiers.Count; i++)
        {
            PhysicsModifier modifier = physicsModifiers[i];
            if (modifier == null || modifier.modifierType != modifierType)
                continue;

            if (!modifier.ContainsTarget(coin))
                continue;

            result *= modifier.multiplier;
        }

        return result;
    }

    public int BlockEnemyShieldGeneration(int roundCount, string sourceId = null, TrigramCollisionSkillSO sourceSkill = null)
    {
        int safeRoundCount = Mathf.Max(1, roundCount);
        int runtimeId = AllocateRuntimeId();
        enemyShieldGenerationBlocks.Add(new EnemyShieldGenerationBlock
        {
            id = runtimeId,
            sourceId = sourceId,
            sourceSkill = sourceSkill,
            remainingRounds = safeRoundCount
        });

        if (debugLog)
        {
            Debug.Log($"[CoinRoundEffectManager] 停止敌方护盾生成 | source:{sourceId} | rounds:{safeRoundCount}");
        }

        NotifyRuntimeEffectsChanged();
        return runtimeId;
    }

    public bool IsEnemyShieldGenerationBlocked()
    {
        for (int i = 0; i < enemyShieldGenerationBlocks.Count; i++)
        {
            EnemyShieldGenerationBlock block = enemyShieldGenerationBlocks[i];
            if (block != null && block.remainingRounds > 0)
                return true;
        }

        return false;
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

    private int AddColliderDamageZone(GameObject instance, Collider zoneCollider, Vector3 fallbackCenter, int damage, int durationTicks, string sourceId = null)
    {
        int runtimeId = AllocateRuntimeId();
        damageZones.Add(new RoundDamageZone
        {
            id = runtimeId,
            sourceId = sourceId,
            instance = instance,
            collider = zoneCollider,
            fallbackCenter = fallbackCenter,
            damage = damage,
            remainingTicks = durationTicks
        });

        return runtimeId;
    }

    private TurnTriggerCounter FindOrCreateTurnTriggerCounter(string counterId, string sourceId)
    {
        for (int i = 0; i < turnTriggerCounters.Count; i++)
        {
            TurnTriggerCounter counter = turnTriggerCounters[i];
            if (counter == null || counter.counterId != counterId)
                continue;

            if (counter.round != currentRound)
            {
                counter.round = currentRound;
                counter.count = 0;
                counter.triggeredThisRound = false;
            }

            return counter;
        }

        TurnTriggerCounter created = new TurnTriggerCounter
        {
            id = AllocateRuntimeId(),
            counterId = counterId,
            sourceId = sourceId,
            round = currentRound,
            count = 0,
            triggeredThisRound = false
        };

        turnTriggerCounters.Add(created);
        return created;
    }

    private static List<CoinStats> CopyTargets(IReadOnlyList<CoinStats> targets)
    {
        if (targets == null)
            return null;

        List<CoinStats> result = new List<CoinStats>();
        for (int i = 0; i < targets.Count; i++)
        {
            CoinStats target = targets[i];
            if (target == null || result.Contains(target))
                continue;

            result.Add(target);
        }

        return result;
    }

    private Dictionary<CoinStats, int> CaptureFlipVersions(IReadOnlyList<CoinStats> targets)
    {
        Dictionary<CoinStats, int> result = new Dictionary<CoinStats, int>();
        for (int i = 0; i < targets.Count; i++)
        {
            CoinStats target = targets[i];
            if (target == null || result.ContainsKey(target))
                continue;

            result.Add(target, GetFlipVersion(target));
        }

        return result;
    }

    private int GetFlipVersion(CoinStats target)
    {
        int version;
        return target != null && coinFlipVersions.TryGetValue(target, out version) ? version : 0;
    }

    private bool HasAnyWatchedCoinFlipped(IReadOnlyList<CoinStats> targets, Dictionary<CoinStats, int> startVersions)
    {
        if (targets == null || startVersions == null)
            return false;

        for (int i = 0; i < targets.Count; i++)
        {
            CoinStats target = targets[i];
            if (target == null)
                continue;

            int startVersion;
            startVersions.TryGetValue(target, out startVersion);
            if (GetFlipVersion(target) > startVersion)
                return true;
        }

        return false;
    }

    private void MarkCoinFlipped(CoinStats stats, string source)
    {
        if (stats == null)
            return;

        coinFlipVersions[stats] = nextFlipVersion++;

        if (debugLog)
        {
            Debug.Log($"[CoinRoundEffectManager] 记录硬币翻面 | coin:{stats.name} | source:{source} | round:{currentRound}");
        }
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

    private void SubscribeChessTurnController()
    {
        ChessTurnController controller = FindObjectOfType<ChessTurnController>();
        if (subscribedChessTurnController == controller)
            return;

        UnsubscribeChessTurnController();
        subscribedChessTurnController = controller;

        if (subscribedChessTurnController != null)
        {
            subscribedChessTurnController.PieceActionResolved += OnPieceActionResolved;
        }
    }

    private void UnsubscribeChessTurnController()
    {
        if (subscribedChessTurnController == null)
            return;

        subscribedChessTurnController.PieceActionResolved -= OnPieceActionResolved;
        subscribedChessTurnController = null;
    }

    private void RefreshCoinSubscriptions()
    {
        CoinRuntimeData[] coinDataArray = FindObjectsOfType<CoinRuntimeData>();
        for (int i = 0; i < coinDataArray.Length; i++)
        {
            CoinRuntimeData data = coinDataArray[i];
            if (data == null || subscribedCoinData.Contains(data))
                continue;

            data.RuntimeTrigramChanged += OnRuntimeTrigramChanged;
            subscribedCoinData.Add(data);
        }

        for (int i = subscribedCoinData.Count - 1; i >= 0; i--)
        {
            if (subscribedCoinData[i] != null)
                continue;

            subscribedCoinData.RemoveAt(i);
        }
    }

    private void UnsubscribeAllCoinData()
    {
        for (int i = 0; i < subscribedCoinData.Count; i++)
        {
            CoinRuntimeData data = subscribedCoinData[i];
            if (data != null)
            {
                data.RuntimeTrigramChanged -= OnRuntimeTrigramChanged;
            }
        }

        subscribedCoinData.Clear();
    }

    private void OnRuntimeTrigramChanged(CoinRuntimeData data, TrigramType oldTrigram, TrigramType newTrigram)
    {
        if (data == null || oldTrigram == newTrigram)
            return;

        CoinStats stats = data.GetComponent<CoinStats>();
        MarkCoinFlipped(stats, "RuntimeSetFace");
    }

    private void OnPieceActionResolved(ChessPiece piece, TrigramType startTrigram, TrigramType endTrigram)
    {
        if (piece == null || startTrigram == endTrigram)
            return;

        CoinStats stats = piece.GetComponent<CoinStats>();
        MarkCoinFlipped(stats, "PlayerActionResolved");
    }

    private void OnRoundStarted(int roundIndex)
    {
        currentRound = roundIndex;
        ExecuteScheduledOutcomes(roundIndex, CoinSkillScheduleTiming.RoundStarted);
        StartReadyDamageModifiers();
        ExecutePendingCoinLosses(roundIndex);
        TickUntilFlipStacks(roundIndex);
        CleanupDestroyedReferences();
        NotifyRuntimeEffectsChanged();
    }

    private void OnRoundEnded(int roundIndex)
    {
        ExecuteScheduledOutcomes(roundIndex, CoinSkillScheduleTiming.RoundEnded);
        TickDamageZones();
        ResolveFlipConditions(roundIndex);
        TickDamageModifierDurations(roundIndex);
        TickProtectionDurations();
        TickEnemyShieldGenerationBlocks();
        ClearRoundOnlyEffects();
        CleanupDestroyedReferences();
        NotifyRuntimeEffectsChanged();
    }

    private void TickEnemyShieldGenerationBlocks()
    {
        for (int i = enemyShieldGenerationBlocks.Count - 1; i >= 0; i--)
        {
            EnemyShieldGenerationBlock block = enemyShieldGenerationBlocks[i];
            if (block == null)
            {
                enemyShieldGenerationBlocks.RemoveAt(i);
                continue;
            }

            block.remainingRounds--;
            if (block.remainingRounds <= 0)
            {
                enemyShieldGenerationBlocks.RemoveAt(i);
            }
        }
    }

    private void ExecuteScheduledOutcomes(int roundIndex, CoinSkillScheduleTiming timing)
    {
        for (int i = scheduledOutcomes.Count - 1; i >= 0; i--)
        {
            ScheduledOutcome scheduled = scheduledOutcomes[i];
            if (scheduled == null)
            {
                scheduledOutcomes.RemoveAt(i);
                continue;
            }

            if (scheduled.timing != timing || scheduled.executeRound > roundIndex)
                continue;

            scheduledOutcomes.RemoveAt(i);

            if (scheduled.outcome != null)
            {
                scheduled.outcome.Apply(scheduled.context, BuildChildSourceId(scheduled.sourceId, "ScheduledOutcome"));
            }
        }
    }

    private void ClearRoundOnlyEffects()
    {
        physicsModifiers.Clear();
        turnTriggerCounters.RemoveAll(counter => counter == null || counter.round <= currentRound);
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
        bool removedAny = false;
        for (int i = damageModifiers.Count - 1; i >= 0; i--)
        {
            DamageModifier modifier = damageModifiers[i];
            if (modifier == null)
            {
                damageModifiers.RemoveAt(i);
                removedAny = true;
                continue;
            }

            if (modifier.remainingRounds < 0 || modifier.activateRound > roundIndex)
                continue;

            modifier.remainingRounds--;
            if (modifier.remainingRounds <= 0)
            {
                damageModifiers.RemoveAt(i);
                removedAny = true;
                NotifyDamageModifierEnded(modifier);
            }
        }

        if (removedAny)
        {
            RefreshDamageModifierStackCounts();
        }
    }

    private void StartReadyDamageModifiers()
    {
        for (int i = 0; i < damageModifiers.Count; i++)
        {
            TryStartDamageModifier(damageModifiers[i]);
        }
    }

    private void TryStartDamageModifier(DamageModifier modifier)
    {
        if (modifier == null || modifier.started || modifier.activateRound > currentRound)
            return;

        modifier.started = true;
        DamageModifierStarted?.Invoke(modifier.id, modifier.sourceId);
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
                NotifyProtectionEnded(protection);
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

            if (zone.collider == null)
            {
                DestroyDamageZone(zone);
                damageZones.RemoveAt(i);
                continue;
            }

            List<EnemyStats> enemies = FindEnemiesInsideZone(zone);
            for (int j = 0; j < enemies.Count; j++)
            {
                enemies[j].TakeDamage(zone.damage);
            }

            zone.remainingTicks--;
            if (zone.remainingTicks <= 0)
            {
                DestroyDamageZone(zone);
                damageZones.RemoveAt(i);
            }
        }
    }

    private List<EnemyStats> FindEnemiesInsideZone(RoundDamageZone zone)
    {
        List<EnemyStats> result = new List<EnemyStats>();
        Collider[] hits = GetZoneOverlap(zone.collider);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            EnemyStats enemy = hit.GetComponentInParent<EnemyStats>();
            if (enemy == null || enemy.IsDead || result.Contains(enemy))
                continue;

            result.Add(enemy);
        }

        return result;
    }

    private Collider[] GetZoneOverlap(Collider zoneCollider)
    {
        SphereCollider sphere = zoneCollider as SphereCollider;
        if (sphere != null)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            float radius = sphere.radius * MaxAbsScale(sphere.transform.lossyScale);
            return Physics.OverlapSphere(center, radius);
        }

        BoxCollider box = zoneCollider as BoxCollider;
        if (box != null)
        {
            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, AbsScale(box.transform.lossyScale));
            return Physics.OverlapBox(center, halfExtents, box.transform.rotation);
        }

        CapsuleCollider capsule = zoneCollider as CapsuleCollider;
        if (capsule != null)
        {
            Vector3 center = capsule.transform.TransformPoint(capsule.center);
            Vector3 axis = GetCapsuleAxis(capsule);
            float scaleOnAxis = Mathf.Abs(Vector3.Dot(capsule.transform.lossyScale, AbsVector(axis)));
            float height = Mathf.Max(capsule.height * scaleOnAxis, capsule.radius * 2f);
            float radius = capsule.radius * MaxCapsuleRadiusScale(capsule);
            float halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
            Vector3 p1 = center + axis * halfSegment;
            Vector3 p2 = center - axis * halfSegment;
            return Physics.OverlapCapsule(p1, p2, radius);
        }

        Bounds bounds = zoneCollider.bounds;
        return Physics.OverlapBox(bounds.center, bounds.extents, zoneCollider.transform.rotation);
    }

    private void ResolveFlipConditions(int roundIndex)
    {
        for (int i = flipConditions.Count - 1; i >= 0; i--)
        {
            FlipCondition condition = flipConditions[i];
            if (condition == null || condition.checkRoundEnd > roundIndex)
                continue;

            flipConditions.RemoveAt(i);

            bool flipped = HasAnyWatchedCoinFlipped(condition.watchedTargets, condition.startFlipVersions);
            bool success = condition.requireNoFlip ? !flipped : flipped;
            CoinSkillOutcomeConfig outcome = success ? condition.successOutcome : condition.failureOutcome;

            if (outcome != null)
            {
                string outcomeSourceId = BuildChildSourceId(
                    condition.sourceId,
                    success ? "SuccessOutcome" : "FailureOutcome");
                outcome.Apply(condition.context, outcomeSourceId);
            }
        }
    }

    private void TickUntilFlipStacks(int roundIndex)
    {
        for (int i = untilFlipStacks.Count - 1; i >= 0; i--)
        {
            UntilFlipDamageStack stack = untilFlipStacks[i];
            if (stack == null)
            {
                untilFlipStacks.RemoveAt(i);
                continue;
            }

            if (roundIndex < stack.startRound)
                continue;

            if (HasAnyWatchedCoinFlipped(stack.watchedTargets, stack.startFlipVersions))
            {
                untilFlipStacks.RemoveAt(i);
                continue;
            }

            stack.stackOutcome.Apply(stack.context, BuildChildSourceId(stack.sourceId, "StackOutcome"));
            stack.appliedStacks++;

            if (stack.appliedStacks >= stack.maxStacks)
            {
                untilFlipStacks.RemoveAt(i);
            }
        }
    }

    private void CleanupDestroyedReferences()
    {
        pendingCoinLosses.RemoveAll(pending => pending == null || pending.target == null);
        List<CoinStats> removedCoins = null;
        foreach (KeyValuePair<CoinStats, int> pair in coinFlipVersions)
        {
            if (pair.Key != null)
                continue;

            if (removedCoins == null)
            {
                removedCoins = new List<CoinStats>();
            }

            removedCoins.Add(pair.Key);
        }

        if (removedCoins != null)
        {
            for (int i = 0; i < removedCoins.Count; i++)
            {
                coinFlipVersions.Remove(removedCoins[i]);
            }
        }

        for (int i = protections.Count - 1; i >= 0; i--)
        {
            CoinProtection protection = protections[i];
            if (protection != null && protection.target != null)
                continue;

            protections.RemoveAt(i);
            NotifyProtectionEnded(protection);
        }
    }

    private void DestroyDamageZone(RoundDamageZone zone)
    {
        if (zone != null && zone.instance != null)
        {
            Destroy(zone.instance);
        }
    }

    private void NotifyProtectionEnded(CoinProtection protection)
    {
        if (protection == null || protection.id <= 0)
            return;

        CoinProtectionEnded?.Invoke(protection.id, protection.target);
    }

    private void NotifyDamageModifierEnded(DamageModifier modifier)
    {
        if (modifier == null || modifier.id <= 0)
            return;

        DamageModifierEnded?.Invoke(modifier.id, modifier.sourceId);
    }

    private static string BuildChildSourceId(string sourceId, string childName)
    {
        string root = string.IsNullOrWhiteSpace(sourceId) ? "CoinSkillEffect" : sourceId.Trim();
        string child = string.IsNullOrWhiteSpace(childName) ? "Child" : childName.Trim();
        return root + "/" + child;
    }

    private static Vector3 AbsScale(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private static Vector3 AbsVector(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private static float MaxAbsScale(Vector3 scale)
    {
        scale = AbsScale(scale);
        return Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
    }

    private static Vector3 GetCapsuleAxis(CapsuleCollider capsule)
    {
        if (capsule.direction == 0)
            return capsule.transform.right;

        if (capsule.direction == 1)
            return capsule.transform.up;

        return capsule.transform.forward;
    }

    private static float MaxCapsuleRadiusScale(CapsuleCollider capsule)
    {
        Vector3 scale = AbsScale(capsule.transform.lossyScale);
        if (capsule.direction == 0)
            return Mathf.Max(scale.y, scale.z);

        if (capsule.direction == 1)
            return Mathf.Max(scale.x, scale.z);

        return Mathf.Max(scale.x, scale.y);
    }
}
