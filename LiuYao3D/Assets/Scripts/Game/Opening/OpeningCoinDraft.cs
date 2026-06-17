/// <summary>
/// 实现功能：保存开局摇卦抽币结果、玩家选择结果和背包候选硬币数据。
/// </summary>
using System.Collections.Generic;

public class OpeningCoinDraft
{
    private readonly List<OpeningCoinDraftSlot> rolledCoins = new List<OpeningCoinDraftSlot>();
    private readonly List<OpeningCoinDraftSlot> selectedCoins = new List<OpeningCoinDraftSlot>();
    private readonly List<OpeningCoinDraftSlot> inventoryCoins = new List<OpeningCoinDraftSlot>();

    public IReadOnlyList<OpeningCoinDraftSlot> RolledCoins => rolledCoins;
    public IReadOnlyList<OpeningCoinDraftSlot> SelectedCoins => selectedCoins;
    public IReadOnlyList<OpeningCoinDraftSlot> InventoryCoins => inventoryCoins;

    public int RolledCount => rolledCoins.Count;
    public int SelectedCount => selectedCoins.Count;
    public int InventoryCount => inventoryCoins.Count;
    public bool HasRolledEnough => rolledCoins.Count >= OpeningCoinDraftRules.TotalRollCount;
    public bool HasSelectedEnough => selectedCoins.Count >= OpeningCoinDraftRules.SelectedCount;

    public void Clear()
    {
        rolledCoins.Clear();
        selectedCoins.Clear();
        inventoryCoins.Clear();
    }

    public void AddRolledCoin(OpeningCoinDraftSlot slot)
    {
        if (slot == null)
            return;

        rolledCoins.Add(slot);
    }

    public void SelectCoin(OpeningCoinDraftSlot slot)
    {
        if (slot == null || selectedCoins.Contains(slot))
            return;

        if (selectedCoins.Count >= OpeningCoinDraftRules.SelectedCount)
            return;

        selectedCoins.Add(slot);
    }

    public void RebuildInventoryFromUnselected()
    {
        inventoryCoins.Clear();

        for (int i = 0; i < rolledCoins.Count; i++)
        {
            OpeningCoinDraftSlot slot = rolledCoins[i];
            if (slot == null || selectedCoins.Contains(slot))
                continue;

            inventoryCoins.Add(slot);
        }
    }

    public List<CoinDefinition> GetSelectedDefinitions()
    {
        return GetValidDefinitions(selectedCoins);
    }

    public List<CoinDefinition> GetInventoryDefinitions()
    {
        return GetValidDefinitions(inventoryCoins);
    }

    private static List<CoinDefinition> GetValidDefinitions(IReadOnlyList<OpeningCoinDraftSlot> slots)
    {
        List<CoinDefinition> result = new List<CoinDefinition>();
        if (slots == null)
            return result;

        for (int i = 0; i < slots.Count; i++)
        {
            OpeningCoinDraftSlot slot = slots[i];
            if (slot != null && slot.Definition != null)
            {
                result.Add(slot.Definition);
            }
        }

        return result;
    }
}

public static class OpeningCoinDraftRules
{
    public const int RoundCount = 3;
    public const int CoinsPerRound = 3;
    public const int TotalRollCount = RoundCount * CoinsPerRound;
    public const int SelectedCount = 3;
}

public class OpeningCoinDraftSlot
{
    public OpeningCoinDraftSlot(CoinDefinition definition, string debugName, bool isPlaceholder)
    {
        Definition = definition;
        DebugName = debugName;
        IsPlaceholder = isPlaceholder;
    }

    public CoinDefinition Definition { get; }
    public string DebugName { get; }
    public bool IsPlaceholder { get; }

    public string DisplayName
    {
        get
        {
            if (Definition != null)
            {
                return Definition.coinName;
            }

            return string.IsNullOrEmpty(DebugName) ? "未命名模拟硬币" : DebugName;
        }
    }
}
