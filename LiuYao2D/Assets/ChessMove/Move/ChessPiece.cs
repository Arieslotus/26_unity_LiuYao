using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("该棋子的移动参数")]
    [SerializeField] private MovementConfig movementConfig;

    private MovementController movement;

    private void Awake()
    {
        movement = GetComponent<MovementController>();
    }

    /// <summary>
    /// 发射棋子
    /// </summary>
    public void Fire(Vector2 direction, float power = 1f)
    {
        if (movementConfig == null)
        {
            Debug.LogError("MovementConfig 未设置！");
            return;
        }

        movement.Init(direction, movementConfig, power);
    }

    public bool IsMoving => movement != null && movement.IsMoving;
}