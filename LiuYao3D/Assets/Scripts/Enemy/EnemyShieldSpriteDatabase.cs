/// <summary>
/// 实现功能：配置敌人不同属性护盾对应的 UI Sprite。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyShieldSpriteDatabase", menuName = "Config/Enemy Shield Sprite Database")]
public class EnemyShieldSpriteDatabase : ScriptableObject
{
    [Serializable]
    private class ShieldSpriteEntry
    {
        public TrigramType shieldType = TrigramType.None;
        public Sprite sprite;
    }

    [SerializeField] private List<ShieldSpriteEntry> entries = new List<ShieldSpriteEntry>();

    public Sprite GetSprite(TrigramType shieldType)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            ShieldSpriteEntry entry = entries[i];
            if (entry == null)
                continue;

            if (entry.shieldType == shieldType)
                return entry.sprite;
        }

        return null;
    }
}
