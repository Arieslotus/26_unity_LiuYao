/// <summary>
/// 实现功能：配置并格式化跨回合 Buff 显示文本，供全局 Buff 面板根据运行时效果快照生成描述。
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

[CreateAssetMenu(fileName = "BuffDisplayTextConfig", menuName = "Config/Buff Display Text Config")]
public sealed class BuffDisplayTextConfig : ScriptableObject
{
    [Header("运行时效果模板")]
    [TextArea]
    [SerializeField] private string damageModifierTemplate = "攻击力增加 {AddDamagePercent}%，剩余 {RemainingRounds} 回合，层数 {StackCount}";
    [TextArea]
    [SerializeField] private string enemyShieldGenerationBlockTemplate = "停止敌方护盾生成，剩余 {RemainingRounds} 回合";
    [TextArea]
    [SerializeField] private string pendingCoinLossTemplate = "{RemainingRounds} 回合后，{Target} 增加 {Loss} 点损耗";
    [TextArea]
    [SerializeField] private string flipConditionNoFlipTemplate = "控制 {Target} {RemainingRounds} 回合不翻面后获得：{SuccessOutcome}";
    [TextArea]
    [SerializeField] private string flipConditionFlipTemplate = "控制 {Target} {RemainingRounds} 回合翻面后获得：{SuccessOutcome}";
    [TextArea]
    [SerializeField] private string untilFlipDamageStackTemplate = "控制 {Target} 不翻面，每回合攻击力增加 {AddDamagePercent}%，剩余可叠 {RemainingRounds} 次，当前 {StackCount} 层";
    [TextArea]
    [SerializeField] private string scheduledOutcomeTemplate = "延迟执行：{OutcomeText}，剩余 {RemainingRounds} 回合";

    [Header("结果模板")]
    [TextArea]
    [SerializeField] private string noneOutcomeTemplate = "无效果";
    [TextArea]
    [SerializeField] private string addLossOutcomeTemplate = "{Target} 增加 {Loss} 点损耗";
    [TextArea]
    [SerializeField] private string reduceLossOutcomeTemplate = "{Target} 恢复 {ReduceLoss} 点损耗";
    [TextArea]
    [SerializeField] private string addDamageModifierOutcomeTemplate = "{Target} 攻击力增加 {AddDamagePercent}%，持续 {DurationRoundsText}";

    [Header("通用文本")]
    [SerializeField] private string unknownSkillText = "未知技能";
    [SerializeField] private string globalTargetText = "全局";
    [SerializeField] private string targetSeparator = "、";
    [SerializeField] private string permanentDurationText = "永久";
    [SerializeField] private string roundSuffix = " 回合";

    public bool IsDisplayable(CoinSkillRuntimeEffectSnapshot snapshot)
    {
        return snapshot.kind == CoinSkillRuntimeEffectKind.DamageModifier ||
            snapshot.kind == CoinSkillRuntimeEffectKind.EnemyShieldGenerationBlock ||
            snapshot.kind == CoinSkillRuntimeEffectKind.PendingCoinLoss ||
            snapshot.kind == CoinSkillRuntimeEffectKind.FlipCondition ||
            snapshot.kind == CoinSkillRuntimeEffectKind.UntilFlipDamageStack ||
            snapshot.kind == CoinSkillRuntimeEffectKind.ScheduledOutcome;
    }

    public string Format(CoinSkillRuntimeEffectSnapshot snapshot)
    {
        return ReplaceCommonPlaceholders(GetTemplate(snapshot), snapshot);
    }

    public string FormatOutcome(CoinSkillOutcomeConfig outcome, IReadOnlyList<CoinStats> targets)
    {
        if (outcome == null)
            return noneOutcomeTemplate;

        return ReplaceOutcomePlaceholders(GetOutcomeTemplate(outcome.OutcomeType), outcome, targets);
    }

    private string GetTemplate(CoinSkillRuntimeEffectSnapshot snapshot)
    {
        switch (snapshot.kind)
        {
            case CoinSkillRuntimeEffectKind.DamageModifier:
                return damageModifierTemplate;
            case CoinSkillRuntimeEffectKind.EnemyShieldGenerationBlock:
                return enemyShieldGenerationBlockTemplate;
            case CoinSkillRuntimeEffectKind.PendingCoinLoss:
                return pendingCoinLossTemplate;
            case CoinSkillRuntimeEffectKind.FlipCondition:
                return snapshot.requireNoFlip ? flipConditionNoFlipTemplate : flipConditionFlipTemplate;
            case CoinSkillRuntimeEffectKind.UntilFlipDamageStack:
                return untilFlipDamageStackTemplate;
            case CoinSkillRuntimeEffectKind.ScheduledOutcome:
                return scheduledOutcomeTemplate;
            default:
                return "{SourceSkillName}";
        }
    }

    private string GetOutcomeTemplate(CoinSkillOutcomeType outcomeType)
    {
        switch (outcomeType)
        {
            case CoinSkillOutcomeType.AddLoss:
                return addLossOutcomeTemplate;
            case CoinSkillOutcomeType.ReduceLoss:
                return reduceLossOutcomeTemplate;
            case CoinSkillOutcomeType.AddDamageModifier:
                return addDamageModifierOutcomeTemplate;
            default:
                return noneOutcomeTemplate;
        }
    }

    private string ReplaceCommonPlaceholders(string template, CoinSkillRuntimeEffectSnapshot snapshot)
    {
        string result = string.IsNullOrEmpty(template) ? string.Empty : template;
        result = result.Replace("{SourceSkillName}", GetSourceSkillName(snapshot));
        result = result.Replace("{Target}", FormatTargets(snapshot.targets, snapshot.target));
        result = result.Replace("{RemainingRounds}", Mathf.Max(0, snapshot.remainingRounds).ToString());
        result = result.Replace("{StackCount}", Mathf.Max(1, snapshot.stackCount).ToString());
        result = result.Replace("{AddDamagePercent}", FormatPercent(snapshot.addDamagePercent));
        result = result.Replace("{Loss}", Mathf.Max(0, snapshot.loss).ToString());
        result = result.Replace("{SuccessOutcome}", FormatOutcome(snapshot.successOutcome, ResolveOutcomeTargets(snapshot.successOutcome, snapshot)));
        result = result.Replace("{FailureOutcome}", FormatOutcome(snapshot.failureOutcome, ResolveOutcomeTargets(snapshot.failureOutcome, snapshot)));
        result = result.Replace("{OutcomeText}", FormatOutcome(snapshot.outcome, ResolveOutcomeTargets(snapshot.outcome, snapshot)));
        result = result.Replace("{OutcomeType}", snapshot.outcome != null ? snapshot.outcome.OutcomeType.ToString() : CoinSkillOutcomeType.None.ToString());
        return result;
    }

    private string ReplaceOutcomePlaceholders(string template, CoinSkillOutcomeConfig outcome, IReadOnlyList<CoinStats> targets)
    {
        string result = string.IsNullOrEmpty(template) ? string.Empty : template;
        result = result.Replace("{Target}", FormatTargets(targets, null));
        result = result.Replace("{Loss}", Mathf.Max(0, outcome.Loss).ToString());
        result = result.Replace("{ReduceLoss}", Mathf.Max(0, outcome.ReduceLoss).ToString());
        result = result.Replace("{AddDamagePercent}", FormatPercent(outcome.AddDamagePercent));
        result = result.Replace("{DurationRounds}", outcome.DurationRounds.ToString());
        result = result.Replace("{DurationRoundsText}", FormatDuration(outcome.DurationRounds));
        result = result.Replace("{ActivateAfterRounds}", Mathf.Max(0, outcome.ActivateAfterRounds).ToString());
        result = result.Replace("{OutcomeType}", outcome.OutcomeType.ToString());
        return result;
    }

    private IReadOnlyList<CoinStats> ResolveOutcomeTargets(CoinSkillOutcomeConfig outcome, CoinSkillRuntimeEffectSnapshot snapshot)
    {
        if (outcome == null || snapshot.context == null)
            return snapshot.targets;

        return outcome.ResolveTargets(snapshot.context);
    }

    private string GetSourceSkillName(CoinSkillRuntimeEffectSnapshot snapshot)
    {
        if (snapshot.sourceSkill != null && !string.IsNullOrWhiteSpace(snapshot.sourceSkill.SkillName))
            return snapshot.sourceSkill.SkillName;

        return string.IsNullOrWhiteSpace(snapshot.sourceId) ? unknownSkillText : snapshot.sourceId;
    }

    private string FormatTargets(IReadOnlyList<CoinStats> targets, UnityObject fallbackTarget)
    {
        if (targets != null && targets.Count > 0)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                    continue;

                names.Add(GetCoinDisplayName(targets[i]));
            }

            if (names.Count > 0)
                return string.Join(targetSeparator, names);
        }

        if (fallbackTarget != null)
            return GetCoinDisplayName(fallbackTarget);

        return globalTargetText;
    }

    private string GetCoinDisplayName(UnityObject target)
    {
        if (target == null)
            return globalTargetText;

        CoinDefinition definition = null;

        CoinStats stats = target as CoinStats;
        if (stats != null)
        {
            definition = stats.AppliedDefinition;
            if (definition == null)
            {
                ChessPiece statsPiece = stats.GetComponent<ChessPiece>();
                definition = statsPiece != null ? statsPiece.CoinDefinition : null;
            }
        }

        ChessPiece piece = target as ChessPiece;
        if (definition == null && piece != null)
        {
            definition = piece.CoinDefinition;
        }

        GameObject gameObject = target as GameObject;
        if (definition == null && gameObject != null)
        {
            ChessPiece gameObjectPiece = gameObject.GetComponent<ChessPiece>();
            definition = gameObjectPiece != null ? gameObjectPiece.CoinDefinition : null;

            if (definition == null)
            {
                CoinStats gameObjectStats = gameObject.GetComponent<CoinStats>();
                definition = gameObjectStats != null ? gameObjectStats.AppliedDefinition : null;
            }
        }

        Component component = target as Component;
        if (definition == null && component != null)
        {
            ChessPiece componentPiece = component.GetComponent<ChessPiece>();
            definition = componentPiece != null ? componentPiece.CoinDefinition : null;

            if (definition == null)
            {
                CoinStats componentStats = component.GetComponent<CoinStats>();
                definition = componentStats != null ? componentStats.AppliedDefinition : null;
            }
        }

        if (definition != null && !string.IsNullOrWhiteSpace(definition.coinName))
            return definition.coinName;

        return target.name;
    }

    private string FormatDuration(int durationRounds)
    {
        if (durationRounds < 0)
            return permanentDurationText;

        return Mathf.Max(0, durationRounds) + roundSuffix;
    }

    private static string FormatPercent(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.#");
    }
}
