/// <summary>
/// 实现功能：定义一种敌人的基础配置，包括预制体、生命值与攻击力。
/// </summary>
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition_", menuName = "Config/Enemy Definition")]
public class EnemyDefinitionSO : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("敌人名称，仅用于编辑器与调试显示。")]
    public string enemyName;

    [Tooltip("该敌人使用的预制体。预制体上应包含 EnemyController、EnemyStats 与主要非 Trigger Collider。")]
    public GameObject enemyPrefab;

    [Header("战斗数值")]
    [Min(1)]
    [Tooltip("敌人最大生命值。")]
    public int maxHP = 10;

    [Min(0)]
    [Tooltip("敌人攻击力。默认 1。")]
    public int attack = 1;

    [Min(1)]
    [Tooltip("敌人每次行动最多攻击的目标数量。")]
    public int maxAttackTargetCount = 2;

    [Header("护盾")]
    [Tooltip("该敌人是否允许生成护盾。具体护盾类型建议由运行时规则决定。")]
    public bool allowShield = true;
}
