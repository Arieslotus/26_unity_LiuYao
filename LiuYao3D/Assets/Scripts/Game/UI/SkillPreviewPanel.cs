/// <summary>
/// 实现功能：根据当前可操控硬币动态生成与其他场上硬币的卦象碰撞技能预览。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class SkillPreviewPanel : MonoBehaviour
{
    private const string EmptyText = "暂无";

    [Header("数据来源")]
    [Tooltip("玩家回合控制器，用于获取当前可操控硬币和场上硬币顺序")]
    [SerializeField] private ChessTurnController turnController;

    [Tooltip("卦象碰撞技能数据库，用两枚硬币的卦象查询技能，是否区分主从由数据库配置决定")]
    [SerializeField] private TrigramSkillDatabase skillDatabase;

    [Header("UI")]
    [Tooltip("技能预览条目预制体")]
    [SerializeField] private SkillPreviewItem itemPrefab;

    [Tooltip("技能预览条目的父节点")]
    [SerializeField] private Transform contentRoot;

    private readonly List<SkillPreviewItem> spawnedItems = new List<SkillPreviewItem>();
    private readonly List<CoinRuntimeData> subscribedCoins = new List<CoinRuntimeData>();

    private void Awake()
    {
        if (turnController == null)
        {
            turnController = FindObjectOfType<ChessTurnController>();
        }

        if (contentRoot == null)
        {
            contentRoot = transform;
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
            Debug.LogWarning($"[SkillPreviewPanel] {name} 未绑定 ChessTurnController，无法刷新技能预览。");
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
                Debug.LogWarning($"[SkillPreviewPanel] {name} 的 Pieces[{i}]({piece.name}) 缺少 CoinRuntimeData。");
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

    [ContextMenu("刷新技能预览")]
    public void RefreshAll()
    {
        if (!ValidateReferences())
        {
            ClearItems();
            return;
        }

        ChessPiece activePiece = turnController.CurrentPiece;
        if (!turnController.IsPlayerRoundActive || activePiece == null)
        {
            ClearItems();
            return;
        }

        IReadOnlyList<ChessPiece> pieces = turnController.Pieces;
        int previewCount = CountPreviewTargets(pieces, activePiece);
        EnsureItemCount(previewCount);

        int itemIndex = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            ChessPiece passivePiece = pieces[i];
            if (passivePiece == null || passivePiece == activePiece)
                continue;

            RefreshItem(spawnedItems[itemIndex], activePiece, passivePiece);
            itemIndex++;
        }
    }

    private bool ValidateReferences()
    {
        if (turnController == null)
        {
            Debug.LogWarning($"[SkillPreviewPanel] {name} 未绑定 ChessTurnController。");
            return false;
        }

        if (itemPrefab == null)
        {
            Debug.LogWarning($"[SkillPreviewPanel] {name} 未绑定技能预览条目 Prefab。");
            return false;
        }

        if (contentRoot == null)
        {
            Debug.LogWarning($"[SkillPreviewPanel] {name} 未绑定 contentRoot。");
            return false;
        }

        return true;
    }

    private int CountPreviewTargets(IReadOnlyList<ChessPiece> pieces, ChessPiece activePiece)
    {
        int count = 0;

        for (int i = 0; i < pieces.Count; i++)
        {
            ChessPiece piece = pieces[i];
            if (piece != null && piece != activePiece)
            {
                count++;
            }
        }

        return count;
    }

    private void EnsureItemCount(int targetCount)
    {
        while (spawnedItems.Count < targetCount)
        {
            SkillPreviewItem item = Instantiate(itemPrefab, contentRoot);
            spawnedItems.Add(item);
        }

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
            {
                spawnedItems[i].gameObject.SetActive(i < targetCount);
            }
        }
    }

    private void ClearItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
            {
                spawnedItems[i].gameObject.SetActive(false);
            }
        }
    }

    private void RefreshItem(SkillPreviewItem item, ChessPiece activePiece, ChessPiece passivePiece)
    {
        if (item == null)
            return;

        TrigramType activeTrigram = activePiece.CurrentTrigram;
        TrigramType passiveTrigram = passivePiece.CurrentTrigram;
        TrigramCollisionSkillSO skill = skillDatabase != null
            ? skillDatabase.GetSkill(activeTrigram, passiveTrigram)
            : null;

        string skillName = skill != null ? skill.SkillName : EmptyText;
        string description = skill != null ? skill.Description : EmptyText;

        item.Set(
            skillName,
            GetCurrentCoinSprite(activePiece),
            GetCurrentCoinSprite(passivePiece),
            description
        );
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
