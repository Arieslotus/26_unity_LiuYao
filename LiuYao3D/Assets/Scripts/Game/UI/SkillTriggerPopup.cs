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

    [Header("卦象显示")]
    [Tooltip("八卦视觉资源配置。用于根据技能上下卦生成对应 UI 预制体。")]
    [SerializeField] private TrigramVisualDatabase trigramVisualDatabase;

    [Tooltip("上卦 UI 挂点。默认使用技能的主动卦 ActiveTrigram。")]
    [SerializeField] private RectTransform upperTrigramRoot;

    [Tooltip("下卦 UI 挂点。默认使用技能的被动卦 PassiveTrigram。")]
    [SerializeField] private RectTransform lowerTrigramRoot;

    [Header("动画")]
    [Tooltip("显示时是否重置图标和文字节点缩放")]
    [SerializeField] private bool resetScaleOnShow = true;

    [Tooltip("显示后需要延迟隐藏的动效物体，可为空。")]
    [SerializeField] private GameObject delayedHideEffectObject;

    [Tooltip("弹窗显示后经过多少秒隐藏指定动效物体。")]
    [Min(0f)]
    [SerializeField] private float delayedHideEffectSeconds = 0.5f;

    private Coroutine delayedHideEffectCoroutine;
    private GameObject upperTrigramInstance;
    private GameObject lowerTrigramInstance;

    public void Show(TrigramCollisionSkillSO skill)
    {
        ClearTrigramInstances();

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

        if (skill != null)
        {
            upperTrigramInstance = CreateTrigramInstance(skill.ActiveTrigram, upperTrigramRoot, "上卦");
            lowerTrigramInstance = CreateTrigramInstance(skill.PassiveTrigram, lowerTrigramRoot, "下卦");
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
        ClearTrigramInstances();
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

    private GameObject CreateTrigramInstance(TrigramType trigramType, RectTransform root, string label)
    {
        if (trigramVisualDatabase == null)
        {
            Debug.LogWarning($"[SkillTriggerPopup] {name} 未绑定 TrigramVisualDatabase，无法生成{label}。");
            return null;
        }

        if (root == null)
        {
            Debug.LogWarning($"[SkillTriggerPopup] {name} 未绑定{label}挂点，无法生成卦象 UI。");
            return null;
        }

        GameObject prefab = trigramVisualDatabase.GetUIPrefab(trigramType);
        if (prefab == null)
        {
            Debug.LogWarning($"[SkillTriggerPopup] 未找到{label} UI 预制体 | popup:{name} | trigram:{trigramType}");
            return null;
        }

        GameObject instance = Instantiate(prefab, root);
        instance.name = $"{prefab.name}_{trigramType}";

        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        return instance;
    }

    private void ClearTrigramInstances()
    {
        if (upperTrigramInstance != null)
        {
            Destroy(upperTrigramInstance);
            upperTrigramInstance = null;
        }

        if (lowerTrigramInstance != null)
        {
            Destroy(lowerTrigramInstance);
            lowerTrigramInstance = null;
        }
    }
}
