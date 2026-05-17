using UnityEngine;

[CreateAssetMenu(fileName = "ChargeInputConfig", menuName = "Config/Charge Input Config")]
public class ChargeInputConfig : ScriptableObject
{
    [Header("按下检测")]
    [Tooltip("允许开始蓄力的输入半径")]
    public float inputRadius = 1.5f;

    [Header("阶段1：拖拽距离蓄力")]
    [Tooltip("阶段1最大有效拖拽距离 a。达到该距离后，阶段1蓄力封顶")]
    public float stage1MaxDistance = 2f;

    [Tooltip("阶段1最大蓄力值，范围建议 0~1，例如 0.5 表示最多充到50%")]
    [Range(0f, 1f)]
    public float stage1MaxPower = 0.5f;

    [Tooltip("拖拽距离缩放系数。实际拖拽距离会先乘这个值，再参与蓄力计算。值越小，操作空间越大，越容易精细控制")]
    public float dragDistanceScale = 0.1f;

    [Header("阶段2：按住时间蓄力")]
    [Tooltip("保持达到阶段1上限后，继续按住多久可以从 stage1MaxPower 充到 1")]
    public float maxHoldTime = 1.0f;

    [Header("发射限制")]
    [Tooltip("低于这个力度时，松手不会发射")]
    [Range(0f, 1f)]
    public float minFirePower = 0.05f;
}