using UnityEngine;

[CreateAssetMenu(fileName = "MovementConfig", menuName = "Config/Movement Config")]
public class MovementConfig : ScriptableObject
{
    [Header("路径控制")]
    [Tooltip("初始总路径长度")]
    public float totalDistance = 10f;

    [Tooltip("每次反弹后的路径衰减系数")]
    [Range(0.1f, 1f)]
    public float bounceDamping = 0.8f;

    [Header("速度控制")]
    [Tooltip("基础速度")]
    public float baseSpeed = 10f;

    [Tooltip("最小速度（低于此值停止）")]
    public float minSpeed = 0.1f;

    [Tooltip("速度衰减指数（推荐2~3）")]
    public float speedDecayPower = 2f;

    [Header("碰撞体积")]
    [Tooltip("棋子半径（用于CircleCast）")]
    public float radius = 0.5f;

    [Header("调试")]
    public bool debugDraw = true;
}