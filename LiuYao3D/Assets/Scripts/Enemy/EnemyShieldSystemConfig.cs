/// <summary>
/// 实现功能：提供敌人护盾系统的全局运行时配置入口，避免每个敌人预制体重复配置破盾规则。
/// </summary>
using UnityEngine;

public class EnemyShieldSystemConfig : MonoBehaviour
{
    public static EnemyShieldSystemConfig Instance { get; private set; }

    [Header("破盾规则")]
    [Tooltip("全局敌人护盾破盾规则。普通敌人会优先使用这里的配置。")]
    [SerializeField] private EnemyShieldBreakConfigSO shieldBreakConfig;

    public EnemyShieldBreakConfigSO ShieldBreakConfig => shieldBreakConfig;

    private void Awake()
    {
        RegisterInstance();
    }

    private void OnEnable()
    {
        RegisterInstance();
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void RegisterInstance()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                $"[EnemyShieldSystemConfig] 场景中存在多个敌人护盾系统配置，将继续使用已有实例 | " +
                $"existing:{Instance.name} | ignored:{name}"
            );
            return;
        }

        Instance = this;
    }
}
