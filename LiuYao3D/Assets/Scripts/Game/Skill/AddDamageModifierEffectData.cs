/// <summary>
/// 实现功能：配置碰撞技能为己方硬币添加可延迟生效、可叠加的全局伤害增益。
/// </summary>
using UnityEngine;

[CreateAssetMenu(fileName = "Effect_AddDamageModifier_", menuName = "Config/Collision Skill Effects/Add Damage Modifier")]
public class AddDamageModifierEffectData : CollisionSkillEffectData
{
    [Header("增伤")]
    [Tooltip("伤害增加比例。例如 30 表示伤害增加 30%。")]
    [SerializeField] private float addDamagePercent = 30f;

    [Tooltip("-1 表示永久有效；正数表示持续对应数量的回合。")]
    [SerializeField] private int durationRounds = -1;

    [Tooltip("0 表示立即生效，1 表示下一回合开始生效。")]
    [Min(0)]
    [SerializeField] private int activateAfterRounds;

    [Tooltip("开启后，同一个效果可重复叠加。")]
    [SerializeField] private bool stackable = true;

    [Tooltip("非叠加效果使用该标识覆盖旧效果。留空时使用当前 Data 资源名。")]
    [SerializeField] private string modifierId;

    public override ICollisionSkillEffectController CreateController()
    {
        return new Controller(this);
    }

    private sealed class Controller : ICollisionSkillEffectController
    {
        private readonly AddDamageModifierEffectData data;

        public Controller(AddDamageModifierEffectData data)
        {
            this.data = data;
        }

        public void Execute(CollisionSkillContext context)
        {
            if (CoinRoundEffectManager.Instance == null)
            {
                Debug.LogWarning("[AddDamageModifierEffectData] 缺少 CoinRoundEffectManager，无法添加增伤效果。");
                return;
            }

            string sourceId = string.IsNullOrWhiteSpace(data.modifierId)
                ? data.name
                : data.modifierId.Trim();

            CoinRoundEffectManager.Instance.AddDamageModifier(
                sourceId,
                data.addDamagePercent / 100f,
                data.durationRounds,
                data.activateAfterRounds,
                data.stackable);
        }
    }
}
