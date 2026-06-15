/// <summary>
/// 实现功能：在回合结束时显示背包中的可替换硬币模型，支持部分补位；宿主对象常驻监听，只控制子面板显示与隐藏。
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinReplacementPanel : UIPopupBase
{
    [Header("引用")]
    [Tooltip("硬币阵容管理器。为空时自动使用 CoinRosterManager.Instance。")]
    [SerializeField] private CoinRosterManager rosterManager;

    [Tooltip("可选硬币模型项预制体，需要带 Collider 和 CoinReplacementModelItem。")]
    [SerializeField] private CoinReplacementModelItem itemPrefab;

    [Tooltip("可选硬币模型的生成根节点。")]
    [SerializeField] private Transform itemRoot;

    [Tooltip("实际显示/隐藏的面板根节点。建议脚本宿主常驻激活，只切这个节点。")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("确认替换按钮。")]
    [SerializeField] private Button confirmButton;

    [Tooltip("提示文本，可为空。")]
    [SerializeField] private Text hintText;

    [Header("模型点击")]
    [Tooltip("用于点击替换硬币模型的相机。为空时使用 Camera.main。")]
    [SerializeField] private Camera selectionCamera;

    [Tooltip("可被点击的替换硬币模型 Layer。默认全部 Layer。")]
    [SerializeField] private LayerMask selectionLayerMask = ~0;

    [Tooltip("模型点击射线最大距离。")]
    [Min(0.1f)]
    [SerializeField] private float selectionRayDistance = 100f;

    [Header("模型布局")]
    [Tooltip("模型项的横向间距。")]
    [SerializeField] private float itemSpacing = 1.5f;

    [Tooltip("第一个模型项的生成锚点。为空时使用 itemRoot 自身。")]
    [SerializeField] private Transform startAnchor;

    [Header("提示文字")]
    [SerializeField] private string selectHintFormat = "请选择 {0} 枚硬币进行替换";
    [SerializeField] private string notEnoughSelectionFormat = "还需要选择 {0} 枚硬币";
    [SerializeField] private string maxSelectionHint = "已选满需要替换的硬币数量";

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private readonly List<CoinReplacementModelItem> spawnedItems = new List<CoinReplacementModelItem>();
    private readonly List<CoinReplacementModelItem> selectedItems = new List<CoinReplacementModelItem>();
    private readonly List<ChessPiece> pendingSlots = new List<ChessPiece>();
    private readonly List<ChessPiece> disabledControlledSlots = new List<ChessPiece>();

    private int requiredSelectionCount;
    private bool hasPausedRoundAdvance;
    private bool isPanelVisible;
    private CoinReplacementModelItem hoveredItem;
    private DragChargeInput gameplayInput;
    private bool hasLockedGameplayInput;
    private bool gameplayInputWasEnabled;

    private void Awake()
    {
        if (rosterManager == null)
        {
            rosterManager = CoinRosterManager.Instance;
        }

        gameplayInput = FindObjectOfType<DragChargeInput>(true);

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

    private void Update()
    {
        if (!isPanelVisible)
            return;

        UpdateHoveredModel();

        if (!Input.GetMouseButtonDown(0))
            return;

        if (hoveredItem != null)
        {
            ToggleSelection(hoveredItem);
        }
    }

    public void ToggleSelection(CoinReplacementModelItem item)
    {
        if (item == null || requiredSelectionCount <= 0)
            return;

        if (selectedItems.Contains(item))
        {
            selectedItems.Remove(item);
            item.SetSelected(false);
            RefreshHint();
            return;
        }

        if (selectedItems.Count >= requiredSelectionCount)
        {
            SetHint(maxSelectionHint);
            return;
        }

        selectedItems.Add(item);
        item.SetSelected(true);
        RefreshHint();
    }

    private void Show(IReadOnlyList<ChessPiece> brokenSlots, IReadOnlyList<CoinDefinition> inventoryCoins)
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
        SpawnItems(validInventory);
        RefreshHint();

        if (debugLog)
        {
            Debug.Log(
                $"[CoinReplacementPanel] 打开替换面板 | need:{requiredSelectionCount} | " +
                $"pending:{pendingSlots.Count} | inventory:{validInventory.Count}"
            );
        }
    }

    private void ConfirmReplacement()
    {
        if (rosterManager == null)
        {
            Debug.LogWarning("[CoinReplacementPanel] 缺少 CoinRosterManager，无法确认替换。");
            return;
        }

        if (selectedItems.Count < requiredSelectionCount)
        {
            SetHint(string.Format(notEnoughSelectionFormat, requiredSelectionCount - selectedItems.Count));
            return;
        }

        for (int i = 0; i < requiredSelectionCount; i++)
        {
            CoinReplacementModelItem selectedItem = selectedItems[i];
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
        CloseReplacementPanel();
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

    private void ResetRuntimeState()
    {
        ClearItems();
        pendingSlots.Clear();
        selectedItems.Clear();
        ClearHoveredItem();
        requiredSelectionCount = 0;
    }

    private void SpawnItems(IReadOnlyList<CoinDefinition> definitions)
    {
        if (itemPrefab == null || itemRoot == null)
        {
            Debug.LogWarning(
                $"[CoinReplacementPanel] 缺少生成引用 | itemPrefab:{itemPrefab != null} | itemRoot:{itemRoot != null}"
            );
            return;
        }

        float totalWidth = Mathf.Max(0, definitions.Count - 1) * itemSpacing;
        Vector3 anchorPosition = startAnchor != null
            ? itemRoot.InverseTransformPoint(startAnchor.position)
            : Vector3.zero;
        Vector3 firstPosition = anchorPosition - new Vector3(totalWidth * 0.5f, 0f, 0f);

        for (int i = 0; i < definitions.Count; i++)
        {
            CoinReplacementModelItem item = Instantiate(itemPrefab, itemRoot);
            item.transform.localPosition = firstPosition + new Vector3(itemSpacing * i, 0f, 0f);
            item.Bind(this, definitions[i]);
            spawnedItems.Add(item);
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
        if (selectedItems.Count >= requiredSelectionCount)
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

    private void UpdateHoveredModel()
    {
        CoinReplacementModelItem item = FindModelUnderPointer();
        if (hoveredItem == item)
            return;

        if (hoveredItem != null)
        {
            hoveredItem.SetHovered(false);
        }

        hoveredItem = item;

        if (hoveredItem != null)
        {
            hoveredItem.SetHovered(true);
        }
    }

    private CoinReplacementModelItem FindModelUnderPointer()
    {
        Camera cameraToUse = selectionCamera != null ? selectionCamera : Camera.main;
        if (cameraToUse == null)
        {
            Debug.LogWarning("[CoinReplacementPanel] 没有可用于模型点击检测的 Camera。");
            return null;
        }

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, selectionRayDistance, selectionLayerMask, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<CoinReplacementModelItem>();
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

    private void ClearItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            CoinReplacementModelItem item = spawnedItems[i];
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        spawnedItems.Clear();
    }

    private void ClearHoveredItem()
    {
        if (hoveredItem != null)
        {
            hoveredItem.SetHovered(false);
            hoveredItem = null;
        }
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
        if (hasLockedGameplayInput)
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
        hasLockedGameplayInput = true;
    }

    private void UnlockGameplayInput()
    {
        if (!hasLockedGameplayInput)
            return;

        if (gameplayInput != null)
        {
            gameplayInput.enabled = gameplayInputWasEnabled;
        }

        hasLockedGameplayInput = false;
        gameplayInputWasEnabled = false;
    }
}
