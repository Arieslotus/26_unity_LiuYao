/// <summary>
/// 实现功能：鼠标悬停敌人时实例化并显示敌人属性面板，根据敌人在屏幕左右侧切换面板方向。
/// </summary>
using UnityEngine;

public class EnemyInfoPanelController : MonoBehaviour
{
    [Header("面板预制体")]
    [Tooltip("敌人在屏幕右侧时显示的左侧面板预制体。面板内部 Text/Image 引用在预制体内配置。")]
    [SerializeField] private EnemyInfoPanelView leftPanelPrefab;

    [Tooltip("敌人在屏幕左侧时显示的右侧面板预制体。面板内部 Text/Image 引用在预制体内配置。")]
    [SerializeField] private EnemyInfoPanelView rightPanelPrefab;

    [Tooltip("面板实例挂点。为空时挂在当前敌人节点下。")]
    [SerializeField] private Transform panelRoot;

    [Header("引用")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private TrigramVisualDatabase trigramVisualDatabase;
    [SerializeField] private EnemyStats stats;
    [SerializeField] private EnemyController controller;
    [SerializeField] private EnemyShieldController shieldController;

    private bool isHovering;
    private EnemyInfoPanelView leftPanelInstance;
    private EnemyInfoPanelView rightPanelInstance;
    private EnemyInfoPanelView activePanel;

    private void Awake()
    {
        ResolveReferences();
        EnsurePanels();
        HidePanels();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsurePanels();
        HidePanels();
    }

    private void OnDisable()
    {
        isHovering = false;
        HidePanels();
    }

    private void Update()
    {
        EnsurePanels();

        bool hoveringNow = IsPointerOverThisEnemy();
        if (hoveringNow != isHovering)
        {
            isHovering = hoveringNow;

            if (!isHovering)
            {
                HidePanels();
                return;
            }
        }

        if (!isHovering)
            return;

        ShowPreferredPanel();
        RefreshActivePanel();
    }

    private void ResolveReferences()
    {
        if (stats == null)
        {
            stats = GetComponentInParent<EnemyStats>();
        }

        if (controller == null)
        {
            controller = GetComponentInParent<EnemyController>();
        }

        if (shieldController == null)
        {
            shieldController = GetComponentInChildren<EnemyShieldController>(true);
        }

        if (shieldController == null)
        {
            shieldController = GetComponentInParent<EnemyShieldController>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (panelRoot == null)
        {
            panelRoot = transform;
        }
    }

    private void EnsurePanels()
    {
        Transform root = panelRoot != null ? panelRoot : transform;

        if (leftPanelInstance == null && leftPanelPrefab != null)
        {
            leftPanelInstance = Instantiate(leftPanelPrefab, root, false);
            leftPanelInstance.name = leftPanelPrefab.name;
            leftPanelInstance.SetVisible(false);
        }

        if (rightPanelInstance == null && rightPanelPrefab != null)
        {
            rightPanelInstance = Instantiate(rightPanelPrefab, root, false);
            rightPanelInstance.name = rightPanelPrefab.name;
            rightPanelInstance.SetVisible(false);
        }
    }

    private bool IsPointerOverThisEnemy()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null)
            return false;

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        float nearestDistance = float.MaxValue;
        EnemyInfoPanelController nearestPanel = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            EnemyInfoPanelController panel = hitCollider.GetComponentInParent<EnemyInfoPanelController>();
            if (panel == null)
                continue;

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                nearestPanel = panel;
            }
        }

        return nearestPanel == this;
    }

    private void ShowPreferredPanel()
    {
        EnemyInfoPanelView preferredPanel = GetPreferredPanel();
        if (activePanel == preferredPanel)
            return;

        HidePanels();
        activePanel = preferredPanel;

        if (activePanel != null)
        {
            activePanel.SetVisible(true);
        }
    }

    private EnemyInfoPanelView GetPreferredPanel()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null)
            return rightPanelInstance;

        Vector3 viewportPosition = cameraToUse.WorldToViewportPoint(transform.position);
        return viewportPosition.x < 0.5f ? rightPanelInstance : leftPanelInstance;
    }

    private void RefreshActivePanel()
    {
        if (activePanel == null)
            return;

        activePanel.Refresh(stats, controller, shieldController, trigramVisualDatabase);
    }

    private void HidePanels()
    {
        if (leftPanelInstance != null)
        {
            leftPanelInstance.SetVisible(false);
        }

        if (rightPanelInstance != null)
        {
            rightPanelInstance.SetVisible(false);
        }

        activePanel = null;
    }
}
