/// <summary>
/// 实现功能：显示游戏结束弹窗，并根据胜利或失败切换一组或多组结果图片。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameEndPopup : UIPopupBase
{
    [Serializable]
    private sealed class ResultImageEntry
    {
        [Tooltip("需要根据胜负切换图片的 Image。")]
        public Image image;

        [Tooltip("胜利时显示的图片。")]
        public Sprite victorySprite;

        [Tooltip("失败时显示的图片。")]
        public Sprite defeatSprite;
    }

    [Header("结果图片")]
    [Tooltip("兼容旧配置：单个结果 Image。新配置建议使用下方 Result Images。")]
    [SerializeField] private Image resultImage;

    [Tooltip("兼容旧配置：单个结果 Image 的胜利图片。")]
    [SerializeField] private Sprite victorySprite;

    [Tooltip("兼容旧配置：单个结果 Image 的失败图片。")]
    [SerializeField] private Sprite defeatSprite;

    [Tooltip("多组结果 Image 配置。可用于同一个结束 UI 中有多个需要随胜负切换的图片。")]
    [SerializeField] private List<ResultImageEntry> resultImages = new List<ResultImageEntry>();

    private void Reset()
    {
        resultImage = GetComponentInChildren<Image>(true);
    }

    public void SetResult(bool isVictory)
    {
        bool appliedAny = false;

        if (resultImage != null)
        {
            ApplyResultImage(resultImage, victorySprite, defeatSprite, isVictory);
            appliedAny = true;
        }

        for (int i = 0; i < resultImages.Count; i++)
        {
            ResultImageEntry entry = resultImages[i];
            if (entry == null || entry.image == null)
                continue;

            ApplyResultImage(entry.image, entry.victorySprite, entry.defeatSprite, isVictory);
            appliedAny = true;
        }

        if (!appliedAny)
        {
            Debug.LogWarning($"[GameEndPopup] {name} 未绑定任何结果 Image，无法显示游戏结束图片。");
        }
    }

    private void ApplyResultImage(Image image, Sprite victory, Sprite defeat, bool isVictory)
    {
        if (image == null)
            return;

        Sprite resultSprite = isVictory ? victory : defeat;
        if (resultSprite == null)
        {
            Debug.LogWarning($"[GameEndPopup] {name} 未配置 {(isVictory ? "胜利" : "失败")} 图片 | image:{image.name}");
        }

        image.sprite = resultSprite;
        image.enabled = resultSprite != null;
    }
}
