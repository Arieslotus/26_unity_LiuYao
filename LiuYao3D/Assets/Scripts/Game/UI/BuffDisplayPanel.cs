/// <summary>
/// 实现功能：全局显示跨回合 Buff 列表，合并同来源/同类型/同目标效果，并在悬停时显示总说明弹窗。
/// </summary>
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class BuffDisplayPanel : MonoBehaviour
{
    private sealed class DisplayEntry
    {
        public CoinSkillRuntimeEffectSnapshot snapshot;
        public int stackCount;
        public int effectCount;
    }

    [Header("数据")]
    [SerializeField] private CoinRoundEffectManager effectManager;
    [SerializeField] private BuffDisplayTextConfig textConfig;
    [SerializeField] private TrigramVisualDatabase visualDatabase;

    [Header("UI")]
    [SerializeField] private BuffDisplayItem itemPrefab;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private BuffDisplayTooltip tooltip;

    [Header("图标合成")]
    [SerializeField] private Vector3 activeTrigramScale = Vector3.one;
    [SerializeField] private Vector3 passiveTrigramScale = Vector3.one;
    [SerializeField] private Vector2 activeTrigramOffset = Vector2.zero;
    [SerializeField] private Vector2 passiveTrigramOffset = Vector2.zero;

    [Header("调试")]
    [SerializeField] private bool debugLog = false;

    private readonly List<BuffDisplayItem> spawnedItems = new List<BuffDisplayItem>();
    private readonly List<DisplayEntry> displayEntries = new List<DisplayEntry>();
    private readonly List<string> tooltipDescriptions = new List<string>();

    private void Awake()
    {
        if (effectManager == null)
        {
            effectManager = CoinRoundEffectManager.Instance != null
                ? CoinRoundEffectManager.Instance
                : FindObjectOfType<CoinRoundEffectManager>();
        }

        if (contentRoot == null)
        {
            contentRoot = transform;
        }
    }

    private void OnEnable()
    {
        SubscribeEffectManager();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeEffectManager();
        HideTooltip();
    }

    [ContextMenu("刷新 Buff 显示")]
    public void Refresh()
    {
        displayEntries.Clear();
        tooltipDescriptions.Clear();

        if (!ValidateReferences())
        {
            SetItemCount(0);
            return;
        }

        List<CoinSkillRuntimeEffectSnapshot> snapshots = effectManager.GetRuntimeEffectSnapshots();
        Dictionary<string, DisplayEntry> mergedEntries = new Dictionary<string, DisplayEntry>();

        for (int i = 0; i < snapshots.Count; i++)
        {
            CoinSkillRuntimeEffectSnapshot snapshot = snapshots[i];
            if (!IsDisplayable(snapshot))
                continue;

            string key = BuildMergeKey(snapshot);
            DisplayEntry entry;
            if (!mergedEntries.TryGetValue(key, out entry))
            {
                entry = new DisplayEntry
                {
                    snapshot = snapshot,
                    stackCount = Mathf.Max(1, snapshot.stackCount),
                    effectCount = 1
                };
                mergedEntries.Add(key, entry);
                displayEntries.Add(entry);
            }
            else
            {
                entry.effectCount++;
                entry.stackCount = Mathf.Max(entry.stackCount, Mathf.Max(1, snapshot.stackCount), entry.effectCount);
            }
        }

        SetItemCount(displayEntries.Count);
        for (int i = 0; i < displayEntries.Count; i++)
        {
            RefreshItem(spawnedItems[i], displayEntries[i]);
            tooltipDescriptions.Add(FormatDescription(displayEntries[i].snapshot));
        }

        if (debugLog)
        {
            Debug.Log($"[BuffDisplayPanel] 刷新 Buff 显示 | count:{displayEntries.Count}");
        }
    }

    private void SubscribeEffectManager()
    {
        if (effectManager == null)
        {
            effectManager = CoinRoundEffectManager.Instance != null
                ? CoinRoundEffectManager.Instance
                : FindObjectOfType<CoinRoundEffectManager>();
        }

        if (effectManager == null)
            return;

        effectManager.RuntimeEffectsChanged -= OnRuntimeEffectsChanged;
        effectManager.RuntimeEffectsChanged += OnRuntimeEffectsChanged;
    }

    private void UnsubscribeEffectManager()
    {
        if (effectManager != null)
        {
            effectManager.RuntimeEffectsChanged -= OnRuntimeEffectsChanged;
        }
    }

    private void OnRuntimeEffectsChanged()
    {
        Refresh();
    }

    private bool ValidateReferences()
    {
        if (effectManager == null)
        {
            if (debugLog)
            {
                Debug.LogWarning($"[BuffDisplayPanel] {name} 未找到 CoinRoundEffectManager，无法刷新 Buff 显示。");
            }

            return false;
        }

        if (itemPrefab == null || contentRoot == null)
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    $"[BuffDisplayPanel] 缺少 UI 引用 | itemPrefab:{itemPrefab != null} | contentRoot:{contentRoot != null}");
            }

            return false;
        }

        return true;
    }

    private void SetItemCount(int count)
    {
        while (spawnedItems.Count < count)
        {
            BuffDisplayItem item = Instantiate(itemPrefab, contentRoot);
            item.PointerEntered += OnItemPointerEntered;
            item.PointerExited += OnItemPointerExited;
            spawnedItems.Add(item);
        }

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
            {
                spawnedItems[i].gameObject.SetActive(i < count);
            }
        }
    }

    private void RefreshItem(BuffDisplayItem item, DisplayEntry entry)
    {
        if (item == null || entry == null)
            return;

        item.Set(
            entry.snapshot,
            entry.stackCount,
            visualDatabase,
            activeTrigramScale,
            passiveTrigramScale,
            activeTrigramOffset,
            passiveTrigramOffset);
    }

    private void OnItemPointerEntered(BuffDisplayItem item)
    {
        if (tooltip != null)
        {
            tooltip.Show(tooltipDescriptions);
        }
    }

    private void OnItemPointerExited(BuffDisplayItem item)
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }

    private string FormatDescription(CoinSkillRuntimeEffectSnapshot snapshot)
    {
        if (textConfig == null)
            return snapshot.kind.ToString();

        string sourceName = snapshot.sourceSkill != null && !string.IsNullOrWhiteSpace(snapshot.sourceSkill.SkillName)
            ? snapshot.sourceSkill.SkillName
            : snapshot.sourceId;

        return $"{(string.IsNullOrWhiteSpace(sourceName) ? "未知技能" : sourceName)}：{textConfig.Format(snapshot)}";
    }

    private bool IsDisplayable(CoinSkillRuntimeEffectSnapshot snapshot)
    {
        if (textConfig != null)
            return textConfig.IsDisplayable(snapshot);

        return snapshot.kind == CoinSkillRuntimeEffectKind.DamageModifier ||
            snapshot.kind == CoinSkillRuntimeEffectKind.EnemyShieldGenerationBlock ||
            snapshot.kind == CoinSkillRuntimeEffectKind.PendingCoinLoss ||
            snapshot.kind == CoinSkillRuntimeEffectKind.FlipCondition ||
            snapshot.kind == CoinSkillRuntimeEffectKind.UntilFlipDamageStack ||
            snapshot.kind == CoinSkillRuntimeEffectKind.ScheduledOutcome;
    }

    private string BuildMergeKey(CoinSkillRuntimeEffectSnapshot snapshot)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(snapshot.kind);
        builder.Append("|");
        builder.Append(snapshot.sourceSkill != null ? snapshot.sourceSkill.GetInstanceID().ToString() : snapshot.sourceId);
        builder.Append("|");
        AppendTargets(builder, snapshot);
        return builder.ToString();
    }

    private void AppendTargets(StringBuilder builder, CoinSkillRuntimeEffectSnapshot snapshot)
    {
        if (snapshot.targets != null && snapshot.targets.Count > 0)
        {
            for (int i = 0; i < snapshot.targets.Count; i++)
            {
                CoinStats target = snapshot.targets[i];
                if (target == null)
                    continue;

                builder.Append(target.GetInstanceID());
                builder.Append(",");
            }

            return;
        }

        builder.Append(snapshot.target != null ? snapshot.target.GetInstanceID().ToString() : "Global");
    }
}
