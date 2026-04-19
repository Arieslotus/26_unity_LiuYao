/// <summary>
/// 实现功能：控制敌人在敌方回合中按 XZ 平面逻辑搜索目标、移动到攻击位置、执行攻击、返回原位，并更新血条与攻击范围显示。
/// </summary>
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour, IAttackable
{
    [Header("数值设定")]
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int currentHP = 0;

    [SerializeField] private int attackDamage = 3;
    [SerializeField] private float moveSpeed = 2f;

    [Tooltip("攻击时与玩家保持的距离")]
    [SerializeField] private float attackDistance = 1.2f;

    [Tooltip("最大索敌/攻击范围")]
    [SerializeField] private float maxChaseDistance = 5f;

    [Header("可视化")]
    [Tooltip("用于显示攻击范围的物体，推荐放一个地面圆环/圆盘")]
    [SerializeField] private Transform attackVisualizer;

    [Header("引用")]
    [SerializeField] private HealthBar healthBar;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private void Start()
    {
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<HealthBar>();
        }

        currentHP = maxHP;
        UpdateHPUI();
    }

    private void Update()
    {
        UpdateVisualizer();
    }

    public IEnumerator TakeTurn()
    {
        Vector3 startPos = transform.position;

        List<IAttackable> targets = FindTargetsInRange(2);

        if (targets.Count == 0)
        {
            if (debugLog)
            {
                Debug.Log($"[EnemyController] {name} 范围内没有目标，结束本回合行动。");
            }

            yield break;
        }

        foreach (IAttackable target in targets)
        {
            if (target == null || target.GetTransform() == null)
                continue;

            float distanceToTarget = GetFlatDistanceXZ(startPos, target.GetTransform().position);

            if (distanceToTarget > maxChaseDistance)
            {
                if (debugLog)
                {
                    Debug.Log($"[EnemyController] {name} 目标过远，不行动。目标:{target.GetTransform().name} 距离:{distanceToTarget:F2}");
                }

                yield break;
            }

            yield return MoveToTarget(target.GetTransform());

            yield return new WaitForSeconds(0.3f);

            yield return DoAttackFeedback();

            target.TakeDamage(attackDamage);

            if (debugLog)
            {
                Debug.Log($"[EnemyController] {name} 攻击了 {target.GetTransform().name} | Damage:{attackDamage}");
            }

            yield return new WaitForSeconds(0.3f);

            yield return MoveBack(startPos);

            yield return new WaitForSeconds(0.2f);
        }
    }

    private List<IAttackable> FindTargetsInRange(int maxTargetCount)
    {
        MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>();
        List<IAttackable> inRange = new List<IAttackable>();

        for (int i = 0; i < allBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = allBehaviours[i];
            if (behaviour == null)
                continue;

            if (behaviour == this)
                continue;

            if (behaviour is not IAttackable attackable)
                continue;

            Transform targetTransform = attackable.GetTransform();
            if (targetTransform == null)
                continue;

            float dist = GetFlatDistanceXZ(transform.position, targetTransform.position);
            if (dist <= maxChaseDistance)
            {
                inRange.Add(attackable);
            }
        }

        inRange.Sort((a, b) =>
        {
            float da = GetFlatDistanceXZ(transform.position, a.GetTransform().position);
            float db = GetFlatDistanceXZ(transform.position, b.GetTransform().position);
            return da.CompareTo(db);
        });

        List<IAttackable> result = new List<IAttackable>();
        int count = Mathf.Min(maxTargetCount, inRange.Count);

        for (int i = 0; i < count; i++)
        {
            result.Add(inRange[i]);
        }

        return result;
    }

    private IEnumerator MoveToTarget(Transform target)
    {
        transform.DOKill();

        Vector3 currentPos = transform.position;
        Vector3 targetPos = target.position;

        Vector3 flatDir = targetPos - currentPos;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude <= 0.0001f)
        {
            if (debugLog)
            {
                Debug.LogWarning($"[EnemyController] {name} MoveToTarget 失败：目标与自身过近。");
            }

            yield break;
        }

        flatDir.Normalize();

        Vector3 desiredPos = targetPos - flatDir * attackDistance;
        desiredPos.y = currentPos.y;

        float distance = GetFlatDistanceXZ(currentPos, desiredPos);
        float duration = moveSpeed <= 0.0001f ? 0f : distance / moveSpeed;

        Tween tween = transform.DOMove(desiredPos, duration)
            .SetEase(Ease.OutQuad);

        yield return tween.WaitForCompletion();
    }

    private IEnumerator MoveBack(Vector3 startPos)
    {
        transform.DOKill();

        Vector3 currentPos = transform.position;
        Vector3 desiredPos = startPos;
        desiredPos.y = currentPos.y;

        float distance = GetFlatDistanceXZ(currentPos, desiredPos);
        float duration = moveSpeed <= 0.0001f ? 0f : distance / moveSpeed;

        Tween tween = transform.DOMove(desiredPos, duration)
            .SetEase(Ease.OutBack);

        yield return tween.WaitForCompletion();
    }

    private IEnumerator DoAttackFeedback()
    {
        transform.DOKill();

        transform.DOShakePosition(0.15f, 0.2f, 10, 90f, false, true);
        transform.DOScale(1.2f, 0.1f).SetLoops(2, LoopType.Yoyo);

        yield return new WaitForSeconds(0.15f);
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);

        UpdateHPUI();

        if (debugLog)
        {
            Debug.Log($"[EnemyController] {name} 受伤 | Damage:{damage} | HP:{currentHP}/{maxHP}");
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (debugLog)
        {
            Debug.Log($"[EnemyController] {name} 死亡。");
        }

        transform.DOKill();
        Destroy(gameObject);
    }

    public Transform GetTransform()
    {
        return transform;
    }

    private void UpdateHPUI()
    {
        if (healthBar != null)
        {
            healthBar.HealthNormalized = maxHP <= 0 ? 0f : (float)currentHP / maxHP;
        }
    }

    private void UpdateVisualizer()
    {
        if (attackVisualizer == null)
            return;

        Vector3 scale = attackVisualizer.localScale;
        scale.x = maxChaseDistance * 2f;
        scale.y = maxChaseDistance * 2f;
        attackVisualizer.localScale = scale;
    }

    private float GetFlatDistanceXZ(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxChaseDistance);
    }
}