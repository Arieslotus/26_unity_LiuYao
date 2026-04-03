using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 三棋子顺序控制器
/// </summary>
public class ChessTurnController : MonoBehaviour
{
    [Header("棋子列表（按顺序）")]
    [SerializeField] private List<ChessPiece> pieces = new List<ChessPiece>();

    [Header("输入系统")]
    [SerializeField] private DragChargeInput input;

    [Header("颜色控制")]
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private int currentIndex = 0;
    private bool hasFiredCurrent = false;

    private ChessPiece CurrentPiece =>
        (currentIndex >= 0 && currentIndex < pieces.Count) ? pieces[currentIndex] : null;

    private void Start()
    {
        if (pieces.Count == 0)
        {
            Debug.LogError("[ChessTurnController] 没有配置棋子列表！");
            return;
        }

        // 只有当前确实是玩家回合时才初始化
        if (TurnManager.Instance == null || TurnManager.Instance.currentState == TurnState.PlayerTurn)
        {
            BeginNewPlayerRound();
        }
    }

    private void Update()
    {
        // 非玩家回合时，不处理玩家棋子逻辑
        if (TurnManager.Instance != null && TurnManager.Instance.currentState != TurnState.PlayerTurn)
            return;

        if (CurrentPiece == null)
            return;

        // 等当前棋子完成移动后切换
        if (hasFiredCurrent && !CurrentPiece.IsMoving)
        {
            Debug.Log($"[ChessTurnController] 棋子 {currentIndex} 已完成移动，准备切换到下一个棋子。");
            NextPiece();
        }
    }

    /// <summary>
    /// 外部通知：当前棋子已经发射
    /// </summary>
    public void NotifyPieceFired()
    {
        // 非玩家回合不允许记录发射
        if (TurnManager.Instance != null && TurnManager.Instance.currentState != TurnState.PlayerTurn)
            return;

        hasFiredCurrent = true;
    }

    /// <summary>
    /// 开始新一轮玩家回合
    /// 由 TurnManager 在敌人回合结束后调用
    /// </summary>
    public void BeginNewPlayerRound()
    {
        if (pieces.Count == 0)
        {
            Debug.LogError("[ChessTurnController] 没有配置棋子列表，无法开始玩家新回合！");
            return;
        }

        Debug.Log("[ChessTurnController] 玩家新回合开始，重置到第一个棋子。");
        SetCurrentPiece(0);
    }

    /// <summary>
    /// 切换到下一个棋子
    /// </summary>
    private void NextPiece()
    {
        currentIndex++;

        if (currentIndex >= pieces.Count)
        {
            Debug.Log("[ChessTurnController] 所有棋子已操作完");

            if (input != null)
            {
                input.SetControlledPiece(null);
            }

            if (TurnManager.Instance != null)
            {
                Debug.Log("[ChessTurnController] 敌人回合：通知 TurnManager 进入敌人回合。");
                TurnManager.Instance.EndPlayerTurn();
            }
            else
            {
                //Debug.LogWarning("[ChessTurnController] 未找到 TurnManager，无法进入敌人回合！");

                Debug.Log("[ChessTurnController] 跳过敌人回合：直接进入下一回合。");
                BeginNewPlayerRound();
            }

            return;
        }

        SetCurrentPiece(currentIndex);
    }

    /// <summary>
    /// 设置当前棋子
    /// </summary>
    private void SetCurrentPiece(int index)
    {
        currentIndex = index;
        hasFiredCurrent = false;

        ChessPiece piece = CurrentPiece;

        if (piece == null)
        {
            Debug.LogError("[ChessTurnController] 当前棋子为空！");
            return;
        }

        // 切换输入目标
        if (input != null)
        {
            input.SetControlledPiece(piece);
        }

        UpdateColors();

        Debug.Log($"[Turn] 当前棋子 index = {currentIndex}");
    }

    /// <summary>
    /// 更新所有棋子颜色
    /// </summary>
    private void UpdateColors()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null) continue;

            var sr = pieces[i].GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            sr.color = (i == currentIndex) ? activeColor : normalColor;
        }
    }
}