/// <summary>
/// 实现功能：负责开局抽币数据生成，保证三轮共九枚硬币来自配置币池且同一池条目不重复，并拆分开局选择与背包候选。
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

    public bool ValidateCanDrawOpeningCoins()
    {
        if (allowPlaceholderCoins)
            return true;

        if (drawConfig == null)
        {
            Debug.LogError($"[OpeningCoinDraftService] 未绑定 CoinDrawConfig，无法进行开局抽币 | owner:{logOwner}");
            return false;
        }

        if (drawConfig.CoinPool == null || drawConfig.CoinPool.Count == 0)
        {
            Debug.LogError($"[OpeningCoinDraftService] CoinDrawConfig 币池为空，无法进行开局抽币 | owner:{logOwner}");
            return false;
        }

        int availableCount = CountValidPoolEntries(drawConfig.CoinPool);
        if (availableCount < OpeningCoinDraftRules.TotalRollCount)
        {
            Debug.LogError(
                $"[OpeningCoinDraftService] 开局币池条目不足，无法抽满九枚硬币 | owner:{logOwner} | " +
                $"available:{availableCount} | need:{OpeningCoinDraftRules.TotalRollCount}"
            );
            return false;
        }

        return true;
    }

    public bool DrawRound(int roundIndex)
    {
        int beforeCount = draft.RolledCount;

        for (int i = 0; i < OpeningCoinDraftRules.CoinsPerRound; i++)
        {
            if (!DrawOneCoin(roundIndex))
            {
                Debug.LogError(
                    $"[OpeningCoinDraftService] 第{roundIndex}轮抽币失败 | owner:{logOwner} | " +
                    $"before:{beforeCount} | current:{draft.RolledCount}"
                );
                return false;
            }
        }

        if (debugLog)
        {
            Debug.Log(
                $"[OpeningCoinDraftService] 抽取一轮硬币 | owner:{logOwner} | " +
                $"round:{roundIndex} | rolled:{draft.RolledCount} | coins:{BuildLastRoundDebugText()}"
            );
        }

        return true;
    }

    public bool CompleteMissingRolls()
    {
        while (draft.RolledCount < OpeningCoinDraftRules.TotalRollCount)
        {
            int roundIndex = draft.RolledCount / OpeningCoinDraftRules.CoinsPerRound + 1;
            if (!DrawOneCoin(roundIndex))
            {
                Debug.LogError($"[OpeningCoinDraftService] 自动补齐开局硬币失败 | owner:{logOwner} | rolled:{draft.RolledCount}");
                return false;
            }
        }

        if (debugLog)
        {
            Debug.Log($"[OpeningCoinDraftService] 补齐开局硬币 | owner:{logOwner} | rolled:{draft.RolledCount}");
        }

        return true;
    }

    public void AutoSelectFirstCoins()
    {
        draft.AutoSelectFirstCoins();

        if (debugLog)
        {
            Debug.Log(
                $"[OpeningCoinDraftService] 自动选择开局硬币 | owner:{logOwner} | " +
                $"selected:{draft.SelectedCount} | inventory:{draft.InventoryCount}"
            );
        }
    }

    private bool DrawOneCoin(int roundIndex)
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
            Debug.LogError($"[OpeningCoinDraftService] 抽币失败：可用币池已空 | owner:{logOwner} | round:{roundIndex}");
            return false;
        }

        draft.AddRolledCoin(slot);
        return true;
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
            if (definition == null)
                continue;

            availablePool.Add(definition);
        }
    }

    private static int CountValidPoolEntries(List<CoinDefinition> pool)
    {
        if (pool == null)
            return 0;

        int count = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private string BuildLastRoundDebugText()
    {
        if (draft.RolledCount <= 0)
            return "空";

        int startIndex = Mathf.Max(0, draft.RolledCount - OpeningCoinDraftRules.CoinsPerRound);
        List<string> names = new List<string>();

        for (int i = startIndex; i < draft.RolledCoins.Count; i++)
        {
            OpeningCoinDraftSlot slot = draft.RolledCoins[i];
            names.Add(slot != null ? slot.DisplayName : "空");
        }

        return string.Join(" / ", names);
    }
}
