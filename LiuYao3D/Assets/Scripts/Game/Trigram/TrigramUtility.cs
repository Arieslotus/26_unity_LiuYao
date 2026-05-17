/// <summary>
/// 实现功能：提供卦象固定规则查询，包括八卦对应五行属性。
/// </summary>
public static class TrigramUtility
{
    public static FiveElementType GetElement(TrigramType trigram)
    {
        return trigram switch
        {
            TrigramType.Qian => FiveElementType.Metal, // 乾 金
            TrigramType.Zhen => FiveElementType.Wood,  // 震 木
            TrigramType.Dui => FiveElementType.Metal,  // 兑 金
            TrigramType.Gen => FiveElementType.Earth,  // 艮 土
            TrigramType.Xun => FiveElementType.Wood,   // 巽 木
            TrigramType.Kun => FiveElementType.Earth,  // 坤 土
            TrigramType.Kan => FiveElementType.Water,  // 坎 水
            TrigramType.Li => FiveElementType.Fire,    // 离 火
            _ => FiveElementType.None
        };
    }
}
