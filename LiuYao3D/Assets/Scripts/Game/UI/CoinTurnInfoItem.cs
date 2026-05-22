/// <summary>
/// 实现功能：显示单枚硬币的轮次信息，包括名称、当前面 Sprite 与背面 Sprite，并支持已操作置灰。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class CoinTurnInfoItem : MonoBehaviour
{
    [Header("显示组件")]
    [SerializeField] private Text coinNameText;
    [SerializeField] private Image currentSideImage;
    [SerializeField] private Image backSideImage;

    [Header("透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float activeAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float actedAlpha = 0.6f;

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
        ApplyAlpha(hasActed ? actedAlpha : activeAlpha);
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
        SetGraphicAlpha(coinNameText, alpha);
        SetGraphicAlpha(currentSideImage, alpha);
        SetGraphicAlpha(backSideImage, alpha);
    }

    private void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
