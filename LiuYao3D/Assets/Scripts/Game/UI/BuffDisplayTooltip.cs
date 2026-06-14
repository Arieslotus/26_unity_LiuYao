/// <summary>
/// 实现功能：显示跨回合 Buff 总说明弹窗，将所有 Buff 描述按条目排列。
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BuffDisplayTooltip : MonoBehaviour
{
    [Header("根节点")]
    [SerializeField] private GameObject tooltipRoot;

    [Header("条目")]
    [Tooltip("条目父节点，建议挂 VerticalLayoutGroup。")]
    [SerializeField] private Transform entryRoot;

    [Tooltip("单条描述文本模板。未配置时会使用 fallbackText 合并显示。")]
    [SerializeField] private Text entryTextPrefab;

    [Tooltip("未配置 entryTextPrefab 时使用的兜底文本。")]
    [SerializeField] private Text fallbackText;

    private readonly List<Text> spawnedEntries = new List<Text>();

    private void Awake()
    {
        Hide();
    }

    public void Show(IReadOnlyList<string> descriptions)
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(true);
        }

        if (entryRoot != null && entryTextPrefab != null)
        {
            ShowAsEntries(descriptions);
            SetFallbackTextVisible(false);
            return;
        }

        ShowAsFallbackText(descriptions);
    }

    public void Hide()
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(false);
        }
    }

    private void ShowAsEntries(IReadOnlyList<string> descriptions)
    {
        int count = descriptions != null ? descriptions.Count : 0;
        EnsureEntryCount(count);

        for (int i = 0; i < spawnedEntries.Count; i++)
        {
            Text entry = spawnedEntries[i];
            if (entry == null)
                continue;

            bool active = i < count;
            entry.gameObject.SetActive(active);
            if (active)
            {
                entry.text = descriptions[i];
            }
        }
    }

    private void ShowAsFallbackText(IReadOnlyList<string> descriptions)
    {
        ClearEntries();

        if (fallbackText == null)
            return;

        fallbackText.gameObject.SetActive(true);
        fallbackText.text = descriptions == null || descriptions.Count == 0
            ? string.Empty
            : string.Join("\n", descriptions);
    }

    private void EnsureEntryCount(int count)
    {
        while (spawnedEntries.Count < count)
        {
            Text entry = Instantiate(entryTextPrefab, entryRoot);
            spawnedEntries.Add(entry);
        }
    }

    private void ClearEntries()
    {
        for (int i = 0; i < spawnedEntries.Count; i++)
        {
            if (spawnedEntries[i] != null)
            {
                spawnedEntries[i].gameObject.SetActive(false);
            }
        }
    }

    private void SetFallbackTextVisible(bool visible)
    {
        if (fallbackText != null)
        {
            fallbackText.gameObject.SetActive(visible);
        }
    }
}
