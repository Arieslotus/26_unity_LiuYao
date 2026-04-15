/// <summary>
/// 实现功能：根据抽取配置从卡池中抽取若干硬币，并分配到场上的固定棋子位。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class CoinLoadoutManager : MonoBehaviour
{
    [Header("抽取配置")]
    [Tooltip("硬币抽取规则配置")]
    [SerializeField] private CoinDrawConfig drawConfig;

    [Header("场上棋子位")]
    [Tooltip("用于接收抽取结果的棋子列表，按顺序分配")]
    [SerializeField] private List<ChessPiece> targetPieces = new List<ChessPiece>();

    [Header("调试")]
    [Tooltip("是否输出抽取与分配日志")]
    [SerializeField] private bool debugLog = true;

    private readonly List<CoinDefinition> currentLoadout = new List<CoinDefinition>();

    public IReadOnlyList<CoinDefinition> CurrentLoadout => currentLoadout;

    private void Start()
    {
        if (drawConfig == null)
        {
            Debug.LogError("[CoinLoadoutManager] 未绑定 CoinDrawConfig，无法执行抽取。");
            return;
        }

        if (drawConfig.AutoDrawOnStart)
        {
            GenerateLoadout();
        }
    }

    [ContextMenu("生成本局硬币配置")]
    public void GenerateLoadout()
    {
        if (!ValidateBeforeDraw())
            return;

        currentLoadout.Clear();

        List<CoinDefinition> result = DrawCoins(drawConfig);
        if (result == null || result.Count == 0)
        {
            Debug.LogError("[CoinLoadoutManager] 抽取失败，未生成任何结果。");
            return;
        }

        currentLoadout.AddRange(result);
        ApplyLoadoutToPieces(currentLoadout);

        if (debugLog)
        {
            Debug.Log($"[CoinLoadoutManager] 本局硬币生成完成 | 结果:{BuildLoadoutDebugText(currentLoadout)}");
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

        if (!drawConfig.AllowDuplicate && drawConfig.CoinPool.Count < drawConfig.DrawCount)
        {
            Debug.LogError(
                $"[CoinLoadoutManager] 当前配置为不允许重复，但卡池数量不足 | pool:{drawConfig.CoinPool.Count} | drawCount:{drawConfig.DrawCount}"
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

    private List<CoinDefinition> DrawCoins(CoinDrawConfig config)
    {
        if (config.AllowDuplicate)
        {
            return DrawWithDuplicate(config.CoinPool, config.DrawCount);
        }

        return DrawWithoutDuplicate(config.CoinPool, config.DrawCount);
    }

    private List<CoinDefinition> DrawWithDuplicate(List<CoinDefinition> pool, int count)
    {
        List<CoinDefinition> result = new List<CoinDefinition>();

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            CoinDefinition picked = pool[randomIndex];

            if (picked == null)
            {
                Debug.LogWarning($"[CoinLoadoutManager] 抽到空 CoinDefinition | index:{randomIndex}");
            }

            result.Add(picked);
        }

        return result;
    }

    private List<CoinDefinition> DrawWithoutDuplicate(List<CoinDefinition> pool, int count)
    {
        List<CoinDefinition> tempPool = new List<CoinDefinition>(pool);
        List<CoinDefinition> result = new List<CoinDefinition>();

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

            result.Add(picked);
            tempPool.RemoveAt(randomIndex);
        }

        return result;
    }

    private void ApplyLoadoutToPieces(List<CoinDefinition> loadout)
    {
        int assignCount = Mathf.Min(targetPieces.Count, loadout.Count);

        for (int i = 0; i < assignCount; i++)
        {
            ChessPiece piece = targetPieces[i];
            CoinDefinition definition = loadout[i];

            if (piece == null)
            {
                Debug.LogWarning($"[CoinLoadoutManager] targetPieces[{i}] 为空，跳过分配。");
                continue;
            }

            piece.SetCoinDefinition(definition, true);

            if (debugLog)
            {
                string coinName = definition != null ? definition.coinName : "空定义";
                Debug.Log($"[CoinLoadoutManager] 分配硬币 | slot:{i} | piece:{piece.name} | coin:{coinName}");
            }
        }

        for (int i = assignCount; i < targetPieces.Count; i++)
        {
            ChessPiece piece = targetPieces[i];
            if (piece == null)
                continue;

            piece.SetCoinDefinition(null, true);

            if (debugLog)
            {
                Debug.Log($"[CoinLoadoutManager] 清空多余棋子位 | slot:{i} | piece:{piece.name}");
            }
        }
    }

    private string BuildLoadoutDebugText(List<CoinDefinition> loadout)
    {
        if (loadout == null || loadout.Count == 0)
            return "空";

        List<string> names = new List<string>();

        for (int i = 0; i < loadout.Count; i++)
        {
            CoinDefinition definition = loadout[i];
            names.Add(definition != null ? definition.coinName : "空定义");
        }

        return string.Join(" / ", names);
    }
}