/// <summary>
/// 实现功能：配置每种硬币类型在游戏内的名称与说明文本，供硬币说明面板查询。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CoinTypeInfoConfig", menuName = "Config/Coin Type Info Config")]
public class CoinTypeInfoConfig : ScriptableObject
{
    [Serializable]
    public class CoinTypeInfoEntry
    {
        [Tooltip("硬币类型。")]
        public CoinType coinType;

        [Tooltip("该类型在游戏内显示的名称。")]
        public string displayName;

        [TextArea(2, 5)]
        [Tooltip("该类型的说明文本。")]
        public string description;
    }

    [Header("类型说明")]
    [SerializeField] private List<CoinTypeInfoEntry> entries = new List<CoinTypeInfoEntry>();

    public bool TryGetInfo(CoinType coinType, out string displayName, out string description)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            CoinTypeInfoEntry entry = entries[i];
            if (entry == null || entry.coinType != coinType)
                continue;

            displayName = string.IsNullOrWhiteSpace(entry.displayName)
                ? coinType.ToString()
                : entry.displayName;
            description = entry.description ?? string.Empty;
            return true;
        }

        displayName = coinType.ToString();
        description = string.Empty;
        return false;
    }
}
