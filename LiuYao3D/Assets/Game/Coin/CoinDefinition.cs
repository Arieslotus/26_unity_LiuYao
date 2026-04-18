/// <summary>
/// 实现功能：定义一枚硬币固定的正反面卦象与对应显示资源。
/// </summary>
using UnityEngine;

[CreateAssetMenu(fileName = "CoinDefinition_", menuName = "Config/Coin Definition")]
public class CoinDefinition : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("硬币名称，仅用于编辑器和调试查看")]
    public string coinName;

    [Header("正反面卦象")]
    [Tooltip("正面对应的卦象")]
    public TrigramType frontTrigram;

    [Tooltip("反面对应的卦象")]
    public TrigramType backTrigram;

    [Header("正反面显示")]
    [Tooltip("正面显示的 Sprite")]
    public Sprite frontSprite;

    [Tooltip("反面显示的 Sprite")]
    public Sprite backSprite;
}