/// <summary>
/// 实现功能：管理单个敌人的属性护盾生成、显示与被技能冲击波破除。
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [Tooltip("护盾属性循环列表。每次成功生成护盾时按顺序取下一个属性。")]
    [SerializeField] private List<TrigramType> shieldCycle = new List<TrigramType>();

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
    private int nextShieldIndex;
    private bool hasShield;
    private TrigramType currentShieldType = TrigramType.None;
    private TurnManager subscribedTurnManager;

    public bool HasShield => hasShield;
    public TrigramType CurrentShieldType => currentShieldType;
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
        if (!hasShield)
            return false;

        if (trigram == TrigramType.None || trigram != currentShieldType)
            return false;

        BreakShield(trigram, sourceName);
        return true;
    }

    private void Start()
    {
        CacheShieldImage();
        SubscribeEvents();
        HideShieldVisual();
    }

    private void OnEnable()
    {
        CacheShieldImage();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
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
        if (shieldCycle == null || shieldCycle.Count == 0)
        {
            Debug.LogWarning($"[EnemyShieldController] 护盾属性列表为空，无法生成护盾 | enemy:{name} | round:{roundIndex}");
            return;
        }

        TrigramType shieldType = shieldCycle[nextShieldIndex % shieldCycle.Count];
        nextShieldIndex++;

        if (shieldType == TrigramType.None)
        {
            Debug.LogWarning($"[EnemyShieldController] 护盾属性不能为 None，已跳过 | enemy:{name} | round:{roundIndex}");
            return;
        }

        hasShield = true;
        currentShieldType = shieldType;
        ShowShieldVisual(shieldType);

        if (debugLog)
        {
            Debug.Log($"[EnemyShieldController] 生成护盾 | enemy:{name} | shield:{currentShieldType} | round:{roundIndex}");
        }
    }

    private void OnSkillImpactWaveRequested(TrigramType waveType)
    {
        if (!hasShield)
            return;

        if (waveType != currentShieldType)
            return;

        BreakShield(waveType, "技能冲击波");
    }

    private void BreakShield(TrigramType triggerType, string sourceName)
    {
        hasShield = false;
        currentShieldType = TrigramType.None;
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

    private void CacheShieldImage()
    {
        if (shieldImage != null)
            return;

        shieldImage = GetComponentInChildren<Image>(true);
    }
}
