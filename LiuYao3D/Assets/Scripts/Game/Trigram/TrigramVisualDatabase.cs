/// <summary>
/// 实现功能：配置八卦类型对应的通用视觉资源，包括 Sprite 与 UI 预制体。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrigramVisualDatabase", menuName = "Config/Trigram Visual Database")]
public class TrigramVisualDatabase : ScriptableObject
{
    [Serializable]
    private class TrigramVisualEntry
    {
        public TrigramType trigramType = TrigramType.None;
        public Sprite sprite;
        public GameObject uiPrefab;
    }

    [SerializeField] private List<TrigramVisualEntry> entries = new List<TrigramVisualEntry>();

    public Sprite GetSprite(TrigramType trigramType)
    {
        TrigramVisualEntry entry = FindEntry(trigramType);
        return entry != null ? entry.sprite : null;
    }

    public GameObject GetUIPrefab(TrigramType trigramType)
    {
        TrigramVisualEntry entry = FindEntry(trigramType);
        return entry != null ? entry.uiPrefab : null;
    }

    private TrigramVisualEntry FindEntry(TrigramType trigramType)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            TrigramVisualEntry entry = entries[i];
            if (entry == null)
                continue;

            if (entry.trigramType == trigramType)
                return entry;
        }

        return null;
    }
}
