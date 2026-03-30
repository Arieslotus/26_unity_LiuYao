using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurnState
{
    PlayerTurn,
    EnemyTurn
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public TurnState currentState;

    [SerializeField] private List<EnemyController> enemies = new List<EnemyController>();

    //[Header("测试")]
    //public bool testPlayerTurnEnd = false;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentState = TurnState.PlayerTurn;
        FindAllEnemies();
    }

    void FindAllEnemies()
    {
        enemies.Clear();
        enemies.AddRange(FindObjectsOfType<EnemyController>());
    }




    // 👉 玩家操作结束时调用
    [ContextMenu("玩家操作结束")]
    public void EndPlayerTurn()
    {
        currentState = TurnState.EnemyTurn;
        StartCoroutine(EnemyTurnCoroutine());
    }

    IEnumerator EnemyTurnCoroutine()
    {
        Debug.Log("敌人回合开始");

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                yield return enemy.TakeTurn();
            }
        }

        Debug.Log("敌人回合结束");

        currentState = TurnState.PlayerTurn;
    }
}