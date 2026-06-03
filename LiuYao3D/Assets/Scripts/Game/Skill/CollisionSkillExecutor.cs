/// <summary>
/// 实现功能：统一创建并执行硬币碰撞技能中的效果控制器。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public static class CollisionSkillExecutor
{
    public static void Execute(TrigramCollisionSkillSO skill, CollisionSkillContext context)
    {
        if (skill == null || context == null)
            return;

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
                Debug.LogWarning($"[CollisionSkillExecutor] 效果未创建 Controller | skill:{skill.SkillName} | effect:{effect.name}");
                continue;
            }

            controller.Execute(context);
        }
    }
}
