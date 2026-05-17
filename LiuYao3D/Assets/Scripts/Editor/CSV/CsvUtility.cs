/// <summary>
/// 实现功能：提供 Editor 导表用 CSV 解析、表头校验与字段读取工具。
/// </summary>
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CsvUtility
{
    public static CsvTable LoadFromAsset(string csvPath, params string[] requiredHeaders)
    {
        TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
        if (csv == null)
        {
            throw new FileNotFoundExceptionForCsv(csvPath);
        }

        CsvTable table = Parse(csv.text);
        RequireHeaders(table.Headers, requiredHeaders);
        return table;
    }

    public static CsvTable Parse(string text)
    {
        List<List<string>> rawRows = ParseRows(text);
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

        if (rawRows.Count == 0)
            return new CsvTable(new List<string>(), rows);

        List<string> headers = rawRows[0];
        for (int i = 0; i < headers.Count; i++)
        {
            headers[i] = headers[i].Trim();
        }

        for (int i = 1; i < rawRows.Count; i++)
        {
            List<string> rawRow = rawRows[i];
            if (IsEmptyRow(rawRow))
                continue;

            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int column = 0; column < headers.Count; column++)
            {
                string value = column < rawRow.Count ? rawRow[column] : string.Empty;
                row[headers[column]] = value;
            }

            rows.Add(row);
        }

        return new CsvTable(headers, rows);
    }

    public static string GetString(Dictionary<string, string> row, string key)
    {
        if (row != null && row.TryGetValue(key, out string value) && value != null)
            return value.Trim();

        return string.Empty;
    }

    public static void RequireHeaders(List<string> headers, params string[] requiredHeaders)
    {
        if (requiredHeaders == null)
            return;

        for (int i = 0; i < requiredHeaders.Length; i++)
        {
            RequireHeader(headers, requiredHeaders[i]);
        }
    }

    private static List<List<string>> ParseRows(string text)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> row = new List<string>();
        StringBuilder field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                row.Add(field.ToString());
                field.Clear();
                continue;
            }

            if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                row.Add(field.ToString());
                field.Clear();
                rows.Add(row);
                row = new List<string>();
                continue;
            }

            field.Append(c);
        }

        row.Add(field.ToString());
        if (!IsEmptyRow(row))
            rows.Add(row);

        return rows;
    }

    private static void RequireHeader(List<string> headers, string required)
    {
        if (string.IsNullOrWhiteSpace(required))
            return;

        for (int i = 0; i < headers.Count; i++)
        {
            if (headers[i] == required)
                return;
        }

        throw new FormatException($"缺少必需表头: {required}");
    }

    private static bool IsEmptyRow(List<string> row)
    {
        if (row == null || row.Count == 0)
            return true;

        for (int i = 0; i < row.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(row[i]))
                return false;
        }

        return true;
    }

    private class FileNotFoundExceptionForCsv : Exception
    {
        public FileNotFoundExceptionForCsv(string path)
            : base($"找不到 CSV 文件: {path}")
        {
        }
    }
}
#endif
