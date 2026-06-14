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

        if (ExecuteInlineEffects(skill, context) == CollisionSkillEffectExecutionResult.StopSkill)
            return;

        ExecuteLegacyEffects(skill, context);
    }

    private static CollisionSkillEffectExecutionResult ExecuteInlineEffects(TrigramCollisionSkillSO skill, CollisionSkillContext context)
    {
        IReadOnlyList<CollisionSkillEffectConfig> effects = skill.InlineEffects;
        if (effects == null)
            return CollisionSkillEffectExecutionResult.Continue;

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

            string previousRuntimeEffectId = context.runtimeEffectId;
            context.runtimeEffectId = BuildRuntimeEffectId(skill, "Inline", i, effect.GetType().Name);
            CollisionSkillEffectExecutionResult result = controller.Execute(context);
            context.runtimeEffectId = previousRuntimeEffectId;
            if (result == CollisionSkillEffectExecutionResult.StopSkill)
                return result;
        }

        return CollisionSkillEffectExecutionResult.Continue;
    }

    private static CollisionSkillEffectExecutionResult ExecuteLegacyEffects(TrigramCollisionSkillSO skill, CollisionSkillContext context)
    {
        IReadOnlyList<CollisionSkillEffectData> effects = skill.Effects;
        if (effects == null)
            return CollisionSkillEffectExecutionResult.Continue;

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

            string previousRuntimeEffectId = context.runtimeEffectId;
            context.runtimeEffectId = BuildRuntimeEffectId(skill, "Legacy", i, effect.name);
            CollisionSkillEffectExecutionResult result = controller.Execute(context);
            context.runtimeEffectId = previousRuntimeEffectId;
            if (result == CollisionSkillEffectExecutionResult.StopSkill)
                return result;
        }

        return CollisionSkillEffectExecutionResult.Continue;
    }

    private static string BuildRuntimeEffectId(TrigramCollisionSkillSO skill, string group, int index, string effectName)
    {
        string skillName = skill != null && !string.IsNullOrWhiteSpace(skill.SkillName)
            ? skill.SkillName
            : "TrigramCollisionSkill";

        string safeEffectName = string.IsNullOrWhiteSpace(effectName)
            ? "Effect"
            : effectName;

        return $"{skillName}/{group}#{index}:{safeEffectName}";
    }
}
