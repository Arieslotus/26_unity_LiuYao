/// <summary>
/// 实现功能：显示一次技能触发弹窗，包含技能图标和技能文字，并预留图标/文字独立动效节点。
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    [Tooltip("显示后需要延迟隐藏的动效物体，可为空。")]
    [SerializeField] private GameObject delayedHideEffectObject;

    [Tooltip("弹窗显示后经过多少秒隐藏指定动效物体。")]
    [Min(0f)]
    [SerializeField] private float delayedHideEffectSeconds = 0.5f;

    private Coroutine delayedHideEffectCoroutine;

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
        RestartDelayedHideEffect();
    }

    public void Hide()
    {
        StopDelayedHideEffect();
        gameObject.SetActive(false);
    }

    private void RestartDelayedHideEffect()
    {
        StopDelayedHideEffect();

        if (delayedHideEffectObject == null)
            return;

        delayedHideEffectObject.SetActive(true);
        delayedHideEffectCoroutine = StartCoroutine(DelayedHideEffectRoutine());
    }

    private IEnumerator DelayedHideEffectRoutine()
    {
        if (delayedHideEffectSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delayedHideEffectSeconds);
        }

        if (delayedHideEffectObject != null)
        {
            delayedHideEffectObject.SetActive(false);
        }

        delayedHideEffectCoroutine = null;
    }

    private void StopDelayedHideEffect()
    {
        if (delayedHideEffectCoroutine == null)
            return;

        StopCoroutine(delayedHideEffectCoroutine);
        delayedHideEffectCoroutine = null;
    }
}
