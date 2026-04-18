using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 实现功能：管理玩家三枚棋子的顺序操作流程，负责切换当前可操作棋子、绑定输入、高亮显示，并在全部行动完成后结束玩家回合。
/// </summary>
public class ChessTurnController : MonoBehaviour
{
    [Header("棋子列表（按顺序）")]
    [SerializeField] private List<ChessPiece> pieces = new List<ChessPiece>();

    [Header("输入系统")]
    [SerializeField] private DragChargeInput input;

    private int currentIndex = -1;
    private bool hasFiredCurrent = false;
    private bool isPlayerRoundActive = false;
    private bool hasEndedPlayerRound = false;

    public ChessPiece CurrentPiece =>
        (currentIndex >= 0 && currentIndex < pieces.Count) ? pieces[currentIndex] : null;

    public int CurrentIndex => currentIndex;
    public bool IsPlayerRoundActive => isPlayerRoundActive;

    private void Start()
    {
        if (pieces.Count == 0)
        {
            Debug.LogError("[ChessTurnController] 没有配置棋子列表！");
        }
    }

    private void Update()
    {
        if (!IsInPlayerTurn())
            return;

        if (!isPlayerRoundActive)
            return;

        if (CurrentPiece == null)
            return;

        if (hasFiredCurrent && !CurrentPiece.IsMoving)
        {
            Debug.Log($"[ChessTurnController] 棋子 {currentIndex} 已完成行动，准备切换。");
            AdvanceToNextControllablePiece();
        }
    }

    public void NotifyPieceFired()
    {
        if (!IsInPlayerTurn())
            return;

        if (!isPlayerRoundActive)
            return;

        if (CurrentPiece == null)
            return;

        hasFiredCurrent = true;

        Debug.Log($"[ChessTurnController] 当前棋子已发射 | index:{currentIndex} | piece:{CurrentPiece.name}");
    }

    public void BeginNewPlayerRound()
    {
        if (pieces.Count == 0)
        {
            Debug.LogError("[ChessTurnController] 没有配置棋子列表，无法开始玩家新回合！");
            return;
        }

        isPlayerRoundActive = true;
        hasEndedPlayerRound = false;
        hasFiredCurrent = false;

        ExitCurrentPieceControl();

        Debug.Log("[ChessTurnController] 玩家新回合开始。");

        int firstIndex = FindNextControllablePieceIndex(0);
        if (firstIndex < 0)
        {
            Debug.LogWarning("[ChessTurnController] 本回合没有可操作棋子，直接结束玩家回合。");
            EndPlayerRound();
            return;
        }

        EnterPieceControl(firstIndex);
    }

    private void AdvanceToNextControllablePiece()
    {
        int nextIndex = FindNextControllablePieceIndex(currentIndex + 1);

        if (nextIndex < 0)
        {
            Debug.Log("[ChessTurnController] 所有可操作棋子已完成行动。");
            EndPlayerRound();
            return;
        }

        EnterPieceControl(nextIndex);
    }

    private int FindNextControllablePieceIndex(int startIndex)
    {
        for (int i = startIndex; i < pieces.Count; i++)
        {
            if (IsPieceControllable(pieces[i]))
                return i;
        }

        return -1;
    }

    private bool IsPieceControllable(ChessPiece piece)
    {
        if (piece == null)
            return false;

        if (!piece.CanBeControlledThisTurn)
            return false;

        return true;
    }

    private void EnterPieceControl(int index)
    {
        ExitCurrentPieceControl();

        currentIndex = index;
        hasFiredCurrent = false;

        ChessPiece piece = CurrentPiece;
        if (piece == null)
        {
            Debug.LogError($"[ChessTurnController] 进入控制失败，index:{index} 对应棋子为空。");
            return;
        }

        if (input != null)
        {
            input.SetControlledPiece(piece);
        }

        piece.SetTurnHighlight(true);
        RefreshAllHighlights();

        Debug.Log($"[ChessTurnController] 进入棋子控制 | index:{currentIndex} | piece:{piece.name}");
    }

    private void ExitCurrentPieceControl()
    {
        ChessPiece oldPiece = CurrentPiece;

        if (oldPiece != null)
        {
            oldPiece.SetTurnHighlight(false);
        }

        if (input != null)
        {
            input.SetControlledPiece(null);
        }
    }

    private void RefreshAllHighlights()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            ChessPiece piece = pieces[i];
            if (piece == null)
                continue;

            bool isCurrent = (i == currentIndex) && isPlayerRoundActive;
            piece.SetTurnHighlight(isCurrent);
        }
    }

    private void EndPlayerRound()
    {
        if (hasEndedPlayerRound)
            return;

        hasEndedPlayerRound = true;
        isPlayerRoundActive = false;
        hasFiredCurrent = false;

        ExitCurrentPieceControl();
        currentIndex = -1;
        RefreshAllHighlights();

        if (TurnManager.Instance != null)
        {
            Debug.Log("[ChessTurnController] 通知 TurnManager：玩家回合结束。");
            TurnManager.Instance.EndPlayerTurn();
        }
        else
        {
            Debug.LogWarning("[ChessTurnController] 未找到 TurnManager，无法结束玩家回合。");
        }
    }

    private bool IsInPlayerTurn()
    {
        return TurnManager.Instance == null || TurnManager.Instance.currentState == TurnState.PlayerTurn;
    }
}