/// <summary>
/// 实现功能：保存 CSV 解析后的表头与行数据，供各类 Editor 导入器复用。
/// </summary>
#if UNITY_EDITOR
using System.Collections.Generic;

public class CsvTable
{
    public CsvTable(List<string> headers, List<Dictionary<string, string>> rows)
    {
        Headers = headers;
        Rows = rows;
    }

    public List<string> Headers { get; }
    public List<Dictionary<string, string>> Rows { get; }
}
#endif
