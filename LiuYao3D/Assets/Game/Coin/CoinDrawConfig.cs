/// <summary>
/// 实现功能：配置硬币抽取规则，包括候选卡池、抽取数量、是否允许重复，以及是否在开始时自动抽取。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CoinDrawConfig_", menuName = "Config/Coin Draw Config")]
public class CoinDrawConfig : ScriptableObject
{
    [Header("卡池配置")]
    [Tooltip("可参与抽取的硬币定义列表")]
    [SerializeField] private List<CoinDefinition> coinPool = new List<CoinDefinition>();

    [Tooltip("本局需要抽取的硬币数量")]
    [Min(1)]
    [SerializeField] private int drawCount = 3;

    [Tooltip("是否允许抽到重复硬币。当前 4 抽 3 一般应关闭")]
    [SerializeField] private bool allowDuplicate = false;

    [Header("流程配置")]
    [Tooltip("是否在管理器启动时自动执行一次抽取并分配")]
    [SerializeField] private bool autoDrawOnStart = true;

    public List<CoinDefinition> CoinPool => coinPool;
    public int DrawCount => drawCount;
    public bool AllowDuplicate => allowDuplicate;
    public bool AutoDrawOnStart => autoDrawOnStart;
}