/// <summary>
/// 实现功能：显示单个硬币说明面板的文本内容，供硬币说明控制器复用。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class CoinInfoPanelView : MonoBehaviour
{
    [Header("文本")]
    [Tooltip("硬币名称文本。")]
    [SerializeField] private Text coinNameText;

    [Tooltip("硬币类型名称文本。")]
    [SerializeField] private Text coinTypeNameText;

    [Tooltip("硬币类型说明文本。")]
    [SerializeField] private Text coinTypeDescriptionText;

    [Tooltip("基础攻击力文本。")]
    [SerializeField] private Text attackText;

    [Tooltip("满损耗值文本。")]
    [SerializeField] private Text maxLossText;

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void Refresh(CoinDefinition definition, CoinTypeInfoConfig typeInfoConfig)
    {
        string coinName = definition != null ? definition.coinName : string.Empty;
        CoinType coinType = definition != null ? definition.coinType : CoinType.DoubleSided;
        int attack = definition != null ? Mathf.Max(0, definition.attack) : 0;
        int maxLoss = definition != null ? Mathf.Max(1, definition.maxLoss) : 0;

        string typeDisplayName = coinType.ToString();
        string typeDescription = string.Empty;
        if (typeInfoConfig != null)
        {
            typeInfoConfig.TryGetInfo(coinType, out typeDisplayName, out typeDescription);
        }

        if (coinNameText != null)
        {
            coinNameText.text = $"{coinName}";
        }

        if (coinTypeNameText != null)
        {
            coinTypeNameText.text = $"{typeDisplayName}";
        }

        if (coinTypeDescriptionText != null)
        {
            coinTypeDescriptionText.text = $"{typeDescription}";
        }

        if (attackText != null)
        {
            attackText.text = $"基础攻击力：{attack}";
        }

        if (maxLossText != null)
        {
            maxLossText.text = $"满损耗值：{maxLoss}";
        }
    }
}
