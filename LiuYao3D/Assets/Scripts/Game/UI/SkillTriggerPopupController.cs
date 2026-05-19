/// <summary>
/// 实现功能：监听战斗技能触发事件，在屏幕中央显示技能图标与技能文字，并按指定持续时间自动隐藏。
/// </summary>
using UnityEngine;

public class SkillTriggerPopupController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("技能触发弹窗预制体")]
    [SerializeField] private SkillTriggerPopup popupPrefab;

    [Tooltip("弹窗生成父节点，通常是 Canvas 中央区域")]
    [SerializeField] private Transform popupRoot;

    [Tooltip("是否复用同一个弹窗实例")]
    [SerializeField] private bool reusePopup = true;

    [Header("调试")]
    [SerializeField] private bool debugLog = false;

    private SkillTriggerPopup currentPopup;
    private float hideTimer;

    private void Awake()
    {
        if (popupRoot == null)
        {
            popupRoot = transform;
        }
    }

    private void OnEnable()
    {
        CombatSkillEvents.SkillTriggerFeedbackRequested += OnSkillTriggerFeedbackRequested;
    }

    private void OnDisable()
    {
        CombatSkillEvents.SkillTriggerFeedbackRequested -= OnSkillTriggerFeedbackRequested;
    }

    private void Update()
    {
        if (currentPopup == null || hideTimer <= 0f)
            return;

        hideTimer -= Time.unscaledDeltaTime;
        if (hideTimer <= 0f)
        {
            currentPopup.Hide();
        }
    }

    private void OnSkillTriggerFeedbackRequested(TrigramCollisionSkillSO skill, float duration)
    {
        SkillTriggerPopup popup = GetPopup();
        if (popup == null)
            return;

        hideTimer = Mathf.Max(0f, duration);
        popup.Show(skill);

        if (debugLog)
        {
            Debug.Log(
                $"[SkillTriggerPopupController] 显示技能触发 UI | " +
                $"skill:{(skill != null ? skill.SkillName : "空")} | duration:{duration:F3}"
            );
        }
    }

    private SkillTriggerPopup GetPopup()
    {
        if (reusePopup && currentPopup != null)
            return currentPopup;

        if (popupPrefab == null)
        {
            Debug.LogWarning($"[SkillTriggerPopupController] {name} 未绑定 SkillTriggerPopup 预制体。");
            return null;
        }

        currentPopup = Instantiate(popupPrefab, popupRoot);
        Debug.Log($"[SkillTriggerPopupController] {name} 创建了新的 SkillTriggerPopup 实例。");
        return currentPopup;
    }
}
