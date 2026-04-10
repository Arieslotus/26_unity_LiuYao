/// <summary>
/// 实现功能：定义可被击中的对象接口（敌人可实现）
/// </summary>
using UnityEngine;

public interface IHittable
{
    void OnHit(Vector2 hitDirection, float impactStrength);
    //冲击强度:剩余路程比例 ctx.self.RemainingDistance / movementConfig.totalDistance;
    //方向：指向敌人即将被推走的方向（并非指向玩家硬币来的方向）
}

/*
 //实现示例：
 public class Enemy : MonoBehaviour, IHittable
{
    public void OnHit(Vector2 hitDirection, float impactStrength)
    {
        // 这里自己做：
        // - 是否被击退
        // - 播动画
        // - 扣血
    }
}
 */