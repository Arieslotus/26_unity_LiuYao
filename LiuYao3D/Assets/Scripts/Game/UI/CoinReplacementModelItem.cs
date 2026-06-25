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

    [Header("Selection Outline")]
    [Tooltip("Runtime outline material appended to the coin renderer when selected.")]
    [SerializeField] private Material selectionOutlineMaterial;

    [Header("选中反馈")]
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f);

    [Header("调试")]
    [SerializeField] private bool debugLog;

    private CoinDefinition definition;
    private bool selected;
    private bool outlineVisible;
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
        SetSelectionOutlineVisible(false);
    }

    public void Bind(CoinDefinition coinDefinition)
    {
        definition = coinDefinition;
        targetLocalPosition = transform.localPosition;
        ApplyDefinitionVisual();
        ApplySelectedState(false);
        SetSelectionOutlineVisible(false);

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

    public void SetSelectionOutlineMaterial(Material material)
    {
        if (selectionOutlineMaterial == material)
            return;

        bool restoreOutline = outlineVisible;
        if (restoreOutline)
        {
            RemoveOutlineMaterial();
        }

        selectionOutlineMaterial = material;

        if (restoreOutline)
        {
            AddOutlineMaterial();
        }
    }

    public void SetSelectionOutlineVisible(bool visible)
    {
        if (outlineVisible == visible)
            return;

        Debug.Log($"Outline Visible -> {visible}");

        outlineVisible = visible;

        if (outlineVisible)
        {
            AddOutlineMaterial();
        }
        else
        {
            RemoveOutlineMaterial();
        }
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
            bool restoreOutline = outlineVisible;
            if (restoreOutline)
            {
                RemoveOutlineMaterial();
            }

            coinRenderer.sharedMaterial = definition.coinMaterial;

            if (restoreOutline)
            {
                AddOutlineMaterial();
            }
        }
    }

    private void AddOutlineMaterial()
    {
        if (coinRenderer == null || selectionOutlineMaterial == null)
            return;

        Material[] materials = coinRenderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            if (IsOutlineMaterial(materials[i]))
                return;
        }

        Material[] nextMaterials = new Material[materials.Length + 1];
        for (int i = 0; i < materials.Length; i++)
        {
            nextMaterials[i] = materials[i];
        }

        nextMaterials[nextMaterials.Length - 1] = selectionOutlineMaterial;
        coinRenderer.materials = nextMaterials;
    }

    private void RemoveOutlineMaterial()
    {
        if (coinRenderer == null || selectionOutlineMaterial == null)
            return;

        Material[] materials = coinRenderer.materials;
        int removeCount = 0;

        for (int i = 0; i < materials.Length; i++)
        {
            if (IsOutlineMaterial(materials[i]))
            {
                removeCount++;
            }
        }

        if (removeCount <= 0)
            return;

        Material[] nextMaterials = new Material[materials.Length - removeCount];
        int nextIndex = 0;
        for (int i = 0; i < materials.Length; i++)
        {
            if (IsOutlineMaterial(materials[i]))
                continue;

            nextMaterials[nextIndex] = materials[i];
            nextIndex++;
        }

        coinRenderer.materials = nextMaterials;
    }

    private bool IsOutlineMaterial(Material material)
    {
        if (material == null || selectionOutlineMaterial == null)
            return false;

        string runtimeName = material.name.Replace(" (Instance)", "");

        return runtimeName == selectionOutlineMaterial.name;
    }
}
