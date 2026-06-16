/// <summary>
/// 实现功能：显示单枚硬币的当前状态信息，包括名称、当前面 Sprite、背面 Sprite、剩余完整度与损耗小块。
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CoinLossBlockDirection
{
    LeftToRight,
    RightToLeft
}

public enum CoinLossBlockDisplayMode
{
    Replace,
    Overlay
}

public class CoinTurnInfoItem : MonoBehaviour
{
    [Header("显示组件")]
    [SerializeField] private Text coinNameText;
    [SerializeField] private Image currentSideImage;
    [SerializeField] private Image backSideImage;

    [Tooltip("硬币本回合碎裂时显示的 UI 根节点。")]
    [SerializeField] private GameObject brokenStateRoot;

    [Tooltip("当前硬币攻击力文本，可选。")]
    [SerializeField] private Text attackText;

    [Header("损耗小块")]
    [Tooltip("损耗小块父节点，可挂 HorizontalLayoutGroup 或 GridLayoutGroup 自动排列与对齐。")]
    [SerializeField] private Transform lossBlockRoot;

    [Tooltip("单个损耗小块的完整形态 Image 模板。运行时会根据 MaxLoss 自动复用/生成。")]
    [SerializeField] private Image lossBlockTemplate;

    [Tooltip("完整形态小块 Sprite。")]
    [SerializeField] private Sprite intactBlockSprite;

    [Tooltip("损耗形态小块 Sprite。")]
    [SerializeField] private Sprite damagedBlockSprite;

    [Tooltip("损耗值增加时，小块从哪个方向开始变为损耗形态。")]
    [SerializeField] private CoinLossBlockDirection lossBlockDirection = CoinLossBlockDirection.LeftToRight;

    [Tooltip("Replace：直接替换 Sprite。Overlay：保留完整图，并叠加损耗图。")]
    [SerializeField] private CoinLossBlockDisplayMode lossBlockDisplayMode = CoinLossBlockDisplayMode.Replace;

    [Tooltip("缺少 CoinStats 时是否隐藏损耗小块。")]
    [SerializeField] private bool hideLossBlocksWhenMissingStats = true;

    [Header("透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float activeAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float actedAlpha = 0.6f;

    private readonly List<LossBlockView> lossBlocks = new List<LossBlockView>();
    private CoinStats currentStats;
    private CoinRoundEffectManager subscribedRoundEffectManager;
    private float currentAlpha = 1f;
    private bool isBrokenThisRound;

    public void Set(ChessPiece piece, bool hasActed)
    {
        SubscribeRoundEffectManager();

        CoinDefinition definition = piece != null ? piece.CoinDefinition : null;
        bool isFrontSide = piece == null || piece.IsFrontSide;

        if (coinNameText != null)
        {
            coinNameText.text = definition != null && !string.IsNullOrWhiteSpace(definition.coinName)
                ? definition.coinName
                : "未知硬币";
        }

        Sprite currentSprite = null;
        Sprite backSprite = null;

        if (definition != null)
        {
            currentSprite = isFrontSide ? definition.frontSprite : definition.backSprite;
            backSprite = isFrontSide ? definition.backSprite : definition.frontSprite;
        }

        SetImage(currentSideImage, currentSprite);
        SetImage(backSideImage, backSprite);
        BindStats(piece != null ? piece.GetComponent<CoinStats>() : null);
        RefreshAttack();
        RefreshLossDisplay();
        RefreshBrokenState();
        ApplyAlpha(hasActed ? actedAlpha : activeAlpha);
    }

    private void OnEnable()
    {
        SubscribeRoundEffectManager();
    }

    private void OnDisable()
    {
        UnsubscribeRoundEffectManager();
        UnbindStats();
    }

    private void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private void ApplyAlpha(float alpha)
    {
        currentAlpha = alpha;
        SetGraphicAlpha(coinNameText, alpha);
        SetGraphicAlpha(currentSideImage, alpha);
        SetGraphicAlpha(backSideImage, alpha);
        SetGraphicAlpha(attackText, alpha);
        ApplyLossBlockAlpha(alpha);
    }

    private void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private void BindStats(CoinStats stats)
    {
        if (currentStats == stats)
            return;

        UnbindStats();
        currentStats = stats;

        if (currentStats != null)
        {
            if (attackText != null)
                RefreshAttack();
            currentStats.LossChanged -= OnLossChanged;
            currentStats.Broken -= OnBroken;
            currentStats.LossChanged += OnLossChanged;
            currentStats.Broken += OnBroken;
        }
    }

    private void UnbindStats()
    {
        if (currentStats != null)
        {
            currentStats.LossChanged -= OnLossChanged;
            currentStats.Broken -= OnBroken;
            currentStats = null;
        }
    }

    private void SubscribeRoundEffectManager()
    {
        if (subscribedRoundEffectManager == CoinRoundEffectManager.Instance)
            return;

        UnsubscribeRoundEffectManager();
        subscribedRoundEffectManager = CoinRoundEffectManager.Instance;
        if (subscribedRoundEffectManager == null)
            return;

        subscribedRoundEffectManager.DamageModifierStarted += OnDamageModifierChanged;
        subscribedRoundEffectManager.DamageModifierEnded += OnDamageModifierChanged;
        subscribedRoundEffectManager.RuntimeEffectsChanged += OnRuntimeEffectsChanged;
    }

    private void UnsubscribeRoundEffectManager()
    {
        if (subscribedRoundEffectManager == null)
            return;

        subscribedRoundEffectManager.DamageModifierStarted -= OnDamageModifierChanged;
        subscribedRoundEffectManager.DamageModifierEnded -= OnDamageModifierChanged;
        subscribedRoundEffectManager.RuntimeEffectsChanged -= OnRuntimeEffectsChanged;
        subscribedRoundEffectManager = null;
    }

    private void OnDamageModifierChanged(int modifierId, string sourceId)
    {
        RefreshAttack();
    }

    private void OnRuntimeEffectsChanged()
    {
        RefreshAttack();
    }

    private void OnLossChanged(int currentLoss, int maxLoss)
    {
        RefreshLossDisplay(currentLoss, maxLoss);
        RefreshAttack();
    }

    private void OnBroken()
    {
        isBrokenThisRound = true;
        RefreshBrokenState();
        RefreshAttack();
    }

    public void ClearRoundState()
    {
        isBrokenThisRound = false;
        RefreshBrokenState();
    }

    private void RefreshLossDisplay()
    {
        if (currentStats == null)
        {
            SetLossBlocksVisible(!hideLossBlocksWhenMissingStats);
            RefreshLossDisplay(0, 1);
            return;
        }

        SetLossBlocksVisible(true);
        RefreshLossDisplay(currentStats.CurrentLoss, currentStats.MaxLoss);
    }

    private void RefreshAttack()
    {
        if (attackText == null)
            return;

        attackText.gameObject.SetActive(currentStats != null);
        if (currentStats != null)
        {
            attackText.text = $"攻击:{CoinDamageCalculator.Calculate(currentStats)}";
        }
    }

    private void RefreshLossDisplay(int currentLoss, int maxLoss)
    {
        maxLoss = Mathf.Max(1, maxLoss);
        currentLoss = Mathf.Clamp(currentLoss, 0, maxLoss);

        RefreshLossBlocks(currentLoss, maxLoss);
        ApplyAlpha(currentAlpha);
    }

    private void RefreshLossBlocks(int currentLoss, int maxLoss)
    {
        if (lossBlockRoot == null || lossBlockTemplate == null)
            return;

        EnsureLossBlockCount(maxLoss);

        for (int i = 0; i < lossBlocks.Count; i++)
        {
            LossBlockView block = lossBlocks[i];
            bool active = i < maxLoss;

            if (block.Root != null)
            {
                block.Root.SetActive(active);
            }

            if (!active)
                continue;

            int lossIndex = lossBlockDirection == CoinLossBlockDirection.LeftToRight
                ? i
                : maxLoss - 1 - i;
            bool damaged = lossIndex < currentLoss;
            RefreshLossBlock(block, damaged);
        }
    }

    private void EnsureLossBlockCount(int maxLoss)
    {
        for (int i = lossBlocks.Count; i < maxLoss; i++)
        {
            Image intactImage = i == 0 ? lossBlockTemplate : Instantiate(lossBlockTemplate, lossBlockRoot);
            if (intactImage.transform.parent != lossBlockRoot)
            {
                intactImage.transform.SetParent(lossBlockRoot, false);
            }

            GameObject root = intactImage.gameObject;
            Image damagedImage = GetOrCreateDamagedImage(root.transform);

            lossBlocks.Add(new LossBlockView
            {
                Root = root,
                IntactImage = intactImage,
                DamagedImage = damagedImage
            });
        }
    }

    private Image GetOrCreateDamagedImage(Transform blockRoot)
    {
        Transform damagedChild = blockRoot.Find("Damaged");
        if (damagedChild != null)
        {
            Image existingImage = damagedChild.GetComponent<Image>();
            if (existingImage != null)
                return existingImage;
        }

        GameObject damagedObject = new GameObject("Damaged", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        damagedObject.transform.SetParent(blockRoot, false);

        RectTransform rectTransform = damagedObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = damagedObject.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private void RefreshLossBlock(LossBlockView block, bool damaged)
    {
        if (block == null)
            return;

        if (block.IntactImage != null)
        {
            block.IntactImage.sprite = lossBlockDisplayMode == CoinLossBlockDisplayMode.Replace && damaged
                ? damagedBlockSprite
                : intactBlockSprite;
            block.IntactImage.enabled = block.IntactImage.sprite != null;
        }

        if (block.DamagedImage != null)
        {
            block.DamagedImage.sprite = damagedBlockSprite;
            block.DamagedImage.enabled = lossBlockDisplayMode == CoinLossBlockDisplayMode.Overlay &&
                                         damaged &&
                                         damagedBlockSprite != null;
        }
    }

    private void ApplyLossBlockAlpha(float alpha)
    {
        for (int i = 0; i < lossBlocks.Count; i++)
        {
            LossBlockView block = lossBlocks[i];
            if (block == null)
                continue;

            SetGraphicAlpha(block.IntactImage, alpha);
            SetGraphicAlpha(block.DamagedImage, alpha);
        }
    }

    private void SetLossBlocksVisible(bool visible)
    {
        if (lossBlockRoot != null)
        {
            lossBlockRoot.gameObject.SetActive(visible);
        }
    }

    private void RefreshBrokenState()
    {
        if (brokenStateRoot != null)
        {
            brokenStateRoot.SetActive(isBrokenThisRound);
        }
    }

    private sealed class LossBlockView
    {
        public GameObject Root;
        public Image IntactImage;
        public Image DamagedImage;
    }
}
