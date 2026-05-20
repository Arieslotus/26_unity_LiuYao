/// <summary>
/// 实现功能：监听场上敌人和硬币死亡状态，在敌人全灭或硬币全灭时打开游戏结束弹窗。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class GameEndController : MonoBehaviour
{
    [Header("弹窗")]
    [SerializeField] private GameEndPopup gameEndPopupPrefab;

    [Tooltip("指定弹窗管理器。为空时使用 UIPopupManager.Instance。")]
    [SerializeField] private UIPopupManager popupManager;

    [Header("检测对象")]
    [Tooltip("启动时自动收集场景中的敌人和硬币数值组件。")]
    [SerializeField] private bool autoFindTargetsOnStart = true;

    [SerializeField] private List<EnemyStats> enemies = new List<EnemyStats>();
    [SerializeField] private List<CoinStats> coins = new List<CoinStats>();

    [Header("行为")]
    [Tooltip("游戏结束后是否暂停 Time.timeScale。")]
    [SerializeField] private bool pauseTimeOnGameEnd = false;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private bool hasGameEnded;

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

        enemies.AddRange(FindObjectsOfType<EnemyStats>());
        coins.AddRange(FindObjectsOfType<CoinStats>());

        if (debugLog)
        {
            Debug.Log($"[GameEndController] 收集检测对象 | enemies:{enemies.Count} | coins:{coins.Count}");
        }
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
            Debug.Log($"[GameEndController] 检查游戏结束 | aliveEnemies:{aliveEnemyCount} | aliveCoins:{aliveCoinCount}");
        }

        if (enemies.Count > 0 && aliveEnemyCount <= 0)
        {
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

            if (!coin.IsDead)
            {
                count++;
            }
        }

        return count;
    }

    private void EndGame(bool isVictory)
    {
        hasGameEnded = true;

        if (pauseTimeOnGameEnd)
        {
            Time.timeScale = 0f;
        }

        UIPopupManager manager = popupManager != null ? popupManager : UIPopupManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[GameEndController] 游戏结束但场景中没有 UIPopupManager，无法打开结算弹窗。");
            return;
        }

        if (gameEndPopupPrefab == null)
        {
            Debug.LogWarning("[GameEndController] 游戏结束但未配置 GameEndPopup 预制体。");
            return;
        }

        GameEndPopup popup = manager.Open(gameEndPopupPrefab);
        if (popup != null)
        {
            popup.SetResult(isVictory);
        }

        if (debugLog)
        {
            Debug.Log($"[GameEndController] 游戏结束 | result:{(isVictory ? "胜利" : "失败")}");
        }
    }
}
