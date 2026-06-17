/// <summary>
/// 实现功能：显示单个敌人属性面板的文本与图标内容，供敌人悬停面板控制器复用。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class EnemyInfoPanelView : MonoBehaviour
{
    [Header("文本")]
    [Tooltip("敌人名称文本。")]
    [SerializeField] private Text nameText;

    [Tooltip("血量文本，格式为 当前/总量。")]
    [SerializeField] private Text healthText;

    [Tooltip("攻击力文本。")]
    [SerializeField] private Text attackText;

    [Tooltip("护盾剩余量文本，格式为 剩余/总量。")]
    [SerializeField] private Text shieldValueText;

    [Tooltip("攻击目标数量文本。")]
    [SerializeField] private Text targetCountText;

    [Header("图标")]
    [Tooltip("护盾属性图标。")]
    [SerializeField] private Image shieldImage;

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void Refresh(
        EnemyStats stats,
        EnemyController controller,
        EnemyShieldController shield,
        TrigramVisualDatabase visualDatabase)
    {
        if (nameText != null)
        {
            nameText.text = $"名称：{GetEnemyName(stats)}";
        }

        if (healthText != null)
        {
            int currentHP = stats != null ? stats.CurrentHP : 0;
            int maxHP = stats != null ? stats.MaxHP : 0;
            healthText.text = $"血量：{currentHP}/{maxHP}";
        }

        if (attackText != null)
        {
            int attack = stats != null ? stats.Attack : 0;
            attackText.text = $"攻击力：{attack}";
        }

        RefreshShield(shield, visualDatabase);

        if (targetCountText != null)
        {
            int targetCount = controller != null ? controller.MaxAttackTargetCount : 0;
            targetCountText.text = $"攻击目标：{targetCount}";
        }
    }

    private void RefreshShield(EnemyShieldController shield, TrigramVisualDatabase visualDatabase)
    {
        bool hasShield = shield != null && shield.HasShield;
        if (shieldValueText != null)
        {
            if (hasShield)
            {
                int total = Mathf.Max(1, shield.RequiredBreakValue);
                int remaining = Mathf.Clamp(total - shield.CurrentBreakValue, 0, total);
                shieldValueText.text = $"{remaining}/{total}";
            }
            else
            {
                shieldValueText.text = "无";
            }
        }

        if (shieldImage == null)
            return;

        if (!hasShield)
        {
            shieldImage.enabled = false;
            return;
        }

        Sprite sprite = visualDatabase != null
            ? visualDatabase.GetIcon(shield.CurrentShieldType)
            : null;

        shieldImage.sprite = sprite;
        shieldImage.enabled = sprite != null;
    }

    private static string GetEnemyName(EnemyStats stats)
    {
        if (stats != null && stats.Definition != null && !string.IsNullOrWhiteSpace(stats.Definition.enemyName))
            return stats.Definition.enemyName;

        return stats != null ? stats.name : string.Empty;
    }
}
