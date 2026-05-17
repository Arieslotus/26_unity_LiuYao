/// <summary>
/// 实现功能：配置硬币抽取规则，包括候选卡池、抽取数量、是否允许重复，以及是否在开始时自动抽取。
/// </summary>
using System.Collections.Generic;
using System;
using UnityEngine;

public enum CoinInitialSideMode
{
    Random,
    Front,
    Back
}

[Serializable]
public class FixedCoinDrawRule
{
    [Tooltip("内定一定会被抽到的硬币")]
    public CoinDefinition coinDefinition;

    [Tooltip("内定币的初始正反面。随机表示抽到后再随机决定正反面")]
    public CoinInitialSideMode initialSide = CoinInitialSideMode.Random;
}

[CreateAssetMenu(fileName = "CoinDrawConfig_", menuName = "Config/Coin Draw Config")]
public class CoinDrawConfig : ScriptableObject
{
    [Header("卡池配置")]
    [Tooltip("可参与抽取的硬币定义列表")]
    [SerializeField] private List<CoinDefinition> coinPool = new List<CoinDefinition>();

    [Tooltip("内定一定会被抽到的硬币。可不填；填写时建议数量为 1 到 3，且不要超过本局抽取数量")]
    [SerializeField] private List<FixedCoinDrawRule> fixedCoins = new List<FixedCoinDrawRule>();

    [Tooltip("本局需要抽取的硬币数量")]
    [Min(1)]
    [SerializeField] private int drawCount = 3;

    [Tooltip("是否允许抽到重复硬币。当前 4 抽 3 一般应关闭")]
    [SerializeField] private bool allowDuplicate = false;

    [Header("流程配置")]
    [Tooltip("是否在管理器启动时自动执行一次抽取并分配")]
    [SerializeField] private bool autoDrawOnStart = true;

    public List<CoinDefinition> CoinPool => coinPool;
    public List<FixedCoinDrawRule> FixedCoins => fixedCoins;
    public int DrawCount => drawCount;
    public bool AllowDuplicate => allowDuplicate;
    public bool AutoDrawOnStart => autoDrawOnStart;
}
