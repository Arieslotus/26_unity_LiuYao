using UnityEngine;

//挂载在棋子上，负责处理移动逻辑
public class MovementController : MonoBehaviour
{
    private MovementConfig config;

    private Vector2 direction;
    private float remainingDistance;
    private float currentSpeed;
    private bool isMoving = false;
    public bool IsMoving => isMoving;

    /// <summary>
    /// 初始化移动（由外部传入配置）
    /// </summary>
    public void Init(Vector2 dir, MovementConfig movementConfig, float power = 1f)
    {
        config = movementConfig;

        direction = dir.normalized;

        remainingDistance = config.totalDistance * power;
        currentSpeed = config.baseSpeed;

        isMoving = true;

        Debug.Log($"[Movement] 开始移动 | 方向:{direction} | 总路径:{remainingDistance}");
    }

    private void Update()
    {
        if (!isMoving || config == null) return;

        // ===== 停止条件 =====
        if (remainingDistance <= 0 || currentSpeed <= config.minSpeed)
        {
            Stop();
            return;
        }
        //步长限制，防止高速穿透
        float maxStep = config.radius * 0.5f; // 推荐：半径的一半
        float remainingMoveThisFrame = currentSpeed * Time.deltaTime;
        remainingMoveThisFrame = Mathf.Min(remainingMoveThisFrame, remainingDistance);
        //限制（防止单帧跨墙）
        remainingMoveThisFrame = Mathf.Min(remainingMoveThisFrame, maxStep);

        int loopCount = 0;
        int maxBouncePerFrame = 8;

        while (remainingMoveThisFrame > 0.0001f && loopCount < maxBouncePerFrame)
        {
            loopCount++;

            Vector2 currentPos = transform.position;

            var result = PhysicsBounceUtility.SimulateStep(
                currentPos,
                direction,
                remainingMoveThisFrame,
                config
            );

            float traveled = result.traveledDistance;

            // ===== 防止0距离卡死 =====
            if (traveled <= 0.0001f)
            {
                Move(direction * 0.02f);
                continue;
            }

            // ===== 更新位置 =====
            transform.position = result.newPos;

            remainingDistance -= traveled;
            remainingDistance = Mathf.Max(remainingDistance, 0f);

            remainingMoveThisFrame -= traveled;

            if (result.hit)
            {
                Debug.Log($"[Movement] 碰撞: 剩余路径:{remainingDistance}");

                // 更新方向
                direction = result.newDir;

                // 路径衰减
                remainingDistance *= config.bounceDamping;

                Debug.Log(
    $"[BounceDebug] traveled:{traveled:F4} | " +
    $"pos:{(Vector2)transform.position} | " +
    $"dir:{direction} | " +
    $"remainingMoveThisFrame:{remainingMoveThisFrame:F4}"
                );

            }
            else
            {
                // 没碰撞直接结束
                break;
            }
        }

        // ===== 速度更新（指数/S型）=====
        float t = Mathf.Clamp01(remainingDistance / config.totalDistance);

        // 指数减速
        // currentSpeed = config.baseSpeed * Mathf.Pow(t, config.speedDecayPower);

        //t: 1->0 
        //currentSpeed=baseSpeed (1-t^3)
        currentSpeed = config.baseSpeed * (1f - Mathf.Pow(1f - t, config.speedDecayPower));

        if (config.debugDraw)
        {
            Debug.Log($"[Movement] Speed:{currentSpeed:F2} | 剩余:{remainingDistance:F2}");
        }
    }

    /// <summary>
    /// 安全移动（统一处理Vector2→Vector3）
    /// </summary>
    private void Move(Vector2 delta)
    {
        Vector2 pos = transform.position;
        pos += delta;
        transform.position = pos;
    }

    private void OnDrawGizmos()
    {
        if (config == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, config.radius);
    }

    private void Stop()
    {
        isMoving = false;
        currentSpeed = 0;

        Debug.Log("[Movement] 停止移动");
    }
}