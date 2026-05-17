/// <summary>
/// 负责：我是谁、当前是哪一面、当前是什么卦、血量攻击力
/// 实现功能：管理单枚硬币的运行时数据，包括硬币定义、当前正反面、当前卦象、血量、攻击力，并对接硬币视觉显示。
/// </summary>
using System;
using UnityEngine;

public class CoinRuntimeData : MonoBehaviour, IAttackable
{
    [Header("硬币定义")]
    [Tooltip("这枚硬币固定的正反面卦象与显示资源")]
    [SerializeField] private CoinDefinition coinDefinition;

    [Header("正反面")]
    [Tooltip("是否默认以正面开始")]
    [SerializeField] private bool startFrontSide = true;

    [Header("战斗属性")]
    [SerializeField] private int maxHp = 10;
    [SerializeField] private int currentHp = 10;
    [SerializeField] private int attack = 1;

    private CoinVisualController visualController;
    private bool isFrontSide = true;

    public CoinDefinition CoinDefinition => coinDefinition;
    public bool IsFrontSide => isFrontSide;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int Attack => attack;

    public event Action<CoinRuntimeData> VisualStateChanged;

    public TrigramType CurrentTrigram
    {
        get
        {
            if (coinDefinition == null)
            {
                Debug.LogWarning($"[CoinRuntimeData] {name} 未配置 CoinDefinition，返回默认卦象 Qian。");
                return TrigramType.Qian;
            }

            return isFrontSide ? coinDefinition.frontTrigram : coinDefinition.backTrigram;
        }
    }
    public TrigramType OppositeTrigram
    {
        get
        {
            if (coinDefinition == null)
            {
                Debug.LogWarning($"[CoinRuntimeData] {name} 未配置 CoinDefinition，返回默认卦象 Qian。");
                return TrigramType.Qian;
            }

            return isFrontSide ? coinDefinition.backTrigram : coinDefinition.frontTrigram;
        }
    }

    //---实现接口---
    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        Debug.Log(name + " 被打了，剩余HP: " + currentHp);

        if (currentHp <= 0)
        {
            Destroy(gameObject);
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    //--------------

    private void Awake()
    {
        visualController = GetComponentInChildren<CoinVisualController>();

        isFrontSide = startFrontSide;
        currentHp = maxHp;

        RefreshVisualImmediate();

        Debug.Log(
            $"[CoinRuntimeData] 初始化 | 物体:{name} | " +
            $"coin:{(coinDefinition != null ? coinDefinition.coinName : "未配置")} | " +
            $"当前面:{(isFrontSide ? "正面" : "反面")} | 当前卦象:{CurrentTrigram} | " +
            $"HP:{currentHp}/{maxHp} | ATK:{attack}"
        );
    }

    public void PlayChargeFlip(Action onComplete = null)
    {
        isFrontSide = !isFrontSide;

        Action notifyAndComplete = () =>
        {
            NotifyVisualStateChanged();
            onComplete?.Invoke();
        };

        if (visualController != null)
        {
            visualController.PlayFlipToFace(isFrontSide, coinDefinition, notifyAndComplete);
        }
        else
        {
            notifyAndComplete.Invoke();
        }

        Debug.Log(
            $"[CoinRuntimeData] 蓄力翻面 | 物体:{name} | " +
            $"当前面:{(isFrontSide ? "正面" : "反面")} | 当前卦象:{CurrentTrigram}"
        );
    }

    public void RestoreFaceImmediate(bool targetFrontSide)
    {
        isFrontSide = targetFrontSide;

        if (visualController != null)
        {
            visualController.CancelFlipAndSetFace(isFrontSide, coinDefinition);
        }

        NotifyVisualStateChanged();

        Debug.Log(
            $"[CoinRuntimeData] 恢复初始面 | 物体:{name} | " +
            $"当前面:{(isFrontSide ? "正面" : "反面")} | 当前卦象:{CurrentTrigram}"
        );
    }

    public void SetFace(bool frontSide, bool playAnimation)
    {
        isFrontSide = frontSide;

        if (visualController == null)
        {
            NotifyVisualStateChanged();
            return;
        }

        if (playAnimation)
        {
            visualController.PlayFlipToFace(isFrontSide, coinDefinition, NotifyVisualStateChanged);
        }
        else
        {
            visualController.SetFaceImmediate(isFrontSide, coinDefinition);
            NotifyVisualStateChanged();
        }

        Debug.Log(
            $"[CoinRuntimeData] 设置正反面 | 物体:{name} | " +
            $"当前面:{(isFrontSide ? "正面" : "反面")} | 当前卦象:{CurrentTrigram}"
        );
    }

    public void SetCoinDefinition(CoinDefinition definition, bool refreshVisual = true)
    {
        coinDefinition = definition;

        if (refreshVisual)
        {
            RefreshVisualImmediate();
        }

        NotifyVisualStateChanged();

        Debug.Log(
            definition != null
                ? $"[CoinRuntimeData] 设置硬币定义 | 物体:{name} | coin:{definition.coinName} | 当前卦象:{CurrentTrigram}"
                : $"[CoinRuntimeData] 清空硬币定义 | 物体:{name}"
        );
    }

    public void Heal(int value)
    {
        if (value <= 0)
            return;

        currentHp = Mathf.Min(maxHp, currentHp + value);

        Debug.Log(
            $"[CoinRuntimeData] 恢复生命 | 物体:{name} | " +
            $"恢复:{value} | 当前HP:{currentHp}/{maxHp}"
        );
    }

    private void RefreshVisualImmediate()
    {
        if (visualController != null)
        {
            visualController.SetFaceImmediate(isFrontSide, coinDefinition);
        }
    }

    private void NotifyVisualStateChanged()
    {
        VisualStateChanged?.Invoke(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            visualController = GetComponentInChildren<CoinVisualController>();
            isFrontSide = startFrontSide;
            RefreshVisualImmediate();
        }
    }
#endif

}
