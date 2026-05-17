/// <summary>
/// 实现功能：从 skillTable.csv 导入卦象碰撞技能配置，并生成或更新 TrigramCollisionSkillSO 资源。
/// </summary>
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SkillCSVImporter
{
    private const string CsvPath = "Assets/Data/CSV/skillTable.csv";
    private const string OutputFolder = "Assets/Data/Skills";
    private const string DatabaseFolder = "Assets/Data/SkillDatabase";
    private const string DatabasePath = DatabaseFolder + "/TrigramSkillDatabase.asset";

    [MenuItem("Tools/六爻/导入卦象碰撞技能表", priority = 100)]
    public static void Import()
    {
        CsvTable table;
        try
        {
            table = CsvUtility.LoadFromAsset(
                CsvPath,
                "name",
                "activeTrigram",
                "passiveTrigram",
                "description",
                "skillEffect"
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[SkillCSVImporter] CSV 读取或解析失败: {e.Message}");
            return;
        }

        EditorAssetUtility.EnsureFolder(OutputFolder);

        int created = 0;
        int updated = 0;
        int skipped = 0;
        List<TrigramCollisionSkillSO> importedSkills = new List<TrigramCollisionSkillSO>();

        for (int i = 0; i < table.Rows.Count; i++)
        {
            Dictionary<string, string> row = table.Rows[i];

            string skillName = CsvUtility.GetString(row, "name");
            string activeText = CsvUtility.GetString(row, "activeTrigram");
            string passiveText = CsvUtility.GetString(row, "passiveTrigram");

            if (string.IsNullOrWhiteSpace(skillName))
            {
                skipped++;
                Debug.LogWarning($"[SkillCSVImporter] 第 {i + 2} 行 name 为空，已跳过。");
                continue;
            }

            if (!TryParseTrigram(activeText, out TrigramType activeTrigram))
            {
                skipped++;
                Debug.LogWarning($"[SkillCSVImporter] 第 {i + 2} 行 activeTrigram 无法识别: {activeText}");
                continue;
            }

            if (!TryParseTrigram(passiveText, out TrigramType passiveTrigram))
            {
                skipped++;
                Debug.LogWarning($"[SkillCSVImporter] 第 {i + 2} 行 passiveTrigram 无法识别: {passiveText}");
                continue;
            }

            string assetPath = BuildAssetPath(activeTrigram, passiveTrigram, skillName);
            TrigramCollisionSkillSO skill = AssetDatabase.LoadAssetAtPath<TrigramCollisionSkillSO>(assetPath);
            bool isNew = false;

            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<TrigramCollisionSkillSO>();
                AssetDatabase.CreateAsset(skill, assetPath);
                isNew = true;
            }

            SerializedObject serializedSkill = new SerializedObject(skill);
            SetEnum(serializedSkill, "activeTrigram", activeTrigram);
            SetEnum(serializedSkill, "passiveTrigram", passiveTrigram);
            SetString(serializedSkill, "skillName", skillName);
            SetString(serializedSkill, "description", CsvUtility.GetString(row, "description"));
            SetString(serializedSkill, "effectText", CsvUtility.GetString(row, "skillEffect"));
            serializedSkill.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(skill);
            importedSkills.Add(skill);

            if (isNew)
                created++;
            else
                updated++;
        }

        UpdateSkillDatabase(importedSkills);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SkillCSVImporter] 导入完成 | Created:{created} | Updated:{updated} | Skipped:{skipped} | Output:{OutputFolder}");
    }

    private static string BuildAssetPath(TrigramType activeTrigram, TrigramType passiveTrigram, string skillName)
    {
        string safeName = EditorAssetUtility.MakeFileSafe(skillName);
        return $"{OutputFolder}/{activeTrigram}_{passiveTrigram}_{safeName}.asset";
    }

    private static void UpdateSkillDatabase(List<TrigramCollisionSkillSO> importedSkills)
    {
        EditorAssetUtility.EnsureFolder(DatabaseFolder);

        TrigramSkillDatabase database = AssetDatabase.LoadAssetAtPath<TrigramSkillDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<TrigramSkillDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        SerializedObject serializedDatabase = new SerializedObject(database);
        SerializedProperty skillsProperty = serializedDatabase.FindProperty("skills");
        if (skillsProperty == null || !skillsProperty.isArray)
        {
            Debug.LogError("[SkillCSVImporter] TrigramSkillDatabase 中找不到 skills 列表，无法自动填充数据库。");
            return;
        }

        skillsProperty.arraySize = importedSkills != null ? importedSkills.Count : 0;
        for (int i = 0; i < skillsProperty.arraySize; i++)
        {
            SerializedProperty element = skillsProperty.GetArrayElementAtIndex(i);
            element.objectReferenceValue = importedSkills[i];
        }

        serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[SkillCSVImporter] 找不到字段: {propertyName}");
            return;
        }

        property.stringValue = value ?? string.Empty;
    }

    private static void SetEnum(SerializedObject serializedObject, string propertyName, TrigramType value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[SkillCSVImporter] 找不到字段: {propertyName}");
            return;
        }

        property.enumValueIndex = (int)value;
    }

    private static bool TryParseTrigram(string text, out TrigramType trigram)
    {
        trigram = TrigramType.None;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        switch (text.Trim())
        {
            case "乾":
            case "Qian":
                trigram = TrigramType.Qian;
                return true;
            case "坤":
            case "Kun":
                trigram = TrigramType.Kun;
                return true;
            case "震":
            case "Zhen":
                trigram = TrigramType.Zhen;
                return true;
            case "艮":
            case "Gen":
                trigram = TrigramType.Gen;
                return true;
            case "离":
            case "Li":
                trigram = TrigramType.Li;
                return true;
            case "坎":
            case "Kan":
                trigram = TrigramType.Kan;
                return true;
            case "兑":
            case "Dui":
                trigram = TrigramType.Dui;
                return true;
            case "巽":
            case "Xun":
                trigram = TrigramType.Xun;
                return true;
            default:
                return Enum.TryParse(text.Trim(), ignoreCase: true, out trigram) && trigram != TrigramType.None;
        }
    }

}
#endif
