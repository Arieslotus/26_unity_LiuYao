/// <summary>
/// 实现功能：动态显示当前可操作硬币与其余硬币的轮次信息，并在硬币正反面变化时局部刷新。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class CoinTurnInfoPanel : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private ChessTurnController turnController;
    [SerializeField] private CoinTurnInfoItem itemPrefab;

    [Header("根节点")]
    [Tooltip("当前玩家可操作硬币信息的生成根物体。")]
    [SerializeField] private Transform currentRoot;

    [Tooltip("剩余硬币信息的生成根物体。")]
    [SerializeField] private Transform remainingRoot;

    [Header("显示")]
    [SerializeField] private Vector3 currentItemScale = Vector3.one;
    [SerializeField] private Vector3 remainingItemScale = new Vector3(0.8f, 0.8f, 0.8f);

    [Header("调试")]
    [SerializeField] private bool debugLog = false;

    private readonly Dictionary<ChessPiece, CoinTurnInfoItem> itemMap = new Dictionary<ChessPiece, CoinTurnInfoItem>();
    private readonly List<CoinTurnInfoItem> spawnedItems = new List<CoinTurnInfoItem>();
    private readonly List<CoinRuntimeData> subscribedCoinData = new List<CoinRuntimeData>();

    private void Awake()
    {
        if (turnController == null)
        {
            turnController = FindObjectOfType<ChessTurnController>();
        }
    }

    private void OnEnable()
    {
        SubscribeTurnController();
        SubscribeCoinData();
        Rebuild();
    }

    private void OnDisable()
    {
        UnsubscribeTurnController();
        UnsubscribeCoinData();
        ClearItems();
    }

    private void SubscribeTurnController()
    {
        if (turnController == null)
        {
            Debug.LogWarning($"[CoinTurnInfoPanel] {name} 未绑定 ChessTurnController，无法刷新硬币轮次信息。");
            return;
        }

        turnController.CurrentPieceChanged -= OnCurrentPieceChanged;
        turnController.CurrentPieceChanged += OnCurrentPieceChanged;
    }

    private void UnsubscribeTurnController()
    {
        if (turnController != null)
        {
            turnController.CurrentPieceChanged -= OnCurrentPieceChanged;
        }
    }

    private void SubscribeCoinData()
    {
        UnsubscribeCoinData();

        if (turnController == null)
            return;

        IReadOnlyList<ChessPiece> pieces = turnController.Pieces;
        for (int i = 0; i < pieces.Count; i++)
        {
            ChessPiece piece = pieces[i];
            if (piece == null || piece.CoinRuntimeData == null)
                continue;

            CoinRuntimeData coinData = piece.CoinRuntimeData;
            coinData.VisualStateChanged -= OnCoinVisualStateChanged;
            coinData.VisualStateChanged += OnCoinVisualStateChanged;
            subscribedCoinData.Add(coinData);
        }
    }

    private void UnsubscribeCoinData()
    {
        for (int i = 0; i < subscribedCoinData.Count; i++)
        {
            CoinRuntimeData coinData = subscribedCoinData[i];
            if (coinData != null)
            {
                coinData.VisualStateChanged -= OnCoinVisualStateChanged;
            }
        }

        subscribedCoinData.Clear();
    }

    private void OnCurrentPieceChanged(int currentIndex, ChessPiece currentPiece)
    {
        SubscribeCoinData();
        Rebuild();
    }

    private void OnCoinVisualStateChanged(CoinRuntimeData changedCoinData)
    {
        if (changedCoinData == null || turnController == null)
            return;

        ChessPiece piece = FindPieceByCoinData(changedCoinData);
        if (piece == null)
            return;

        RefreshItem(piece);
    }

    private void Rebuild()
    {
        ClearItems();

        if (!CanBuild())
            return;

        int currentIndex = turnController.CurrentIndex;
        IReadOnlyList<ChessPiece> pieces = turnController.Pieces;

        if (!turnController.IsPlayerRoundActive || currentIndex < 0 || currentIndex >= pieces.Count)
            return;

        ChessPiece currentPiece = turnController.CurrentPiece;
        if (currentPiece != null)
        {
            CreateItem(currentPiece, currentRoot, currentItemScale, false);
        }

        for (int i = currentIndex + 1; i < pieces.Count; i++)
        {
            CreateRemainingItem(pieces[i], false);
        }

        for (int i = 0; i < currentIndex; i++)
        {
            CreateRemainingItem(pieces[i], true);
        }

        if (debugLog)
        {
            Debug.Log($"[CoinTurnInfoPanel] 重建硬币轮次信息 | currentIndex:{currentIndex} | items:{spawnedItems.Count}");
        }
    }

    private bool CanBuild()
    {
        if (turnController == null || itemPrefab == null || currentRoot == null || remainingRoot == null)
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    $"[CoinTurnInfoPanel] 缺少必要引用 | " +
                    $"turnController:{turnController != null} | itemPrefab:{itemPrefab != null} | " +
                    $"currentRoot:{currentRoot != null} | remainingRoot:{remainingRoot != null}"
                );
            }

            return false;
        }

        return true;
    }

    private void CreateRemainingItem(ChessPiece piece, bool hasActed)
    {
        if (piece == null)
            return;

        CreateItem(piece, remainingRoot, remainingItemScale, hasActed);
    }

    private void CreateItem(ChessPiece piece, Transform root, Vector3 scale, bool hasActed)
    {
        if (piece == null || root == null || itemPrefab == null)
            return;

        CoinTurnInfoItem item = Instantiate(itemPrefab, root);
        item.transform.localScale = scale;
        item.Set(piece, hasActed);

        spawnedItems.Add(item);
        itemMap[piece] = item;
    }

    private void RefreshItem(ChessPiece piece)
    {
        if (piece == null)
            return;

        if (!itemMap.TryGetValue(piece, out CoinTurnInfoItem item) || item == null)
            return;

        bool hasActed = IsPieceActed(piece);
        item.Set(piece, hasActed);
    }

    private bool IsPieceActed(ChessPiece piece)
    {
        if (turnController == null || piece == null)
            return false;

        IReadOnlyList<ChessPiece> pieces = turnController.Pieces;
        int currentIndex = turnController.CurrentIndex;

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == piece)
                return currentIndex >= 0 && i < currentIndex;
        }

        return false;
    }

    private ChessPiece FindPieceByCoinData(CoinRuntimeData coinData)
    {
        if (turnController == null || coinData == null)
            return null;

        IReadOnlyList<ChessPiece> pieces = turnController.Pieces;
        for (int i = 0; i < pieces.Count; i++)
        {
            ChessPiece piece = pieces[i];
            if (piece != null && piece.CoinRuntimeData == coinData)
                return piece;
        }

        return null;
    }

    private void ClearItems()
    {
        itemMap.Clear();

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            CoinTurnInfoItem item = spawnedItems[i];
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        spawnedItems.Clear();
    }
}
