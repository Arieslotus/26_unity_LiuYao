/// <summary>
/// 实现功能：负责开局硬币三轮展示、复用 CoinModelShelf 布局滑入，并管理玩家选择三枚硬币。
/// </summary>
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class OpeningCoinPresentation : MonoBehaviour
{
    [Serializable]
    private class OpeningCoinRoundLayout
    {
        [Tooltip("本轮三枚硬币滑出的世界起点。")]
        public Transform spawnPoint;

        [Tooltip("本轮硬币展示架，负责生成与排列三枚硬币。")]
        public CoinModelShelf shelf;
    }

    [Header("三轮布局")]
    [Tooltip("三轮硬币出生点与展示架配置。")]
    [SerializeField] private OpeningCoinRoundLayout[] roundLayouts = new OpeningCoinRoundLayout[OpeningCoinDraftRules.RoundCount];

    [Header("滑出表现")]
    [Min(0.01f)]
    [Tooltip("单枚硬币滑出时长。")]
    [SerializeField] private float slideDuration = 0.45f;

    [Min(0f)]
    [Tooltip("同一轮三枚硬币之间的出场间隔。")]
    [SerializeField] private float slideInterval = 0.08f;

    [Tooltip("滑出缓动。OutCubic 比较平滑，没有明显反弹。")]
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    [Tooltip("滑出时是否同步旋转到展示架排布后的旋转。")]
    [SerializeField] private bool alignTargetRotation = true;

    [Header("选择")]
    [Tooltip("最多允许玩家选择的硬币数量。")]
    [SerializeField] private int requiredSelectionCount = OpeningCoinDraftRules.SelectedCount;

    [Header("调试")]
    [Tooltip("是否输出硬币表现日志。")]
    [SerializeField] private bool debugLog = true;

    [Header("确认后退场")]
    [Tooltip("玩家确认选择后，九枚硬币下落的世界方向。")]
    [SerializeField] private Vector3 exitFallDirection = Vector3.down;

    [Min(0f)]
    [Tooltip("玩家确认选择后，九枚硬币沿指定方向移动的距离。")]
    [SerializeField] private float exitFallDistance = 4f;

    [Min(0.01f)]
    [Tooltip("玩家确认选择后，九枚硬币下落退场时长。")]
    [SerializeField] private float exitFallDuration = 0.6f;

    [Min(0f)]
    [Tooltip("九枚硬币下落退场的逐个间隔。为 0 时同时下落。")]
    [SerializeField] private float exitFallInterval = 0.03f;

    [Tooltip("九枚硬币下落退场缓动。")]
    [SerializeField] private Ease exitFallEase = Ease.InCubic;

    [Tooltip("下落完成后是否清理并隐藏展示架硬币。")]
    [SerializeField] private bool hideAfterExitFall = true;

    private readonly List<CoinModelShelf> activeShelves = new List<CoinModelShelf>();
    private readonly Dictionary<CoinReplacementModelItem, OpeningCoinDraftSlot> itemToSlot = new Dictionary<CoinReplacementModelItem, OpeningCoinDraftSlot>();
    private readonly List<OpeningCoinDraftSlot> selectedSlots = new List<OpeningCoinDraftSlot>();
    private Sequence activeSequence;
    private bool selectionEnabled;

    public IReadOnlyList<OpeningCoinDraftSlot> SelectedSlots => selectedSlots;
    public int SelectedCount => selectedSlots.Count;
    public int RequiredSelectionCount => requiredSelectionCount;
    public bool HasEnoughSelection => selectedSlots.Count >= requiredSelectionCount;

    public event Action<int, int> SelectionChanged;
    public event Action<string> SelectionHintRequested;

    public IEnumerator RevealRound(OpeningCoinDraft draft, int roundIndex)
    {
        if (draft == null)
        {
            Debug.LogWarning($"[OpeningCoinPresentation] 硬币展示失败：draft 为空 | object:{name} | round:{roundIndex}");
            yield break;
        }

        OpeningCoinRoundLayout layout = GetRoundLayout(roundIndex);
        if (!ValidateRoundLayout(layout, roundIndex))
            yield break;

        KillActiveTween(false);

        List<OpeningCoinDraftSlot> roundSlots = GetRoundSlots(draft, roundIndex);
        List<CoinDefinition> roundDefinitions = GetDefinitions(roundSlots);
        if (roundDefinitions.Count <= 0)
        {
            Debug.LogWarning($"[OpeningCoinPresentation] 第{roundIndex}组硬币数据为空，跳过滑出 | object:{name}");
            yield break;
        }

        layout.shelf.gameObject.SetActive(true);
        layout.shelf.SetInteractionMode(false, false);
        layout.shelf.ItemClicked -= OnShelfItemClicked;
        layout.shelf.SetItems(roundDefinitions);

        if (!activeShelves.Contains(layout.shelf))
        {
            activeShelves.Add(layout.shelf);
        }

        IReadOnlyList<CoinReplacementModelItem> items = layout.shelf.SpawnedItems;
        if (items == null || items.Count <= 0)
            yield break;

        bool completed = false;
        activeSequence = DOTween.Sequence();
        int tweenIndex = 0;

        for (int i = 0; i < items.Count; i++)
        {
            CoinReplacementModelItem item = items[i];
            if (item == null)
                continue;

            if (i < roundSlots.Count)
            {
                itemToSlot[item] = roundSlots[i];
            }

            Transform itemTransform = item.transform;
            Transform parent = itemTransform.parent;
            Vector3 targetLocalPosition = item.TargetLocalPosition;
            Quaternion targetLocalRotation = itemTransform.localRotation;
            Vector3 spawnLocalPosition = parent != null
                ? parent.InverseTransformPoint(layout.spawnPoint.position)
                : layout.spawnPoint.position;
            Quaternion spawnLocalRotation = parent != null
                ? Quaternion.Inverse(parent.rotation) * layout.spawnPoint.rotation
                : layout.spawnPoint.rotation;

            itemTransform.DOKill(false);
            itemTransform.localPosition = spawnLocalPosition;
            itemTransform.localRotation = spawnLocalRotation;
            itemTransform.localScale = Vector3.one;
            item.SetHovered(false);
            item.SetSelected(false);

            activeSequence.Insert(
                tweenIndex * slideInterval,
                itemTransform.DOLocalMove(targetLocalPosition, slideDuration).SetEase(slideEase)
            );

            if (alignTargetRotation)
            {
                activeSequence.Insert(
                    tweenIndex * slideInterval,
                    itemTransform.DOLocalRotateQuaternion(targetLocalRotation, slideDuration).SetEase(slideEase)
                );
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[OpeningCoinPresentation] 准备硬币滑出 | round:{roundIndex} | item:{item.name} | " +
                    $"spawnLocal:{spawnLocalPosition} | targetLocal:{targetLocalPosition}"
                );
            }

            tweenIndex++;
        }

        if (tweenIndex <= 0)
            yield break;

        activeSequence.OnComplete(() => completed = true);

        if (debugLog)
        {
            Debug.Log($"[OpeningCoinPresentation] 第{roundIndex}组硬币滑出开始 | object:{name} | count:{roundDefinitions.Count}");
        }

        while (!completed && activeSequence != null && activeSequence.IsActive())
        {
            yield return null;
        }

        activeSequence = null;

        if (debugLog)
        {
            Debug.Log($"[OpeningCoinPresentation] 第{roundIndex}组硬币滑出完成 | object:{name}");
        }
    }

    public void EnableSelection()
    {
        selectionEnabled = true;
        selectedSlots.Clear();

        for (int i = 0; i < activeShelves.Count; i++)
        {
            CoinModelShelf shelf = activeShelves[i];
            if (shelf == null)
                continue;

            shelf.ClearSelection();
            shelf.SetInteractionMode(true, true);
            shelf.ItemClicked -= OnShelfItemClicked;
            shelf.ItemClicked += OnShelfItemClicked;
        }

        NotifySelectionChanged();
    }

    public void DisableSelection()
    {
        selectionEnabled = false;

        for (int i = 0; i < activeShelves.Count; i++)
        {
            CoinModelShelf shelf = activeShelves[i];
            if (shelf == null)
                continue;

            shelf.ItemClicked -= OnShelfItemClicked;
            shelf.SetInteractionMode(false, false);
        }
    }

    public IEnumerator PlayExitFall()
    {
        DisableSelection();
        KillActiveTween(false);

        List<CoinReplacementModelItem> items = CollectActiveItems();
        if (items.Count <= 0)
        {
            if (debugLog)
            {
                Debug.Log($"[OpeningCoinPresentation] 硬币下落退场跳过：没有可退场硬币 | object:{name}");
            }

            yield break;
        }

        Vector3 direction = exitFallDirection.sqrMagnitude > 0.0001f
            ? exitFallDirection.normalized
            : Vector3.down;

        bool completed = false;
        activeSequence = DOTween.Sequence();

        for (int i = 0; i < items.Count; i++)
        {
            CoinReplacementModelItem item = items[i];
            if (item == null)
                continue;

            Transform itemTransform = item.transform;
            itemTransform.DOKill(false);
            item.SetHovered(false);
            item.SetSelected(false);

            Vector3 targetPosition = itemTransform.position + direction * exitFallDistance;
            activeSequence.Insert(
                i * exitFallInterval,
                itemTransform.DOMove(targetPosition, exitFallDuration).SetEase(exitFallEase)
            );
        }

        activeSequence.OnComplete(() => completed = true);

        if (debugLog)
        {
            Debug.Log(
                $"[OpeningCoinPresentation] 九枚硬币下落退场开始 | object:{name} | " +
                $"count:{items.Count} | direction:{direction} | distance:{exitFallDistance}"
            );
        }

        while (!completed && activeSequence != null && activeSequence.IsActive())
        {
            yield return null;
        }

        activeSequence = null;

        if (hideAfterExitFall)
        {
            HideImmediate();
        }

        if (debugLog)
        {
            Debug.Log($"[OpeningCoinPresentation] 九枚硬币下落退场完成 | object:{name}");
        }
    }

    public void HideImmediate()
    {
        DisableSelection();
        KillActiveTween(false);

        for (int i = 0; i < activeShelves.Count; i++)
        {
            CoinModelShelf shelf = activeShelves[i];
            if (shelf == null)
                continue;

            shelf.ClearItems();
            shelf.gameObject.SetActive(false);
        }

        activeShelves.Clear();
        itemToSlot.Clear();
        selectedSlots.Clear();
    }

    public void KillActiveTween(bool complete)
    {
        if (activeSequence != null)
        {
            activeSequence.Kill(complete);
            activeSequence = null;
        }

        for (int i = 0; i < activeShelves.Count; i++)
        {
            CoinModelShelf shelf = activeShelves[i];
            if (shelf == null)
                continue;

            IReadOnlyList<CoinReplacementModelItem> items = shelf.SpawnedItems;
            for (int j = 0; j < items.Count; j++)
            {
                if (items[j] != null)
                {
                    items[j].transform.DOKill(complete);
                }
            }
        }
    }

    private List<CoinReplacementModelItem> CollectActiveItems()
    {
        List<CoinReplacementModelItem> result = new List<CoinReplacementModelItem>();

        for (int i = 0; i < activeShelves.Count; i++)
        {
            CoinModelShelf shelf = activeShelves[i];
            if (shelf == null)
                continue;

            IReadOnlyList<CoinReplacementModelItem> items = shelf.SpawnedItems;
            for (int j = 0; j < items.Count; j++)
            {
                if (items[j] != null && !result.Contains(items[j]))
                {
                    result.Add(items[j]);
                }
            }
        }

        return result;
    }

    private void OnShelfItemClicked(CoinReplacementModelItem item)
    {
        if (!selectionEnabled || item == null)
            return;

        if (!itemToSlot.TryGetValue(item, out OpeningCoinDraftSlot slot) || slot == null)
            return;

        CoinModelShelf shelf = FindShelfForItem(item);
        if (shelf == null)
            return;

        if (selectedSlots.Contains(slot))
        {
            selectedSlots.Remove(slot);
            shelf.SetSelected(item, false);
            NotifySelectionChanged();
            return;
        }

        if (selectedSlots.Count >= requiredSelectionCount)
        {
            SelectionHintRequested?.Invoke($"最多只能选择 {requiredSelectionCount} 枚硬币");
            return;
        }

        selectedSlots.Add(slot);
        shelf.SetSelected(item, true);
        NotifySelectionChanged();
    }

    private void NotifySelectionChanged()
    {
        SelectionChanged?.Invoke(selectedSlots.Count, requiredSelectionCount);
    }

    private CoinModelShelf FindShelfForItem(CoinReplacementModelItem item)
    {
        for (int i = 0; i < activeShelves.Count; i++)
        {
            CoinModelShelf shelf = activeShelves[i];
            if (shelf == null)
                continue;

            IReadOnlyList<CoinReplacementModelItem> items = shelf.SpawnedItems;
            for (int j = 0; j < items.Count; j++)
            {
                if (items[j] == item)
                    return shelf;
            }
        }

        return null;
    }

    private List<OpeningCoinDraftSlot> GetRoundSlots(OpeningCoinDraft draft, int roundIndex)
    {
        List<OpeningCoinDraftSlot> result = new List<OpeningCoinDraftSlot>();
        int startIndex = (roundIndex - 1) * OpeningCoinDraftRules.CoinsPerRound;

        for (int i = 0; i < OpeningCoinDraftRules.CoinsPerRound; i++)
        {
            int slotIndex = startIndex + i;
            if (slotIndex < 0 || slotIndex >= draft.RolledCoins.Count)
            {
                Debug.LogWarning($"[OpeningCoinPresentation] 抽币数据不足 | object:{name} | round:{roundIndex} | slotIndex:{slotIndex} | rolled:{draft.RolledCount}");
                continue;
            }

            OpeningCoinDraftSlot slot = draft.RolledCoins[slotIndex];
            if (slot != null)
            {
                result.Add(slot);
            }
        }

        return result;
    }

    private static List<CoinDefinition> GetDefinitions(IReadOnlyList<OpeningCoinDraftSlot> slots)
    {
        List<CoinDefinition> result = new List<CoinDefinition>();
        if (slots == null)
            return result;

        for (int i = 0; i < slots.Count; i++)
        {
            OpeningCoinDraftSlot slot = slots[i];
            if (slot != null)
            {
                result.Add(slot.Definition);
            }
        }

        return result;
    }

    private OpeningCoinRoundLayout GetRoundLayout(int roundIndex)
    {
        int index = roundIndex - 1;
        if (roundLayouts == null || index < 0 || index >= roundLayouts.Length)
            return null;

        return roundLayouts[index];
    }

    private bool ValidateRoundLayout(OpeningCoinRoundLayout layout, int roundIndex)
    {
        if (layout == null)
        {
            Debug.LogWarning($"[OpeningCoinPresentation] 第{roundIndex}轮布局为空，跳过硬币滑出 | object:{name}");
            return false;
        }

        if (layout.spawnPoint == null)
        {
            Debug.LogWarning($"[OpeningCoinPresentation] 第{roundIndex}轮出生点为空，跳过硬币滑出 | object:{name}");
            return false;
        }

        if (layout.shelf == null)
        {
            Debug.LogWarning($"[OpeningCoinPresentation] 第{roundIndex}轮 CoinModelShelf 为空，跳过硬币滑出 | object:{name}");
            return false;
        }

        return true;
    }
}
