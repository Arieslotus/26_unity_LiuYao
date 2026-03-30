using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour, IAttackable
{
    public int maxHP = 10;
    public int currentHP;

    public int attackDamage = 3;
    public float moveSpeed = 2f;
    public float attackRange = 1.2f;
    public float maxChaseDistance = 5f; // 最大可攻击范围（警戒范围）

    public Slider hpSlider; // UI血条

    private void Start()
    {
        currentHP = maxHP;
        UpdateHPUI();
    }
    public IEnumerator TakeTurn()
    {
        // 👉 记录初始位置
        Vector2 startPos = transform.position;

        // 👉 找目标
        IAttackable target = FindNearestTarget();

        if (target == null)
            yield break;

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

        target.TakeDamage(attackDamage);

        Debug.Log(name + " 攻击了 " + target.GetTransform().name);

        yield return new WaitForSeconds(0.3f);

        // 👉 回到原位
        yield return MoveBack(startPos);

        yield return new WaitForSeconds(0.2f);
    }

    IAttackable FindNearestTarget()
    {
        PlayerBallTest[] balls = FindObjectsOfType<PlayerBallTest>();

        float minDist = Mathf.Infinity;
        IAttackable nearest = null;

        foreach (var ball in balls)
        {
            if (ball == null) continue;

            float dist = Vector2.Distance(transform.position, ball.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                nearest = ball;
            }
        }

        return nearest;
    }

    IEnumerator MoveToTarget(Transform target)
    {
        while (Vector2.Distance(transform.position, target.position) > attackRange)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }
    }

    IEnumerator MoveBack(Vector2 startPos)
    {
        while (Vector2.Distance(transform.position, startPos) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                startPos,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }
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

    void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = (float)currentHP / maxHP;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxChaseDistance);
    }
}
