/// <summary>
/// 实现功能：定义开局流程对外可见的五个大阶段，便于流程编排、调试和后续 UI/动画接入。
/// </summary>
public enum OpeningState
{
    None,
    IntroCinematic,
    DrawingCoins,
    SelectingCoins,
    OutroCinematic,
    Finished
}
