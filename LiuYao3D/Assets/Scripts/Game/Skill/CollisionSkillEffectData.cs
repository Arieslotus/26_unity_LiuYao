/// <summary>
/// 实现功能：定义硬币碰撞技能的单项效果配置基类，具体技能效果通过派生 Data 提供可调参数。
/// </summary>
using UnityEngine;

public abstract class CollisionSkillEffectData : ScriptableObject
{
    public abstract ICollisionSkillEffectController CreateController();
}
