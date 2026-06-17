/// <summary>
/// 实现功能：管理开局流程中的提示文字、确认按钮与跳过按钮输入状态。
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using System;

public class OpeningUIController : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("开场 UI 根物体。可为空，为空时只控制下方独立引用。")]
    [SerializeField] private GameObject uiRoot;

    [Tooltip("选择阶段 UI 根物体。可为空。")]
    [SerializeField] private GameObject selectionRoot;

    [Tooltip("开场提示文字。")]
    [SerializeField] private Text hintText;

    [Tooltip("选择确认按钮。")]
    [SerializeField] private Button confirmButton;

    [Tooltip("跳过按钮。")]
    [SerializeField] private Button skipButton;

    [Header("提示文字")]
    [SerializeField] private string selectionHintFormat = "请选择 3 枚开局硬币（已选 {0}/{1}）";
    [SerializeField] private string notEnoughSelectionFormat = "还需要选择 {0} 枚硬币";
    [SerializeField] private string maxSelectionHint = "最多只能选择 3 枚硬币";

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private bool confirmRequested;
    private bool skipRequested;

    public bool ConfirmRequested => confirmRequested;
    public bool SkipRequested => skipRequested;

    public event Action SkipButtonClicked;

    private void Awake()
    {
        BindButtons();
        HideSelection();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(RequestConfirm);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(RequestSkip);
        }
    }

    public void ShowOpeningUI()
    {
        if (uiRoot != null)
        {
            uiRoot.SetActive(true);
        }

        SetConfirmVisible(false);
    }

    public void HideOpeningUI()
    {
        HideSelection();

        if (uiRoot != null)
        {
            uiRoot.SetActive(false);
        }
    }

    public void ShowSelection(int selectedCount, int requiredCount)
    {
        confirmRequested = false;

        ShowOpeningUI();

        if (selectionRoot != null)
        {
            selectionRoot.SetActive(true);
        }

        SetConfirmVisible(true);
        UpdateSelection(selectedCount, requiredCount);
    }

    public void HideSelection()
    {
        confirmRequested = false;

        if (selectionRoot != null)
        {
            selectionRoot.SetActive(false);
        }

        SetConfirmVisible(false);
        SetConfirmInteractable(false);
    }

    public void UpdateSelection(int selectedCount, int requiredCount)
    {
        SetHint(string.Format(selectionHintFormat, selectedCount, requiredCount));
        SetConfirmInteractable(selectedCount >= requiredCount);
    }

    public void ShowNotEnoughSelection(int selectedCount, int requiredCount)
    {
        int missingCount = Mathf.Max(0, requiredCount - selectedCount);
        SetHint(string.Format(notEnoughSelectionFormat, missingCount));
        SetConfirmInteractable(selectedCount >= requiredCount);
    }

    public void ShowMaxSelectionHint()
    {
        SetHint(maxSelectionHint);
    }

    public void SetHint(string text)
    {
        if (hintText != null)
        {
            hintText.text = text;
        }
    }

    public bool ConsumeConfirmRequest()
    {
        if (!confirmRequested)
            return false;

        confirmRequested = false;
        return true;
    }

    public bool ConsumeSkipRequest()
    {
        if (!skipRequested)
            return false;

        skipRequested = false;
        return true;
    }

    public void ResetRequests()
    {
        confirmRequested = false;
        skipRequested = false;
    }

    public void SetSkipVisible(bool visible)
    {
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(visible);
        }
    }

    private void BindButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(RequestConfirm);
            confirmButton.onClick.AddListener(RequestConfirm);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(RequestSkip);
            skipButton.onClick.AddListener(RequestSkip);
        }
    }

    private void SetConfirmInteractable(bool interactable)
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = interactable;
        }
    }

    private void SetConfirmVisible(bool visible)
    {
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(visible);
        }
    }

    public void RequestConfirm()
    {
        confirmRequested = true;

        if (debugLog)
        {
            Debug.Log($"[OpeningUIController] 玩家点击确认 | object:{name}");
        }
    }

    public void RequestSkip()
    {
        skipRequested = true;
        SkipButtonClicked?.Invoke();

        if (debugLog)
        {
            Debug.Log($"[OpeningUIController] 玩家点击跳过 | object:{name}");
        }
    }
}
