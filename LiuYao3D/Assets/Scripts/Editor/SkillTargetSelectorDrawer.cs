/// <summary>
/// 实现功能：为技能目标选择器提供动态 Inspector，仅在需要时显示数量、范围和指定卦象列表。
/// </summary>
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnemySkillTargetSelector))]
public sealed class EnemySkillTargetSelectorDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty targetType = property.FindPropertyRelative("targetType");
        EnemySkillTargetType type = (EnemySkillTargetType)targetType.enumValueIndex;

        int lines = 1;
        if (type == EnemySkillTargetType.EnemiesInCollisionRadius)
        {
            lines++;
        }

        if (type == EnemySkillTargetType.RandomEnemies)
        {
            lines++;
        }

        return GetLinesHeight(lines);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect line = GetLine(position, 0);
        SerializedProperty targetType = property.FindPropertyRelative("targetType");
        EditorGUI.PropertyField(line, targetType, new GUIContent(label.text));

        EnemySkillTargetType type = (EnemySkillTargetType)targetType.enumValueIndex;
        int lineIndex = 1;

        if (type == EnemySkillTargetType.EnemiesInCollisionRadius)
        {
            EditorGUI.PropertyField(
                GetLine(position, lineIndex++),
                property.FindPropertyRelative("radius"),
                new GUIContent("碰撞范围半径"));
        }

        if (type == EnemySkillTargetType.RandomEnemies)
        {
            EditorGUI.PropertyField(
                GetLine(position, lineIndex),
                property.FindPropertyRelative("targetCount"),
                new GUIContent("目标数量"));
        }

        EditorGUI.EndProperty();
    }

    private static float GetLinesHeight(int lines)
    {
        return lines * EditorGUIUtility.singleLineHeight + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
    }

    private static Rect GetLine(Rect position, int lineIndex)
    {
        return new Rect(
            position.x,
            position.y + lineIndex * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing),
            position.width,
            EditorGUIUtility.singleLineHeight);
    }
}

[CustomPropertyDrawer(typeof(CoinSkillTargetSelector))]
public sealed class CoinSkillTargetSelectorDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty targetType = property.FindPropertyRelative("targetType");
        CoinSkillTargetType type = (CoinSkillTargetType)targetType.enumValueIndex;

        float height = EditorGUIUtility.singleLineHeight;

        if (CoinSkillTargetSelector.NeedsCount(type))
        {
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        if (ShouldShowSortByActionOrder(type))
        {
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        if (CoinSkillTargetSelector.UsesCurrentTrigramFilter(type))
        {
            SerializedProperty filter = property.FindPropertyRelative("currentTrigramFilter");
            height += EditorGUI.GetPropertyHeight(filter, true) + EditorGUIUtility.standardVerticalSpacing;
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty targetType = property.FindPropertyRelative("targetType");
        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(line, targetType, new GUIContent(label.text));

        CoinSkillTargetType type = (CoinSkillTargetType)targetType.enumValueIndex;
        float y = line.yMax + EditorGUIUtility.standardVerticalSpacing;

        if (CoinSkillTargetSelector.NeedsCount(type))
        {
            line = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(
                line,
                property.FindPropertyRelative("targetCount"),
                new GUIContent("目标数量"));
            y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
        }

        if (ShouldShowSortByActionOrder(type))
        {
            line = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(
                line,
                property.FindPropertyRelative("sortByActionOrder"),
                new GUIContent("按行动顺序"));
            y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
        }

        if (CoinSkillTargetSelector.UsesCurrentTrigramFilter(type))
        {
            SerializedProperty filter = property.FindPropertyRelative("currentTrigramFilter");
            float filterHeight = EditorGUI.GetPropertyHeight(filter, true);
            line = new Rect(position.x, y, position.width, filterHeight);
            EditorGUI.PropertyField(line, filter, new GUIContent("指定当前卦象"), true);
        }

        EditorGUI.EndProperty();
    }

    private static bool ShouldShowSortByActionOrder(CoinSkillTargetType type)
    {
        return type == CoinSkillTargetType.AllAllies ||
            type == CoinSkillTargetType.AlliesWithCurrentTrigrams ||
            type == CoinSkillTargetType.HighestLossAlly;
    }
}
