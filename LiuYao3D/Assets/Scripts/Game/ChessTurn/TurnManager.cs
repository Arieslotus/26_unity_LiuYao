using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public enum TurnState
{
    PlayerTurn,
    EnemyTurn
}

/// <summary>
/// 实现功能：统一管理玩家回合与敌人回合的大回合切换，负责启动玩家回合、启动敌人回合，并防止重复切换。
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [Header("当前回合状态")]
    public TurnState currentState;

    [Header("大回合设置")]
    [Tooltip("所有敌人行动结束后，等待多少秒再广播新大回合开始并进入玩家回合。")]
    [SerializeField] private float roundStartDelay = 0.5f;

    [Header("敌人列表")]
    [SerializeField] private List<EnemyController> enemies = new List<EnemyController>();

    [Header("玩家回合控制")]
    [SerializeField] private ChessTurnController playerTurnController;

    private bool isEnemyTurnRunning = false;
    private bool hasStartedGameFlow = false;
    private int roundIndex = 0;

    public bool IsEnemyTurnRunning => isEnemyTurnRunning;
    public int RoundIndex => roundIndex;

    public event Action<int> RoundStarted;
    public event Action<int> RoundEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[TurnManager] 场景中存在多个 TurnManager！");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        FindAllEnemies();

        if (GameFlowController.Instance != null && !GameFlowController.Instance.IsGameplayActive)
        {
            Debug.Log("[TurnManager] 检测到 GameFlowController，等待全局游戏开始信号。");
            return;
        }

        StartGameFlow();
    }

    public void StartGameFlow()
    {
        if (hasStartedGameFlow)
            return;

        hasStartedGameFlow = true;
        BeginNextRound();
    }

    public void StopGameFlow()
    {
        if (!hasStartedGameFlow && !isEnemyTurnRunning)
            return;

        hasStartedGameFlow = false;
        isEnemyTurnRunning = false;
        StopAllCoroutines();

        if (playerTurnController != null)
        {
            playerTurnController.ForceStopPlayerRound();
        }

        Debug.Log($"[TurnManager] 游戏流程停止，回合系统已锁定 | round:{roundIndex}");
    }

    /// <summary>
    /// 搜索场景中的敌人
    /// </summary>
    private void FindAllEnemies()
    {
        enemies.Clear();
        enemies.AddRange(FindObjectsOfType<EnemyController>());
    }

    /// <summary>
    /// 开始玩家回合
    /// </summary>
    public void BeginPlayerTurn()
    {
        if (!hasStartedGameFlow)
        {
            Debug.LogWarning("[TurnManager] 游戏流程尚未开始，不能开始玩家回合。");
            return;
        }

        if (isEnemyTurnRunning)
        {
            Debug.LogWarning("[TurnManager] 敌人回合尚未结束，不能开始玩家回合。");
            return;
        }

        currentState = TurnState.PlayerTurn;

        Debug.Log("[TurnManager] 玩家回合开始");

        if (playerTurnController != null)
        {
            playerTurnController.BeginNewPlayerRound();
        }
        else
        {
            Debug.LogWarning("[TurnManager] 未配置 ChessTurnController，无法开始玩家新回合！");
        }
    }

    /// <summary>
    /// 玩家操作结束时调用
    /// </summary>
    [ContextMenu("玩家操作结束")]
    public void EndPlayerTurn()
    {
        if (!hasStartedGameFlow)
            return;

        if (currentState != TurnState.PlayerTurn)
        {
            Debug.LogWarning("[TurnManager] 当前不是玩家回合，忽略 EndPlayerTurn。");
            return;
        }

        if (isEnemyTurnRunning)
        {
            Debug.LogWarning("[TurnManager] 敌人回合已在执行中，忽略重复 EndPlayerTurn。");
            return;
        }

        //BeginPlayerTurn();
        BeginEnemyTurn();
    }

    /// <summary>
    /// 开始敌人回合
    /// </summary>
    private void BeginEnemyTurn()
    {
        if (!hasStartedGameFlow)
            return;

        currentState = TurnState.EnemyTurn;
        isEnemyTurnRunning = true;

        Debug.Log("[TurnManager] 敌人回合开始");

        StartCoroutine(EnemyTurnCoroutine());
    }

    /// <summary>
    /// 敌人回合协程
    /// </summary>
    private IEnumerator EnemyTurnCoroutine()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (!hasStartedGameFlow)
                yield break;

            EnemyController enemy = enemies[i];
            if (enemy == null)
                continue;

            yield return enemy.TakeTurn();
        }

        EndEnemyTurn();
    }

    /// <summary>
    /// 结束敌人回合，切回玩家回合
    /// </summary>
    private void EndEnemyTurn()
    {
        if (!isEnemyTurnRunning)
        {
            Debug.LogWarning("[TurnManager] 当前没有敌人回合在执行，忽略 EndEnemyTurn。");
            return;
        }

        isEnemyTurnRunning = false;

        Debug.Log($"[TurnManager] 敌人回合结束，大回合结束 | round:{roundIndex}");

        RoundEnded?.Invoke(roundIndex);

        StartCoroutine(BeginNextRoundAfterDelay());
    }

    private IEnumerator BeginNextRoundAfterDelay()
    {
        if (roundStartDelay > 0f)
        {
            yield return new WaitForSeconds(roundStartDelay);
        }

        if (!hasStartedGameFlow)
            yield break;

        BeginNextRound();
    }

    private void BeginNextRound()
    {
        if (!hasStartedGameFlow)
            return;

        roundIndex++;

        Debug.Log($"[TurnManager] 大回合开始 | round:{roundIndex}");

        RoundStarted?.Invoke(roundIndex);
        BeginPlayerTurn();
    }
}
