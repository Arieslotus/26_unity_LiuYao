/// <summary>
/// 实现功能：通用硬币模型展示架，负责根据 CoinDefinition 列表生成、排列、hover 与可选点击交互。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public enum CoinShelfLayoutDirection
{
    Horizontal,
    Vertical
}

public class CoinModelShelf : MonoBehaviour
{
    [Header("生成")]
    [SerializeField] private CoinReplacementModelItem itemPrefab;
    [SerializeField] private Transform itemRoot;

    [Header("点击检测")]
    [SerializeField] private Camera selectionCamera;
    [SerializeField] private LayerMask selectionLayerMask = ~0;
    [Min(0.1f)]
    [SerializeField] private float selectionRayDistance = 100f;

    [Header("布局")]
    [SerializeField] private CoinShelfLayoutDirection layoutDirection = CoinShelfLayoutDirection.Horizontal;
    [SerializeField] private float itemSpacing = 1.5f;
    [SerializeField] private Transform startAnchor;
    [Tooltip("自定义排列轴。为零时，横排默认使用 X 轴，竖排默认使用 Z 轴。")]
    [SerializeField] private Vector3 layoutAxis = Vector3.right;

    [Header("交互")]
    [SerializeField] private bool enableHover = true;
    [SerializeField] private bool enableSelection;

    [Header("调试")]
    [SerializeField] private bool debugLog;

    private readonly List<CoinReplacementModelItem> spawnedItems = new List<CoinReplacementModelItem>();
    private readonly List<CoinReplacementModelItem> selectedItems = new List<CoinReplacementModelItem>();
    private CoinReplacementModelItem hoveredItem;

    public IReadOnlyList<CoinReplacementModelItem> SpawnedItems => spawnedItems;
    public IReadOnlyList<CoinReplacementModelItem> SelectedItems => selectedItems;
    public bool EnableHover => enableHover;
    public bool EnableSelection => enableSelection;
    public event Action<CoinReplacementModelItem> ItemClicked;

    private void Update()
    {
        if (!isActiveAndEnabled)
            return;

        UpdateHoveredItem();

        if (!enableSelection || !Input.GetMouseButtonDown(0))
            return;

        if (hoveredItem != null)
        {
            ItemClicked?.Invoke(hoveredItem);
        }
    }

    public void SetInteractionMode(bool allowHover, bool allowSelection)
    {
        enableHover = allowHover;
        enableSelection = allowSelection;
        ClearHoveredItem();
    }

    public void SetItems(IReadOnlyList<CoinDefinition> definitions)
    {
        ClearItems();

        if (itemPrefab == null || itemRoot == null || definitions == null)
        {
            return;
        }

        float totalWidth = Mathf.Max(0, definitions.Count - 1) * itemSpacing;
        Vector3 anchorPosition = startAnchor != null
            ? itemRoot.InverseTransformPoint(startAnchor.position)
            : Vector3.zero;
        Vector3 direction = GetLayoutDirectionVector();
        Vector3 firstPosition = anchorPosition - direction * (totalWidth * 0.5f);

        for (int i = 0; i < definitions.Count; i++)
        {
            CoinReplacementModelItem item = Instantiate(itemPrefab, itemRoot);
            item.transform.localPosition = firstPosition + direction * (itemSpacing * i);
            item.Bind(definitions[i]);
            spawnedItems.Add(item);
        }

        if (debugLog)
        {
            Debug.Log($"[CoinModelShelf] 刷新展示架 | object:{name} | count:{spawnedItems.Count}");
        }
    }

    public void ClearItems()
    {
        ClearHoveredItem();

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            CoinReplacementModelItem item = spawnedItems[i];
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        spawnedItems.Clear();
        selectedItems.Clear();
    }

    public void SetSelected(CoinReplacementModelItem item, bool selected)
    {
        if (item == null)
            return;

        if (selected)
        {
            if (!selectedItems.Contains(item))
            {
                selectedItems.Add(item);
            }
        }
        else
        {
            selectedItems.Remove(item);
        }

        item.SetSelected(selected);
    }

    public void ClearSelection()
    {
        for (int i = 0; i < selectedItems.Count; i++)
        {
            if (selectedItems[i] != null)
            {
                selectedItems[i].SetSelected(false);
            }
        }

        selectedItems.Clear();
    }

    private void UpdateHoveredItem()
    {
        CoinReplacementModelItem item = enableHover ? FindModelUnderPointer() : null;
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
            return null;

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, selectionRayDistance, selectionLayerMask, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<CoinReplacementModelItem>();
    }

    private void ClearHoveredItem()
    {
        if (hoveredItem != null)
        {
            hoveredItem.SetHovered(false);
            hoveredItem = null;
        }
    }

    private Vector3 GetLayoutDirectionVector()
    {
        if (layoutAxis.sqrMagnitude > 0.0001f)
        {
            return layoutAxis.normalized;
        }

        return layoutDirection == CoinShelfLayoutDirection.Vertical
            ? Vector3.forward
            : Vector3.right;
    }
}
