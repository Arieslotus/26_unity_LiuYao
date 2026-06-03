/// <summary>
/// 实现功能：管理单枚硬币的损耗值、攻击力与碎裂状态，并为敌人攻击、主动操作和技能损耗提供统一入口。
/// </summary>
using System;
using UnityEngine;

public enum CoinLossCause
{
    Operation,
    EnemyAttack,
    Skill
}

public class CoinStats : MonoBehaviour, IAttackable
{
    [Header("损耗值")]
    [SerializeField] private int maxLoss = 10;
    [SerializeField] private int currentLoss;

    [Tooltip("玩家主动操作该硬币并停止移动后增加的损耗值")]
    [Min(0)]
    [SerializeField] private int operationLoss = 1;

    [Header("攻击")]
    [SerializeField] private int attack = 1;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    public int MaxLoss => maxLoss;
    public int CurrentLoss => currentLoss;
    public int Attack => attack;
    public bool IsBroken => currentLoss >= maxLoss;

    // 兼容现有胜负检测代码，后续可以逐步迁移为 IsBroken。
    public bool IsDead => IsBroken;

    public event Action<int, int> LossChanged;
    public event Action Broken;

    // 兼容现有监听代码，事件参数仍然是“当前值 / 最大值”。
    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        maxLoss = Mathf.Max(1, maxLoss);
        currentLoss = Mathf.Clamp(currentLoss, 0, maxLoss);
        NotifyLossChanged();

        if (debugLog)
        {
            Debug.Log($"[CoinStats] 初始化 | coin:{name} | 损耗:{currentLoss}/{maxLoss} | ATK:{attack}");
        }
    }

    public void AddOperationLoss()
    {
        AddLoss(operationLoss, CoinLossCause.Operation);
    }

    public void AddLoss(int value, CoinLossCause cause = CoinLossCause.Skill)
    {
        if (value <= 0 || IsBroken)
            return;

        if (CoinRoundEffectManager.Instance != null &&
            CoinRoundEffectManager.Instance.TryBlockCoinLoss(this, value, cause))
        {
            return;
        }

        currentLoss = Mathf.Min(currentLoss + value, maxLoss);
        NotifyLossChanged();

        if (debugLog)
        {
            Debug.Log($"[CoinStats] 增加损耗 | coin:{name} | cause:{cause} | value:{value} | 损耗:{currentLoss}/{maxLoss}");
        }

        if (IsBroken)
        {
            BreakCoin();
        }
    }

    public void ReduceLoss(int value)
    {
        if (value <= 0 || IsBroken)
            return;

        currentLoss = Mathf.Max(currentLoss - value, 0);
        NotifyLossChanged();

        if (debugLog)
        {
            Debug.Log($"[CoinStats] 降低损耗 | coin:{name} | value:{value} | 损耗:{currentLoss}/{maxLoss}");
        }
    }

    public void TakeDamage(int damage)
    {
        AddLoss(damage, CoinLossCause.EnemyAttack);
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void Heal(int value)
    {
        ReduceLoss(value);
    }

    public void ResetLoss()
    {
        currentLoss = 0;
        NotifyLossChanged();
    }

    public void ResetHealth()
    {
        ResetLoss();
    }

    private void BreakCoin()
    {
        if (debugLog)
        {
            Debug.Log($"[CoinStats] 硬币碎裂 | coin:{name} | 损耗:{currentLoss}/{maxLoss}");
        }

        Broken?.Invoke();
        Died?.Invoke();
        Destroy(gameObject);
    }

    private void NotifyLossChanged()
    {
        LossChanged?.Invoke(currentLoss, maxLoss);
        HealthChanged?.Invoke(currentLoss, maxLoss);
    }
}
