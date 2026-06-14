/// <summary>
/// 实现功能：根据波次配置生成敌人，处理固定中心出生、局部单位重排、波次推进与最终胜利通知。
/// </summary>
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class EnemyWaveManager : MonoBehaviour
{
    [Header("波次配置")]
    [SerializeField] private GameWaveConfigSO waveConfig;

    [Header("核心引用")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private Transform enemyRoot;

    [Header("重排设置")]
    [Tooltip("生成敌人前是否同步 Transform，适合硬币使用手写位移后立刻刷怪的情况。")]
    [SerializeField] private bool syncTransformsBeforeSpawn = true;

    [Tooltip("重排时不同单位之间额外保留的水平间距。")]
    [Min(0f)]
    [SerializeField] private float placementPadding = 0.02f;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private readonly Dictionary<string, EnemySpawnPoint> spawnPoints = new Dictionary<string, EnemySpawnPoint>();
    private readonly Dictionary<EnemyStats, int> enemyWaveLookup = new Dictionary<EnemyStats, int>();
    private readonly List<EnemyController> activeEnemies = new List<EnemyController>();
    private readonly List<PlacementCircle> virtualCircles = new List<PlacementCircle>();
    private readonly List<UnitPlacement> unitPlacements = new List<UnitPlacement>();
    private readonly List<UnitPlacement> movers = new List<UnitPlacement>();
    private readonly List<UnitRelocation> pendingRelocations = new List<UnitRelocation>();
    private readonly Collider[] overlapBuffer = new Collider[64];

    private List<EnemyStats>[] waveEnemies;
    private bool[] spawnedWaves;
    private int pendingClearedWaveIndex = -1;
    private bool hasCompletedAllWaves;
    private Coroutine waitCoinsStoppedCoroutine;

    public bool IsWaveModeActive => waveConfig != null && waveConfig.waves != null && waveConfig.waves.Count > 0;
    public bool HasCompletedAllWaves => hasCompletedAllWaves;

    private void Awake()
    {
        ResolveReferences();
        CacheSpawnPoints();
        InitializeRuntimeState();
    }

    private void OnEnable()
    {
        SubscribeTurnManager();
    }

    private void OnDisable()
    {
        UnsubscribeTurnManager();
        UnsubscribeAllEnemies();

        if (waitCoinsStoppedCoroutine != null)
        {
            StopCoroutine(waitCoinsStoppedCoroutine);
            waitCoinsStoppedCoroutine = null;
        }
    }

    private void ResolveReferences()
    {
        if (turnManager == null)
        {
            turnManager = FindObjectOfType<TurnManager>();
        }

        if (gameFlowController == null)
        {
            gameFlowController = FindObjectOfType<GameFlowController>();
        }
    }

    private void CacheSpawnPoints()
    {
        spawnPoints.Clear();

        EnemySpawnPoint[] points = FindObjectsOfType<EnemySpawnPoint>();
        for (int i = 0; i < points.Length; i++)
        {
            EnemySpawnPoint point = points[i];
            if (point == null || string.IsNullOrWhiteSpace(point.SpawnPointId))
                continue;

            if (spawnPoints.ContainsKey(point.SpawnPointId))
            {
                Debug.LogWarning($"[EnemyWaveManager] 出生点 ID 重复，已忽略后者 | id:{point.SpawnPointId} | point:{point.name}");
                continue;
            }

            spawnPoints.Add(point.SpawnPointId, point);
        }
    }

    private void InitializeRuntimeState()
    {
        int waveCount = IsWaveModeActive ? waveConfig.waves.Count : 0;
        spawnedWaves = new bool[waveCount];
        waveEnemies = new List<EnemyStats>[waveCount];

        for (int i = 0; i < waveCount; i++)
        {
            waveEnemies[i] = new List<EnemyStats>();
        }
    }

    private void SubscribeTurnManager()
    {
        ResolveReferences();

        if (turnManager == null)
            return;

        turnManager.RoundStarted -= OnRoundStarted;
        turnManager.RoundStarted += OnRoundStarted;
    }

    private void UnsubscribeTurnManager()
    {
        if (turnManager == null)
            return;

        turnManager.RoundStarted -= OnRoundStarted;
    }

    private void OnRoundStarted(int roundIndex)
    {
        if (!IsWaveModeActive || hasCompletedAllWaves)
            return;

        if (syncTransformsBeforeSpawn)
        {
            Physics.SyncTransforms();
        }

        if (pendingClearedWaveIndex >= 0)
        {
            int nextWaveIndex = pendingClearedWaveIndex + 1;
            pendingClearedWaveIndex = -1;

            if (CanSpawnWaveByClear(nextWaveIndex))
            {
                SpawnWave(nextWaveIndex, $"上一波清空后进入下一回合 | round:{roundIndex}");
            }
        }

        for (int i = 0; i < waveConfig.waves.Count; i++)
        {
            WaveDefinition wave = waveConfig.waves[i];
            if (wave == null || spawnedWaves[i])
                continue;

            if (wave.spawnAtRound > 0 && roundIndex >= wave.spawnAtRound)
            {
                SpawnWave(i, $"指定回合生成 | round:{roundIndex}");
            }
        }

        CheckAllWavesCompleted();
    }

    private bool CanSpawnWaveByClear(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= waveConfig.waves.Count)
            return false;

        WaveDefinition wave = waveConfig.waves[waveIndex];
        return wave != null && !spawnedWaves[waveIndex] && wave.spawnWhenPreviousWaveCleared;
    }

    private void SpawnWave(int waveIndex, string reason)
    {
        if (waveIndex < 0 || waveIndex >= waveConfig.waves.Count)
            return;

        if (spawnedWaves[waveIndex])
            return;

        WaveDefinition wave = waveConfig.waves[waveIndex];
        if (wave == null)
            return;

        spawnedWaves[waveIndex] = true;

        if (debugLog)
        {
            Debug.Log($"[EnemyWaveManager] 生成波次 | index:{waveIndex} | name:{wave.waveName} | reason:{reason}");
        }

        if (wave.enemies == null)
            return;

        for (int i = 0; i < wave.enemies.Count; i++)
        {
            EnemySpawnEntry entry = wave.enemies[i];
            SpawnEntry(waveIndex, entry);
        }
    }

    private void SpawnEntry(int waveIndex, EnemySpawnEntry entry)
    {
        if (entry == null || entry.enemyDefinition == null)
        {
            Debug.LogWarning($"[EnemyWaveManager] 波次敌人配置为空，已跳过 | wave:{waveIndex}");
            return;
        }

        EnemyDefinitionSO definition = entry.enemyDefinition;
        if (definition.enemyPrefab == null)
        {
            Debug.LogWarning($"[EnemyWaveManager] 敌人预制体为空，已跳过 | wave:{waveIndex} | enemy:{definition.enemyName}");
            return;
        }

        if (!spawnPoints.TryGetValue(entry.spawnPointId, out EnemySpawnPoint spawnPoint) || spawnPoint == null)
        {
            Debug.LogWarning($"[EnemyWaveManager] 未找到出生点，已跳过 | wave:{waveIndex} | enemy:{definition.enemyName} | spawnPointId:{entry.spawnPointId}");
            return;
        }

        int count = Mathf.Max(1, entry.count);
        for (int i = 0; i < count; i++)
        {
            TrySpawnEnemyAtPoint(waveIndex, definition, spawnPoint, i);
        }
    }

    private void TrySpawnEnemyAtPoint(int waveIndex, EnemyDefinitionSO definition, EnemySpawnPoint spawnPoint, int entryIndex)
    {
        float enemyRadius = EnemySpawnFootprintUtility.GetHorizontalRadius(definition.enemyPrefab.transform);
        Vector3 center = spawnPoint.Center;

        if (!spawnPoint.IsCircleOnGround(center, enemyRadius))
        {
            Debug.LogWarning($"[EnemyWaveManager] 出生点中心不在有效地面内，无法生成 | wave:{waveIndex} | enemy:{definition.enemyName} | point:{spawnPoint.SpawnPointId} | radius:{enemyRadius:F2}");
            return;
        }

        if (!TryResolveCenterConflicts(spawnPoint, center, enemyRadius, out string failure))
        {
            Debug.LogWarning($"[EnemyWaveManager] 生成失败：无法为出生点中心让位 | wave:{waveIndex} | enemy:{definition.enemyName} | point:{spawnPoint.SpawnPointId} | reason:{failure}");
            return;
        }

        ApplyRelocations();

        GameObject enemyObject = Instantiate(definition.enemyPrefab, center, definition.enemyPrefab.transform.rotation);
        if (enemyRoot != null)
        {
            enemyObject.transform.SetParent(enemyRoot, true);
        }

        EnemyStats stats = enemyObject.GetComponentInChildren<EnemyStats>();
        if (stats != null)
        {
            stats.Initialize(definition);
            stats.Died -= OnEnemyDied;
            stats.Died += OnEnemyDied;
            enemyWaveLookup[stats] = waveIndex;
            waveEnemies[waveIndex].Add(stats);
        }
        else
        {
            Debug.LogWarning($"[EnemyWaveManager] 生成的敌人缺少 EnemyStats | enemy:{enemyObject.name} | prefab:{definition.enemyPrefab.name}");
        }

        EnemyShieldController shieldController = enemyObject.GetComponentInChildren<EnemyShieldController>();
        if (shieldController != null && !definition.allowShield)
        {
            shieldController.enabled = false;
        }
        else if (shieldController != null)
        {
            shieldController.GenerateInitialShield();
        }

        EnemyController controller = enemyObject.GetComponentInChildren<EnemyController>();
        if (controller != null)
        {
            activeEnemies.Add(controller);
            turnManager?.RegisterEnemy(controller);
        }
        else
        {
            Debug.LogWarning($"[EnemyWaveManager] 生成的敌人缺少 EnemyController | enemy:{enemyObject.name} | prefab:{definition.enemyPrefab.name}");
        }

        gameFlowController?.RegisterEnemy(stats);

        if (debugLog)
        {
            Debug.Log($"[EnemyWaveManager] 敌人生成完成 | wave:{waveIndex} | index:{entryIndex} | enemy:{enemyObject.name} | point:{spawnPoint.SpawnPointId} | pos:{center}");
        }
    }

    private bool TryResolveCenterConflicts(EnemySpawnPoint spawnPoint, Vector3 center, float enemyRadius, out string failure)
    {
        failure = string.Empty;
        pendingRelocations.Clear();
        movers.Clear();
        unitPlacements.Clear();
        virtualCircles.Clear();

        BuildUnitPlacements();

        for (int i = 0; i < unitPlacements.Count; i++)
        {
            UnitPlacement unit = unitPlacements[i];
            if (Overlaps(center, enemyRadius + placementPadding, unit.Position, unit.Radius))
            {
                movers.Add(unit);
            }
        }

        if (HasUnmovableBlockingCollider(spawnPoint, center, enemyRadius + placementPadding))
        {
            failure = "出生点中心存在不可移动阻挡物";
            return false;
        }

        if (movers.Count == 0)
            return true;

        movers.Sort((a, b) => b.Radius.CompareTo(a.Radius));

        virtualCircles.Add(new PlacementCircle(center, enemyRadius + placementPadding));

        for (int i = 0; i < unitPlacements.Count; i++)
        {
            UnitPlacement unit = unitPlacements[i];
            if (!ContainsUnit(movers, unit.Root))
            {
                virtualCircles.Add(new PlacementCircle(unit.Position, unit.Radius + placementPadding));
            }
        }

        IReadOnlyList<SpawnCandidate> candidates = spawnPoint.GetCandidates(false);
        for (int i = 0; i < movers.Count; i++)
        {
            UnitPlacement mover = movers[i];
            if (!TryFindRelocation(spawnPoint, candidates, mover, out Vector3 target))
            {
                failure = $"没有足够空间重排单位 | unit:{mover.Root.name} | radius:{mover.Radius:F2}";
                pendingRelocations.Clear();
                return false;
            }

            pendingRelocations.Add(new UnitRelocation(mover.Root, target));
            virtualCircles.Add(new PlacementCircle(target, mover.Radius + placementPadding));
        }

        return true;
    }

    private bool HasUnmovableBlockingCollider(EnemySpawnPoint spawnPoint, Vector3 center, float radius)
    {
        int count = Physics.OverlapSphereNonAlloc(
            center,
            radius,
            overlapBuffer,
            ~0,
            spawnPoint.TriggerInteraction
        );

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];
            if (hit == null)
                continue;

            if (!spawnPoint.IsBlockingCollider(hit))
                continue;

            Transform hitRoot = hit.transform.root;
            if (IsMovingRoot(hitRoot))
                continue;

            return true;
        }

        return false;
    }

    private void BuildUnitPlacements()
    {
        CleanupActiveEnemies();

        EnemyController[] sceneEnemies = FindObjectsOfType<EnemyController>();
        for (int i = 0; i < sceneEnemies.Length; i++)
        {
            EnemyController enemy = sceneEnemies[i];
            if (enemy == null || enemy.Stats == null || enemy.Stats.IsDead)
                continue;

            unitPlacements.Add(new UnitPlacement(enemy.transform, enemy.transform.position, EnemySpawnFootprintUtility.GetHorizontalRadius(enemy.transform)));
        }

        CoinStats[] coins = FindObjectsOfType<CoinStats>();
        for (int i = 0; i < coins.Length; i++)
        {
            CoinStats coin = coins[i];
            if (coin == null || coin.IsBroken)
                continue;

            unitPlacements.Add(new UnitPlacement(coin.transform, coin.transform.position, EnemySpawnFootprintUtility.GetHorizontalRadius(coin.transform)));
        }
    }

    private bool TryFindRelocation(EnemySpawnPoint spawnPoint, IReadOnlyList<SpawnCandidate> candidates, UnitPlacement mover, out Vector3 target)
    {
        int bestRing = int.MaxValue;
        List<Vector3> ringTargets = new List<Vector3>();

        for (int i = 0; i < candidates.Count; i++)
        {
            SpawnCandidate candidate = candidates[i];
            if (candidate.RingIndex > bestRing)
                continue;

            Vector3 candidatePos = new Vector3(candidate.Position.x, mover.Position.y, candidate.Position.z);
            if (!IsCandidateAvailable(spawnPoint, candidatePos, mover))
                continue;

            if (candidate.RingIndex < bestRing)
            {
                bestRing = candidate.RingIndex;
                ringTargets.Clear();
            }

            ringTargets.Add(candidatePos);
        }

        if (ringTargets.Count == 0)
        {
            target = default;
            return false;
        }

        target = ringTargets[Random.Range(0, ringTargets.Count)];
        return true;
    }

    private bool IsCandidateAvailable(EnemySpawnPoint spawnPoint, Vector3 candidatePos, UnitPlacement mover)
    {
        if (!spawnPoint.IsCircleOnGround(candidatePos, mover.Radius))
            return false;

        for (int i = 0; i < virtualCircles.Count; i++)
        {
            PlacementCircle circle = virtualCircles[i];
            if (Overlaps(candidatePos, mover.Radius + placementPadding, circle.Position, circle.Radius))
                return false;
        }

        int count = Physics.OverlapSphereNonAlloc(
            candidatePos,
            mover.Radius + placementPadding,
            overlapBuffer,
            ~0,
            spawnPoint.TriggerInteraction
        );

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];
            if (hit == null)
                continue;

            if (!spawnPoint.IsBlockingCollider(hit))
                continue;

            Transform hitRoot = hit.transform.root;
            if (IsMovingRoot(hitRoot))
                continue;

            return false;
        }

        return true;
    }

    private bool IsMovingRoot(Transform root)
    {
        for (int i = 0; i < movers.Count; i++)
        {
            if (movers[i].Root.root == root)
                return true;
        }

        return false;
    }

    private void ApplyRelocations()
    {
        for (int i = 0; i < pendingRelocations.Count; i++)
        {
            UnitRelocation relocation = pendingRelocations[i];
            if (relocation.Root == null)
                continue;

            if (debugLog)
            {
                Debug.Log($"[EnemyWaveManager] 单位重排 | unit:{relocation.Root.name} | from:{relocation.Root.position} | to:{relocation.TargetPosition}");
            }

            relocation.Root.position = relocation.TargetPosition;
        }
    }

    private void OnEnemyDied()
    {
        CleanupActiveEnemies();
        EvaluateClearedWaves();
        CheckAllWavesCompleted();
    }

    private void EvaluateClearedWaves()
    {
        if (!IsWaveModeActive || hasCompletedAllWaves)
            return;

        for (int i = 0; i < waveEnemies.Length; i++)
        {
            if (!spawnedWaves[i] || !IsWaveCleared(i))
                continue;

            int nextWaveIndex = i + 1;
            if (!CanSpawnWaveByClear(nextWaveIndex))
                continue;

            pendingClearedWaveIndex = Mathf.Max(pendingClearedWaveIndex, i);

            if (turnManager != null && turnManager.currentState == TurnState.PlayerTurn)
            {
                if (debugLog)
                {
                    Debug.Log($"[EnemyWaveManager] 当前波已清空，等待全场硬币停止后结束玩家回合 | wave:{i}");
                }

                StartWaitCoinsStoppedThenEndPlayerTurn(i);
            }

            return;
        }
    }

    private void StartWaitCoinsStoppedThenEndPlayerTurn(int waveIndex)
    {
        if (waitCoinsStoppedCoroutine != null)
            return;

        waitCoinsStoppedCoroutine = StartCoroutine(WaitCoinsStoppedThenEndPlayerTurn(waveIndex));
    }

    private IEnumerator WaitCoinsStoppedThenEndPlayerTurn(int waveIndex)
    {
        while (turnManager != null && turnManager.currentState == TurnState.PlayerTurn && HasMovingCoins())
        {
            yield return null;
        }

        waitCoinsStoppedCoroutine = null;

        if (turnManager == null || turnManager.currentState != TurnState.PlayerTurn)
            yield break;

        if (!IsWaveModeActive || hasCompletedAllWaves)
            yield break;

        if (debugLog)
        {
            Debug.Log($"[EnemyWaveManager] 全场硬币已停止，结束玩家回合进入下一波流程 | wave:{waveIndex}");
        }

        turnManager.EndPlayerTurn();
    }

    private bool HasMovingCoins()
    {
        ChessPiece[] pieces = FindObjectsOfType<ChessPiece>();
        for (int i = 0; i < pieces.Length; i++)
        {
            ChessPiece piece = pieces[i];
            if (piece != null && piece.IsMoving)
                return true;
        }

        return false;
    }

    private bool IsWaveCleared(int waveIndex)
    {
        List<EnemyStats> list = waveEnemies[waveIndex];
        for (int i = list.Count - 1; i >= 0; i--)
        {
            EnemyStats stats = list[i];
            if (stats == null)
            {
                list.RemoveAt(i);
                continue;
            }

            if (!stats.IsDead)
                return false;
        }

        return true;
    }

    private void CheckAllWavesCompleted()
    {
        if (!IsWaveModeActive || hasCompletedAllWaves)
            return;

        for (int i = 0; i < spawnedWaves.Length; i++)
        {
            if (!spawnedWaves[i] || !IsWaveCleared(i))
                return;
        }

        hasCompletedAllWaves = true;

        if (debugLog)
        {
            Debug.Log("[EnemyWaveManager] 所有波次完成，通知游戏胜利。");
        }

        gameFlowController?.EndGame(true);
    }

    private void CleanupActiveEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = activeEnemies[i];
            if (enemy == null || enemy.Stats == null || enemy.Stats.IsDead)
            {
                activeEnemies.RemoveAt(i);
            }
        }
    }

    private void UnsubscribeAllEnemies()
    {
        foreach (KeyValuePair<EnemyStats, int> pair in enemyWaveLookup)
        {
            if (pair.Key != null)
            {
                pair.Key.Died -= OnEnemyDied;
            }
        }

        enemyWaveLookup.Clear();
    }

    private bool ContainsUnit(List<UnitPlacement> list, Transform root)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Root == root)
                return true;
        }

        return false;
    }

    private bool Overlaps(Vector3 a, float radiusA, Vector3 b, float radiusB)
    {
        float distance = EnemySpawnFootprintUtility.GetFlatDistanceXZ(a, b);
        return distance < radiusA + radiusB;
    }

    private readonly struct UnitPlacement
    {
        public readonly Transform Root;
        public readonly Vector3 Position;
        public readonly float Radius;

        public UnitPlacement(Transform root, Vector3 position, float radius)
        {
            Root = root;
            Position = position;
            Radius = radius;
        }
    }

    private readonly struct UnitRelocation
    {
        public readonly Transform Root;
        public readonly Vector3 TargetPosition;

        public UnitRelocation(Transform root, Vector3 targetPosition)
        {
            Root = root;
            TargetPosition = targetPosition;
        }
    }

    private readonly struct PlacementCircle
    {
        public readonly Vector3 Position;
        public readonly float Radius;

        public PlacementCircle(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
    }
}
