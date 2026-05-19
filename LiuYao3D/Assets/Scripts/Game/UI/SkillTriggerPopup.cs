/// <summary>
/// 实现功能：显示一次技能触发弹窗，包含技能图标和技能文字，并预留图标/文字独立动效节点。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class SkillTriggerPopup : MonoBehaviour
{
    [Header("节点")]
    [Tooltip("技能图标动效根节点")]
    [SerializeField] private RectTransform iconRoot;

    [Tooltip("技能文字动效根节点")]
    [SerializeField] private RectTransform textRoot;

    [Header("显示组件")]
    [Tooltip("技能图标")]
    [SerializeField] private Image skillIcon;

    [Tooltip("技能未配置图标时使用的默认图片")]
    [SerializeField] private Sprite defaultSkillIcon;

    [Tooltip("技能名称文本")]
    [SerializeField] private Text skillNameText;

    [Header("动画")]
    [Tooltip("显示时是否重置图标和文字节点缩放")]
    [SerializeField] private bool resetScaleOnShow = true;

    public void Show(TrigramCollisionSkillSO skill)
    {
        if (skillIcon != null)
        {
            Sprite sprite = skill != null && skill.SkillIcon != null
                ? skill.SkillIcon
                : defaultSkillIcon;

            skillIcon.sprite = sprite;
            skillIcon.enabled = skillIcon.sprite != null;
        }

        if (skillNameText != null)
        {
            skillNameText.text = skill != null && !string.IsNullOrWhiteSpace(skill.SkillName)
                ? skill.SkillName
                : "未知技能";
        }

        if (resetScaleOnShow)
        {
            if (iconRoot != null)
                iconRoot.localScale = Vector3.one;

            if (textRoot != null)
                textRoot.localScale = Vector3.one;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
