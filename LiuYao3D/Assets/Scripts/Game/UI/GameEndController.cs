/// <summary>
/// 实现功能：监听场上敌人与硬币死亡状态，只负责判断胜负条件并把结果交给游戏流程控制器。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public class GameEndController : MonoBehaviour
{
    [Header("结果接收者")]
    [Tooltip("游戏流程控制器。为空时优先使用 GameFlowController.Instance。")]
    [SerializeField] private GameFlowController flowController;

    [Header("检测对象")]
    [Tooltip("启动时自动收集场景中的敌人和硬币数值组件。")]
    [SerializeField] private bool autoFindTargetsOnStart = true;

    [SerializeField] private List<EnemyStats> enemies = new List<EnemyStats>();
    [SerializeField] private List<CoinStats> coins = new List<CoinStats>();

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private bool hasGameEnded;

    public event Action<bool> GameEndDetected;

    private void Awake()
    {
        ResolveFlowController();
    }

    private void Start()
    {
        if (autoFindTargetsOnStart)
        {
            FindTargets();
        }

        SubscribeTargets();
        CheckGameEnd();
    }

    private void OnDestroy()
    {
        UnsubscribeTargets();
    }

    [ContextMenu("重新收集检测对象")]
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
            Debug.Log($"[GameEndController] 收集检测对象 | object:{name} | enemies:{enemies.Count} | coins:{coins.Count}");
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
                    Debug.LogWarning($"[GameEndController] 敌人对象上存在多个 EnemyStats，已只保留实际检测用组件 | enemy:{enemy.name} | kept:{existingEnemy.GetInstanceID()} | ignored:{enemy.GetInstanceID()}");
                }

                return;
            }
        }

        enemies.Add(enemy);
    }

    private void SubscribeTargets()
    {
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

            coin.Died -= OnAnyTargetDied;
            coin.Died += OnAnyTargetDied;
        }
    }

    private void UnsubscribeTargets()
    {
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
                coin.Died -= OnAnyTargetDied;
            }
        }
    }

    private void OnAnyTargetDied()
    {
        CheckGameEnd();
    }

    private void CheckGameEnd()
    {
        if (hasGameEnded)
            return;

        int aliveEnemyCount = CountAliveEnemies();
        int aliveCoinCount = CountAliveCoins();

        if (debugLog)
        {
            Debug.Log($"[GameEndController] 检查游戏结束 | object:{name} | aliveEnemies:{aliveEnemyCount} | aliveCoins:{aliveCoinCount}");
        }

        if (enemies.Count > 0 && aliveEnemyCount <= 0)
        {
            NotifyGameEnd(true);
            return;
        }

        if (coins.Count > 0 && aliveCoinCount <= 0)
        {
            NotifyGameEnd(false);
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

            if (!coin.IsDead)
            {
                count++;
            }
        }

        return count;
    }

    private void NotifyGameEnd(bool isVictory)
    {
        hasGameEnded = true;
        GameEndDetected?.Invoke(isVictory);

        ResolveFlowController();
        if (flowController != null)
        {
            flowController.EndGame(isVictory);
        }
        else
        {
            Debug.LogWarning($"[GameEndController] 已检测到游戏结束，但未找到 GameFlowController | object:{name} | result:{(isVictory ? "胜利" : "失败")}");
        }

        if (debugLog)
        {
            Debug.Log($"[GameEndController] 检测到游戏结束 | object:{name} | result:{(isVictory ? "胜利" : "失败")}");
        }
    }

    private void ResolveFlowController()
    {
        if (flowController == null)
        {
            flowController = GameFlowController.Instance;
        }

        if (flowController == null)
        {
            flowController = FindObjectOfType<GameFlowController>();
        }
    }
}
