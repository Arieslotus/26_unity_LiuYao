/// <summary>
/// 实现功能：统一管理游戏开始与结束流程，支持多段 UIPositionEffect 组成的开场退场和结束进场动画组。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameFlowState
{
    WaitingToStart,
    Starting,
    Playing,
    Ended
}

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("开始流程")]
    [Tooltip("进入场景后是否等待玩家点击鼠标左键再开始游戏。")]
    [SerializeField] private bool waitForClickToStart = true;

    [Tooltip("游戏开始时播放退场动画的 UI 组。把每个需要独立参数的子物体 UIPositionEffect 都拖进来。")]
    [SerializeField] private List<UIPositionEffect> startUiEffects = new List<UIPositionEffect>();

    [Header("核心引用")]
    [Tooltip("回合管理器。为空时自动从场景查找。")]
    [SerializeField] private TurnManager turnManager;

    [Header("结束流程")]
    [Tooltip("游戏结束后是否暂停 Time.timeScale。")]
    [SerializeField] private bool pauseTimeOnGameEnd = false;

    [Tooltip("进入场景时是否先隐藏结算 UI。")]
    [SerializeField] private bool hideEndUiOnStart = true;

    [Tooltip("需要整体隐藏/显示的结算 UI 根节点。适合 EndUIGroup 这类父物体。可为空。")]
    [SerializeField] private List<GameObject> endUiRoots = new List<GameObject>();

    [Tooltip("游戏结束时播放进场动画的 UI 组。把每个需要独立参数的子物体 UIPositionEffect 都拖进来。")]
    [SerializeField] private List<UIPositionEffect> endUiEffects = new List<UIPositionEffect>();

    [Tooltip("场景内结束 UI 的结果面板。为空时会尝试从 endUiEffects 中自动获取。")]
    [SerializeField] private GameEndPopup endPopup;

    [Tooltip("可选：没有场景结束 UI 时，由流程控制器打开这个结算弹窗。")]
    [SerializeField] private GameEndPopup gameEndPopupPrefab;

    [Tooltip("可选：指定弹窗管理器。为空时使用 UIPopupManager.Instance。")]
    [SerializeField] private UIPopupManager popupManager;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    [SerializeField, HideInInspector] private UIPositionEffect startUiEffect;
    [SerializeField, HideInInspector] private UIPositionEffect endUiEffect;

    private readonly List<UIPositionEffect> cachedEffects = new List<UIPositionEffect>();
    private GameFlowState state = GameFlowState.WaitingToStart;
    private bool? lastGameResult;

    public GameFlowState State => state;
    public bool IsGameplayActive => state == GameFlowState.Playing;
    public bool CanAcceptGameplayInput => state == GameFlowState.Playing;
    public bool HasGameEnded => state == GameFlowState.Ended;

    public event Action<GameFlowState> StateChanged;
    public event Action<bool> GameEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"[GameFlowController] 场景中存在多个 GameFlowController，销毁重复对象:{name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    private void Start()
    {
        if (hideEndUiOnStart)
        {
            HideEndUIImmediate();
        }

        if (waitForClickToStart)
        {
            SetState(GameFlowState.WaitingToStart);
            return;
        }

        StartGame();
    }

    private void Update()
    {
        if (state != GameFlowState.WaitingToStart)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (state == GameFlowState.Playing || state == GameFlowState.Starting)
            return;

        if (state == GameFlowState.Ended)
        {
            Debug.LogWarning($"[GameFlowController] 游戏已结束，忽略开始请求 | object:{name}");
            return;
        }

        SetState(GameFlowState.Starting);
        PlayExitEffectsAndWait(CompleteStartGame);
    }

    public void EndGame(bool isVictory)
    {
        if (state == GameFlowState.Ended)
            return;

        lastGameResult = isVictory;
        SetState(GameFlowState.Ended);

        if (turnManager != null)
        {
            turnManager.StopGameFlow();
        }

        if (pauseTimeOnGameEnd)
        {
            Time.timeScale = 0f;
        }

        ShowEndUI(isVictory);
        GameEnded?.Invoke(isVictory);

        if (debugLog)
        {
            Debug.Log($"[GameFlowController] 游戏结束 | object:{name} | result:{(isVictory ? "胜利" : "失败")}");
        }
    }

    private void CompleteStartGame()
    {
        if (state != GameFlowState.Starting)
            return;

        SetState(GameFlowState.Playing);

        if (turnManager != null)
        {
            turnManager.StartGameFlow();
            return;
        }

        Debug.LogWarning($"[GameFlowController] 未找到 TurnManager，游戏状态已开始但无法启动回合 | object:{name}");
    }

    private void PlayExitEffectsAndWait(Action onAllComplete)
    {
        CollectStartEffects();

        if (cachedEffects.Count == 0)
        {
            onAllComplete?.Invoke();
            return;
        }

        int remainingCount = cachedEffects.Count;

        for (int i = 0; i < cachedEffects.Count; i++)
        {
            UIPositionEffect effect = cachedEffects[i];
            effect.PlayExit(() =>
            {
                remainingCount--;
                if (remainingCount <= 0)
                {
                    onAllComplete?.Invoke();
                }
            });
        }
    }

    private void ShowEndUI(bool isVictory)
    {
        ShowEndUIRoots();
        ResolveEndPopupFromSceneUI();

        if (endPopup != null)
        {
            endPopup.SetResult(isVictory);
        }

        bool hasSceneEndUI = PlayEnterEffects();
        if (!hasSceneEndUI)
        {
            OpenEndPopup(isVictory);
        }
    }

    private void HideEndUIImmediate()
    {
        bool hasRoot = false;

        if (endUiRoots != null)
        {
            for (int i = 0; i < endUiRoots.Count; i++)
            {
                GameObject root = endUiRoots[i];
                if (root == null)
                    continue;

                hasRoot = true;
                root.SetActive(false);
            }
        }

        if (hasRoot)
            return;

        CollectEndEffects();

        for (int i = 0; i < cachedEffects.Count; i++)
        {
            cachedEffects[i].gameObject.SetActive(false);
        }
    }

    private void ShowEndUIRoots()
    {
        if (endUiRoots == null)
            return;

        for (int i = 0; i < endUiRoots.Count; i++)
        {
            GameObject root = endUiRoots[i];
            if (root != null)
            {
                root.SetActive(true);
            }
        }
    }

    private bool PlayEnterEffects()
    {
        CollectEndEffects();

        if (cachedEffects.Count == 0)
            return false;

        for (int i = 0; i < cachedEffects.Count; i++)
        {
            cachedEffects[i].PlayEnter();
        }

        return true;
    }

    private void OpenEndPopup(bool isVictory)
    {
        if (gameEndPopupPrefab == null)
            return;

        UIPopupManager manager = popupManager != null ? popupManager : UIPopupManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[GameFlowController] 游戏结束但场景中没有 UIPopupManager，无法打开结算弹窗 | object:{name}");
            return;
        }

        GameEndPopup popup = manager.Open(gameEndPopupPrefab);
        if (popup != null)
        {
            popup.SetResult(isVictory);
        }
    }

    private void ResolveReferences()
    {
        if (turnManager == null)
        {
            turnManager = FindObjectOfType<TurnManager>();
        }

        ResolveEndPopupFromSceneUI();
    }

    private void ResolveEndPopupFromSceneUI()
    {
        if (endPopup != null)
            return;

        CollectEndEffects();

        for (int i = 0; i < cachedEffects.Count; i++)
        {
            endPopup = cachedEffects[i].GetComponent<GameEndPopup>();
            if (endPopup != null)
                return;
        }
    }

    private void CollectStartEffects()
    {
        CollectEffects(startUiEffects, startUiEffect);
    }

    private void CollectEndEffects()
    {
        CollectEffects(endUiEffects, endUiEffect);
    }

    private void CollectEffects(List<UIPositionEffect> effects, UIPositionEffect legacyEffect)
    {
        cachedEffects.Clear();

        if (effects != null)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                AddEffectIfValid(effects[i]);
            }
        }

        AddEffectIfValid(legacyEffect);
    }

    private void AddEffectIfValid(UIPositionEffect effect)
    {
        if (effect == null)
            return;

        if (cachedEffects.Contains(effect))
            return;

        cachedEffects.Add(effect);
    }

    private void SetState(GameFlowState newState)
    {
        if (state == newState)
            return;

        state = newState;
        StateChanged?.Invoke(state);

        if (debugLog)
        {
            Debug.Log($"[GameFlowController] 状态切换 | object:{name} | state:{state} | lastResult:{lastGameResult}");
        }
    }
}
