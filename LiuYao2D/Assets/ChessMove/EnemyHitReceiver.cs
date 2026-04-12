using UnityEngine;

public class EnemyHitReceiver : MonoBehaviour, IHittable
{
    public void OnHit(Vector2 direction, float strength)
    {
        Debug.Log($"[Enemy] 被击中 | dir:{direction} | strength:{strength:F2}");

        // TODO：后面加：
        // - 播动画
        // - 击退
        // - 掉血
    }
}