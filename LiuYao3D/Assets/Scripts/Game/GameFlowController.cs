/// <summary>
/// 实现功能：统一管理游戏核心流程、胜负规则与状态事件，不直接负责开始/结束 UI 表现。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameFlowState
{
    WaitingToStart,
    Starting,
    Playing,
    Ended
}

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("开始流程")]
    [Tooltip("是否等待外部开始表现流程调用 RequestStartSequence。关闭后会在 Start 时直接进入游戏。")]
    [SerializeField] private bool waitForStartSignal = true;

    [Header("核心引用")]
    [Tooltip("回合管理器。为空时自动从场景查找。")]
    [SerializeField] private TurnManager turnManager;

    [Tooltip("敌人波次管理器。存在有效波次配置时，胜利条件由波次系统判定。")]
    [SerializeField] private EnemyWaveManager enemyWaveManager;

    [Header("胜负规则")]
    [Tooltip("启动时自动收集场景中的敌人和硬币数值组件。")]
    [SerializeField] private bool autoFindTargetsOnStart = true;

    [SerializeField] private List<EnemyStats> enemies = new List<EnemyStats>();
    [SerializeField] private List<CoinStats> coins = new List<CoinStats>();

    [Header("结束行为")]
    [Tooltip("游戏结束后是否暂停 Time.timeScale。UI 动画如果使用 unscaled time，暂停后仍可播放。")]
    [SerializeField] private bool pauseTimeOnGameEnd = false;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private GameFlowState state = GameFlowState.WaitingToStart;
    private bool? lastGameResult;
    private bool hasSubscribedTargets;

    public GameFlowState State => state;
    public bool IsGameplayActive => state == GameFlowState.Playing;
    public bool CanAcceptGameplayInput => state == GameFlowState.Playing;
    public bool HasGameEnded => state == GameFlowState.Ended;
    public bool? LastGameResult => lastGameResult;

    public event Action<GameFlowState> StateChanged;
    public event Action<bool> GameEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"[GameFlowController] 场景中存在多个 GameFlowController，销毁重复对象 | object:{name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    private void Start()
    {
        if (autoFindTargetsOnStart)
        {
            FindTargets();
        }

        SubscribeTargets();

        if (waitForStartSignal)
        {
            SetState(GameFlowState.WaitingToStart);
            return;
        }

        if (RequestStartSequence())
        {
            CompleteStartSequence();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeTargets();
    }

    [ContextMenu("重新收集胜负检测对象")]
    public void FindTargets()
    {
        enemies.Clear();
        coins.Clear();

        EnemyController[] enemyControllers = FindObjectsOfType<EnemyController>();
        for (int i = 0; i < enemyControllers.Length; i++)
        {
            EnemyController enemyController = enemyControllers[i];
            if (enemyController == null)
                continue;

            AddEnemyIfValid(enemyController.Stats);
        }

        EnemyStats[] enemyStats = FindObjectsOfType<EnemyStats>();
        for (int i = 0; i < enemyStats.Length; i++)
        {
            AddEnemyIfValid(enemyStats[i]);
        }

        coins.AddRange(FindObjectsOfType<CoinStats>());

        if (debugLog)
        {
            Debug.Log($"[GameFlowController] 收集胜负检测对象 | object:{name} | enemies:{enemies.Count} | coins:{coins.Count}");
        }
    }

    public bool RequestStartSequence()
    {
        if (state == GameFlowState.Starting || state == GameFlowState.Playing)
            return false;

        if (state == GameFlowState.Ended)
        {
            Debug.LogWarning($"[GameFlowController] 游戏已经结束，忽略开始请求 | object:{name}");
            return false;
        }

        SetState(GameFlowState.Starting);
        return true;
    }

    public void CompleteStartSequence()
    {
        if (state != GameFlowState.Starting)
        {
            Debug.LogWarning($"[GameFlowController] 当前状态不是 Starting，忽略完成开始流程请求 | object:{name} | state:{state}");
            return;
        }

        SetState(GameFlowState.Playing);

        if (turnManager != null)
        {
            turnManager.StartGameFlow();
        }
        else
        {
            Debug.LogWarning($"[GameFlowController] 未找到 TurnManager，游戏状态已开始但无法启动回合 | object:{name}");
        }

        CheckGameEnd();
    }

    public void RegisterEnemy(EnemyStats enemy)
    {
        if (enemy == null)
            return;

        AddEnemyIfValid(enemy);

        if (hasSubscribedTargets)
        {
            enemy.Died -= OnAnyTargetDied;
            enemy.Died += OnAnyTargetDied;
        }

        if (debugLog)
        {
            Debug.Log($"[GameFlowController] 注册动态敌人 | object:{name} | enemy:{enemy.name} | enemies:{enemies.Count}");
        }
    }

    public void EndGame(bool isVictory)
    {
        if (state == GameFlowState.Ended)
            return;

        lastGameResult = isVictory;
        SetState(GameFlowState.Ended);

        if (turnManager != null)
        {
            turnManager.StopGameFlow();
        }

        if (pauseTimeOnGameEnd)
        {
            Time.timeScale = 0f;
        }

        GameEnded?.Invoke(isVictory);

        if (debugLog)
        {
            Debug.Log($"[GameFlowController] 游戏结束 | object:{name} | result:{(isVictory ? "胜利" : "失败")}");
        }
    }

    private void ResolveReferences()
    {
        if (turnManager == null)
        {
            turnManager = FindObjectOfType<TurnManager>();
        }

        if (enemyWaveManager == null)
        {
            enemyWaveManager = FindObjectOfType<EnemyWaveManager>();
        }
    }

    private void AddEnemyIfValid(EnemyStats enemy)
    {
        if (enemy == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyStats existingEnemy = enemies[i];
            if (existingEnemy == null)
                continue;

            if (existingEnemy == enemy)
                return;

            if (existingEnemy.gameObject == enemy.gameObject)
            {
                if (debugLog)
                {
                    Debug.LogWarning($"[GameFlowController] 敌人对象上存在多个 EnemyStats，已只保留实际检测用组件 | enemy:{enemy.name} | kept:{existingEnemy.GetInstanceID()} | ignored:{enemy.GetInstanceID()}");
                }

                return;
            }
        }

        enemies.Add(enemy);
    }

    private void SubscribeTargets()
    {
        if (hasSubscribedTargets)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyStats enemy = enemies[i];
            if (enemy == null)
                continue;

            enemy.Died -= OnAnyTargetDied;
            enemy.Died += OnAnyTargetDied;
        }

        for (int i = 0; i < coins.Count; i++)
        {
            CoinStats coin = coins[i];
            if (coin == null)
                continue;

            coin.Broken -= OnAnyTargetDied;
            coin.Broken += OnAnyTargetDied;
        }

        hasSubscribedTargets = true;
    }

    private void UnsubscribeTargets()
    {
        if (!hasSubscribedTargets)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyStats enemy = enemies[i];
            if (enemy != null)
            {
                enemy.Died -= OnAnyTargetDied;
            }
        }

        for (int i = 0; i < coins.Count; i++)
        {
            CoinStats coin = coins[i];
            if (coin != null)
            {
                coin.Broken -= OnAnyTargetDied;
            }
        }

        hasSubscribedTargets = false;
    }

    private void OnAnyTargetDied()
    {
        CheckGameEnd();
    }

    private void CheckGameEnd()
    {
        if (state != GameFlowState.Playing)
            return;

        int aliveEnemyCount = CountAliveEnemies();
        int aliveCoinCount = CountAliveCoins();

        if (debugLog)
        {
            Debug.Log($"[GameFlowController] 检查游戏结束 | object:{name} | aliveEnemies:{aliveEnemyCount} | aliveCoins:{aliveCoinCount}");
        }

        if (enemies.Count > 0 && aliveEnemyCount <= 0)
        {
            if (enemyWaveManager != null && enemyWaveManager.IsWaveModeActive && !enemyWaveManager.HasCompletedAllWaves)
            {
                if (debugLog)
                {
                    Debug.Log($"[GameFlowController] 当前敌人已清空，但波次尚未完成，暂不判定胜利 | object:{name}");
                }

                return;
            }

            EndGame(true);
            return;
        }

        if (coins.Count > 0 && aliveCoinCount <= 0)
        {
            EndGame(false);
        }
    }

    private int CountAliveEnemies()
    {
        int count = 0;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyStats enemy = enemies[i];
            if (enemy == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            if (!enemy.IsDead)
            {
                count++;
            }
        }

        return count;
    }

    private int CountAliveCoins()
    {
        int count = 0;

        for (int i = coins.Count - 1; i >= 0; i--)
        {
            CoinStats coin = coins[i];
            if (coin == null)
            {
                coins.RemoveAt(i);
                continue;
            }

            if (!coin.IsBroken)
            {
                count++;
            }
        }

        return count;
    }

    private void SetState(GameFlowState newState)
    {
        if (state == newState)
            return;

        state = newState;
        StateChanged?.Invoke(state);

        if (debugLog)
        {
            Debug.Log($"[GameFlowController] 状态切换 | object:{name} | state:{state} | lastResult:{lastGameResult}");
        }
    }
}
