/// <summary>
/// 实现功能：局内展示当前背包中的硬币模型，负责在玩家可操作阶段入场、换币阶段退场，并在需要时刷新后重新入场。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class InGameInventoryShelfController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private CoinRosterManager rosterManager;
    [SerializeField] private CoinModelShelf shelf;
    [SerializeField] private CoinShelfSequenceAnimator shelfAnimator;
    [SerializeField] private GameObject shelfRoot;

    [Header("调试")]
    [SerializeField] private bool debugLog;

    private TurnManager subscribedTurnManager;
    private bool isVisible;
    private bool triedLateSubscribe;

    private void Awake()
    {
        if (rosterManager == null)
        {
            rosterManager = CoinRosterManager.Instance;
        }

        if (shelf == null)
        {
            shelf = GetComponentInChildren<CoinModelShelf>(true);
        }

        if (shelfAnimator == null)
        {
            shelfAnimator = GetComponentInChildren<CoinShelfSequenceAnimator>(true);
        }

        if (shelf != null)
        {
            shelf.SetInteractionMode(true, false);
        }

        if (shelfRoot != null)
        {
            shelfRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        ResolveRosterManager();
        SubscribeTurnManager();
    }

    private void Start()
    {
        ResolveRosterManager();
        SubscribeTurnManager();
    }

    private void Update()
    {
        if (subscribedTurnManager != null || triedLateSubscribe)
            return;

        if (TurnManager.Instance == null)
            return;

        triedLateSubscribe = true;
        SubscribeTurnManager();

        if (debugLog)
        {
            Debug.Log("[InGameInventoryShelfController] 延迟订阅 TurnManager 成功。");
        }
    }

    private void OnDisable()
    {
        UnsubscribeTurnManager();
    }

    public void RefreshShelf()
    {
        if (shelf == null || rosterManager == null)
            return;

        List<CoinDefinition> inventory = new List<CoinDefinition>();
        IReadOnlyList<CoinDefinition> source = rosterManager.InventoryCoins;
        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
            {
                inventory.Add(source[i]);
            }
        }

        shelf.SetItems(inventory);

        if (debugLog)
        {
            Debug.Log($"[InGameInventoryShelfController] 刷新局内背包展示 | count:{inventory.Count}");
        }
    }

    public void PlayEnter()
    {
        if (isVisible)
            return;

        isVisible = true;
        RefreshShelf();

        if (shelfRoot != null)
        {
            shelfRoot.SetActive(true);
        }

        if (shelfAnimator != null)
        {
            shelfAnimator.PlayEnter();
            return;
        }
    }

    public void PlayExit(System.Action onComplete = null)
    {
        if (!isVisible)
        {
            onComplete?.Invoke();
            return;
        }

        if (shelfAnimator != null)
        {
            shelfAnimator.PlayExit(() =>
            {
                isVisible = false;
                if (shelfRoot != null)
                {
                    shelfRoot.SetActive(false);
                }

                onComplete?.Invoke();
            });
            return;
        }

        if (shelfRoot != null)
        {
            shelfRoot.SetActive(false);
        }

        isVisible = false;
        onComplete?.Invoke();
    }

    private void ResolveRosterManager()
    {
        if (rosterManager == null)
        {
            rosterManager = CoinRosterManager.Instance;
        }
    }

    private void SubscribeTurnManager()
    {
        if (subscribedTurnManager == TurnManager.Instance)
            return;

        UnsubscribeTurnManager();
        subscribedTurnManager = TurnManager.Instance;

        if (subscribedTurnManager != null)
        {
            subscribedTurnManager.RoundStarted -= OnRoundStarted;
            subscribedTurnManager.RoundStarted += OnRoundStarted;

            if (debugLog)
            {
                Debug.Log("[InGameInventoryShelfController] 已订阅 TurnManager.RoundStarted。");
            }
        }
    }

    private void UnsubscribeTurnManager()
    {
        if (subscribedTurnManager == null)
            return;

        subscribedTurnManager.RoundStarted -= OnRoundStarted;
        subscribedTurnManager = null;
    }

    private void OnRoundStarted(int roundIndex)
    {
        if (debugLog)
        {
            Debug.Log($"[InGameInventoryShelfController] 收到 RoundStarted | round:{roundIndex}");
        }

        PlayEnter();
    }
}
