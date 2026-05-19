/// <summary>
/// 实现功能：提供战斗技能表现事件，解耦碰撞逻辑、UI、音效与特效等表现系统。
/// </summary>
using System;

public static class CombatSkillEvents
{
    public static event Action<TrigramCollisionSkillSO, float> SkillTriggerFeedbackRequested;

    public static void RequestSkillTriggerFeedback(TrigramCollisionSkillSO skill, float duration)
    {
        if (skill == null || duration <= 0f)
            return;

        SkillTriggerFeedbackRequested?.Invoke(skill, duration);
    }
}
