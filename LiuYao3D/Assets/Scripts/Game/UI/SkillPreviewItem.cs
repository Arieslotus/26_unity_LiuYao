/// <summary>
/// 实现功能：显示一条卦象碰撞技能预览，包括技能名、主动/被动卦象图标与描述。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class SkillPreviewItem : MonoBehaviour
{
    [Header("文本")]
    [Tooltip("技能名称文本")]
    [SerializeField] private Text skillNameText;

    [Tooltip("技能描述文本")]
    [SerializeField] private Text descriptionText;

    [Header("图标")]
    [Tooltip("主动卦象图标")]
    [SerializeField] private Image activeTrigramIcon;

    [Tooltip("被动卦象图标")]
    [SerializeField] private Image passiveTrigramIcon;

    public void Set(string skillName, Sprite activeIcon, Sprite passiveIcon, string description)
    {
        if (skillNameText != null)
        {
            skillNameText.text = string.IsNullOrWhiteSpace(skillName) ? "暂无" : skillName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrWhiteSpace(description) ? "暂无" : description;
        }

        SetIcon(activeTrigramIcon, activeIcon);
        SetIcon(passiveTrigramIcon, passiveIcon);
    }

    private void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }
}
