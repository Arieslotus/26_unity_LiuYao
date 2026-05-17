/// <summary>
/// 实现功能：提供 Editor 导入器常用的资源目录创建与安全文件名处理工具。
/// </summary>
#if UNITY_EDITOR
using System.IO;
using UnityEditor;

public static class EditorAssetUtility
{
    public static void EnsureFolder(string assetFolderPath)
    {
        assetFolderPath = assetFolderPath.TrimEnd('/').Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(assetFolderPath))
            return;

        string[] parts = assetFolderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    public static string MakeFileSafe(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Unnamed";

        string result = text.Trim();
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(invalidChar, '_');
        }

        return result.Replace(" ", "_");
    }
}
#endif
