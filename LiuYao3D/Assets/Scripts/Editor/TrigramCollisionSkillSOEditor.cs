/// <summary>
/// 实现功能：为碰撞技能资产提供内嵌效果配置的添加、删除、排序与编辑入口。
/// </summary>
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrigramCollisionSkillSO))]
public sealed class TrigramCollisionSkillSOEditor : Editor
{
    private readonly EffectTypeEntry[] effectTypes =
    {
        new EffectTypeEntry("造成伤害", typeof(DealDamageEffectConfig)),
        new EffectTypeEntry("恢复损耗", typeof(ReduceLossEffectConfig)),
        new EffectTypeEntry("增加己方损耗", typeof(AddCoinLossEffectConfig)),
        new EffectTypeEntry("添加增伤", typeof(AddDamageModifierEffectConfig)),
        new EffectTypeEntry("延迟增加损耗", typeof(ScheduleCoinLossEffectConfig)),
        new EffectTypeEntry("添加保护", typeof(GrantCoinProtectionEffectConfig)),
        new EffectTypeEntry("破除敌方护盾", typeof(BreakEnemyShieldEffectConfig)),
        new EffectTypeEntry("创建持续伤害圈", typeof(CreateDamageZoneEffectConfig)),
        new EffectTypeEntry("翻面条件", typeof(CoinFlipConditionEffectConfig)),
        new EffectTypeEntry("直到翻面停止叠增伤", typeof(UntilFlipDamageStackEffectConfig)),
        new EffectTypeEntry("延迟执行结果", typeof(ScheduleCoinOutcomeEffectConfig)),
        new EffectTypeEntry("本回合触发次数限制", typeof(TurnTriggerCountEffectConfig)),
        new EffectTypeEntry("临时碰撞物理修正", typeof(CoinPhysicsModifierEffectConfig)),
        new EffectTypeEntry("停止敌方护盾生成", typeof(BlockEnemyShieldGenerationEffectConfig)),
    };

    private SerializedProperty activeTrigram;
    private SerializedProperty passiveTrigram;
    private SerializedProperty skillName;
    private SerializedProperty skillIcon;
    private SerializedProperty description;
    private SerializedProperty effectText;
    private SerializedProperty inlineEffects;
    private SerializedProperty legacyEffects;

    private void OnEnable()
    {
        activeTrigram = serializedObject.FindProperty("activeTrigram");
        passiveTrigram = serializedObject.FindProperty("passiveTrigram");
        skillName = serializedObject.FindProperty("skillName");
        skillIcon = serializedObject.FindProperty("skillIcon");
        description = serializedObject.FindProperty("description");
        effectText = serializedObject.FindProperty("effectText");
        inlineEffects = serializedObject.FindProperty("inlineEffects");
        legacyEffects = serializedObject.FindProperty("effects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawBasicInfo();
        EditorGUILayout.Space(8f);
        DrawInlineEffects();
        EditorGUILayout.Space(8f);
        DrawLegacyEffects();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBasicInfo()
    {
        EditorGUILayout.PropertyField(activeTrigram);
        EditorGUILayout.PropertyField(passiveTrigram);

        EditorGUILayout.Space(4f);
        EditorGUILayout.PropertyField(skillName);
        EditorGUILayout.PropertyField(skillIcon);
        EditorGUILayout.PropertyField(description);
        EditorGUILayout.PropertyField(effectText);
    }

    private void DrawInlineEffects()
    {
        EditorGUILayout.LabelField("具体效果（内嵌）", EditorStyles.boldLabel);

        if (inlineEffects == null || !inlineEffects.isArray)
        {
            EditorGUILayout.HelpBox("未找到 inlineEffects 字段，无法编辑内嵌效果。", MessageType.Warning);
            return;
        }

        for (int i = 0; i < inlineEffects.arraySize; i++)
        {
            SerializedProperty element = inlineEffects.GetArrayElementAtIndex(i);
            DrawInlineEffectElement(element, i);
        }

        DrawAddEffectMenu();
    }

    private void DrawInlineEffectElement(SerializedProperty element, int index)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                element.isExpanded = EditorGUILayout.Foldout(
                    element.isExpanded,
                    GetEffectTitle(element, index),
                    true);

                GUI.enabled = index > 0;
                if (GUILayout.Button("上移", GUILayout.Width(42f)))
                {
                    inlineEffects.MoveArrayElement(index, index - 1);
                }

                GUI.enabled = index < inlineEffects.arraySize - 1;
                if (GUILayout.Button("下移", GUILayout.Width(42f)))
                {
                    inlineEffects.MoveArrayElement(index, index + 1);
                }

                GUI.enabled = true;
                if (GUILayout.Button("删除", GUILayout.Width(42f)))
                {
                    inlineEffects.DeleteArrayElementAtIndex(index);
                    return;
                }
            }

            if (element.isExpanded)
            {
                EditorGUILayout.PropertyField(element, GUIContent.none, true);
            }
        }
    }

    private void DrawAddEffectMenu()
    {
        if (GUILayout.Button("添加效果"))
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < effectTypes.Length; i++)
            {
                EffectTypeEntry entry = effectTypes[i];
                menu.AddItem(new GUIContent(entry.label), false, () => AddInlineEffect(entry.type));
            }

            menu.ShowAsContext();
        }
    }

    private void AddInlineEffect(Type effectType)
    {
        serializedObject.Update();

        int index = inlineEffects.arraySize;
        inlineEffects.InsertArrayElementAtIndex(index);
        SerializedProperty element = inlineEffects.GetArrayElementAtIndex(index);
        element.managedReferenceValue = Activator.CreateInstance(effectType);
        element.isExpanded = true;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void DrawLegacyEffects()
    {
        EditorGUILayout.LabelField("旧版效果资产（兼容用）", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("新技能建议使用上方内嵌效果。这里仅用于旧资产迁移前兼容。", MessageType.Info);
        EditorGUILayout.PropertyField(legacyEffects, true);
    }

    private string GetEffectTitle(SerializedProperty element, int index)
    {
        object value = element.managedReferenceValue;
        if (value is CollisionSkillEffectConfig config)
            return $"{index + 1}. {config.DisplayName}";

        return $"{index + 1}. 未设置效果";
    }

    private readonly struct EffectTypeEntry
    {
        public readonly string label;
        public readonly Type type;

        public EffectTypeEntry(string label, Type type)
        {
            this.label = label;
            this.type = type;
        }
    }
}
