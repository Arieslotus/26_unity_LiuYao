/// <summary>
/// 实现功能：配置碰撞技能使用参与碰撞的两种卦象，尝试破除指定敌方目标的护盾。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Effect_BreakEnemyShield_", menuName = "Config/Collision Skill Effects/Break Enemy Shield")]
public class BreakEnemyShieldEffectData : CollisionSkillEffectData
{
    [Header("目标")]
    [SerializeField] private CollisionSkillTargetType targetType = CollisionSkillTargetType.AllEnemies;

    [Tooltip("目标为碰撞范围内敌人时使用。")]
    [Min(0f)]
    [SerializeField] private float radius = 3f;

    [Header("可破护盾卦象")]
    [SerializeField] private BreakShieldTrigramMode trigramMode = BreakShieldTrigramMode.CollisionParticipants;
    [SerializeField] private List<TrigramType> specifiedTrigrams = new List<TrigramType>();

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly BreakEnemyShieldEffectData data;

        public Controller(BreakEnemyShieldEffectData data)
        {
            this.data = data;
        }

        public void Execute(CollisionSkillContext context)
        {
            List<EnemyStats> targets = CollisionSkillTargetResolver.ResolveEnemies(
                context,
                data.targetType,
                data.radius);

            string sourceName = context.skill != null ? context.skill.SkillName : data.name;

            for (int i = 0; i < targets.Count; i++)
            {
                EnemyShieldController shield = targets[i].GetComponent<EnemyShieldController>();
                if (shield == null || !shield.HasShield)
                    continue;

                if (!data.CanBreakShield(context, shield.CurrentShieldType))
                    continue;

                shield.TryBreakShield(shield.CurrentShieldType, sourceName);
            }
        }
    }

    private bool CanBreakShield(CollisionSkillContext context, TrigramType shieldType)
    {
        if (shieldType == TrigramType.None)
            return false;

        if (trigramMode == BreakShieldTrigramMode.SpecifiedTrigrams)
            return specifiedTrigrams != null && specifiedTrigrams.Contains(shieldType);

        if (context == null)
            return false;

        return shieldType == context.activeTrigram ||
            shieldType == context.passiveTrigram;
    }
}
