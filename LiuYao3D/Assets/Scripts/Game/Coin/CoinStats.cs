/// <summary>
/// 实现功能：管理单枚硬币的损耗、攻击力与破裂状态；数值全部由 CoinDefinition 提供，并支持“达到破裂阈值后等待移动结束再破裂”。
/// </summary>
using System;
using UnityEngine;

public enum CoinLossCause
{
    Operation,
    EnemyCollision,
    EnemyAttack,
    Skill
}

public class CoinStats : MonoBehaviour, IAttackable
{
    [Header("运行时状态")]
    [SerializeField] private int currentLoss;
    [SerializeField] private int maxLoss = 1;
    [SerializeField] private int attack;
    [SerializeField] private CoinDefinition appliedDefinition;
    [SerializeField] private bool isBroken;
    [SerializeField] private bool pendingBreak;

    [Header("损耗规则")]
    [Tooltip("玩家主动操作该硬币并停止移动后增加的损耗值。当前规则暂不使用，保留给后续切换。")]
    [Min(0)]
    [SerializeField] private int operationLoss = 1;

    [Tooltip("硬币每次碰撞敌人时增加的损耗值。")]
    [Min(0)]
    [SerializeField] private int enemyCollisionLoss = 1;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private Collider[] cachedColliders;
    private Renderer[] cachedRenderers;
    private ChessPiece chessPiece;

    public int MaxLoss => maxLoss;
    public int CurrentLoss => currentLoss;
    public int Attack => attack;
    public CoinDefinition AppliedDefinition => appliedDefinition;
    public bool IsBroken => isBroken;
    public bool IsPendingBreak => pendingBreak;

    public event Action<int, int> LossChanged;
    public event Action Broken;

    private void Awake()
    {
        cachedColliders = GetComponentsInChildren<Collider>(true);
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        chessPiece = GetComponent<ChessPiece>();

        CoinRuntimeData runtimeData = GetComponent<CoinRuntimeData>();
        if (runtimeData != null && runtimeData.CoinDefinition != null)
        {
            ApplyCoinDefinition(runtimeData.CoinDefinition, true);
            return;
        }

        ClearDefinitionStats();
        Debug.LogWarning($"[CoinStats] {name} 启动时未找到 CoinDefinition，数值将等待 CoinRuntimeData.SetCoinDefinition 应用。");
    }

    private void Update()
    {
        if (!pendingBreak)
            return;

        if (chessPiece != null && chessPiece.IsMoving)
            return;

        ResolvePendingBreak();
    }

    public void ApplyCoinDefinition(CoinDefinition definition, bool resetLoss)
    {
        if (definition == null)
        {
            ClearDefinitionStats();
            return;
        }

        appliedDefinition = definition;
        maxLoss = Mathf.Max(1, definition.maxLoss);
        attack = Mathf.Max(0, definition.attack);
        currentLoss = resetLoss ? 0 : Mathf.Clamp(currentLoss, 0, maxLoss);
        isBroken = false;
        pendingBreak = false;
        SetBrokenPresentation(false);
        NotifyLossChanged();

        if (debugLog)
        {
            Debug.Log(
                $"[CoinStats] 应用硬币定义 | coin:{name} | definition:{definition.coinName} | " +
                $"type:{definition.coinType} | loss:{currentLoss}/{maxLoss} | atk:{attack}"
            );
        }
    }

    public void AddOperationLoss()
    {
        AddLoss(operationLoss, CoinLossCause.Operation);
    }

    public void AddEnemyCollisionLoss()
    {
        AddLoss(enemyCollisionLoss, CoinLossCause.EnemyCollision);
    }

    public void AddLoss(int value, CoinLossCause cause = CoinLossCause.Skill)
    {
        if (value <= 0 || isBroken || pendingBreak)
            return;

        if (CoinRoundEffectManager.Instance != null &&
            CoinRoundEffectManager.Instance.TryBlockCoinLoss(this, value, cause))
        {
            return;
        }

        currentLoss = Mathf.Min(currentLoss + value, maxLoss);
        NotifyLossChanged();
        CombatVfxEvents.RequestCoinDamaged(this, value, cause, transform.position);

        if (debugLog)
        {
            Debug.Log($"[CoinStats] 增加损耗 | coin:{name} | cause:{cause} | value:{value} | loss:{currentLoss}/{maxLoss}");
        }

        if (currentLoss < maxLoss)
            return;

        if (chessPiece != null && chessPiece.IsMoving)
        {
            pendingBreak = true;

            if (debugLog)
            {
                Debug.Log($"[CoinStats] 达到破裂阈值，等待移动结束后破裂 | coin:{name} | loss:{currentLoss}/{maxLoss}");
            }

            return;
        }

        BreakCoin();
    }

    public void ReduceLoss(int value)
    {
        if (value <= 0 || isBroken || pendingBreak)
            return;

        currentLoss = Mathf.Max(currentLoss - value, 0);
        NotifyLossChanged();

        if (debugLog)
        {
            Debug.Log($"[CoinStats] 降低损耗 | coin:{name} | value:{value} | loss:{currentLoss}/{maxLoss}");
        }
    }

    public void TakeDamage(int damage)
    {
        AddLoss(damage, CoinLossCause.EnemyAttack);
    }

    public Transform GetTransform()
    {
        return isBroken ? null : transform;
    }

    public void ResetLoss()
    {
        currentLoss = 0;
        isBroken = false;
        pendingBreak = false;
        SetBrokenPresentation(false);
        NotifyLossChanged();
    }

    public void ResolvePendingBreak()
    {
        if (!pendingBreak || isBroken)
            return;

        BreakCoin();
    }

    private void BreakCoin()
    {
        if (isBroken)
            return;

        pendingBreak = false;
        isBroken = true;

        if (debugLog)
        {
            Debug.Log($"[CoinStats] 硬币破裂 | coin:{name} | loss:{currentLoss}/{maxLoss}");
        }

        SetBrokenPresentation(true);
        Broken?.Invoke();
    }

    private void ClearDefinitionStats()
    {
        appliedDefinition = null;
        currentLoss = 0;
        maxLoss = 1;
        attack = 0;
        isBroken = false;
        pendingBreak = false;
        SetBrokenPresentation(false);
        NotifyLossChanged();
    }

    private void SetBrokenPresentation(bool broken)
    {
        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                Collider targetCollider = cachedColliders[i];
                if (targetCollider == null)
                    continue;

                targetCollider.enabled = !broken;
            }
        }

        if (cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer targetRenderer = cachedRenderers[i];
                if (targetRenderer == null)
                    continue;

                targetRenderer.enabled = !broken;
            }
        }
    }

    private void NotifyLossChanged()
    {
        LossChanged?.Invoke(currentLoss, maxLoss);
    }
}
