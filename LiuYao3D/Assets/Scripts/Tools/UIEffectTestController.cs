using System;
using UnityEngine;

/// <summary>
/// 实现功能：用于测试场景中通过一个开关控制多组 UI 特效同时进场与退场。
/// </summary>
public class UIEffectTestController : MonoBehaviour
{
    [Serializable]
    private class UIEffectGroup
    {
        [Tooltip("仅用于 Inspector 里区分测试对象。")]
        public string groupName = "UI 组合";

        [Tooltip("该组合的平移特效，可为空。")]
        public UIPositionEffect positionEffect;

        [Tooltip("该组合的淡入淡出特效，可为空。")]
        public UIFadeEffect fadeEffect;
    }

    [Header("测试开关")]
    [Tooltip("勾选时所有组合播放进场，取消勾选时所有组合播放退场。")]
    [SerializeField] private bool visible = true;

    [Header("测试组合")]
    [SerializeField] private UIEffectGroup[] effectGroups;

    private bool lastVisible;

    private void Reset()
    {
        UIPositionEffect[] positionEffects = GetComponentsInChildren<UIPositionEffect>(true);
        UIFadeEffect[] fadeEffects = GetComponentsInChildren<UIFadeEffect>(true);
        int groupCount = Mathf.Max(positionEffects.Length, fadeEffects.Length);

        effectGroups = new UIEffectGroup[groupCount];

        for (int i = 0; i < groupCount; i++)
        {
            effectGroups[i] = new UIEffectGroup
            {
                groupName = $"UI 组合 {i + 1}",
                positionEffect = i < positionEffects.Length ? positionEffects[i] : null,
                fadeEffect = i < fadeEffects.Length ? fadeEffects[i] : null
            };
        }
    }

    private void Awake()
    {
        lastVisible = visible;
    }

    private void Update()
    {
        if (visible == lastVisible)
        {
            return;
        }

        lastVisible = visible;

        if (visible)
        {
            PlayEnter();
        }
        else
        {
            PlayExit();
        }
    }

    public void PlayEnter()
    {
        if (effectGroups == null)
        {
            return;
        }

        foreach (UIEffectGroup group in effectGroups)
        {
            if (group == null)
            {
                continue;
            }

            group.positionEffect?.PlayEnter();
            group.fadeEffect?.PlayEnter();
        }
    }

    public void PlayExit()
    {
        if (effectGroups == null)
        {
            return;
        }

        foreach (UIEffectGroup group in effectGroups)
        {
            if (group == null)
            {
                continue;
            }

            group.positionEffect?.PlayExit();
            group.fadeEffect?.PlayExit();
        }
    }
}
