/// <summary>
/// 实现功能：展示一枚硬币的 3D 模型，并处理选中缩放与 hover 表现。
/// </summary>
using UnityEngine;

public class CoinReplacementModelItem : MonoBehaviour
{
    [Header("显示")]
    [Tooltip("用于展示硬币材质的 Renderer。为空时自动查找子物体 Renderer。")]
    [SerializeField] private Renderer coinRenderer;

    [Tooltip("执行选中缩放的根节点。为空时使用当前物体。")]
    [SerializeField] private Transform scaleRoot;

    [Tooltip("鼠标悬停时的上浮旋转效果。为空时自动查找子物体。")]
    [SerializeField] private HoverFloatRotateEffect hoverEffect;

    [Header("选中反馈")]
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f);

    [Header("调试")]
    [SerializeField] private bool debugLog;

    private CoinDefinition definition;
    private bool selected;
    private Vector3 targetLocalPosition;

    public CoinDefinition Definition => definition;
    public bool Selected => selected;
    public Vector3 TargetLocalPosition => targetLocalPosition;

    private void Awake()
    {
        if (coinRenderer == null)
        {
            coinRenderer = GetComponentInChildren<Renderer>(true);
        }

        if (scaleRoot == null)
        {
            scaleRoot = transform;
        }

        if (hoverEffect == null)
        {
            hoverEffect = GetComponentInChildren<HoverFloatRotateEffect>(true);
        }

        ApplySelectedState(false);
    }

    public void Bind(CoinDefinition coinDefinition)
    {
        definition = coinDefinition;
        targetLocalPosition = transform.localPosition;
        ApplyDefinitionVisual();
        ApplySelectedState(false);

        if (debugLog)
        {
            Debug.Log(
                $"[CoinReplacementModelItem] 绑定硬币模型 | object:{name} | " +
                $"coin:{(definition != null ? definition.coinName : "空")}"
            );
        }
    }

    public void SetSelected(bool value)
    {
        ApplySelectedState(value);
    }

    public void SetHovered(bool value)
    {
        if (hoverEffect != null)
        {
            hoverEffect.SetHovered(value);
        }
    }

    private void ApplySelectedState(bool value)
    {
        selected = value;

        if (scaleRoot != null)
        {
            scaleRoot.localScale = selected ? selectedScale : normalScale;
        }
    }

    private void ApplyDefinitionVisual()
    {
        if (coinRenderer == null || definition == null)
            return;

        if (definition.coinMaterial != null)
        {
            coinRenderer.sharedMaterial = definition.coinMaterial;
        }
    }
}
