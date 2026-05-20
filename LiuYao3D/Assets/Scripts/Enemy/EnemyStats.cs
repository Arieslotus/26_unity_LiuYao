/// <summary>
/// 实现功能：管理敌人的生命值，并对外提供受伤、血量变化与死亡事件。
/// </summary>
using System;
using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("生命值")]
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int currentHP = 10;
    [SerializeField] private bool startWithFullHP = true;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private EnemyShieldController shieldController;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public float HealthNormalized => maxHP <= 0 ? 0f : (float)currentHP / maxHP;
    public bool IsDead => currentHP <= 0;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        shieldController = GetComponent<EnemyShieldController>();
        currentHP = startWithFullHP ? maxHP : Mathf.Clamp(currentHP, 0, maxHP);
        NotifyHealthChanged();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
            return;

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

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHP, maxHP);
    }
}
