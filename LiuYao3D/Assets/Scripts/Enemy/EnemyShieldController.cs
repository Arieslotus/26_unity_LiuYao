/// <summary>
/// 实现功能：管理单个敌人的属性护盾生成、显示、减伤、破盾值累计与破裂动画播放。
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyShieldController : MonoBehaviour
{
    [Header("生成规则")]
    [Tooltip("特殊敌人可单独覆盖破盾规则。一般敌人留空，优先使用 EnemyShieldSystemConfig 的全局配置。")]
    [SerializeField] private EnemyShieldBreakConfigSO shieldBreakConfigOverride;

    [Header("可视化")]
    [Tooltip("不同卦象对应的通用视觉资源配置。护盾会使用其中的 Sprite。")]
    [SerializeField] private TrigramVisualDatabase trigramVisualDatabase;

    [Tooltip("敌人身上用于显示护盾的 Image 组件。")]
    [SerializeField] private Image shieldImage;

    [Tooltip("护盾破裂 Animator，建议挂在 ShieldRoot 上。留空时会从护盾 Image 的父级自动查找。")]
    [SerializeField] private Animator shieldBreakAnimator;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private int passedRounds;
    private int currentBreakValue;
    private bool hasShield;
    private TrigramType currentShieldType = TrigramType.None;
    private TurnManager subscribedTurnManager;
    private Coroutine shieldBreakRoutine;
    private Coroutine pendingShieldGenerationRoutine;
    private bool hasCachedShieldVisualState;
    private Vector3 initialShieldLocalPosition;
    private Quaternion initialShieldLocalRotation;
    private Vector3 initialShieldLocalScale;
    private Color initialShieldImageColor;
    private bool hasLoggedMissingTurnManager;
    private bool hasLoggedMissingBreakConfig;
    private static EnemyShieldBreakConfigSO cachedAutoBreakConfig;

    public bool HasShield => hasShield;
    public TrigramType CurrentShieldType => currentShieldType;
    public int CurrentBreakValue => currentBreakValue;
    public int RequiredBreakValue => GetRequiredBreakValue();
    public float DamageReductionPercent => hasShield ? GetDamageReductionPercent() : 0f;

    public int ModifyIncomingDamage(int rawDamage)
    {
        if (rawDamage <= 0)
            return 0;

        if (!hasShield)
            return rawDamage;

        float reduction = GetDamageReductionPercent();
        int finalDamage = Mathf.CeilToInt(rawDamage * (1f - reduction));
        finalDamage = Mathf.Max(0, finalDamage);

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyShieldController] 护盾减伤 | enemy:{name} | shield:{currentShieldType} | " +
                $"raw:{rawDamage} | reduction:{reduction:P0} | final:{finalDamage}"
            );
        }

        return finalDamage;
    }

    public bool TryBreakShield(TrigramType trigram, string sourceName)
    {
        return TryApplyShieldBreak(trigram, sourceName);
    }

    public bool TryApplyShieldBreak(TrigramType trigram, string sourceName)
    {
        if (!hasShield)
            return false;

        int addValue = GetBreakValue(trigram);
        int requiredValue = GetRequiredBreakValue();
        if (addValue <= 0)
        {
            if (debugLog)
            {
                Debug.Log(
                    $"[EnemyShieldController] 破盾值未增加 | enemy:{name} | shield:{currentShieldType} | " +
                    $"trigger:{trigram} | progress:{currentBreakValue}/{requiredValue} | source:{sourceName}"
                );
            }

            return false;
        }

        currentBreakValue = Mathf.Min(requiredValue, currentBreakValue + addValue);

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyShieldController] 破盾值累计 | enemy:{name} | shield:{currentShieldType} | " +
                $"trigger:{trigram} | add:{addValue} | progress:{currentBreakValue}/{requiredValue} | source:{sourceName}"
            );
        }

        if (currentBreakValue < requiredValue)
            return false;

        BreakShield(trigram, sourceName);
        return true;
    }

    public void GenerateInitialShield()
    {
        if (!isActiveAndEnabled || hasShield)
            return;

        int roundIndex = TurnManager.Instance != null ? TurnManager.Instance.RoundIndex : 0;
        if (!TryGenerateNextShield(roundIndex, true))
        {
            ScheduleShieldGenerationRetry(roundIndex);
        }
    }

    private void Start()
    {
        CacheShieldImage();
        CacheInitialShieldVisualState();
        SubscribeEvents();
        RefreshShieldVisual();
    }

    private void OnEnable()
    {
        CacheShieldImage();
        CacheInitialShieldVisualState();
        SubscribeEvents();
        RefreshShieldVisual();
    }

    private void OnDisable()
    {
        StopShieldBreakAnimation();
        StopShieldGenerationRetry();
        UnsubscribeEvents();
    }

    private void Update()
    {
        if (subscribedTurnManager == null && TurnManager.Instance != null)
        {
            SubscribeEvents();
        }
    }

    private void SubscribeEvents()
    {
        CombatSkillEvents.SkillImpactWaveRequested -= OnSkillImpactWaveRequested;
        CombatSkillEvents.SkillImpactWaveRequested += OnSkillImpactWaveRequested;

        if (subscribedTurnManager == TurnManager.Instance)
            return;

        if (subscribedTurnManager != null)
        {
            subscribedTurnManager.RoundStarted -= OnRoundStarted;
        }

        subscribedTurnManager = TurnManager.Instance;
        if (subscribedTurnManager != null)
        {
            subscribedTurnManager.RoundStarted += OnRoundStarted;

            if (debugLog)
            {
                Debug.Log($"[EnemyShieldController] 已订阅回合事件 | enemy:{name} | round:{subscribedTurnManager.RoundIndex}");
            }
        }
        else if (!hasLoggedMissingTurnManager)
        {
            hasLoggedMissingTurnManager = true;
            Debug.LogWarning($"[EnemyShieldController] 暂未找到 TurnManager，将在后续帧重试订阅 | enemy:{name}");
        }
    }

    private void UnsubscribeEvents()
    {
        CombatSkillEvents.SkillImpactWaveRequested -= OnSkillImpactWaveRequested;

        if (subscribedTurnManager != null)
        {
            subscribedTurnManager.RoundStarted -= OnRoundStarted;
            subscribedTurnManager = null;
        }
    }

    private void OnRoundStarted(int roundIndex)
    {
        if (debugLog)
        {
            int intervalRounds = GetShieldIntervalRounds();
            Debug.Log(
                $"[EnemyShieldController] 收到新回合事件 | enemy:{name} | round:{roundIndex} | " +
                $"hasShield:{hasShield} | passedRounds:{passedRounds}/{intervalRounds}"
            );
        }

        if (hasShield)
        {
            if (debugLog)
            {
                Debug.Log($"[EnemyShieldController] 已有护盾，跳过生成计数 | enemy:{name} | shield:{currentShieldType} | round:{roundIndex}");
            }

            return;
        }

        if (CoinRoundEffectManager.Instance != null && CoinRoundEffectManager.Instance.IsEnemyShieldGenerationBlocked())
        {
            if (debugLog)
            {
                Debug.Log($"[EnemyShieldController] 敌方护盾生成被技能阻止 | enemy:{name} | round:{roundIndex}");
            }

            return;
        }

        if (ShouldGenerateShieldOnFirstRound() && roundIndex <= 1)
        {
            passedRounds = 0;
            if (!TryGenerateNextShield(roundIndex, true))
            {
                ScheduleShieldGenerationRetry(roundIndex);
            }
            return;
        }

        passedRounds++;

        int shieldIntervalRounds = GetShieldIntervalRounds();
        if (passedRounds < shieldIntervalRounds)
        {
            if (debugLog)
            {
                Debug.Log($"[EnemyShieldController] 护盾计数推进 | enemy:{name} | count:{passedRounds}/{shieldIntervalRounds} | round:{roundIndex}");
            }

            return;
        }

        passedRounds = 0;
        if (!TryGenerateNextShield(roundIndex, true))
        {
            ScheduleShieldGenerationRetry(roundIndex);
        }
    }

    private bool TryGenerateNextShield(int roundIndex, bool logWarnings)
    {
        HashSet<TrigramType> availableShieldTypes = CollectAvailableShieldTypes();
        if (availableShieldTypes.Count == 0)
        {
            if (logWarnings)
            {
                Debug.LogWarning($"[EnemyShieldController] 未找到场上或背包硬币的有效正反面属性，无法生成护盾 | enemy:{name} | round:{roundIndex}");
            }
            return false;
        }

        if (!TryGetRandomAvailableShieldType(availableShieldTypes, out TrigramType shieldType))
        {
            if (logWarnings)
            {
                Debug.LogWarning(
                    $"[EnemyShieldController] 当前可用硬币属性集合为空，无法随机生成护盾 | " +
                    $"enemy:{name} | round:{roundIndex} | available:{FormatTrigramSet(availableShieldTypes)}"
                );
            }
            return false;
        }

        hasShield = true;
        currentShieldType = shieldType;
        currentBreakValue = 0;
        StopShieldBreakAnimation();
        ResetShieldVisualState();
        ShowShieldVisual(shieldType);

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyShieldController] 随机生成护盾 | enemy:{name} | shield:{currentShieldType} | " +
                $"available:{FormatTrigramSet(availableShieldTypes)} | round:{roundIndex}"
            );
        }

        return true;
    }

    private bool TryGetRandomAvailableShieldType(HashSet<TrigramType> availableShieldTypes, out TrigramType shieldType)
    {
        shieldType = TrigramType.None;

        if (availableShieldTypes == null || availableShieldTypes.Count == 0)
            return false;

        int randomIndex = Random.Range(0, availableShieldTypes.Count);
        int index = 0;
        foreach (TrigramType candidate in availableShieldTypes)
        {
            if (index == randomIndex)
            {
                shieldType = candidate;
                return shieldType != TrigramType.None;
            }

            index++;
        }

        return false;
    }

    private HashSet<TrigramType> CollectAvailableShieldTypes()
    {
        HashSet<TrigramType> result = new HashSet<TrigramType>();

        CoinRosterManager roster = CoinRosterManager.Instance;
        if (roster != null)
        {
            IReadOnlyList<ChessPiece> slots = roster.CoinSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                AddFieldCoinTrigrams(result, slots[i]);
            }

            IReadOnlyList<CoinDefinition> inventory = roster.InventoryCoins;
            for (int i = 0; i < inventory.Count; i++)
            {
                AddDefinitionTrigrams(result, inventory[i]);
            }

            if (result.Count > 0)
            {
                return result;
            }

            if (debugLog)
            {
                Debug.LogWarning($"[EnemyShieldController] CoinRosterManager 暂无可用硬币属性，改用场景 ChessPiece 回退收集 | enemy:{name}");
            }
        }

        ChessPiece[] pieces = FindObjectsOfType<ChessPiece>();
        for (int i = 0; i < pieces.Length; i++)
        {
            AddFieldCoinTrigrams(result, pieces[i]);
        }

        return result;
    }

    private static void AddFieldCoinTrigrams(HashSet<TrigramType> result, ChessPiece piece)
    {
        if (result == null || piece == null)
            return;

        CoinStats stats = piece.GetComponent<CoinStats>();
        if (stats != null && (stats.IsBroken || stats.IsPendingBreak))
            return;

        CoinDefinition definition = piece.CoinDefinition;
        if (definition == null && stats != null)
        {
            definition = stats.AppliedDefinition;
        }

        AddDefinitionTrigrams(result, definition);
    }

    private static void AddDefinitionTrigrams(HashSet<TrigramType> result, CoinDefinition definition)
    {
        if (result == null || definition == null)
            return;

        AddTrigram(result, definition.frontTrigram);
        AddTrigram(result, definition.backTrigram);
    }

    private static void AddTrigram(HashSet<TrigramType> result, TrigramType trigram)
    {
        if (result == null || trigram == TrigramType.None)
            return;

        result.Add(trigram);
    }

    private static string FormatTrigramSet(HashSet<TrigramType> trigrams)
    {
        if (trigrams == null || trigrams.Count == 0)
            return "空";

        return string.Join(",", trigrams);
    }

    private void OnSkillImpactWaveRequested(TrigramType waveType)
    {
        if (!hasShield)
            return;

        TryApplyShieldBreak(waveType, "技能冲击波");
    }

    private void BreakShield(TrigramType triggerType, string sourceName)
    {
        TrigramType brokenShieldType = currentShieldType;
        hasShield = false;
        currentShieldType = TrigramType.None;
        currentBreakValue = 0;
        PlayShieldBreakAnimationOrHide(brokenShieldType);

        if (debugLog)
        {
            Debug.Log($"[EnemyShieldController] 护盾破除 | enemy:{name} | shield:{brokenShieldType} | trigger:{triggerType} | source:{sourceName}");
        }
    }

    private void ScheduleShieldGenerationRetry(int roundIndex)
    {
        if (!isActiveAndEnabled || hasShield || pendingShieldGenerationRoutine != null)
            return;

        pendingShieldGenerationRoutine = StartCoroutine(RetryShieldGenerationRoutine(roundIndex));
    }

    private IEnumerator RetryShieldGenerationRoutine(int roundIndex)
    {
        const int maxRetryFrames = 5;

        for (int i = 0; i < maxRetryFrames; i++)
        {
            yield return null;

            if (!isActiveAndEnabled || hasShield)
                break;

            if (TryGenerateNextShield(roundIndex, i == maxRetryFrames - 1))
            {
                break;
            }
        }

        pendingShieldGenerationRoutine = null;
    }

    private void StopShieldGenerationRetry()
    {
        if (pendingShieldGenerationRoutine == null)
            return;

        StopCoroutine(pendingShieldGenerationRoutine);
        pendingShieldGenerationRoutine = null;
    }

    private void ShowShieldVisual(TrigramType shieldType)
    {
        CacheShieldImage();

        if (shieldImage == null)
        {
            Debug.LogWarning($"[EnemyShieldController] 未配置护盾 Image，无法显示护盾 | enemy:{name} | shield:{shieldType}");
            return;
        }

        shieldImage.gameObject.SetActive(true);
        SetShieldAnimatorEnabled(false);

        Sprite shieldSprite = trigramVisualDatabase != null
            ? trigramVisualDatabase.GetSprite(shieldType)
            : null;

        if (shieldSprite == null)
        {
            Debug.LogWarning($"[EnemyShieldController] 未找到护盾 Sprite，将保留 Image 原图 | enemy:{name} | shield:{shieldType}");
        }
        else
        {
            shieldImage.sprite = shieldSprite;
        }

        ResetShieldVisualState();
        shieldImage.enabled = true;
    }

    private void HideShieldVisual()
    {
        CacheShieldImage();

        if (shieldImage == null)
            return;

        shieldImage.enabled = false;
        SetShieldAnimatorEnabled(false);
    }

    private void PlayShieldBreakAnimationOrHide(TrigramType shieldType)
    {
        string triggerName = GetShieldBreakTriggerName(shieldType);

        CacheShieldAnimator();

        if (string.IsNullOrEmpty(triggerName) || shieldBreakAnimator == null)
        {
            HideShieldVisual();
            return;
        }

        StopShieldBreakAnimation();
        shieldBreakRoutine = StartCoroutine(PlayShieldBreakAnimationRoutine(shieldType, triggerName));
    }

    private IEnumerator PlayShieldBreakAnimationRoutine(TrigramType shieldType, string triggerName)
    {
        CacheShieldImage();
        CacheShieldAnimator();

        if (shieldImage == null || shieldBreakAnimator == null || string.IsNullOrEmpty(triggerName))
        {
            HideShieldVisual();
            yield break;
        }

        shieldImage.gameObject.SetActive(true);

        shieldImage.enabled = true;

        if (shieldBreakAnimator != null && !shieldBreakAnimator.gameObject.activeSelf)
        {
            shieldBreakAnimator.gameObject.SetActive(true);
        }

        SetShieldAnimatorEnabled(true);

        if (shieldBreakAnimator.isActiveAndEnabled && shieldBreakAnimator.gameObject.activeInHierarchy)
        {
            shieldBreakAnimator.Rebind();
            shieldBreakAnimator.Update(0f);
        }

        int previousStateHash = 0;
        if (shieldBreakAnimator.isActiveAndEnabled && shieldBreakAnimator.gameObject.activeInHierarchy)
        {
            previousStateHash = shieldBreakAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        }

        shieldBreakAnimator.SetTrigger(triggerName);

        if (debugLog)
        {
            Debug.Log($"[EnemyShieldController] 播放护盾破裂动画 | enemy:{name} | shield:{shieldType} | trigger:{triggerName}");
        }

        yield return WaitShieldBreakAnimatorComplete(previousStateHash);

        HideShieldVisual();
        shieldBreakRoutine = null;
    }

    private void StopShieldBreakAnimation()
    {
        if (shieldBreakRoutine != null)
        {
            StopCoroutine(shieldBreakRoutine);
            shieldBreakRoutine = null;
        }

        CacheShieldAnimator();

        if (shieldBreakAnimator != null &&
            shieldBreakAnimator.isActiveAndEnabled &&
            shieldBreakAnimator.gameObject.activeInHierarchy)
        {
            shieldBreakAnimator.Rebind();
            shieldBreakAnimator.Update(0f);
        }

        SetShieldAnimatorEnabled(false);
    }

    private IEnumerator WaitShieldBreakAnimatorComplete(int previousStateHash)
    {
        if (shieldBreakAnimator == null)
            yield break;

        bool enteredBreakState = false;
        float elapsed = 0f;
        const float maxWaitSeconds = 5f;

        while (shieldBreakAnimator != null &&
               shieldBreakAnimator.isActiveAndEnabled &&
               shieldBreakAnimator.gameObject.activeInHierarchy)
        {
            AnimatorStateInfo state = shieldBreakAnimator.GetCurrentAnimatorStateInfo(0);
            bool inTransition = shieldBreakAnimator.IsInTransition(0);

            if (!enteredBreakState)
            {
                enteredBreakState = inTransition || state.fullPathHash != previousStateHash;
            }
            else if (!inTransition)
            {
                bool returnedToPreviousState = previousStateHash != 0 && state.fullPathHash == previousStateHash;
                bool completedNonLoopState = !state.loop && state.normalizedTime >= 1f;
                if (returnedToPreviousState || completedNonLoopState)
                    yield break;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= maxWaitSeconds)
            {
                if (debugLog)
                {
                    Debug.LogWarning($"[EnemyShieldController] 等待护盾破裂 Animator 结束超时，将隐藏护盾 | enemy:{name}");
                }

                yield break;
            }

            yield return null;
        }
    }

    private static string GetShieldBreakTriggerName(TrigramType shieldType)
    {
        switch (shieldType)
        {
            case TrigramType.Qian:
                return "tian";
            case TrigramType.Kun:
                return "di";
            case TrigramType.Xun:
                return "feng";
            case TrigramType.Zhen:
                return "lei";
            case TrigramType.Kan:
                return "shui";
            case TrigramType.Li:
                return "huo";
            case TrigramType.Gen:
                return "shan";
            case TrigramType.Dui:
                return "ze";
            default:
                return null;
        }
    }

    private void RefreshShieldVisual()
    {
        if (hasShield && currentShieldType != TrigramType.None)
        {
            ShowShieldVisual(currentShieldType);
            return;
        }

        HideShieldVisual();
    }

    private void CacheShieldImage()
    {
        if (shieldImage != null)
            return;

        shieldImage = GetComponentInChildren<Image>(true);
    }

    private void CacheInitialShieldVisualState()
    {
        if (hasCachedShieldVisualState)
            return;

        CacheShieldImage();
        CacheShieldAnimator();

        Transform target = shieldBreakAnimator != null
            ? shieldBreakAnimator.transform
            : (shieldImage != null ? shieldImage.transform : null);

        if (target == null)
            return;

        initialShieldLocalPosition = target.localPosition;
        initialShieldLocalRotation = target.localRotation;
        initialShieldLocalScale = target.localScale;
        initialShieldImageColor = shieldImage != null ? shieldImage.color : Color.white;
        hasCachedShieldVisualState = true;
    }

    private void ResetShieldVisualState()
    {
        CacheInitialShieldVisualState();

        if (!hasCachedShieldVisualState)
            return;

        Transform target = shieldBreakAnimator != null
            ? shieldBreakAnimator.transform
            : (shieldImage != null ? shieldImage.transform : null);

        if (target != null)
        {
            target.localPosition = initialShieldLocalPosition;
            target.localRotation = initialShieldLocalRotation;
            target.localScale = initialShieldLocalScale;
        }

        if (shieldImage != null)
        {
            shieldImage.color = initialShieldImageColor;
        }
    }

    private void CacheShieldAnimator()
    {
        if (shieldBreakAnimator != null)
            return;

        if (shieldImage != null)
        {
            shieldBreakAnimator = shieldImage.GetComponentInParent<Animator>(true);
        }

        if (shieldBreakAnimator == null)
        {
            shieldBreakAnimator = GetComponentInChildren<Animator>(true);
        }
    }

    private void SetShieldAnimatorEnabled(bool enabled)
    {
        CacheShieldAnimator();

        if (shieldBreakAnimator == null || shieldBreakAnimator.enabled == enabled)
            return;

        shieldBreakAnimator.enabled = enabled;
    }

    private int GetRequiredBreakValue()
    {
        EnemyShieldBreakConfigSO config = ResolveShieldBreakConfig();
        return config != null ? config.RequiredBreakValue : 3;
    }

    private int GetShieldIntervalRounds()
    {
        EnemyShieldBreakConfigSO config = ResolveShieldBreakConfig();
        return config != null ? config.ShieldIntervalRounds : 2;
    }

    private float GetDamageReductionPercent()
    {
        EnemyShieldBreakConfigSO config = ResolveShieldBreakConfig();
        return config != null ? config.DamageReductionPercent : 0.5f;
    }

    private bool ShouldGenerateShieldOnFirstRound()
    {
        EnemyShieldBreakConfigSO config = ResolveShieldBreakConfig();
        return config != null ? config.GenerateShieldOnFirstRound : true;
    }

    private int GetBreakValue(TrigramType trigram)
    {
        EnemyShieldBreakConfigSO config = ResolveShieldBreakConfig();
        if (config != null)
            return config.GetBreakValue(currentShieldType, trigram);

        if (currentShieldType == TrigramType.None || trigram == TrigramType.None)
            return 0;

        return trigram == currentShieldType ? 3 : 1;
    }

    private EnemyShieldBreakConfigSO ResolveShieldBreakConfig()
    {
        if (shieldBreakConfigOverride != null)
            return shieldBreakConfigOverride;

        if (EnemyShieldSystemConfig.Instance != null && EnemyShieldSystemConfig.Instance.ShieldBreakConfig != null)
            return EnemyShieldSystemConfig.Instance.ShieldBreakConfig;

        EnemyShieldBreakConfigSO autoConfig = FindAutoBreakConfig();
        if (autoConfig != null)
            return autoConfig;

        if (!hasLoggedMissingBreakConfig)
        {
            hasLoggedMissingBreakConfig = true;
            Debug.LogWarning(
                $"[EnemyShieldController] 未找到 EnemyShieldBreakConfigSO，将使用代码默认护盾规则 | " +
                $"enemy:{name} | interval:2 | reduction:50% | generateFirstRound:true | required:3 | same:3 | different:1"
            );
        }

        return null;
    }

    private static EnemyShieldBreakConfigSO FindAutoBreakConfig()
    {
        if (cachedAutoBreakConfig != null)
            return cachedAutoBreakConfig;

        EnemyShieldBreakConfigSO[] resourceConfigs = Resources.LoadAll<EnemyShieldBreakConfigSO>(string.Empty);
        if (resourceConfigs != null && resourceConfigs.Length > 0)
        {
            cachedAutoBreakConfig = resourceConfigs[0];
            if (resourceConfigs.Length > 1)
            {
                Debug.LogWarning("[EnemyShieldController] Resources 中找到多个 EnemyShieldBreakConfigSO，默认使用第一个。");
            }

            return cachedAutoBreakConfig;
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:EnemyShieldBreakConfigSO");
        if (guids != null && guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            cachedAutoBreakConfig = AssetDatabase.LoadAssetAtPath<EnemyShieldBreakConfigSO>(assetPath);

            if (guids.Length > 1)
            {
                Debug.LogWarning($"[EnemyShieldController] 项目中找到多个 EnemyShieldBreakConfigSO，默认使用第一个 | path:{assetPath}");
            }

            return cachedAutoBreakConfig;
        }
#endif

        return null;
    }
}
