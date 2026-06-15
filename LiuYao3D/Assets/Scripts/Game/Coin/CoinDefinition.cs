/// <summary>
/// 实现功能：定义一枚硬币固定的正反面卦象与对应显示资源。
/// </summary>
using UnityEngine;

public enum CoinType
{
    DoubleSided,
    SingleSided,
    Fragile
}

[CreateAssetMenu(fileName = "CoinDefinition_", menuName = "Config/Coin Definition")]
public class CoinDefinition : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("硬币名称，仅用于编辑器和调试查看")]
    public string coinName;

    [Tooltip("硬币类型。双面币使用正反卦象；单面币建议正反配置为同一卦象；易碎币通常更低耐久、更高攻击。")]
    public CoinType coinType = CoinType.DoubleSided;

    [Header("战斗数值")]
    [Tooltip("硬币最大损耗值。损耗达到该值时硬币破裂。")]
    [Min(1)]
    public int maxLoss = 10;

    [Tooltip("硬币基础攻击力。")]
    [Min(0)]
    public int attack = 1;

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


    [Header("3D 显示")]
    [Tooltip("该硬币使用的 3D 材质。每种硬币应绑定一个独立 Material")]
    public Material coinMaterial;
}
