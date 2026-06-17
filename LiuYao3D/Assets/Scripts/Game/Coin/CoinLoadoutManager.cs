/// <summary>
/// 实现功能：根据抽取配置从卡池中抽取若干硬币，并分配到场上的固定棋子位。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class CoinLoadoutManager : MonoBehaviour
{
    private class CoinLoadoutEntry
    {
        public CoinDefinition definition;
        public bool isFrontSide;
    }

    [Header("抽取配置")]
    [Tooltip("硬币抽取规则配置")]
    [SerializeField] private CoinDrawConfig drawConfig;

    [Header("场上棋子位")]
    [Tooltip("用于接收抽取结果的棋子列表，按顺序分配")]
    [SerializeField] private List<ChessPiece> targetPieces = new List<ChessPiece>();

    [Tooltip("场景存在 OpeningFlowController 时，进入场景先隐藏场上硬币槽，等正式开始游戏分配硬币时再激活。")]
    [SerializeField] private bool hideTargetPiecesUntilOpeningFinished = true;

    [Header("调试")]
    [Tooltip("是否输出抽取与分配日志")]
    [SerializeField] private bool debugLog = true;

    private readonly List<CoinDefinition> currentLoadout = new List<CoinDefinition>();

    public IReadOnlyList<CoinDefinition> CurrentLoadout => currentLoadout;

    private void Awake()
    {
        if (hideTargetPiecesUntilOpeningFinished && FindObjectOfType<OpeningFlowController>() != null)
        {
            SetTargetPiecesActive(false);
        }
    }

    private void Start()
    {
        if (drawConfig == null)
        {
            Debug.LogError("[CoinLoadoutManager] 未绑定 CoinDrawConfig，无法执行抽取。");
            return;
        }

        if (drawConfig.AutoDrawOnStart && FindObjectOfType<OpeningFlowController>() == null)
        {
            GenerateLoadout();
        }
        else if (drawConfig.AutoDrawOnStart && debugLog)
        {
            Debug.Log("[CoinLoadoutManager] 检测到 OpeningFlowController，跳过启动时自动抽取，等待开局流程分配硬币。");
        }
    }

    [ContextMenu("生成本局硬币配置")]
    public void GenerateLoadout()
    {
        if (!ValidateBeforeDraw())
            return;

        currentLoadout.Clear();

        List<CoinLoadoutEntry> result = DrawCoins(drawConfig);
        if (result == null || result.Count == 0)
        {
            Debug.LogError("[CoinLoadoutManager] 抽取失败，未生成任何结果。");
            return;
        }

        for (int i = 0; i < result.Count; i++)
        {
            currentLoadout.Add(result[i].definition);
        }

        ApplyLoadoutToPieces(result);

        if (debugLog)
        {
            Debug.Log($"[CoinLoadoutManager] 本局硬币生成完成 | 结果:{BuildLoadoutDebugText(result)}");
        }
    }

    public void GenerateLoadout(CoinDrawConfig customConfig)
    {
        if (customConfig == null)
        {
            Debug.LogError("[CoinLoadoutManager] 传入的自定义 CoinDrawConfig 为空，无法执行抽取。");
            return;
        }

        drawConfig = customConfig;
        GenerateLoadout();
    }

    public bool ApplyFixedLoadout(IReadOnlyList<CoinDefinition> definitions)
    {
        if (definitions == null || definitions.Count == 0)
        {
            Debug.LogError("[CoinLoadoutManager] 指定硬币列表为空，无法分配到场上。");
            return false;
        }

        if (targetPieces == null || targetPieces.Count == 0)
        {
            Debug.LogError("[CoinLoadoutManager] 未配置 targetPieces，无法分配指定硬币。");
            return false;
        }

        currentLoadout.Clear();

        List<CoinLoadoutEntry> loadout = new List<CoinLoadoutEntry>();
        for (int i = 0; i < definitions.Count; i++)
        {
            CoinDefinition definition = definitions[i];
            if (definition == null)
            {
                Debug.LogWarning($"[CoinLoadoutManager] 指定硬币列表存在空引用，已跳过 | index:{i}");
                continue;
            }

            currentLoadout.Add(definition);
            loadout.Add(CreateLoadoutEntry(definition, CoinInitialSideMode.Random));
        }

        if (loadout.Count == 0)
        {
            Debug.LogError("[CoinLoadoutManager] 指定硬币列表没有有效硬币，无法分配到场上。");
            return false;
        }

        ApplyLoadoutToPieces(loadout);

        if (debugLog)
        {
            Debug.Log($"[CoinLoadoutManager] 应用开局选择硬币 | 结果:{BuildLoadoutDebugText(loadout)}");
        }

        return true;
    }

    private bool ValidateBeforeDraw()
    {
        if (drawConfig == null)
        {
            Debug.LogError("[CoinLoadoutManager] 未绑定 CoinDrawConfig。");
            return false;
        }

        if (targetPieces == null || targetPieces.Count == 0)
        {
            Debug.LogError("[CoinLoadoutManager] 未配置 targetPieces，无法分配抽取结果。");
            return false;
        }

        if (drawConfig.CoinPool == null || drawConfig.CoinPool.Count == 0)
        {
            Debug.LogError("[CoinLoadoutManager] 卡池为空，无法抽取。");
            return false;
        }

        if (drawConfig.DrawCount <= 0)
        {
            Debug.LogError($"[CoinLoadoutManager] 抽取数量非法 | drawCount:{drawConfig.DrawCount}");
            return false;
        }

        List<CoinDefinition> fixedCoins = GetValidFixedCoinDefinitions(drawConfig);
        if (fixedCoins.Count > drawConfig.DrawCount)
        {
            Debug.LogError(
                $"[CoinLoadoutManager] 内定币数量不能超过本局抽取数量 | fixed:{fixedCoins.Count} | drawCount:{drawConfig.DrawCount}"
            );
            return false;
        }

        if (fixedCoins.Count > 3)
        {
            Debug.LogError($"[CoinLoadoutManager] 内定币数量不能超过 3 | fixed:{fixedCoins.Count}");
            return false;
        }

        if (!drawConfig.AllowDuplicate && HasDuplicateCoin(fixedCoins))
        {
            Debug.LogError("[CoinLoadoutManager] 当前配置不允许重复，但内定币列表中存在重复硬币。");
            return false;
        }

        if (!drawConfig.AllowDuplicate && GetUniqueAvailableCount(drawConfig.CoinPool, fixedCoins) + fixedCoins.Count < drawConfig.DrawCount)
        {
            Debug.LogError(
                $"[CoinLoadoutManager] 当前配置为不允许重复，但卡池数量不足以补齐内定币后的随机结果 | " +
                $"pool:{drawConfig.CoinPool.Count} | fixed:{fixedCoins.Count} | drawCount:{drawConfig.DrawCount}"
            );
            return false;
        }

        if (targetPieces.Count < drawConfig.DrawCount)
        {
            Debug.LogWarning(
                $"[CoinLoadoutManager] 场上棋子位数量少于抽取数量，超出的结果不会被分配 | slots:{targetPieces.Count} | drawCount:{drawConfig.DrawCount}"
            );
        }

        return true;
    }

    private List<CoinLoadoutEntry> DrawCoins(CoinDrawConfig config)
    {
        List<CoinLoadoutEntry> result = DrawFixedCoins(config);
        int randomCount = config.DrawCount - result.Count;

        if (randomCount <= 0)
        {
            return result;
        }

        if (config.AllowDuplicate)
        {
            result.AddRange(DrawWithDuplicate(config.CoinPool, randomCount));
            return result;
        }

        List<CoinDefinition> randomPool = new List<CoinDefinition>(config.CoinPool);
        RemoveCoins(randomPool, GetDefinitions(result));
        result.AddRange(DrawWithoutDuplicate(randomPool, randomCount));
        return result;
    }

    private List<CoinLoadoutEntry> DrawWithDuplicate(List<CoinDefinition> pool, int count)
    {
        List<CoinLoadoutEntry> result = new List<CoinLoadoutEntry>();

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            CoinDefinition picked = pool[randomIndex];

            if (picked == null)
            {
                Debug.LogWarning($"[CoinLoadoutManager] 抽到空 CoinDefinition | index:{randomIndex}");
            }

            result.Add(CreateLoadoutEntry(picked, CoinInitialSideMode.Random));
        }

        return result;
    }

    private List<CoinLoadoutEntry> DrawWithoutDuplicate(List<CoinDefinition> pool, int count)
    {
        List<CoinDefinition> tempPool = new List<CoinDefinition>(pool);
        List<CoinLoadoutEntry> result = new List<CoinLoadoutEntry>();

        for (int i = 0; i < count; i++)
        {
            if (tempPool.Count == 0)
            {
                Debug.LogWarning("[CoinLoadoutManager] 临时卡池已空，提前结束抽取。");
                break;
            }

            int randomIndex = Random.Range(0, tempPool.Count);
            CoinDefinition picked = tempPool[randomIndex];

            if (picked == null)
            {
                Debug.LogWarning($"[CoinLoadoutManager] 抽到空 CoinDefinition | index:{randomIndex}");
            }

            result.Add(CreateLoadoutEntry(picked, CoinInitialSideMode.Random));
            tempPool.RemoveAt(randomIndex);
        }

        return result;
    }

    private List<CoinLoadoutEntry> DrawFixedCoins(CoinDrawConfig config)
    {
        List<CoinLoadoutEntry> result = new List<CoinLoadoutEntry>();
        List<FixedCoinDrawRule> fixedRules = GetValidFixedCoinRules(config);

        for (int i = 0; i < fixedRules.Count; i++)
        {
            FixedCoinDrawRule rule = fixedRules[i];
            result.Add(CreateLoadoutEntry(rule.coinDefinition, rule.initialSide));
        }

        return result;
    }

    private CoinLoadoutEntry CreateLoadoutEntry(CoinDefinition definition, CoinInitialSideMode sideMode)
    {
        return new CoinLoadoutEntry
        {
            definition = definition,
            isFrontSide = ResolveInitialSide(sideMode)
        };
    }

    private bool ResolveInitialSide(CoinInitialSideMode sideMode)
    {
        return sideMode switch
        {
            CoinInitialSideMode.Front => true,
            CoinInitialSideMode.Back => false,
            _ => Random.value < 0.5f
        };
    }

    private List<FixedCoinDrawRule> GetValidFixedCoinRules(CoinDrawConfig config)
    {
        List<FixedCoinDrawRule> result = new List<FixedCoinDrawRule>();

        if (config == null || config.FixedCoins == null)
            return result;

        for (int i = 0; i < config.FixedCoins.Count; i++)
        {
            FixedCoinDrawRule fixedRule = config.FixedCoins[i];
            if (fixedRule == null || fixedRule.coinDefinition == null)
            {
                Debug.LogWarning($"[CoinLoadoutManager] 内定币列表存在空引用，已跳过 | index:{i}");
                continue;
            }

            result.Add(fixedRule);
        }

        return result;
    }

    private List<CoinDefinition> GetValidFixedCoinDefinitions(CoinDrawConfig config)
    {
        List<CoinDefinition> result = new List<CoinDefinition>();
        List<FixedCoinDrawRule> fixedRules = GetValidFixedCoinRules(config);

        for (int i = 0; i < fixedRules.Count; i++)
        {
            result.Add(fixedRules[i].coinDefinition);
        }

        return result;
    }

    private List<CoinDefinition> GetDefinitions(List<CoinLoadoutEntry> entries)
    {
        List<CoinDefinition> result = new List<CoinDefinition>();

        if (entries == null)
            return result;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null)
            {
                result.Add(entries[i].definition);
            }
        }

        return result;
    }

    private bool HasDuplicateCoin(List<CoinDefinition> coins)
    {
        if (coins == null || coins.Count <= 1)
            return false;

        for (int i = 0; i < coins.Count; i++)
        {
            if (coins[i] == null)
                continue;

            for (int j = i + 1; j < coins.Count; j++)
            {
                if (coins[i] == coins[j])
                    return true;
            }
        }

        return false;
    }

    private int GetUniqueAvailableCount(List<CoinDefinition> pool, List<CoinDefinition> excludedCoins)
    {
        if (pool == null)
            return 0;

        List<CoinDefinition> uniqueAvailable = new List<CoinDefinition>();

        for (int i = 0; i < pool.Count; i++)
        {
            CoinDefinition coin = pool[i];
            if (coin == null)
                continue;

            if (ContainsCoin(excludedCoins, coin))
                continue;

            if (ContainsCoin(uniqueAvailable, coin))
                continue;

            uniqueAvailable.Add(coin);
        }

        return uniqueAvailable.Count;
    }

    private void RemoveCoins(List<CoinDefinition> pool, List<CoinDefinition> coinsToRemove)
    {
        if (pool == null || coinsToRemove == null)
            return;

        for (int i = pool.Count - 1; i >= 0; i--)
        {
            if (ContainsCoin(coinsToRemove, pool[i]))
            {
                pool.RemoveAt(i);
            }
        }
    }

    private bool ContainsCoin(List<CoinDefinition> coins, CoinDefinition target)
    {
        if (coins == null || target == null)
            return false;

        for (int i = 0; i < coins.Count; i++)
        {
            if (coins[i] == target)
                return true;
        }

        return false;
    }

    private void ApplyLoadoutToPieces(List<CoinLoadoutEntry> loadout)
    {
        int assignCount = Mathf.Min(targetPieces.Count, loadout.Count);

        for (int i = 0; i < assignCount; i++)
        {
            ChessPiece piece = targetPieces[i];
            CoinLoadoutEntry entry = loadout[i];
            CoinDefinition definition = entry != null ? entry.definition : null;
            bool isFrontSide = entry == null || entry.isFrontSide;

            if (piece == null)
            {
                Debug.LogWarning($"[CoinLoadoutManager] targetPieces[{i}] 为空，跳过分配。");
                continue;
            }

            if (!piece.gameObject.activeSelf)
            {
                piece.gameObject.SetActive(true);
            }

            piece.SetCoinDefinition(definition, false);
            piece.SetFace(isFrontSide, false);

            if (debugLog)
            {
                string coinName = definition != null ? definition.coinName : "空定义";
                Debug.Log($"[CoinLoadoutManager] 分配硬币 | slot:{i} | piece:{piece.name} | coin:{coinName} | 初始面:{(isFrontSide ? "正面" : "反面")}");
            }
        }

        for (int i = assignCount; i < targetPieces.Count; i++)
        {
            ChessPiece piece = targetPieces[i];
            if (piece == null)
                continue;

            if (hideTargetPiecesUntilOpeningFinished)
            {
                piece.gameObject.SetActive(false);
            }
            else
            {
                piece.SetCoinDefinition(null, true);
            }

            if (debugLog)
            {
                Debug.Log($"[CoinLoadoutManager] 清空多余棋子位 | slot:{i} | piece:{piece.name}");
            }
        }
    }

    private void SetTargetPiecesActive(bool active)
    {
        if (targetPieces == null)
            return;

        for (int i = 0; i < targetPieces.Count; i++)
        {
            ChessPiece piece = targetPieces[i];
            if (piece == null)
                continue;

            piece.gameObject.SetActive(active);
        }

        if (debugLog)
        {
            Debug.Log($"[CoinLoadoutManager] 设置场上硬币槽显隐 | active:{active} | count:{targetPieces.Count}");
        }
    }

    private string BuildLoadoutDebugText(List<CoinLoadoutEntry> loadout)
    {
        if (loadout == null || loadout.Count == 0)
            return "空";

        List<string> names = new List<string>();

        for (int i = 0; i < loadout.Count; i++)
        {
            CoinLoadoutEntry entry = loadout[i];
            CoinDefinition definition = entry != null ? entry.definition : null;
            string coinName = definition != null ? definition.coinName : "空定义";
            string sideName = entry == null || entry.isFrontSide ? "正面" : "反面";
            names.Add($"{coinName}({sideName})");
        }

        return string.Join(" / ", names);
    }
}
