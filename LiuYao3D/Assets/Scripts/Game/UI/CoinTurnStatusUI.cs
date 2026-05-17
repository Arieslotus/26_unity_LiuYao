/// <summary>
/// 实现功能：按玩家棋子控制顺序显示场上硬币图标，并用缩放标识当前可操控硬币。
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinTurnStatusUI : MonoBehaviour
{
    [Header("数据来源")]
    [Tooltip("玩家回合控制器，图标顺序会使用其中的 Pieces 列表顺序")]
    [SerializeField] private ChessTurnController turnController;

    [Header("图标槽位")]
    [Tooltip("硬币图标槽位，数量应与玩家硬币数量一致，当前为 3 个")]
    [SerializeField] private List<Image> iconSlots = new List<Image>();

    [Header("缩放")]
    [Tooltip("非当前操控硬币的图标缩放")]
    [SerializeField] private float normalScale = 0.8f;

    [Tooltip("当前操控硬币的图标缩放")]
    [SerializeField] private float activeScale = 1f;

    [Header("空槽表现")]
    [Tooltip("没有对应硬币或没有硬币定义时，是否隐藏图标")]
    [SerializeField] private bool hideEmptySlot = true;

    private readonly List<CoinRuntimeData> subscribedCoins = new List<CoinRuntimeData>();

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
        RebindCoinEvents();
        RefreshAll();
    }

    private void Start()
    {
        RebindCoinEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnsubscribeTurnController();
        UnbindCoinEvents();
    }

    private void SubscribeTurnController()
    {
        if (turnController == null)
        {
            Debug.LogWarning($"[CoinTurnStatusUI] {name} 未绑定 ChessTurnController，无法刷新硬币 UI。");
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

    private void RebindCoinEvents()
    {
        UnbindCoinEvents();

        if (turnController == null)
            return;

        IReadOnlyList<ChessPiece> pieces = turnController.Pieces;
        for (int i = 0; i < pieces.Count; i++)
        {
            ChessPiece piece = pieces[i];
            if (piece == null)
                continue;

            CoinRuntimeData coinData = piece.GetComponent<CoinRuntimeData>();
            if (coinData == null)
            {
                Debug.LogWarning($"[CoinTurnStatusUI] {name} 的 Pieces[{i}]({piece.name}) 缺少 CoinRuntimeData。");
                continue;
            }

            coinData.VisualStateChanged -= OnCoinVisualStateChanged;
            coinData.VisualStateChanged += OnCoinVisualStateChanged;
            subscribedCoins.Add(coinData);
        }
    }

    private void UnbindCoinEvents()
    {
        for (int i = 0; i < subscribedCoins.Count; i++)
        {
            CoinRuntimeData coinData = subscribedCoins[i];
            if (coinData != null)
            {
                coinData.VisualStateChanged -= OnCoinVisualStateChanged;
            }
        }

        subscribedCoins.Clear();
    }

    private void OnCurrentPieceChanged(int currentIndex, ChessPiece currentPiece)
    {
        RefreshAll();
    }

    private void OnCoinVisualStateChanged(CoinRuntimeData coinData)
    {
        RefreshAll();
    }

    [ContextMenu("刷新硬币状态 UI")]
    public void RefreshAll()
    {
        if (turnController == null)
            return;

        IReadOnlyList<ChessPiece> pieces = turnController.Pieces;
        int slotCount = iconSlots != null ? iconSlots.Count : 0;

        for (int i = 0; i < slotCount; i++)
        {
            Image icon = iconSlots[i];
            if (icon == null)
                continue;

            ChessPiece piece = i < pieces.Count ? pieces[i] : null;
            RefreshSlot(icon, piece, i == turnController.CurrentIndex && turnController.IsPlayerRoundActive);
        }
    }

    private void RefreshSlot(Image icon, ChessPiece piece, bool isActive)
    {
        Sprite sprite = GetCurrentCoinSprite(piece);
        bool hasSprite = sprite != null;

        icon.sprite = sprite;
        icon.enabled = hasSprite || !hideEmptySlot;
        icon.transform.localScale = Vector3.one * (isActive ? activeScale : normalScale);
    }

    private Sprite GetCurrentCoinSprite(ChessPiece piece)
    {
        if (piece == null)
            return null;

        CoinDefinition definition = piece.CoinDefinition;
        if (definition == null)
            return null;

        return piece.IsFrontSide ? definition.frontSprite : definition.backSprite;
    }
}
