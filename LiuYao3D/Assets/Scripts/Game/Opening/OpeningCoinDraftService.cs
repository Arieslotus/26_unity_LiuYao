/// <summary>
/// 实现功能：负责开局抽币数据的模拟生成、补齐、自动选择和背包候选拆分。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class OpeningCoinDraftService
{
    private readonly OpeningCoinDraft draft = new OpeningCoinDraft();
    private readonly List<CoinDefinition> availablePool = new List<CoinDefinition>();

    private CoinDrawConfig drawConfig;
    private bool allowPlaceholderCoins;
    private bool debugLog;
    private string logOwner;
    private int placeholderIndex;

    public OpeningCoinDraft Draft => draft;

    public OpeningCoinDraftService(CoinDrawConfig drawConfig, bool allowPlaceholderCoins, bool debugLog, string logOwner)
    {
        Reset(drawConfig, allowPlaceholderCoins, debugLog, logOwner);
    }

    public void Reset(CoinDrawConfig config, bool allowPlaceholders, bool enableDebugLog, string ownerName)
    {
        drawConfig = config;
        allowPlaceholderCoins = allowPlaceholders;
        debugLog = enableDebugLog;
        logOwner = string.IsNullOrEmpty(ownerName) ? nameof(OpeningCoinDraftService) : ownerName;
        placeholderIndex = 0;

        draft.Clear();
        RebuildAvailablePool();
    }

    public void DrawRound(int roundIndex)
    {
        for (int i = 0; i < OpeningCoinDraftRules.CoinsPerRound; i++)
        {
            DrawOneCoin(roundIndex);
        }

        if (debugLog)
        {
            Debug.Log($"[OpeningCoinDraftService] 模拟抽取一轮硬币 | owner:{logOwner} | round:{roundIndex} | rolled:{draft.RolledCount}");
        }
    }

    public void CompleteMissingRolls()
    {
        while (draft.RolledCount < OpeningCoinDraftRules.TotalRollCount)
        {
            int roundIndex = draft.RolledCount / OpeningCoinDraftRules.CoinsPerRound + 1;
            DrawOneCoin(roundIndex);
        }

        if (debugLog)
        {
            Debug.Log($"[OpeningCoinDraftService] 补齐开局硬币 | owner:{logOwner} | rolled:{draft.RolledCount}");
        }
    }

    public void AutoSelectFirstCoins()
    {
        for (int i = 0; i < draft.RolledCoins.Count; i++)
        {
            if (draft.SelectedCount >= OpeningCoinDraftRules.SelectedCount)
                break;

            draft.SelectCoin(draft.RolledCoins[i]);
        }

        draft.RebuildInventoryFromUnselected();

        if (debugLog)
        {
            Debug.Log(
                $"[OpeningCoinDraftService] 自动选择开局硬币 | owner:{logOwner} | " +
                $"selected:{draft.SelectedCount} | inventory:{draft.InventoryCount}"
            );
        }
    }

    private void DrawOneCoin(int roundIndex)
    {
        CoinDefinition definition = PickDefinition();
        OpeningCoinDraftSlot slot;

        if (definition != null)
        {
            slot = new OpeningCoinDraftSlot(definition, definition.coinName, false);
        }
        else if (allowPlaceholderCoins)
        {
            placeholderIndex++;
            slot = new OpeningCoinDraftSlot(null, $"模拟硬币{placeholderIndex}", true);
        }
        else
        {
            Debug.LogWarning($"[OpeningCoinDraftService] 抽币失败，且未允许模拟占位 | owner:{logOwner} | round:{roundIndex}");
            return;
        }

        draft.AddRolledCoin(slot);
    }

    private CoinDefinition PickDefinition()
    {
        if (availablePool.Count <= 0)
            return null;

        int index = Random.Range(0, availablePool.Count);
        CoinDefinition definition = availablePool[index];
        availablePool.RemoveAt(index);
        return definition;
    }

    private void RebuildAvailablePool()
    {
        availablePool.Clear();

        if (drawConfig == null || drawConfig.CoinPool == null)
            return;

        for (int i = 0; i < drawConfig.CoinPool.Count; i++)
        {
            CoinDefinition definition = drawConfig.CoinPool[i];
            if (definition == null || availablePool.Contains(definition))
                continue;

            availablePool.Add(definition);
        }

        if (debugLog && availablePool.Count < OpeningCoinDraftRules.TotalRollCount && !allowPlaceholderCoins)
        {
            Debug.LogWarning(
                $"[OpeningCoinDraftService] 币池不足以抽满九枚且未允许占位 | owner:{logOwner} | " +
                $"pool:{availablePool.Count} | need:{OpeningCoinDraftRules.TotalRollCount}"
            );
        }
    }
}
