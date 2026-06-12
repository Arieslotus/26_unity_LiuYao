/// <summary>
/// 实现功能：统一创建并执行硬币碰撞技能中的内嵌效果与旧版效果资产。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public static class CollisionSkillExecutor
{
    public static void Execute(TrigramCollisionSkillSO skill, CollisionSkillContext context)
    {
        if (skill == null || context == null)
            return;

        ExecuteInlineEffects(skill, context);
        ExecuteLegacyEffects(skill, context);
    }

    private static void ExecuteInlineEffects(TrigramCollisionSkillSO skill, CollisionSkillContext context)
    {
        IReadOnlyList<CollisionSkillEffectConfig> effects = skill.InlineEffects;
        if (effects == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            CollisionSkillEffectConfig effect = effects[i];
            if (effect == null)
                continue;

            ICollisionSkillEffectController controller = effect.CreateController();
            if (controller == null)
            {
                Debug.LogWarning($"[CollisionSkillExecutor] 内嵌效果未创建 Controller | skill:{skill.SkillName} | effect:{effect.DisplayName}");
                continue;
            }

            controller.Execute(context);
        }
    }

    private static void ExecuteLegacyEffects(TrigramCollisionSkillSO skill, CollisionSkillContext context)
    {
        IReadOnlyList<CollisionSkillEffectData> effects = skill.Effects;
        if (effects == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            CollisionSkillEffectData effect = effects[i];
            if (effect == null)
                continue;

            ICollisionSkillEffectController controller = effect.CreateController();
            if (controller == null)
            {
                Debug.LogWarning($"[CollisionSkillExecutor] 旧版效果资产未创建 Controller | skill:{skill.SkillName} | effect:{effect.name}");
                continue;
            }

            controller.Execute(context);
        }
    }
}
