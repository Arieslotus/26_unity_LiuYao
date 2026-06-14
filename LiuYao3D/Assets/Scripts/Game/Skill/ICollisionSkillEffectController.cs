/// <summary>
/// 实现功能：定义硬币碰撞技能效果的运行时执行接口。
/// </summary>
public enum CollisionSkillEffectExecutionResult
{
    Continue,
    StopSkill
}

public interface ICollisionSkillEffectController
{
    CollisionSkillEffectExecutionResult Execute(CollisionSkillContext context);
}
