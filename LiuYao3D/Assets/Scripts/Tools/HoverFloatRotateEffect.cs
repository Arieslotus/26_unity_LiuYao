/// <summary>
/// 实现功能：为 3D 展示物体提供鼠标悬停上浮与旋转表现，可选择按世界坐标或目标自身坐标解释位移方向。
/// </summary>
using UnityEngine;

public enum HoverDirectionSpace
{
    Local,
    World
}

public class HoverFloatRotateEffect : MonoBehaviour
{
    [Header("目标")]
    [Tooltip("需要移动和旋转的目标。为空时使用当前物体。")]
    [SerializeField] private Transform target;

    [Header("悬停位移")]
    [Tooltip("悬停时位移所使用的方向空间。")]
    [SerializeField] private HoverDirectionSpace hoverDirectionSpace = HoverDirectionSpace.World;

    [Tooltip("悬停时位移方向。")]
    [SerializeField] private Vector3 hoverDirection = Vector3.up;

    [Tooltip("悬停时沿方向移动的距离。")]
    [Min(0f)]
    [SerializeField] private float hoverDistance = 0.2f;

    [Tooltip("移动到目标位置的速度。")]
    [Min(0.01f)]
    [SerializeField] private float moveSpeed = 8f;

    [Header("悬停旋转")]
    [Tooltip("旋转轴，使用目标本地空间。")]
    [SerializeField] private Vector3 rotateLocalAxis = Vector3.up;

    [Tooltip("旋转速度，单位：度/秒。")]
    [SerializeField] private float rotateSpeed = 90f;

    [Tooltip("是否只在悬停时旋转。关闭后会一直旋转。")]
    [SerializeField] private bool rotateOnlyWhenHovered = true;

    [Tooltip("鼠标离开后是否恢复初始旋转。")]
    [SerializeField] private bool restoreRotationOnExit = false;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private bool initialized;
    private bool hovered;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
    }

    private void Update()
    {
        EnsureInitialized();
        UpdatePosition();
        UpdateRotation();
    }

    public void SetHovered(bool value)
    {
        hovered = value;
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        if (target == null)
        {
            target = transform;
        }

        baseLocalPosition = target.localPosition;
        baseLocalRotation = target.localRotation;
        initialized = true;
    }

    private void UpdatePosition()
    {
        Vector3 direction = GetHoverDirection();
        Vector3 targetPosition = baseLocalPosition + direction * (hovered ? hoverDistance : 0f);
        target.localPosition = Vector3.Lerp(
            target.localPosition,
            targetPosition,
            Mathf.Clamp01(moveSpeed * Time.deltaTime)
        );
    }

    private void UpdateRotation()
    {
        bool shouldRotate = hovered || !rotateOnlyWhenHovered;
        if (shouldRotate)
        {
            Vector3 axis = rotateLocalAxis.sqrMagnitude > 0.0001f
                ? rotateLocalAxis.normalized
                : Vector3.up;
            target.Rotate(axis, rotateSpeed * Time.deltaTime, Space.Self);
            return;
        }

        if (restoreRotationOnExit)
        {
            target.localRotation = Quaternion.Slerp(
                target.localRotation,
                baseLocalRotation,
                Mathf.Clamp01(moveSpeed * Time.deltaTime)
            );
        }
    }

    private Vector3 GetHoverDirection()
    {
        Vector3 direction = hoverDirection.sqrMagnitude > 0.0001f
            ? hoverDirection.normalized
            : Vector3.up;

        if (hoverDirectionSpace == HoverDirectionSpace.World)
        {
            Transform parent = target != null ? target.parent : null;
            return parent != null
                ? parent.InverseTransformDirection(direction).normalized
                : direction;
        }

        return direction;
    }
}
