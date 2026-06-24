/// <summary>
/// 实现功能：管理敌人的运行时生命值与攻击力，并从 EnemyDefinitionSO 初始化基础数值。
/// </summary>
using System;
using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("敌人配置")]
    [SerializeField] private EnemyDefinitionSO definition;

    [Header("生命值")]
    private int maxHP;
    private int currentHP;
    private bool startWithFullHP = true;

    [Header("攻击")]
    private int attack = 1;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private EnemyShieldController shieldController;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int Attack => attack;
    public EnemyDefinitionSO Definition => definition;
    public float HealthNormalized => maxHP <= 0 ? 0f : (float)currentHP / maxHP;
    public bool IsDead => currentHP <= 0;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        shieldController = GetComponentInChildren<EnemyShieldController>(true);
        ApplyDefinition();
        currentHP = startWithFullHP ? maxHP : Mathf.Clamp(currentHP, 0, maxHP);
        NotifyHealthChanged();
    }

    public void Initialize(EnemyDefinitionSO enemyDefinition)
    {
        definition = enemyDefinition;
        ApplyDefinition();
        ResetHealth();

        if (debugLog)
        {
            string definitionName = definition != null ? definition.enemyName : "空";
            Debug.Log($"[EnemyStats] 初始化敌人数值 | enemy:{name} | definition:{definitionName} | HP:{currentHP}/{maxHP} | ATK:{attack}");
        }
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
            return;

        ResolveShieldController();

        int finalDamage = shieldController != null
            ? shieldController.ModifyIncomingDamage(damage)
            : damage;

        if (finalDamage <= 0)
        {
            if (debugLog)
            {
                Debug.Log($"[EnemyStats] 伤害被完全减免 | enemy:{name} | rawDamage:{damage}");
            }

            return;
        }

        currentHP = Mathf.Max(currentHP - finalDamage, 0);
        NotifyHealthChanged();
        CombatVfxEvents.RequestEnemyDamaged(this, finalDamage, transform.position);

        if (debugLog)
        {
            Debug.Log($"[EnemyStats] 受伤 | enemy:{name} | rawDamage:{damage} | finalDamage:{finalDamage} | HP:{currentHP}/{maxHP}");
        }

        if (currentHP <= 0)
        {
            Died?.Invoke();
        }
    }

    public void ResetHealth()
    {
        currentHP = Mathf.Max(0, maxHP);
        NotifyHealthChanged();
    }

    private void ApplyDefinition()
    {
        if (definition == null)
            return;

        maxHP = Mathf.Max(1, definition.maxHP);
        attack = Mathf.Max(0, definition.attack);
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHP, maxHP);
    }

    private void ResolveShieldController()
    {
        if (shieldController != null)
            return;

        shieldController = GetComponentInChildren<EnemyShieldController>(true);
    }
}
