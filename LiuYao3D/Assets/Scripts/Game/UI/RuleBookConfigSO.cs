/// <summary>
/// 实现功能：配置规则说明书的页面图片列表，并保持页面顺序。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RuleBookConfig", menuName = "Config/Rule Book Config")]
public class RuleBookConfigSO : ScriptableObject
{
    [Header("页面")]
    [Tooltip("规则说明书页面图片。列表顺序就是翻页顺序。")]
    [SerializeField] private List<Sprite> pageSprites = new List<Sprite>();

    public int PageCount => pageSprites != null ? pageSprites.Count : 0;

    public Sprite GetPageSprite(int index)
    {
        if (pageSprites == null || index < 0 || index >= pageSprites.Count)
            return null;

        return pageSprites[index];
    }
}
