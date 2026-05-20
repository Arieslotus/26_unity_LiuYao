/// <summary>
/// 实现功能：管理单枚硬币的独立战斗数值，包括生命值与攻击力。
/// </summary>
using System;
using UnityEngine;

public class CoinStats : MonoBehaviour, IAttackable
{
    [Header("生命值")]
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int currentHP = 10;
    [SerializeField] private bool startWithFullHP = true;

    [Header("攻击")]
    [SerializeField] private int attack = 1;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int Attack => attack;
    public bool IsDead => currentHP <= 0;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        currentHP = startWithFullHP ? maxHP : Mathf.Clamp(currentHP, 0, maxHP);
        NotifyHealthChanged();

        if (debugLog)
        {
            Debug.Log($"[CoinStats] 初始化 | coin:{name} | HP:{currentHP}/{maxHP} | ATK:{attack}");
        }
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
            return;

        currentHP = Mathf.Max(currentHP - damage, 0);
        NotifyHealthChanged();

        if (debugLog)
        {
            Debug.Log($"[CoinStats] 受伤 | coin:{name} | damage:{damage} | HP:{currentHP}/{maxHP}");
        }

        if (currentHP <= 0)
        {
            Died?.Invoke();
            Destroy(gameObject);
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void Heal(int value)
    {
        if (value <= 0 || IsDead)
            return;

        currentHP = Mathf.Min(maxHP, currentHP + value);
        NotifyHealthChanged();

        if (debugLog)
        {
            Debug.Log($"[CoinStats] 恢复生命 | coin:{name} | value:{value} | HP:{currentHP}/{maxHP}");
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
