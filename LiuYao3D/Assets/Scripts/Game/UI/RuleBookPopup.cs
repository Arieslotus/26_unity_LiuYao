/// <summary>
/// 实现功能：显示规则说明书弹窗，支持按顺序切换说明图片并刷新页码与箭头状态。
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class RuleBookPopup : UIPopupBase
{
    [Header("配置")]
    [Tooltip("规则说明书页面配置。")]
    [SerializeField] private RuleBookConfigSO ruleBookConfig;

    [Header("UI")]
    [Tooltip("用于显示当前说明页图片的 Image。")]
    [SerializeField] private Image pageImage;

    [Tooltip("向左翻页按钮。")]
    [SerializeField] private Button previousButton;

    [Tooltip("向右翻页按钮。")]
    [SerializeField] private Button nextButton;

    [Tooltip("页码文本，例如 1/4。")]
    [SerializeField] private Text pageNumberText;

    [Header("图片显示")]
    [Tooltip("是否保持说明图片原始宽高比。")]
    [SerializeField] private bool preserveAspect = true;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private int currentPageIndex;

    protected override bool ShouldLockGameplayInputOnOpen => true;

    private void Awake()
    {
        BindButtons();
    }

    private void Reset()
    {
        pageImage = GetComponentInChildren<Image>(true);
    }

    protected override void OnOpen()
    {
        ShowPage(0);
    }

    private void BindButtons()
    {
        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(ShowPreviousPage);
            previousButton.onClick.AddListener(ShowPreviousPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextPage);
            nextButton.onClick.AddListener(ShowNextPage);
        }
    }

    public void ShowPreviousPage()
    {
        ShowPage(currentPageIndex - 1);
    }

    public void ShowNextPage()
    {
        ShowPage(currentPageIndex + 1);
    }

    private void ShowPage(int pageIndex)
    {
        int pageCount = ruleBookConfig != null ? ruleBookConfig.PageCount : 0;
        if (pageCount <= 0)
        {
            currentPageIndex = 0;
            ApplyPageSprite(null);
            RefreshPageNumber(0, 0);
            RefreshArrowButtons(0, 0);
            Debug.LogWarning($"[RuleBookPopup] 规则说明书没有可显示页面 | popup:{name} | config:{GetConfigName()}");
            return;
        }

        currentPageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        Sprite pageSprite = ruleBookConfig.GetPageSprite(currentPageIndex);

        ApplyPageSprite(pageSprite);
        RefreshPageNumber(currentPageIndex + 1, pageCount);
        RefreshArrowButtons(currentPageIndex, pageCount);

        if (debugLog)
        {
            Debug.Log($"[RuleBookPopup] 切换规则说明页 | popup:{name} | page:{currentPageIndex + 1}/{pageCount} | sprite:{GetSpriteName(pageSprite)}");
        }
    }

    private void ApplyPageSprite(Sprite pageSprite)
    {
        if (pageImage == null)
        {
            Debug.LogWarning($"[RuleBookPopup] 未绑定说明图片 Image，无法显示规则说明页 | popup:{name}");
            return;
        }

        pageImage.sprite = pageSprite;
        pageImage.enabled = pageSprite != null;
        pageImage.preserveAspect = preserveAspect;
    }

    private void RefreshPageNumber(int currentPage, int pageCount)
    {
        if (pageNumberText == null)
            return;

        pageNumberText.text = $"{currentPage}/{pageCount}";
    }

    private void RefreshArrowButtons(int pageIndex, int pageCount)
    {
        bool hasPages = pageCount > 0;

        if (previousButton != null)
        {
            previousButton.interactable = hasPages && pageIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = hasPages && pageIndex < pageCount - 1;
        }
    }

    private string GetConfigName()
    {
        return ruleBookConfig != null ? ruleBookConfig.name : "None";
    }

    private static string GetSpriteName(Sprite sprite)
    {
        return sprite != null ? sprite.name : "None";
    }
}
