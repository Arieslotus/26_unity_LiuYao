/// <summary>
/// 实现功能：显示单枚硬币的当前状态信息，包括名称、当前面 Sprite、背面 Sprite 与剩余血量，并支持已操作置灰。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class CoinTurnInfoItem : MonoBehaviour
{
    [Header("显示组件")]
    [SerializeField] private Text coinNameText;
    [SerializeField] private Image currentSideImage;
    [SerializeField] private Image backSideImage;

    [Header("血量显示")]
    [Tooltip("硬币血条 Slider，可选；绑定后会显示剩余血量比例。")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("硬币血条填充 Image，可选；如果不用 Slider，可以直接绑定 Fill Image。")]
    [SerializeField] private Image healthFillImage;

    [Tooltip("硬币血量文本，可选；显示格式为 当前剩余/最大值。")]
    [SerializeField] private Text healthText;

    [Tooltip("缺少 CoinStats 时是否隐藏血条相关 UI。")]
    [SerializeField] private bool hideHealthWhenMissingStats = true;

    [Header("透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float activeAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float actedAlpha = 0.6f;

    private CoinStats currentStats;
    private float currentAlpha = 1f;

    public void Set(ChessPiece piece, bool hasActed)
    {
        CoinDefinition definition = piece != null ? piece.CoinDefinition : null;
        bool isFrontSide = piece == null || piece.IsFrontSide;

        if (coinNameText != null)
        {
            coinNameText.text = definition != null && !string.IsNullOrWhiteSpace(definition.coinName)
                ? definition.coinName
                : "未知硬币";
        }

        Sprite currentSprite = null;
        Sprite backSprite = null;

        if (definition != null)
        {
            currentSprite = isFrontSide ? definition.frontSprite : definition.backSprite;
            backSprite = isFrontSide ? definition.backSprite : definition.frontSprite;
        }

        SetImage(currentSideImage, currentSprite);
        SetImage(backSideImage, backSprite);
        BindStats(piece != null ? piece.GetComponent<CoinStats>() : null);
        RefreshHealth();
        ApplyAlpha(hasActed ? actedAlpha : activeAlpha);
    }

    private void OnDisable()
    {
        UnbindStats();
    }

    private void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private void ApplyAlpha(float alpha)
    {
        currentAlpha = alpha;
        SetGraphicAlpha(coinNameText, alpha);
        SetGraphicAlpha(currentSideImage, alpha);
        SetGraphicAlpha(backSideImage, alpha);
        SetGraphicAlpha(healthFillImage, alpha);
        SetGraphicAlpha(healthText, alpha);

        if (healthSlider != null)
        {
            Graphic[] graphics = healthSlider.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                SetGraphicAlpha(graphics[i], alpha);
            }
        }
    }

    private void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private void BindStats(CoinStats stats)
    {
        if (currentStats == stats)
            return;

        UnbindStats();
        currentStats = stats;

        if (currentStats != null)
        {
            currentStats.LossChanged -= OnLossChanged;
            currentStats.LossChanged += OnLossChanged;
        }
    }

    private void UnbindStats()
    {
        if (currentStats != null)
        {
            currentStats.LossChanged -= OnLossChanged;
            currentStats = null;
        }
    }

    private void OnLossChanged(int currentLoss, int maxLoss)
    {
        RefreshHealth(currentLoss, maxLoss);
    }

    private void RefreshHealth()
    {
        if (currentStats == null)
        {
            SetHealthVisible(!hideHealthWhenMissingStats);
            RefreshHealth(0, 1);
            return;
        }

        SetHealthVisible(true);
        RefreshHealth(currentStats.CurrentLoss, currentStats.MaxLoss);
    }

    private void RefreshHealth(int currentLoss, int maxLoss)
    {
        maxLoss = Mathf.Max(1, maxLoss);
        currentLoss = Mathf.Clamp(currentLoss, 0, maxLoss);

        int remainingHealth = maxLoss - currentLoss;
        float normalized = (float)remainingHealth / maxLoss;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = normalized;
        }

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = normalized;
        }

        if (healthText != null)
        {
            healthText.text = $"{remainingHealth}/{maxLoss}";
        }

        ApplyAlpha(currentAlpha);
    }

    private void SetHealthVisible(bool visible)
    {
        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(visible);
        }

        if (healthFillImage != null)
        {
            healthFillImage.gameObject.SetActive(visible);
        }

        if (healthText != null)
        {
            healthText.gameObject.SetActive(visible);
        }
    }
}
