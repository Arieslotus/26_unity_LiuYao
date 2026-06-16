/// <summary>
/// 实现功能：监听战斗表现事件，并按配置播放基础碰撞、八卦冲击波与通用受伤特效。
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionVfxController : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private CollisionVfxConfig config;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private void OnEnable()
    {
        CombatVfxEvents.CoinCollisionRequested += OnCoinCollisionRequested;
        CombatVfxEvents.CoinEnemyCollisionRequested += OnCoinEnemyCollisionRequested;
        CombatVfxEvents.EnemyDamagedRequested += OnEnemyDamagedRequested;
        CombatVfxEvents.CoinDamagedRequested += OnCoinDamagedRequested;
        CombatVfxEvents.CoinHealedRequested += OnCoinHealedRequested;
        CombatVfxEvents.DamageModifierAddedRequested += OnDamageModifierAddedRequested;
    }

    private void OnDisable()
    {
        CombatVfxEvents.CoinCollisionRequested -= OnCoinCollisionRequested;
        CombatVfxEvents.CoinEnemyCollisionRequested -= OnCoinEnemyCollisionRequested;
        CombatVfxEvents.EnemyDamagedRequested -= OnEnemyDamagedRequested;
        CombatVfxEvents.CoinDamagedRequested -= OnCoinDamagedRequested;
        CombatVfxEvents.CoinHealedRequested -= OnCoinHealedRequested;
        CombatVfxEvents.DamageModifierAddedRequested -= OnDamageModifierAddedRequested;
    }

    private void OnCoinCollisionRequested(ChessPiece activePiece, ChessPiece passivePiece, Vector3 hitPoint)
    {
        if (!ValidateConfig())
            return;

        Spawn(config.coinCollisionPrefab, hitPoint, Quaternion.identity, null, config.collisionLifetime);

        StartCoroutine(PlayShockwaves(
            hitPoint,
            activePiece != null ? activePiece.CurrentTrigram : TrigramType.None,
            passivePiece != null ? passivePiece.CurrentTrigram : TrigramType.None));
    }

    private void OnCoinEnemyCollisionRequested(ChessPiece activePiece, EnemyStats enemy, Vector3 hitPoint)
    {
        if (!ValidateConfig())
            return;

        GameObject prefab = config.coinEnemyCollisionPrefab != null
            ? config.coinEnemyCollisionPrefab
            : config.coinCollisionPrefab;

        Spawn(prefab, hitPoint, Quaternion.identity, null, config.collisionLifetime);
    }

    private void OnEnemyDamagedRequested(EnemyStats enemy, int damage, Vector3 hitPoint)
    {
        if (!ValidateConfig() || enemy == null)
            return;

        Transform parent = config.parentDamagedVfxToTarget ? enemy.transform : null;
        Vector3 position = hitPoint != Vector3.zero ? hitPoint : enemy.transform.position;
        Spawn(config.enemyDamagedPrefab, position, Quaternion.identity, parent, config.damagedLifetime);
    }

    private void OnCoinDamagedRequested(CoinStats coin, int loss, CoinLossCause cause, Vector3 hitPoint)
    {
        if (!ValidateConfig() || coin == null)
            return;

        if (cause == CoinLossCause.Operation)
            return;

        Transform parent = config.parentDamagedVfxToTarget ? coin.transform : null;
        Vector3 position = hitPoint != Vector3.zero ? hitPoint : coin.transform.position;
        Spawn(config.coinDamagedPrefab, position, Quaternion.identity, parent, config.damagedLifetime);
    }

    private void OnCoinHealedRequested(CoinStats coin, int reduceLoss, Vector3 hitPoint)
    {
        if (!ValidateConfig() || coin == null)
            return;

        SkillEffectVfxPlayer.PlayForCoins(config.coinHealedVfx, new List<CoinStats> { coin });
    }

    private void OnDamageModifierAddedRequested(int modifierId, IReadOnlyList<CoinStats> targets, int activateAfterRounds)
    {
        if (!ValidateConfig())
            return;

        SkillEffectVfxPlayer.PlayForDamageModifierTargets(
            config.damageModifierVfx,
            targets,
            modifierId,
            activateAfterRounds);
    }

    private IEnumerator PlayShockwaves(Vector3 hitPoint, TrigramType firstTrigram, TrigramType secondTrigram)
    {
        SpawnShockwave(firstTrigram, hitPoint);

        if (config.shockwaveInterval > 0f)
        {
            yield return new WaitForSecondsRealtime(config.shockwaveInterval);
        }

        SpawnShockwave(secondTrigram, hitPoint);
    }

    private void SpawnShockwave(TrigramType trigram, Vector3 hitPoint)
    {
        if (trigram == TrigramType.None)
            return;

        GameObject prefab = config.GetShockwavePrefab(trigram);
        if (prefab == null)
        {
            if (debugLog)
            {
                Debug.LogWarning($"[CollisionVfxController] 未配置冲击波预制体 | trigram:{trigram}");
            }

            return;
        }

        Spawn(prefab, hitPoint, Quaternion.identity, null, config.shockwaveLifetime);
    }

    private GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, float lifetime)
    {
        if (prefab == null)
            return null;

        GameObject instance = Instantiate(prefab, position + config.worldOffset, rotation, parent);
        ApplyParticleTimeMode(instance);

        if (lifetime > 0f)
        {
            Destroy(instance, lifetime);
        }

        return instance;
    }

    private void ApplyParticleTimeMode(GameObject instance)
    {
        if (instance == null || config == null || !config.useUnscaledTimeForParticles)
            return;

        ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = particleSystems[i].main;
            main.useUnscaledTime = true;
        }
    }

    private bool ValidateConfig()
    {
        if (config != null)
            return true;

        if (debugLog)
        {
            Debug.LogWarning($"[CollisionVfxController] 未配置 CollisionVfxConfig | object:{name}");
        }

        return false;
    }
}

public enum SkillEffectVfxLifetimeMode
{
    Instant,
    Timed,
    Persistent
}

[System.Serializable]
public sealed class SkillEffectVfxData
{
    [Header("特效预制体")]
    [SerializeField] private GameObject prefab;

    [Header("生成目标")]
    [SerializeField] private CollisionSkillTargetType targetType = CollisionSkillTargetType.ActiveCoin;

    [Tooltip("目标为碰撞范围内敌人时使用。")]
    [Min(0f)]
    [SerializeField] private float radius = 3f;

    [Tooltip("是否挂到目标物体下。适合护盾、回血、增益光环。")]
    [SerializeField] private bool parentToTarget = true;

    [Tooltip("生成时追加的世界坐标偏移。")]
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    [Header("生命周期")]
    [SerializeField] private SkillEffectVfxLifetimeMode lifetimeMode = SkillEffectVfxLifetimeMode.Instant;

    [Tooltip("Instant/Timed 模式下的销毁时间。小于等于 0 时不自动销毁。")]
    [Min(0f)]
    [SerializeField] private float lifetime = 2f;

    [Tooltip("延迟播放秒数，使用真实时间，不受慢动作影响。")]
    [Min(0f)]
    [SerializeField] private float delay;

    [Tooltip("生成后自动将所有 ParticleSystem 设置为不受 Time.timeScale 影响。")]
    [SerializeField] private bool useUnscaledTimeForParticles = true;

    public GameObject Prefab => prefab;
    public CollisionSkillTargetType TargetType => targetType;
    public float Radius => radius;
    public bool ParentToTarget => parentToTarget;
    public Vector3 WorldOffset => worldOffset;
    public SkillEffectVfxLifetimeMode LifetimeMode => lifetimeMode;
    public float Lifetime => lifetime;
    public float Delay => delay;
    public bool UseUnscaledTimeForParticles => useUnscaledTimeForParticles;
    public bool HasPrefab => prefab != null;
}

public class SkillEffectVfxPlayer : MonoBehaviour
{
    private sealed class PendingDamageModifierVfx
    {
        public SkillEffectVfxData vfx;
        public readonly List<Transform> targets = new List<Transform>();
        public readonly List<Vector3> fallbackPositions = new List<Vector3>();
    }

    private static SkillEffectVfxPlayer instance;

    private readonly Dictionary<string, List<GameObject>> persistentInstances = new Dictionary<string, List<GameObject>>();
    private readonly Dictionary<int, PendingDamageModifierVfx> pendingDamageModifierVfx = new Dictionary<int, PendingDamageModifierVfx>();
    private readonly HashSet<string> cancelledPersistentKeys = new HashSet<string>();

    private static SkillEffectVfxPlayer Instance
    {
        get
        {
            if (instance != null)
                return instance;

            SkillEffectVfxPlayer existing = FindObjectOfType<SkillEffectVfxPlayer>();
            if (existing != null)
            {
                instance = existing;
                return instance;
            }

            GameObject root = new GameObject(nameof(SkillEffectVfxPlayer));
            instance = root.AddComponent<SkillEffectVfxPlayer>();
            return instance;
        }
    }

    private CoinRoundEffectManager subscribedRoundEffectManager;

    private void OnEnable()
    {
        if (instance == null)
        {
            instance = this;
        }

        TrySubscribeRoundEffectManager();
    }

    private void Update()
    {
        TrySubscribeRoundEffectManager();
    }

    private void OnDisable()
    {
        UnsubscribeRoundEffectManager();
    }

    public static void PlayForCoins(SkillEffectVfxData vfx, IReadOnlyList<CoinStats> targets)
    {
        if (vfx == null || !vfx.HasPrefab || targets == null)
            return;

        for (int i = 0; i < targets.Count; i++)
        {
            CoinStats target = targets[i];
            if (target == null)
                continue;

            Instance.Play(vfx, target.transform, target.transform.position, null);
        }
    }

    public static void PlayForEnemies(SkillEffectVfxData vfx, IReadOnlyList<EnemyStats> targets)
    {
        if (vfx == null || !vfx.HasPrefab || targets == null)
            return;

        for (int i = 0; i < targets.Count; i++)
        {
            EnemyStats target = targets[i];
            if (target == null)
                continue;

            Instance.Play(vfx, target.transform, target.transform.position, null);
        }
    }

    public static void PlayForProtection(SkillEffectVfxData vfx, CoinStats target, int protectionId)
    {
        if (vfx == null || !vfx.HasPrefab || target == null || protectionId <= 0)
            return;

        Instance.Play(vfx, target.transform, target.transform.position, BuildProtectionKey(protectionId));
    }

    public static void PlayForDamageModifier(
        SkillEffectVfxData vfx,
        CollisionSkillContext context,
        int modifierId,
        int activateAfterRounds)
    {
        if (vfx == null || !vfx.HasPrefab || context == null)
            return;

        if (modifierId <= 0 || vfx.LifetimeMode != SkillEffectVfxLifetimeMode.Persistent)
        {
            PlayByContext(vfx, context);
            return;
        }

        Instance.TrySubscribeRoundEffectManager();

        PendingDamageModifierVfx pending = Instance.CreatePendingDamageModifierVfx(vfx, context);
        if (pending.targets.Count <= 0)
            return;

        if (activateAfterRounds <= 0)
        {
            Instance.PlayPendingDamageModifierVfx(modifierId, pending);
            return;
        }

        Instance.pendingDamageModifierVfx[modifierId] = pending;
    }

    public static void PlayForDamageModifierTargets(
        SkillEffectVfxData vfx,
        IReadOnlyList<CoinStats> targets,
        int modifierId,
        int activateAfterRounds)
    {
        if (vfx == null || !vfx.HasPrefab || targets == null)
            return;

        if (modifierId <= 0 || vfx.LifetimeMode != SkillEffectVfxLifetimeMode.Persistent)
        {
            PlayForCoins(vfx, targets);
            return;
        }

        Instance.TrySubscribeRoundEffectManager();

        PendingDamageModifierVfx pending = Instance.CreatePendingDamageModifierVfx(vfx, targets);
        if (pending.targets.Count <= 0)
            return;

        if (activateAfterRounds <= 0)
        {
            Instance.PlayPendingDamageModifierVfx(modifierId, pending);
            return;
        }

        Instance.pendingDamageModifierVfx[modifierId] = pending;
    }

    public static void PlayByContext(SkillEffectVfxData vfx, CollisionSkillContext context)
    {
        if (vfx == null || !vfx.HasPrefab || context == null)
            return;

        List<CoinStats> coinTargets = CollisionSkillTargetResolver.ResolveCoins(context, vfx.TargetType);
        if (coinTargets.Count > 0)
        {
            PlayForCoins(vfx, coinTargets);
            return;
        }

        List<EnemyStats> enemyTargets = CollisionSkillTargetResolver.ResolveEnemies(context, vfx.TargetType, vfx.Radius);
        if (enemyTargets.Count > 0)
        {
            PlayForEnemies(vfx, enemyTargets);
        }
    }

    private void Play(SkillEffectVfxData vfx, Transform target, Vector3 fallbackPosition, string persistentKey)
    {
        if (vfx.Delay > 0f)
        {
            StartCoroutine(PlayDelayed(vfx, target, fallbackPosition, persistentKey));
            return;
        }

        Spawn(vfx, target, fallbackPosition, persistentKey);
    }

    private IEnumerator PlayDelayed(SkillEffectVfxData vfx, Transform target, Vector3 fallbackPosition, string persistentKey)
    {
        yield return new WaitForSecondsRealtime(vfx.Delay);
        Spawn(vfx, target, fallbackPosition, persistentKey);
    }

    private void Spawn(SkillEffectVfxData vfx, Transform target, Vector3 fallbackPosition, string persistentKey)
    {
        if (vfx == null || !vfx.HasPrefab)
            return;

        if (!string.IsNullOrEmpty(persistentKey) && cancelledPersistentKeys.Contains(persistentKey))
            return;

        Transform parent = vfx.ParentToTarget ? target : null;
        Vector3 position = (target != null ? target.position : fallbackPosition) + vfx.WorldOffset;
        GameObject instance = Instantiate(vfx.Prefab, position, Quaternion.identity, parent);
        ApplyParticleTimeMode(instance, vfx.UseUnscaledTimeForParticles);

        if (vfx.LifetimeMode == SkillEffectVfxLifetimeMode.Persistent && !string.IsNullOrEmpty(persistentKey))
        {
            RegisterPersistentInstance(persistentKey, instance);
            return;
        }

        if (vfx.Lifetime > 0f)
        {
            Destroy(instance, vfx.Lifetime);
        }
    }

    private PendingDamageModifierVfx CreatePendingDamageModifierVfx(SkillEffectVfxData vfx, CollisionSkillContext context)
    {
        PendingDamageModifierVfx pending = new PendingDamageModifierVfx
        {
            vfx = vfx
        };

        List<CoinStats> coinTargets = CollisionSkillTargetResolver.ResolveCoins(context, vfx.TargetType);
        for (int i = 0; i < coinTargets.Count; i++)
        {
            CoinStats target = coinTargets[i];
            if (target == null)
                continue;

            pending.targets.Add(target.transform);
            pending.fallbackPositions.Add(target.transform.position);
        }

        if (pending.targets.Count > 0)
            return pending;

        List<EnemyStats> enemyTargets = CollisionSkillTargetResolver.ResolveEnemies(context, vfx.TargetType, vfx.Radius);
        for (int i = 0; i < enemyTargets.Count; i++)
        {
            EnemyStats target = enemyTargets[i];
            if (target == null)
                continue;

            pending.targets.Add(target.transform);
            pending.fallbackPositions.Add(target.transform.position);
        }

        return pending;
    }

    private PendingDamageModifierVfx CreatePendingDamageModifierVfx(SkillEffectVfxData vfx, IReadOnlyList<CoinStats> coinTargets)
    {
        PendingDamageModifierVfx pending = new PendingDamageModifierVfx
        {
            vfx = vfx
        };

        if (coinTargets == null)
            return pending;

        for (int i = 0; i < coinTargets.Count; i++)
        {
            CoinStats target = coinTargets[i];
            if (target == null)
                continue;

            pending.targets.Add(target.transform);
            pending.fallbackPositions.Add(target.transform.position);
        }

        return pending;
    }

    private void PlayPendingDamageModifierVfx(int modifierId, PendingDamageModifierVfx pending)
    {
        if (pending == null || pending.vfx == null)
            return;

        string persistentKey = BuildDamageModifierKey(modifierId);
        for (int i = 0; i < pending.targets.Count; i++)
        {
            Transform target = pending.targets[i];
            Vector3 fallbackPosition = i < pending.fallbackPositions.Count ? pending.fallbackPositions[i] : Vector3.zero;
            Play(pending.vfx, target, fallbackPosition, persistentKey);
        }
    }

    private void RegisterPersistentInstance(string persistentKey, GameObject instance)
    {
        cancelledPersistentKeys.Remove(persistentKey);

        if (!persistentInstances.TryGetValue(persistentKey, out List<GameObject> instances))
        {
            instances = new List<GameObject>();
            persistentInstances.Add(persistentKey, instances);
        }

        instances.Add(instance);
    }

    private void OnCoinProtectionEnded(int protectionId, CoinStats target)
    {
        DestroyPersistent(BuildProtectionKey(protectionId));
    }

    private void OnDamageModifierStarted(int modifierId, string sourceId)
    {
        if (!pendingDamageModifierVfx.TryGetValue(modifierId, out PendingDamageModifierVfx pending))
            return;

        pendingDamageModifierVfx.Remove(modifierId);
        PlayPendingDamageModifierVfx(modifierId, pending);
    }

    private void OnDamageModifierEnded(int modifierId, string sourceId)
    {
        pendingDamageModifierVfx.Remove(modifierId);
        DestroyPersistent(BuildDamageModifierKey(modifierId));
    }

    private void DestroyPersistent(string persistentKey)
    {
        if (string.IsNullOrEmpty(persistentKey))
            return;

        cancelledPersistentKeys.Add(persistentKey);

        if (!persistentInstances.TryGetValue(persistentKey, out List<GameObject> instances))
            return;

        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i] != null)
            {
                Destroy(instances[i]);
            }
        }

        persistentInstances.Remove(persistentKey);
    }

    private void TrySubscribeRoundEffectManager()
    {
        if (subscribedRoundEffectManager == CoinRoundEffectManager.Instance)
            return;

        UnsubscribeRoundEffectManager();
        subscribedRoundEffectManager = CoinRoundEffectManager.Instance;

        if (subscribedRoundEffectManager != null)
        {
            subscribedRoundEffectManager.CoinProtectionEnded += OnCoinProtectionEnded;
            subscribedRoundEffectManager.DamageModifierStarted += OnDamageModifierStarted;
            subscribedRoundEffectManager.DamageModifierEnded += OnDamageModifierEnded;
        }
    }

    private void UnsubscribeRoundEffectManager()
    {
        if (subscribedRoundEffectManager == null)
            return;

        subscribedRoundEffectManager.CoinProtectionEnded -= OnCoinProtectionEnded;
        subscribedRoundEffectManager.DamageModifierStarted -= OnDamageModifierStarted;
        subscribedRoundEffectManager.DamageModifierEnded -= OnDamageModifierEnded;
        subscribedRoundEffectManager = null;
    }

    private static string BuildProtectionKey(int protectionId)
    {
        return "protection:" + protectionId;
    }

    private static string BuildDamageModifierKey(int modifierId)
    {
        return "damageModifier:" + modifierId;
    }

    private void ApplyParticleTimeMode(GameObject instance, bool useUnscaledTime)
    {
        if (instance == null || !useUnscaledTime)
            return;

        ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = particleSystems[i].main;
            main.useUnscaledTime = true;
        }
    }
}
