/// <summary>
/// 实现功能：显示一个跨回合 Buff 图标项，使用主动卦与被动卦 UI 预制体上下合成图标，并显示层数。
/// </summary>
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BuffDisplayItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("图标根节点")]
    [SerializeField] private Transform activeTrigramRoot;
    [SerializeField] private Transform passiveTrigramRoot;

    [Header("层数")]
    [SerializeField] private GameObject stackRoot;
    [SerializeField] private Text stackText;

    public event Action<BuffDisplayItem> PointerEntered;
    public event Action<BuffDisplayItem> PointerExited;

    public void Set(
        CoinSkillRuntimeEffectSnapshot snapshot,
        int stackCount,
        TrigramVisualDatabase visualDatabase,
        Vector3 activeScale,
        Vector3 passiveScale,
        Vector2 activeOffset,
        Vector2 passiveOffset)
    {
        SpawnTrigramIcon(activeTrigramRoot, visualDatabase, snapshot.activeTrigram, activeScale, activeOffset);
        SpawnTrigramIcon(passiveTrigramRoot, visualDatabase, snapshot.passiveTrigram, passiveScale, passiveOffset);
        RefreshStack(stackCount);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExited?.Invoke(this);
    }

    private void SpawnTrigramIcon(
        Transform root,
        TrigramVisualDatabase visualDatabase,
        TrigramType trigram,
        Vector3 scale,
        Vector2 offset)
    {
        ClearChildren(root);

        if (root == null || visualDatabase == null || trigram == TrigramType.None)
            return;

        GameObject prefab = visualDatabase.GetUIPrefab(trigram);
        if (prefab == null)
            return;

        GameObject instance = Instantiate(prefab, root);
        instance.transform.localScale = scale;

        RectTransform rectTransform = instance.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = offset;
        }
        else
        {
            instance.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        }
    }

    private void RefreshStack(int stackCount)
    {
        int safeStackCount = Mathf.Max(1, stackCount);

        if (stackRoot != null)
        {
            stackRoot.SetActive(safeStackCount > 1);
        }

        if (stackText != null)
        {
            stackText.text = safeStackCount.ToString();
            stackText.gameObject.SetActive(safeStackCount > 1);
        }
    }

    private void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }
}
