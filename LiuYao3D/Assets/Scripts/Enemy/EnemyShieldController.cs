/// <summary>
/// 实现功能：管理单个敌人的属性护盾生成、显示、减伤与破盾值累计。
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyShieldController : MonoBehaviour
{
    [Header("生成规则")]
    [Tooltip("每经过多少个大回合尝试生成一次护盾。已有护盾时不会生成新护盾。")]
    [Min(1)]
    [SerializeField] private int shieldIntervalRounds = 2;

    [Tooltip("持有护盾时受到的伤害减免比例。0 表示不减伤，1 表示完全免伤。")]
    [Range(0f, 1f)]
    [SerializeField] private float damageReductionPercent = 0.5f;

    [Tooltip("是否忽略游戏开始时的第一轮 RoundStarted。开启后，护盾只会在敌人行动完成后的新回合开始时计数。")]
    [SerializeField] private bool ignoreFirstRoundStart = true;

    [Tooltip("特殊敌人可单独覆盖破盾规则。一般敌人留空，优先使用 EnemyShieldSystemConfig 的全局配置。")]
    [SerializeField] private EnemyShieldBreakConfigSO shieldBreakConfigOverride;

    [Header("可视化")]
    [Tooltip("不同卦象对应的通用视觉资源配置。护盾会使用其中的 Sprite。")]
    [SerializeField] private TrigramVisualDatabase trigramVisualDatabase;

    [Tooltip("敌人身上用于显示护盾的 Image 组件。")]
    [SerializeField] private Image shieldImage;

    [Tooltip("隐藏护盾时是否禁用 Image 所在物体。关闭则只禁用 Image 组件。")]
    [SerializeField] private bool deactivateShieldImageObjectWhenHidden = false;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private int passedRounds;
    private int currentBreakValue;
    private bool hasShield;
    private TrigramType currentShieldType = TrigramType.None;
    private TurnManager subscribedTurnManager;
    private bool hasLoggedMissingTurnManager;
    private bool hasLoggedMissingBreakConfig;
    private static EnemyShieldBreakConfigSO cachedAutoBreakConfig;

    public bool HasShield => hasShield;
    public TrigramType CurrentShieldType => currentShieldType;
    public int CurrentBreakValue => currentBreakValue;
    public int RequiredBreakValue => GetRequiredBreakValue();
    public float DamageReductionPercent => hasShield ? damageReductionPercent : 0f;

    public int ModifyIncomingDamage(int rawDamage)
    {
        if (rawDamage <= 0)
            return 0;

        if (!hasShield)
            return rawDamage;

        float reduction = Mathf.Clamp01(damageReductionPercent);
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

        GenerateNextShield(TurnManager.Instance != null ? TurnManager.Instance.RoundIndex : 0);
    }

    private void Start()
    {
        CacheShieldImage();
        SubscribeEvents();
        RefreshShieldVisual();
    }

    private void OnEnable()
    {
        CacheShieldImage();
        SubscribeEvents();
        RefreshShieldVisual();
    }

    private void OnDisable()
    {
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
            Debug.Log(
                $"[EnemyShieldController] 收到新回合事件 | enemy:{name} | round:{roundIndex} | " +
                $"hasShield:{hasShield} | passedRounds:{passedRounds}/{shieldIntervalRounds}"
            );
        }

        if (ignoreFirstRoundStart && roundIndex <= 1)
            return;

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

        passedRounds++;

        if (passedRounds < shieldIntervalRounds)
        {
            if (debugLog)
            {
                Debug.Log($"[EnemyShieldController] 护盾计数推进 | enemy:{name} | count:{passedRounds}/{shieldIntervalRounds} | round:{roundIndex}");
            }

            return;
        }

        passedRounds = 0;
        GenerateNextShield(roundIndex);
    }

    private void GenerateNextShield(int roundIndex)
    {
        HashSet<TrigramType> availableShieldTypes = CollectAvailableShieldTypes();
        if (availableShieldTypes.Count == 0)
        {
            Debug.LogWarning($"[EnemyShieldController] 未找到场上或背包硬币的有效正反面属性，无法生成护盾 | enemy:{name} | round:{roundIndex}");
            return;
        }

        if (!TryGetRandomAvailableShieldType(availableShieldTypes, out TrigramType shieldType))
        {
            Debug.LogWarning(
                $"[EnemyShieldController] 当前可用硬币属性集合为空，无法随机生成护盾 | " +
                $"enemy:{name} | round:{roundIndex} | available:{FormatTrigramSet(availableShieldTypes)}"
            );
            return;
        }

        hasShield = true;
        currentShieldType = shieldType;
        currentBreakValue = 0;
        ShowShieldVisual(shieldType);

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyShieldController] 随机生成护盾 | enemy:{name} | shield:{currentShieldType} | " +
                $"available:{FormatTrigramSet(availableShieldTypes)} | round:{roundIndex}"
            );
        }
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

            return result;
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
        hasShield = false;
        currentShieldType = TrigramType.None;
        currentBreakValue = 0;
        HideShieldVisual();

        if (debugLog)
        {
            Debug.Log($"[EnemyShieldController] 护盾破除 | enemy:{name} | trigger:{triggerType} | source:{sourceName}");
        }
    }

    private void ShowShieldVisual(TrigramType shieldType)
    {
        CacheShieldImage();

        if (shieldImage == null)
        {
            Debug.LogWarning($"[EnemyShieldController] 未配置护盾 Image，无法显示护盾 | enemy:{name} | shield:{shieldType}");
            return;
        }

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

        shieldImage.enabled = true;

        if (deactivateShieldImageObjectWhenHidden)
        {
            shieldImage.gameObject.SetActive(true);
        }
    }

    private void HideShieldVisual()
    {
        CacheShieldImage();

        if (shieldImage == null)
            return;

        shieldImage.enabled = false;

        if (deactivateShieldImageObjectWhenHidden)
        {
            shieldImage.gameObject.SetActive(false);
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

    private int GetRequiredBreakValue()
    {
        EnemyShieldBreakConfigSO config = ResolveShieldBreakConfig();
        return config != null ? config.RequiredBreakValue : 3;
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
                $"[EnemyShieldController] 未找到 EnemyShieldBreakConfigSO，将使用代码默认破盾规则 | " +
                $"enemy:{name} | required:3 | same:3 | different:1"
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
