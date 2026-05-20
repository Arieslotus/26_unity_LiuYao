/// <summary>
/// 实现功能：显示游戏结束弹窗，并根据胜利或失败切换不同图片。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class GameEndPopup : UIPopupBase
{
    [Header("结果图片")]
    [SerializeField] private Image resultImage;
    [SerializeField] private Sprite victorySprite;
    [SerializeField] private Sprite defeatSprite;

    private void Reset()
    {
        resultImage = GetComponentInChildren<Image>(true);
    }

    public void SetResult(bool isVictory)
    {
        if (resultImage == null)
        {
            Debug.LogWarning($"[GameEndPopup] {name} 未绑定结果 Image，无法显示游戏结束图片。");
            return;
        }

        Sprite resultSprite = isVictory ? victorySprite : defeatSprite;
        if (resultSprite == null)
        {
            Debug.LogWarning($"[GameEndPopup] {name} 未配置 {(isVictory ? "胜利" : "失败")} 图片。");
        }

        resultImage.sprite = resultSprite;
        resultImage.enabled = resultSprite != null;
    }
}
