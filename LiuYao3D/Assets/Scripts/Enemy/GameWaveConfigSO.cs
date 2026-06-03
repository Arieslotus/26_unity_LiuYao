/// <summary>
/// 实现功能：配置一局游戏中的敌人波次、生成回合与每波敌人组成。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameWaveConfig_", menuName = "Config/Game Wave Config")]
public class GameWaveConfigSO : ScriptableObject
{
    [Header("波次列表")]
    [Tooltip("按顺序配置一局内的所有敌人波次。")]
    public List<WaveDefinition> waves = new List<WaveDefinition>();
}

[Serializable]
public class WaveDefinition
{
    [Header("基础信息")]
    [Tooltip("波次名称，仅用于编辑器与调试显示。")]
    public string waveName;

    [Min(0)]
    [Tooltip("指定在第几个大回合开始时生成。0 表示不按回合自动生成。")]
    public int spawnAtRound = 1;

    [Tooltip("上一波敌人全部死亡后，是否在下一次大回合开始时生成本波。")]
    public bool spawnWhenPreviousWaveCleared;

    [Header("敌人")]
    [Tooltip("本波敌人组成。")]
    public List<EnemySpawnEntry> enemies = new List<EnemySpawnEntry>();
}

[Serializable]
public class EnemySpawnEntry
{
    [Tooltip("敌人类型配置。")]
    public EnemyDefinitionSO enemyDefinition;

    [Min(1)]
    [Tooltip("生成数量。")]
    public int count = 1;

    [Tooltip("出生点 ID，需要与场景中 EnemySpawnPoint 的 ID 对应。")]
    public string spawnPointId;
}
