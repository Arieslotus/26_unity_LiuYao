using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour, IAttackable
{
    [Header("数值设定")]
    public int maxHP = 10;
    public int currentHP;

    public int attackDamage = 3;
    public float moveSpeed = 2f;
    float attackDistance = 1.2f; // 攻击玩家时，和玩家的距离
    public float maxChaseDistance = 5f; // 最大可攻击范围（警戒范围）

    [Header("可视化")]
    public Transform attackVisualizer; // 用于显示攻击范围的物体

    [Header("引用")]
    public HealthBar healthBar;

    private void Start()
    {
        //ref
        if(healthBar == null)
        {
            healthBar = GetComponentInChildren<HealthBar>();
        }

        //set
        currentHP = maxHP;
        UpdateHPUI();
    }

    private void Update()
    {
        // visualize
        UpdateVisualizer();
    }
    public IEnumerator TakeTurn()
    {
        // 👉 记录初始位置
        Vector2 startPos = transform.position;

        // 👉 找目标
        var targets = FindTargetsInRange(2); // 👉 最多打2个

        if (targets.Count == 0)
        {
            Debug.Log("范围内没有目标");
            yield break;
        }

        // attack each target
        foreach (var target in targets)
        {
            float distanceToTarget = Vector2.Distance(startPos, target.GetTransform().position);

            // ❗ 如果超出攻击范围 → 直接跳过
            if (distanceToTarget > maxChaseDistance)
            {
                Debug.Log(name + " 目标太远，不行动");
                yield break;
            }

            // 👉 移动过去
            yield return MoveToTarget(target.GetTransform());

            // 👉 攻击
            yield return new WaitForSeconds(0.3f);

            // 👉 打击反馈
            yield return DoAttackFeedback();

            target.TakeDamage(attackDamage);

            Debug.Log(name + " 攻击了 " + target.GetTransform().name);

            yield return new WaitForSeconds(0.3f);

            // 👉 回到原位
            yield return MoveBack(startPos);

            yield return new WaitForSeconds(0.2f);
        }

    }


    List<IAttackable> FindTargetsInRange(int maxTargetCount)
    {
        PlayerBallTest[] balls = FindObjectsOfType<PlayerBallTest>();

        List<IAttackable> result = new List<IAttackable>();

        // 👉 先筛选“在攻击范围内”的
        List<PlayerBallTest> inRange = new List<PlayerBallTest>();

        foreach (var ball in balls)
        {
            if (ball == null) continue;

            float dist = Vector2.Distance(transform.position, ball.transform.position);

            if (dist <= maxChaseDistance) // 👉 用你的攻击/警戒范围
            {
                inRange.Add(ball);
            }
        }

        // 👉 按距离排序（从近到远）
        inRange.Sort((a, b) =>
        {
            float da = Vector2.Distance(transform.position, a.transform.position);
            float db = Vector2.Distance(transform.position, b.transform.position);
            return da.CompareTo(db);
        });

        // 👉 取前X个
        int count = Mathf.Min(maxTargetCount, inRange.Count);

        for (int i = 0; i < count; i++)
        {
            result.Add(inRange[i]);
        }

        return result;
    }

    IEnumerator MoveToTarget(Transform target)
    {
        transform.DOKill();

        Vector2 dir = (target.position - transform.position).normalized;
        Vector2 targetPos = (Vector2)target.position - dir * attackDistance;

        float distance = Vector2.Distance(transform.position, targetPos);
        float duration = distance / moveSpeed;

        // 👉 冲刺过去（更有攻击欲望）
        Tween tween = transform.DOMove(targetPos, duration)
            .SetEase(Ease.OutQuad);

        yield return tween.WaitForCompletion();
    }

    IEnumerator MoveBack(Vector2 startPos)
    {
        transform.DOKill();

        float distance = Vector2.Distance(transform.position, startPos);
        float duration = distance / moveSpeed;

        // 👉 回去带一点弹性（关键手感）
        Tween tween = transform.DOMove(startPos, duration)
            .SetEase(Ease.OutBack);

        yield return tween.WaitForCompletion();
    }

    IEnumerator DoAttackFeedback()
    {
        // 👉 轻微抖动（命中感）
        transform.DOShakePosition(0.15f, 0.2f, 10, 90, false, true);

        // 👉 轻微缩放（打击反馈）
        transform.DOScale(1.2f, 0.1f).SetLoops(2, LoopType.Yoyo);

        yield return new WaitForSeconds(0.15f);
    }

    // 👉 受伤
    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        UpdateHPUI();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(name + " 死亡了");
        Destroy(gameObject);
    }

    public Transform GetTransform()
    {
        return transform;
    }


    // update
    void UpdateHPUI()
    {
        if (healthBar != null)
        {
            healthBar.HealthNormalized = (float)((float)currentHP / (float)maxHP); // *
            Debug.Log(name + " HP: " + currentHP + "/" + maxHP + "==" + healthBar.HealthNormalized);
        }
    }
    void UpdateVisualizer()
    {
        if (attackVisualizer != null)
        {
            attackVisualizer.localScale = Vector3.one * (maxChaseDistance * 2f - 1f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxChaseDistance);
    }
}
