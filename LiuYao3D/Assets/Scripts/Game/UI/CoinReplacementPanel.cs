/// <summary>
/// 实现功能：在回合结束时显示背包中的可替换硬币模型，支持部分补位；宿主对象常驻监听，只控制子面板显示与隐藏。
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinReplacementPanel : UIPopupBase
{
    [Header("引用")]
    [SerializeField] private CoinRosterManager rosterManager;
    [SerializeField] private CoinModelShelf modelShelf;
    [SerializeField] private CoinShelfSequenceAnimator shelfAnimator;
    [SerializeField] private InGameInventoryShelfController inGameShelfController;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Text hintText;

    [Header("提示文字")]
    [SerializeField] private string selectHintFormat = "请选择 {0} 枚硬币进行替换";
    [SerializeField] private string notEnoughSelectionFormat = "还需要选择 {0} 枚硬币";
    [SerializeField] private string maxSelectionHint = "已选满需要替换的硬币数量";

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private readonly List<ChessPiece> pendingSlots = new List<ChessPiece>();
    private readonly List<ChessPiece> disabledControlledSlots = new List<ChessPiece>();

    private int requiredSelectionCount;
    private bool hasPausedRoundAdvance;
    private bool isPanelVisible;
    private DragChargeInput gameplayInput;
    private bool hasLockedReplacementInput;
    private bool gameplayInputWasEnabled;

    private void Awake()
    {
        if (rosterManager == null)
        {
            rosterManager = CoinRosterManager.Instance;
        }

        if (modelShelf == null)
        {
            modelShelf = GetComponentInChildren<CoinModelShelf>(true);
        }

        if (shelfAnimator == null)
        {
            shelfAnimator = GetComponentInChildren<CoinShelfSequenceAnimator>(true);
        }

        gameplayInput = FindObjectOfType<DragChargeInput>(true);

        if (modelShelf != null)
        {
            modelShelf.SetInteractionMode(true, true);
            modelShelf.ItemClicked -= OnShelfItemClicked;
            modelShelf.ItemClicked += OnShelfItemClicked;
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ConfirmReplacement);
            confirmButton.onClick.AddListener(ConfirmReplacement);
        }

        HidePanelImmediate();
    }

    private void OnEnable()
    {
        ResolveRosterManager();
        SubscribeRosterManager();
    }

    private void OnDisable()
    {
        UnsubscribeRosterManager();
    }

    private void Show(IReadOnlyList<ChessPiece> brokenSlots, IReadOnlyList<CoinDefinition> inventoryCoins)
    {
        if (inGameShelfController != null)
        {
            inGameShelfController.PlayExit(() => ShowReplacementContent(brokenSlots, inventoryCoins));
            return;
        }

        ShowReplacementContent(brokenSlots, inventoryCoins);
    }

    private void OnShelfItemClicked(CoinReplacementModelItem item)
    {
        if (!isPanelVisible || item == null || modelShelf == null)
            return;

        bool isSelected = item.Selected;
        if (isSelected)
        {
            modelShelf.SetSelected(item, false);
            RefreshHint();
            return;
        }

        if (modelShelf.SelectedItems.Count >= requiredSelectionCount)
        {
            SetHint(maxSelectionHint);
            return;
        }

        modelShelf.SetSelected(item, true);
        RefreshHint();
    }

    private void ConfirmReplacement()
    {
        if (rosterManager == null || modelShelf == null)
        {
            Debug.LogWarning("[CoinReplacementPanel] 缺少必要引用，无法确认替换。");
            return;
        }

        if (modelShelf.SelectedItems.Count < requiredSelectionCount)
        {
            SetHint(string.Format(notEnoughSelectionFormat, requiredSelectionCount - modelShelf.SelectedItems.Count));
            return;
        }

        for (int i = 0; i < requiredSelectionCount; i++)
        {
            CoinReplacementModelItem selectedItem = modelShelf.SelectedItems[i];
            ChessPiece slot = pendingSlots[i];
            CoinDefinition replacement = selectedItem != null ? selectedItem.Definition : null;

            if (!rosterManager.TryReplaceBrokenSlot(slot, replacement))
            {
                Debug.LogWarning(
                    $"[CoinReplacementPanel] 替换失败 | index:{i} | " +
                    $"slot:{(slot != null ? slot.name : "空")} | " +
                    $"coin:{(replacement != null ? replacement.coinName : "空")}"
                );
                return;
            }
        }

        DestroyUnfilledBrokenSlots(requiredSelectionCount);
        PlayReplacementExitAndReturnToInGame();
    }

    private void CloseReplacementPanel()
    {
        isPanelVisible = false;
        RestoreCoinControls();
        ResumeRoundAdvance();
        HidePanelImmediate();
    }

    private void HidePanelImmediate()
    {
        ResetRuntimeState();

        if (hintText != null)
        {
            hintText.text = string.Empty;
            hintText.gameObject.SetActive(false);
        }

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
        }

        SetPanelVisible(false);
    }

    private void ShowReplacementContent(IReadOnlyList<ChessPiece> brokenSlots, IReadOnlyList<CoinDefinition> inventoryCoins)
    {
        ResetRuntimeState();
        AddValidBrokenSlots(brokenSlots);

        List<CoinDefinition> validInventory = GetValidInventoryCoins(inventoryCoins);
        requiredSelectionCount = Mathf.Min(pendingSlots.Count, validInventory.Count);

        if (pendingSlots.Count <= 0)
        {
            HidePanelImmediate();
            return;
        }

        if (requiredSelectionCount <= 0)
        {
            DestroyUnfilledBrokenSlots(0);
            HidePanelImmediate();
            return;
        }

        PauseRoundAdvance();
        DisableCoinControls();

        isPanelVisible = true;
        SetPanelVisible(true);
        EnsureUiVisible();

        if (modelShelf != null)
        {
            modelShelf.SetInteractionMode(true, true);
            modelShelf.SetItems(validInventory);
        }

        if (shelfAnimator != null)
        {
            shelfAnimator.PlayEnter();
        }

        RefreshHint();

        if (debugLog)
        {
            Debug.Log(
                $"[CoinReplacementPanel] 打开替换面板 | need:{requiredSelectionCount} | " +
                $"pending:{pendingSlots.Count} | inventory:{validInventory.Count}"
            );
        }
    }

    private void PlayReplacementExitAndReturnToInGame()
    {
        if (shelfAnimator != null)
        {
            shelfAnimator.PlayExit(() =>
            {
                CloseReplacementPanel();
                if (inGameShelfController != null)
                {
                    inGameShelfController.PlayEnter();
                }
            });
            return;
        }

        CloseReplacementPanel();
        if (inGameShelfController != null)
        {
            inGameShelfController.PlayEnter();
        }
    }

    private void ResetRuntimeState()
    {
        pendingSlots.Clear();
        requiredSelectionCount = 0;

        if (modelShelf != null)
        {
            modelShelf.ClearSelection();
            modelShelf.ClearItems();
        }
    }

    private void AddValidBrokenSlots(IReadOnlyList<ChessPiece> brokenSlots)
    {
        if (brokenSlots == null)
            return;

        for (int i = 0; i < brokenSlots.Count; i++)
        {
            ChessPiece slot = brokenSlots[i];
            if (slot == null)
                continue;

            CoinStats stats = slot.GetComponent<CoinStats>();
            if (stats == null || !stats.IsBroken)
                continue;

            pendingSlots.Add(slot);
        }
    }

    private List<CoinDefinition> GetValidInventoryCoins(IReadOnlyList<CoinDefinition> inventoryCoins)
    {
        List<CoinDefinition> result = new List<CoinDefinition>();
        if (inventoryCoins == null)
            return result;

        for (int i = 0; i < inventoryCoins.Count; i++)
        {
            if (inventoryCoins[i] != null)
            {
                result.Add(inventoryCoins[i]);
            }
        }

        return result;
    }

    private void DestroyUnfilledBrokenSlots(int replacedCount)
    {
        if (rosterManager == null)
            return;

        for (int i = pendingSlots.Count - 1; i >= replacedCount; i--)
        {
            ChessPiece slot = pendingSlots[i];
            if (slot == null)
                continue;

            rosterManager.DestroyBrokenSlot(slot);
        }
    }

    private void RefreshHint()
    {
        if (modelShelf == null)
            return;

        if (modelShelf.SelectedItems.Count >= requiredSelectionCount)
        {
            SetHint(string.Empty);
            return;
        }

        SetHint(string.Format(selectHintFormat, requiredSelectionCount));
    }

    private void SetHint(string text)
    {
        if (hintText != null)
        {
            hintText.text = text;
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
        }
    }

    private void EnsureUiVisible()
    {
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
        }
    }

    private void ResolveRosterManager()
    {
        if (rosterManager == null)
        {
            rosterManager = CoinRosterManager.Instance;
        }
    }

    private void SubscribeRosterManager()
    {
        if (rosterManager == null)
            return;

        rosterManager.ReplacementSelectionRequested -= OnReplacementSelectionRequested;
        rosterManager.ReplacementSelectionRequested += OnReplacementSelectionRequested;
    }

    private void UnsubscribeRosterManager()
    {
        if (rosterManager != null)
        {
            rosterManager.ReplacementSelectionRequested -= OnReplacementSelectionRequested;
        }
    }

    private void OnReplacementSelectionRequested(
        IReadOnlyList<ChessPiece> brokenSlots,
        IReadOnlyList<CoinDefinition> inventoryCoins)
    {
        Show(brokenSlots, inventoryCoins);
    }

    protected override void OnClose()
    {
        CloseReplacementPanel();
    }

    private void PauseRoundAdvance()
    {
        if (hasPausedRoundAdvance)
            return;

        TurnManager turnManager = TurnManager.Instance;
        if (turnManager == null)
            return;

        turnManager.PauseRoundAdvance("CoinReplacementPanel");
        hasPausedRoundAdvance = true;
    }

    private void ResumeRoundAdvance()
    {
        if (!hasPausedRoundAdvance)
            return;

        TurnManager turnManager = TurnManager.Instance;
        if (turnManager != null)
        {
            turnManager.ResumeRoundAdvance("CoinReplacementPanel");
        }

        hasPausedRoundAdvance = false;
    }

    private void DisableCoinControls()
    {
        disabledControlledSlots.Clear();

        if (rosterManager == null)
            return;

        IReadOnlyList<ChessPiece> slots = rosterManager.CoinSlots;
        for (int i = 0; i < slots.Count; i++)
        {
            ChessPiece slot = slots[i];
            if (slot == null || !slot.CanBeControlledThisTurn)
                continue;

            slot.SetCanBeControlledThisTurn(false);
            slot.SetControlHintVisible(false);
            disabledControlledSlots.Add(slot);
        }

        LockGameplayInput();
    }

    private void RestoreCoinControls()
    {
        UnlockGameplayInput();

        for (int i = 0; i < disabledControlledSlots.Count; i++)
        {
            ChessPiece slot = disabledControlledSlots[i];
            if (slot == null || !slot.gameObject.activeSelf)
                continue;

            CoinStats stats = slot.GetComponent<CoinStats>();
            if (stats != null && stats.IsBroken)
                continue;

            slot.SetCanBeControlledThisTurn(true);
        }

        disabledControlledSlots.Clear();
    }

    private void LockGameplayInput()
    {
        if (hasLockedReplacementInput)
            return;

        if (gameplayInput == null)
        {
            gameplayInput = FindObjectOfType<DragChargeInput>(true);
        }

        if (gameplayInput == null)
            return;

        gameplayInputWasEnabled = gameplayInput.enabled;
        gameplayInput.SetControlledPiece(null);
        gameplayInput.enabled = false;
        hasLockedReplacementInput = true;
    }

    private void UnlockGameplayInput()
    {
        if (!hasLockedReplacementInput)
            return;

        if (gameplayInput != null)
        {
            gameplayInput.enabled = gameplayInputWasEnabled;
        }

        hasLockedReplacementInput = false;
        gameplayInputWasEnabled = false;
    }
}
