using UnityEngine;

public class MovementController : MonoBehaviour
{
    private MovementConfig config;

    private Vector2 direction;
    private float remainingDistance;
    private float currentSpeed;
    private bool isMoving = false;

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
        // ⭐关键限制（防止单帧跨墙）
        remainingMoveThisFrame = Mathf.Min(remainingMoveThisFrame, maxStep);

        int loopCount = 0;
        int maxBouncePerFrame = 8;

        while (remainingMoveThisFrame > 0.0001f && loopCount < maxBouncePerFrame)
        {
            loopCount++;

            Vector2 currentPos = transform.position;

            RaycastHit2D hit = Physics2D.CircleCast(currentPos, config.radius, direction, remainingMoveThisFrame);

            if (config.debugDraw)
            {
                Debug.DrawLine(currentPos, currentPos + direction * remainingMoveThisFrame, Color.red);
            }

            if (hit.collider != null)
            {
                // ===== 防止0距离死循环 =====
                if (hit.distance <= 0.0001f)
                {
                    Move(direction * 0.02f);
                    continue;
                }

                // ===== 移动到碰撞点 =====
                float traveled = hit.distance;
                transform.position = hit.point;

                remainingDistance -= traveled;
                remainingDistance = Mathf.Max(remainingDistance, 0f);

                remainingMoveThisFrame -= traveled;

                Debug.Log($"[Movement] 碰撞: {hit.collider.name} | 剩余路径:{remainingDistance}");

                // ===== 反弹 =====
                direction = Vector2.Reflect(direction, hit.normal).normalized;

                // ===== 推出表面（关键）=====
                Vector2 safePos = hit.point + hit.normal * (config.radius + 0.001f);
                transform.position = safePos;

                // 再沿反弹方向推一点，避免再次命中
                Move(direction * 0.01f);

                // ===== 路径衰减 =====
                remainingDistance *= config.bounceDamping;
            }
            else
            {
                // ===== 没碰撞，一次走完 =====
                Move(direction * remainingMoveThisFrame);

                remainingDistance -= remainingMoveThisFrame;
                remainingMoveThisFrame = 0f;
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