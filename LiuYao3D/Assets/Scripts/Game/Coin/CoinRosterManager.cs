/// <summary>
/// 实现功能：管理场上硬币槽位、背包硬币和回合结束后的破裂硬币替换入口，支持部分补位与剩余槽位销毁。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public class CoinRosterManager : MonoBehaviour
{
    public static CoinRosterManager Instance { get; private set; }

    [Header("场上槽位")]
    [Tooltip("场上硬币槽位。为空时会优先从 ChessTurnController 的行动顺序中自动收集。")]
    [SerializeField] private List<ChessPiece> coinSlots = new List<ChessPiece>();

    [Tooltip("启动时是否自动收集场上槽位。")]
    [SerializeField] private bool autoFindSlotsOnStart = true;

    [Header("背包")]
    [Tooltip("背包中的可替换硬币定义。")]
    [SerializeField] private List<CoinDefinition> inventoryCoins = new List<CoinDefinition>();

    [Header("替换规则")]
    [Tooltip("替换上场时是否随机决定初始正反面。关闭时默认正面。")]
    [SerializeField] private bool randomizeReplacementSide = true;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private readonly List<ChessPiece> pendingBrokenSlots = new List<ChessPiece>();
    private TurnManager subscribedTurnManager;

    public IReadOnlyList<ChessPiece> CoinSlots => coinSlots;
    public IReadOnlyList<CoinDefinition> InventoryCoins => inventoryCoins;
    public IReadOnlyList<ChessPiece> PendingBrokenSlots => pendingBrokenSlots;
    public bool HasPendingReplacement => pendingBrokenSlots.Count > 0;
    public bool HasInventoryCoin => CountValidInventoryCoins() > 0;
    public bool HasEnoughInventoryForPendingReplacement => CountValidInventoryCoins() >= pendingBrokenSlots.Count;
    public int ValidInventoryCoinCount => CountValidInventoryCoins();

    public event Action<IReadOnlyList<ChessPiece>, IReadOnlyList<CoinDefinition>> ReplacementSelectionRequested;
    public event Action<ChessPiece, CoinDefinition> CoinReplaced;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"[CoinRosterManager] 场景中存在多个实例 | object:{name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SubscribeTurnManager();

        if (autoFindSlotsOnStart)
        {
            RefreshCoinSlots();
        }
    }

    private void OnEnable()
    {
        SubscribeTurnManager();
        RefreshCoinSubscriptions();
    }

    private void OnDisable()
    {
        UnsubscribeTurnManager();
        UnsubscribeCoinSubscriptions();
    }

    private void OnDestroy()
    {
        UnsubscribeTurnManager();
        UnsubscribeCoinSubscriptions();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    [ContextMenu("重新收集场上硬币槽位")]
    public void RefreshCoinSlots()
    {
        coinSlots.Clear();

        ChessTurnController turnController = FindObjectOfType<ChessTurnController>();
        if (turnController != null && turnController.Pieces != null)
        {
            IReadOnlyList<ChessPiece> pieces = turnController.Pieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                AddSlotIfValid(pieces[i]);
            }
        }

        if (coinSlots.Count == 0)
        {
            ChessPiece[] foundPieces = FindObjectsOfType<ChessPiece>();
            for (int i = 0; i < foundPieces.Length; i++)
            {
                AddSlotIfValid(foundPieces[i]);
            }
        }

        RefreshCoinSubscriptions();

        if (debugLog)
        {
            Debug.Log($"[CoinRosterManager] 收集场上硬币槽位 | object:{name} | slots:{coinSlots.Count}");
        }
    }

    public bool HasRecoverableCoin()
    {
        if (HasAliveCoin())
            return true;

        return HasInventoryCoin;
    }

    public bool HasAliveCoin()
    {
        for (int i = 0; i < coinSlots.Count; i++)
        {
            CoinStats stats = GetStats(coinSlots[i]);
            if (stats != null && !stats.IsBroken)
                return true;
        }

        return false;
    }

    public bool TryReplaceBrokenSlot(ChessPiece slot, CoinDefinition replacement)
    {
        if (slot == null)
        {
            Debug.LogWarning("[CoinRosterManager] 替换失败：槽位为空。");
            return false;
        }

        if (replacement == null)
        {
            Debug.LogWarning($"[CoinRosterManager] 替换失败：替换硬币为空 | slot:{slot.name}");
            return false;
        }

        CoinStats stats = GetStats(slot);
        if (stats == null)
        {
            Debug.LogWarning($"[CoinRosterManager] 替换失败：槽位缺少 CoinStats | slot:{slot.name}");
            return false;
        }

        if (!stats.IsBroken)
        {
            Debug.LogWarning($"[CoinRosterManager] 替换失败：槽位尚未破裂 | slot:{slot.name}");
            return false;
        }

        int inventoryIndex = inventoryCoins.IndexOf(replacement);
        if (inventoryIndex < 0)
        {
            Debug.LogWarning($"[CoinRosterManager] 替换失败：背包中不存在该硬币 | slot:{slot.name} | coin:{replacement.coinName}");
            return false;
        }

        inventoryCoins.RemoveAt(inventoryIndex);
        pendingBrokenSlots.Remove(slot);

        slot.gameObject.SetActive(true);
        slot.SetCoinDefinition(replacement, true);
        slot.SetFace(randomizeReplacementSide ? UnityEngine.Random.value < 0.5f : true, false);
        slot.SetCanBeControlledThisTurn(true);

        if (!coinSlots.Contains(slot))
        {
            coinSlots.Add(slot);
        }

        CoinReplaced?.Invoke(slot, replacement);

        if (debugLog)
        {
            Debug.Log(
                $"[CoinRosterManager] 替换破裂硬币 | slot:{slot.name} | coin:{replacement.coinName} | " +
                $"inventoryLeft:{CountValidInventoryCoins()} | pending:{pendingBrokenSlots.Count}"
            );
        }

        return true;
    }

    public bool DestroyBrokenSlot(ChessPiece slot)
    {
        if (slot == null)
        {
            Debug.LogWarning("[CoinRosterManager] 销毁破裂槽位失败：槽位为空。");
            return false;
        }

        CoinStats stats = GetStats(slot);
        if (stats == null || !stats.IsBroken)
        {
            Debug.LogWarning($"[CoinRosterManager] 销毁破裂槽位失败：目标未处于破裂状态 | slot:{slot.name}");
            return false;
        }

        pendingBrokenSlots.Remove(slot);
        coinSlots.Remove(slot);
        slot.SetCanBeControlledThisTurn(false);
        slot.gameObject.SetActive(false);

        if (debugLog)
        {
            Debug.Log($"[CoinRosterManager] 销毁破裂槽位 | slot:{slot.name} | pending:{pendingBrokenSlots.Count} | slots:{coinSlots.Count}");
        }

        return true;
    }

    public void AddInventoryCoin(CoinDefinition definition)
    {
        if (definition == null)
            return;

        inventoryCoins.Add(definition);
    }

    private void SubscribeTurnManager()
    {
        if (subscribedTurnManager == TurnManager.Instance)
            return;

        UnsubscribeTurnManager();
        subscribedTurnManager = TurnManager.Instance;

        if (subscribedTurnManager != null)
        {
            subscribedTurnManager.RoundEnded += OnRoundEnded;
        }
    }

    private void UnsubscribeTurnManager()
    {
        if (subscribedTurnManager == null)
            return;

        subscribedTurnManager.RoundEnded -= OnRoundEnded;
        subscribedTurnManager = null;
    }

    private void RefreshCoinSubscriptions()
    {
        UnsubscribeCoinSubscriptions();

        for (int i = 0; i < coinSlots.Count; i++)
        {
            CoinStats stats = GetStats(coinSlots[i]);
            if (stats == null)
                continue;

            stats.Broken -= OnAnyCoinBroken;
            stats.Broken += OnAnyCoinBroken;
        }
    }

    private void UnsubscribeCoinSubscriptions()
    {
        for (int i = 0; i < coinSlots.Count; i++)
        {
            CoinStats stats = GetStats(coinSlots[i]);
            if (stats != null)
            {
                stats.Broken -= OnAnyCoinBroken;
            }
        }
    }

    private void OnAnyCoinBroken()
    {
        for (int i = 0; i < coinSlots.Count; i++)
        {
            ChessPiece slot = coinSlots[i];
            CoinStats stats = GetStats(slot);
            if (slot == null || stats == null || !stats.IsBroken || pendingBrokenSlots.Contains(slot))
                continue;

            pendingBrokenSlots.Add(slot);
            slot.SetCanBeControlledThisTurn(false);

            if (debugLog)
            {
                Debug.Log($"[CoinRosterManager] 记录破裂槽位 | slot:{slot.name} | pending:{pendingBrokenSlots.Count}");
            }
        }

        TryEndPlayerTurnIfAllCoinsBroken();
    }

    private void OnRoundEnded(int roundIndex)
    {
        RemoveInvalidPendingSlots();

        if (pendingBrokenSlots.Count == 0)
            return;

        if (!HasInventoryCoin)
        {
            if (debugLog)
            {
                Debug.Log(
                    $"[CoinRosterManager] 背包已无可用硬币，自动销毁所有待替换槽位 | round:{roundIndex} | " +
                    $"pending:{pendingBrokenSlots.Count} | inventory:{CountValidInventoryCoins()}"
                );
            }

            for (int i = pendingBrokenSlots.Count - 1; i >= 0; i--)
            {
                DestroyBrokenSlot(pendingBrokenSlots[i]);
            }

            return;
        }

        if (debugLog)
        {
            Debug.Log(
                $"[CoinRosterManager] 回合结束，请求选择替换硬币 | round:{roundIndex} | " +
                $"pending:{pendingBrokenSlots.Count} | inventory:{CountValidInventoryCoins()}"
            );
        }

        ReplacementSelectionRequested?.Invoke(pendingBrokenSlots, inventoryCoins);
    }

    private void TryEndPlayerTurnIfAllCoinsBroken()
    {
        if (HasAliveCoin())
            return;

        TurnManager turnManager = TurnManager.Instance;
        if (turnManager == null || turnManager.currentState != TurnState.PlayerTurn || turnManager.IsEnemyTurnRunning)
            return;

        if (debugLog)
        {
            Debug.Log("[CoinRosterManager] 场上硬币全部破裂，直接结束玩家回合。");
        }

        turnManager.EndPlayerTurn();
    }

    private void RemoveInvalidPendingSlots()
    {
        for (int i = pendingBrokenSlots.Count - 1; i >= 0; i--)
        {
            CoinStats stats = GetStats(pendingBrokenSlots[i]);
            if (stats == null || !stats.IsBroken)
            {
                pendingBrokenSlots.RemoveAt(i);
            }
        }
    }

    private int CountValidInventoryCoins()
    {
        int count = 0;
        for (int i = 0; i < inventoryCoins.Count; i++)
        {
            if (inventoryCoins[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void AddSlotIfValid(ChessPiece slot)
    {
        if (slot == null || coinSlots.Contains(slot))
            return;

        coinSlots.Add(slot);
    }

    private static CoinStats GetStats(ChessPiece slot)
    {
        return slot != null ? slot.GetComponent<CoinStats>() : null;
    }
}
