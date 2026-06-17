/// <summary>
/// 实现功能：控制硬币说明面板的实例化、显示与隐藏，并根据当前悬停硬币刷新内容。
/// </summary>
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CoinInfoPanelController : MonoBehaviour
{
    [Header("面板预制体")]
    [Tooltip("硬币说明面板预制体。内部 Text 引用在预制体内配置。")]
    [SerializeField] private CoinInfoPanelView panelPrefab;

    [Tooltip("面板实例挂点。为空时挂在当前节点下。")]
    [SerializeField] private Transform panelRoot;

    [Header("引用")]
    [SerializeField] private CoinTypeInfoConfig coinTypeInfoConfig;

    [Header("调试")]
    [SerializeField] private bool debugLog;

    private CoinInfoPanelView panelInstance;
    private CoinDefinition currentDefinition;
    private static CoinTypeInfoConfig cachedAutoConfig;

    private void Awake()
    {
        ResolveTypeInfoConfig();
        EnsurePanel();
        Hide();
    }

    private void OnEnable()
    {
        ResolveTypeInfoConfig();
        EnsurePanel();
        Hide();
    }

    private void OnDisable()
    {
        Hide();
    }

    public void Show(CoinDefinition definition)
    {
        if (definition == null)
        {
            Hide();
            return;
        }

        EnsurePanel();
        currentDefinition = definition;

        if (panelInstance == null)
            return;

        if (debugLog)
        {
            Transform root = panelRoot != null ? panelRoot : transform;
            Debug.Log(
                $"[CoinInfoPanelController] 显示说明面板 | controller:{name} | object:{gameObject.name} | " +
                $"coin:{definition.coinName} | root:{root.name} | " +
                $"instanceParent:{(panelInstance.transform.parent != null ? panelInstance.transform.parent.name : "空")}"
            );
        }

        panelInstance.Refresh(currentDefinition, coinTypeInfoConfig);
        panelInstance.SetVisible(true);
    }

    public void Hide()
    {
        currentDefinition = null;

        if (panelInstance != null)
        {
            panelInstance.SetVisible(false);
        }
    }

    private void EnsurePanel()
    {
        if (panelInstance != null || panelPrefab == null)
            return;

        Transform root = panelRoot != null ? panelRoot : transform;
        panelInstance = Instantiate(panelPrefab, root, false);
        panelInstance.name = panelPrefab.name;
        panelInstance.SetVisible(false);

        if (debugLog)
        {
            Debug.Log(
                $"[CoinInfoPanelController] 创建说明面板实例 | controller:{name} | object:{gameObject.name} | " +
                $"root:{root.name} | instance:{panelInstance.name}"
            );
        }
    }

    private void ResolveTypeInfoConfig()
    {
        if (coinTypeInfoConfig != null)
            return;

        if (cachedAutoConfig != null)
        {
            coinTypeInfoConfig = cachedAutoConfig;
            return;
        }

        CoinTypeInfoConfig[] resourceConfigs = Resources.LoadAll<CoinTypeInfoConfig>(string.Empty);
        if (resourceConfigs != null && resourceConfigs.Length > 0)
        {
            cachedAutoConfig = resourceConfigs[0];
            coinTypeInfoConfig = cachedAutoConfig;

            if (resourceConfigs.Length > 1)
            {
                Debug.LogWarning($"[CoinInfoPanelController] Resources 中找到多个 CoinTypeInfoConfig，默认使用第一个 | object:{name}");
            }

            return;
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:CoinTypeInfoConfig");
        if (guids != null && guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            cachedAutoConfig = AssetDatabase.LoadAssetAtPath<CoinTypeInfoConfig>(assetPath);
            coinTypeInfoConfig = cachedAutoConfig;

            if (guids.Length > 1)
            {
                Debug.LogWarning($"[CoinInfoPanelController] 项目中找到多个 CoinTypeInfoConfig，默认使用第一个 | object:{name} | path:{assetPath}");
            }

            return;
        }
#endif

        Debug.LogWarning($"[CoinInfoPanelController] 未找到 CoinTypeInfoConfig | object:{name}");
    }
}
