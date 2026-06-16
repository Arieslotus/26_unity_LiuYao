/// <summary>
/// 实现功能：监听游戏结束事件，显示结算 UI 根物体、设置胜负结果，并自动播放根物体上的 Animator/Animation。
/// </summary>
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameEndUIController : MonoBehaviour
{
    [Serializable]
    private sealed class ResultImageEntry
    {
        [Tooltip("endUiRoot 下需要根据胜负切换图片的 Image。")]
        public Image image;

        [Tooltip("胜利时显示的图片。")]
        public Sprite victorySprite;

        [Tooltip("失败时显示的图片。")]
        public Sprite defeatSprite;
    }

    [Header("流程引用")]
    [Tooltip("游戏流程控制器。为空时自动使用 GameFlowController.Instance。")]
    [SerializeField] private GameFlowController flowController;

    [Header("结算 UI")]
    [Tooltip("进入场景时是否先隐藏结算 UI。")]
    [SerializeField] private bool hideEndUiOnStart = true;

    [Tooltip("结算 UI 的根物体。Animator 或 Animation 组件应挂在这个根物体上。")]
    [SerializeField] private GameObject endUiRoot;

    [Tooltip("播放完成后是否隐藏结算 UI 根物体。结算界面通常建议保持关闭。")]
    [SerializeField] private bool hideAfterAnimation = false;

    [Tooltip("场景内结算 UI 的结果面板。为空时会尝试从结算 UI 根物体下自动获取。")]
    [SerializeField] private GameEndPopup endPopup;

    [Tooltip("endUiRoot 下需要根据胜负动态替换的 Image。")]
    [SerializeField] private List<ResultImageEntry> resultImages = new List<ResultImageEntry>();

    [Tooltip("可选：没有场景结算 UI 时打开这个结算弹窗。")]
    [SerializeField] private GameEndPopup gameEndPopupPrefab;

    [Tooltip("可选：指定弹窗管理器。为空时使用 UIPopupManager.Instance。")]
    [SerializeField] private UIPopupManager popupManager;

    [Header("等待")]
    [Tooltip("等待动画时是否使用不受 Time.timeScale 影响的时间。")]
    [SerializeField] private bool useUnscaledWait = true;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private bool hasSubscribed;
    private Coroutine endCoroutine;
    private GameEndPopup[] sceneEndPopups = new GameEndPopup[0];

    private void Awake()
    {
        ResolveFlowController();
        ResolveEndPopupFromSceneUI();
        UIAnimationRootPlayer.PrepareWithoutPlaying(endUiRoot);
    }

    private void Start()
    {
        SubscribeFlow();

        if (hideEndUiOnStart)
        {
            HideEndUIImmediate();
        }

        if (flowController != null && flowController.HasGameEnded && flowController.LastGameResult.HasValue)
        {
            ShowEndUI(flowController.LastGameResult.Value);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFlow();
    }

    private void SubscribeFlow()
    {
        if (hasSubscribed)
            return;

        ResolveFlowController();
        if (flowController == null)
        {
            Debug.LogWarning($"[GameEndUIController] 未找到 GameFlowController，无法监听游戏结束事件 | object:{name}");
            return;
        }

        flowController.GameEnded -= OnGameEnded;
        flowController.GameEnded += OnGameEnded;
        hasSubscribed = true;
    }

    private void UnsubscribeFlow()
    {
        if (!hasSubscribed || flowController == null)
            return;

        flowController.GameEnded -= OnGameEnded;
        hasSubscribed = false;
    }

    private void OnGameEnded(bool isVictory)
    {
        ShowEndUI(isVictory);
    }

    private void ShowEndUI(bool isVictory)
    {
        if (endCoroutine != null)
        {
            StopCoroutine(endCoroutine);
        }

        endCoroutine = StartCoroutine(ShowEndUICoroutine(isVictory));
    }

    private IEnumerator ShowEndUICoroutine(bool isVictory)
    {
        if (endUiRoot != null)
        {
            endUiRoot.SetActive(true);
        }

        ResolveEndPopupFromSceneUI();

        ApplyResultImages(isVictory);
        ApplyResultToSceneEndPopups(isVictory);

        bool hasSceneEndUI = endUiRoot != null;
        bool played = false;

        if (hasSceneEndUI)
        {
            played = UIAnimationRootPlayer.Play(endUiRoot, useUnscaledWait, nameof(GameEndUIController));
        }
        else
        {
            OpenEndPopup(isVictory);
        }

        if (debugLog)
        {
            Debug.Log($"[GameEndUIController] 显示结算 UI | object:{name} | root:{GetRootName(endUiRoot)} | result:{(isVictory ? "胜利" : "失败")} | played:{played}");
        }

        if (played)
        {
            yield return UIAnimationRootPlayer.WaitUntilComplete(endUiRoot, useUnscaledWait, nameof(GameEndUIController));
        }

        if (hideAfterAnimation && endUiRoot != null)
        {
            endUiRoot.SetActive(false);
        }

        endCoroutine = null;
    }

    private void HideEndUIImmediate()
    {
        UIAnimationRootPlayer.PrepareWithoutPlaying(endUiRoot);

        if (endUiRoot != null)
        {
            endUiRoot.SetActive(false);
        }
    }

    private void OpenEndPopup(bool isVictory)
    {
        if (gameEndPopupPrefab == null)
            return;

        UIPopupManager manager = popupManager != null ? popupManager : UIPopupManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[GameEndUIController] 游戏结束但场景中没有 UIPopupManager，无法打开结算弹窗 | object:{name}");
            return;
        }

        GameEndPopup popup = manager.Open(gameEndPopupPrefab);
        if (popup != null)
        {
            popup.SetResult(isVictory);
        }
    }

    private void ResolveEndPopupFromSceneUI()
    {
        if (endUiRoot == null)
            return;

        sceneEndPopups = endUiRoot.GetComponentsInChildren<GameEndPopup>(true);
        if (endPopup == null && sceneEndPopups.Length > 0)
        {
            endPopup = sceneEndPopups[0];
        }
    }

    private void ApplyResultToSceneEndPopups(bool isVictory)
    {
        bool appliedAny = false;

        if (sceneEndPopups != null)
        {
            for (int i = 0; i < sceneEndPopups.Length; i++)
            {
                GameEndPopup popup = sceneEndPopups[i];
                if (popup == null)
                    continue;

                popup.SetResult(isVictory);
                appliedAny = true;
            }
        }

        if (!appliedAny && endPopup != null)
        {
            endPopup.SetResult(isVictory);
        }
    }

    private void ApplyResultImages(bool isVictory)
    {
        for (int i = 0; i < resultImages.Count; i++)
        {
            ResultImageEntry entry = resultImages[i];
            if (entry == null || entry.image == null)
                continue;

            Sprite resultSprite = isVictory ? entry.victorySprite : entry.defeatSprite;
            if (resultSprite == null)
            {
                Debug.LogWarning(
                    $"[GameEndUIController] 未配置 {(isVictory ? "胜利" : "失败")} 结算图片 | object:{name} | image:{entry.image.name}");
            }

            entry.image.sprite = resultSprite;
            entry.image.enabled = resultSprite != null;
        }
    }

    private void ResolveFlowController()
    {
        if (flowController == null)
        {
            flowController = GameFlowController.Instance;
        }

        if (flowController == null)
        {
            flowController = FindObjectOfType<GameFlowController>();
        }
    }

    private static string GetRootName(GameObject root)
    {
        return root != null ? root.name : "None";
    }
}
