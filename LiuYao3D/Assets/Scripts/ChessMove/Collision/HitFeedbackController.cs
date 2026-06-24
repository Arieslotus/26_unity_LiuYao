/// <summary>
/// 实现功能：监听棋子的命中反馈事件，并触发简单的全局慢动作/短暂停顿效果。
/// 挂在场景中的任意管理物体上
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class HitFeedbackController : MonoBehaviour
{
    public static HitFeedbackController Instance { get; private set; }

    [Header("配置")]
    [Tooltip("命中反馈配置 SO")]
    [SerializeField] private HitFeedbackConfig config;

    [Header("自动订阅")]
    [Tooltip("启动时自动搜索场景中的所有 ChessPiece 并订阅事件")]
    [SerializeField] private bool autoFindPiecesOnStart = true;

    [Tooltip("手动指定需要监听的棋子；若开启自动搜索，也会补充进来")]
    [SerializeField] private List<ChessPiece> pieces = new List<ChessPiece>();

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private float defaultFixedDeltaTime;
    private float pauseTimer = 0f;
    private float currentPauseTimeScale = 1f;
    private bool isPaused = false;
    private CoinRosterManager subscribedRosterManager;
    private float nextRefreshRetryTime;
    private const float RefreshRetryInterval = 0.25f;

    public bool IsFeedbackActive => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[HitFeedbackController] 场景中存在多个反馈控制器，当前对象:{name}。");
        }

        Instance = this;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("[HitFeedbackController] 未绑定 HitFeedbackConfig，反馈系统无法正常工作。");
            return;
        }

        SubscribeRosterManager();
        RefreshPieceSubscriptions("Start");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        UnsubscribeRosterManager();
        UnregisterAllPieces();
        RestoreTimeScale();
    }

    public bool TryGetPreImpactLookAheadTime(CollisionType targetType, out float lookAheadTime)
    {
        lookAheadTime = 0f;

        if (config == null || !config.enableHitPause)
            return false;

        switch (targetType)
        {
            case CollisionType.Enemy:
                if (!config.enableEnemyPreImpactSlowMotion)
                    return false;

                lookAheadTime = config.enemyPreImpactLookAheadTime;
                return lookAheadTime > 0f;

            case CollisionType.PlayerCoin:
                if (!config.enableCoinPreImpactSlowMotion)
                    return false;

                lookAheadTime = config.coinPreImpactLookAheadTime;
                return lookAheadTime > 0f;

            default:
                return false;
        }
    }

    public float GetPreImpactFeedbackDuration(CollisionType targetType)
    {
        if (config == null || !config.enableHitPause)
            return 0f;

        switch (targetType)
        {
            case CollisionType.Enemy:
                if (!config.enableEnemyPreImpactSlowMotion)
                    return 0f;

                return CalculatePreImpactDuration(
                    config.enemyPreImpactLookAheadTime,
                    config.enemyPreImpactTimeScale
                );

            case CollisionType.PlayerCoin:
                if (!config.enableCoinPreImpactSlowMotion)
                    return 0f;

                return CalculatePreImpactDuration(
                    config.coinPreImpactLookAheadTime,
                    config.coinPreImpactTimeScale
                ) + config.coinHitPauseDuration;

            default:
                return 0f;
        }
    }

    private void Update()
    {
        if (subscribedRosterManager == null)
        {
            SubscribeRosterManager();
        }

        if (config != null && pieces.Count == 0 && Time.unscaledTime >= nextRefreshRetryTime)
        {
            nextRefreshRetryTime = Time.unscaledTime + RefreshRetryInterval;
            RefreshPieceSubscriptions("Retry");
        }

        if (config != null && !config.enableHitPause)
        {
            if (isPaused)
            {
                RestoreTimeScale();
            }
            return;
        }

        if (!isPaused)
            return;

        pauseTimer -= Time.unscaledDeltaTime;

        if (pauseTimer <= 0f)
        {
            RestoreTimeScale();
        }
    }

    private void RefreshPieceSubscriptions(string reason)
    {
        HashSet<ChessPiece> set = new HashSet<ChessPiece>();

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] != null)
                set.Add(pieces[i]);
        }

        if (autoFindPiecesOnStart)
        {
            ChessTurnController turnController = FindObjectOfType<ChessTurnController>();
            if (turnController != null && turnController.Pieces != null)
            {
                IReadOnlyList<ChessPiece> turnPieces = turnController.Pieces;
                for (int i = 0; i < turnPieces.Count; i++)
                {
                    if (turnPieces[i] != null)
                        set.Add(turnPieces[i]);
                }
            }

            CoinRosterManager rosterManager = CoinRosterManager.Instance;
            if (rosterManager != null && rosterManager.CoinSlots != null)
            {
                IReadOnlyList<ChessPiece> rosterPieces = rosterManager.CoinSlots;
                for (int i = 0; i < rosterPieces.Count; i++)
                {
                    if (rosterPieces[i] != null)
                        set.Add(rosterPieces[i]);
                }
            }

#if UNITY_2023_1_OR_NEWER
            ChessPiece[] foundPieces = FindObjectsByType<ChessPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            ChessPiece[] foundPieces = FindObjectsOfType<ChessPiece>(true);
#endif
            for (int i = 0; i < foundPieces.Length; i++)
            {
                if (foundPieces[i] != null)
                    set.Add(foundPieces[i]);
            }
        }

        int previousCount = pieces.Count;
        pieces.Clear();
        pieces.AddRange(set);

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null)
                continue;

            pieces[i].ImpactFeedbackRequested -= OnImpactFeedbackRequested;
            pieces[i].ImpactFeedbackRequested += OnImpactFeedbackRequested;
            pieces[i].PreImpactFeedbackRequested -= OnPreImpactFeedbackRequested;
            pieces[i].PreImpactFeedbackRequested += OnPreImpactFeedbackRequested;
        }

        if (debugLog)
        {
            Debug.Log($"[HitFeedbackController] 刷新订阅 | reason:{reason} | previous:{previousCount} | total:{pieces.Count}");
        }
    }

    private void SubscribeRosterManager()
    {
        CoinRosterManager rosterManager = CoinRosterManager.Instance;
        if (subscribedRosterManager == rosterManager)
            return;

        UnsubscribeRosterManager();
        subscribedRosterManager = rosterManager;

        if (subscribedRosterManager != null)
        {
            subscribedRosterManager.CoinReplaced -= OnCoinReplaced;
            subscribedRosterManager.CoinReplaced += OnCoinReplaced;
        }
    }

    private void UnsubscribeRosterManager()
    {
        if (subscribedRosterManager == null)
            return;

        subscribedRosterManager.CoinReplaced -= OnCoinReplaced;
        subscribedRosterManager = null;
    }

    private void OnCoinReplaced(ChessPiece piece, CoinDefinition definition)
    {
        RefreshPieceSubscriptions("CoinReplaced");
    }

    private void UnregisterAllPieces()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null)
                continue;

            pieces[i].ImpactFeedbackRequested -= OnImpactFeedbackRequested;
            pieces[i].PreImpactFeedbackRequested -= OnPreImpactFeedbackRequested;
        }
    }

    private void OnPreImpactFeedbackRequested(
        ChessPiece piece,
        CollisionType targetType,
        float predictedTime,
        Vector3 hitPoint)
    {
        if (config == null || !config.enableHitPause)
            return;

        switch (targetType)
        {
            case CollisionType.Enemy:
                if (!config.enableEnemyPreImpactSlowMotion)
                    return;

                float enemyDuration = GetPreImpactFeedbackDuration(CollisionType.Enemy);
                TriggerHitPause(config.enemyPreImpactTimeScale, enemyDuration);
                CameraFeedbackController.Instance?.PlayImpactFocus(hitPoint, enemyDuration, config);

                if (debugLog)
                {
                    Debug.Log(
                        $"[HitFeedbackController] 敌人预碰撞慢动作 | piece:{piece.name} | " +
                        $"predictedTime:{predictedTime:F3} | duration:{enemyDuration:F3} | point:{hitPoint}"
                    );
                }
                break;

            case CollisionType.PlayerCoin:
                if (!config.enableCoinPreImpactSlowMotion)
                    return;

                float coinDuration = GetPreImpactFeedbackDuration(CollisionType.PlayerCoin);
                TriggerHitPause(config.coinPreImpactTimeScale, coinDuration);
                CameraFeedbackController.Instance?.PlayImpactFocus(hitPoint, coinDuration, config);

                if (debugLog)
                {
                    Debug.Log(
                        $"[HitFeedbackController] 己方硬币预碰撞慢动作 | piece:{piece.name} | " +
                        $"predictedTime:{predictedTime:F3} | duration:{coinDuration:F3} | point:{hitPoint}"
                    );
                }
                break;
        }
    }

    private float CalculatePreImpactDuration(float lookAheadTime, float timeScale)
    {
        if (lookAheadTime <= 0f)
            return 0f;

        timeScale = Mathf.Clamp(timeScale, 0.01f, 1f);
        return lookAheadTime / timeScale;
    }

    private void OnImpactFeedbackRequested(
    ChessPiece piece,
    CollisionType targetType,
    bool isFlip,
    float strength,
    Vector3 hitPoint)
    {
        if (config == null)
            return;

        if (!config.enableHitPause)
            return;

        strength = Mathf.Clamp01(strength);

        if (isFlip)
        {
            TriggerHitPause(config.flipHitPauseTimeScale, config.flipHitPauseDuration);

            if (debugLog)
            {
                Debug.Log(
                    $"[HitFeedbackController] 翻面停顿 | piece:{piece.name} | " +
                    $"targetType:{targetType} | point:{hitPoint}"
                );
            }

            return;
        }

        switch (targetType)
        {
            case CollisionType.Enemy:
                {
                    float duration = Mathf.Lerp(
                        config.enemyHitPauseMinDuration,
                        config.enemyHitPauseMaxDuration,
                        strength
                    );

                    TriggerHitPause(config.enemyHitPauseTimeScale, duration);

                    if (debugLog)
                    {
                        Debug.Log(
                            $"[HitFeedbackController] 敌人命中停顿 | piece:{piece.name} | " +
                            $"strength:{strength:F2} | duration:{duration:F3} | point:{hitPoint}"
                        );
                    }

                    break;
                }

            case CollisionType.PlayerCoin:
                {
                    if (!config.enableCoinHitPause)
                        return;

                    TriggerHitPause(config.coinHitPauseTimeScale, config.coinHitPauseDuration);

                    if (debugLog)
                    {
                        Debug.Log(
                            $"[HitFeedbackController] 己方互撞停顿 | piece:{piece.name} | " +
                            $"point:{hitPoint}"
                        );
                    }

                    break;
                }
        }
    }

    private void TriggerHitPause(float targetTimeScale, float duration)
    {
        targetTimeScale = Mathf.Clamp(targetTimeScale, 0.01f, 1f);
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
            return;

        if (!isPaused)
        {
            ApplyTimeScale(targetTimeScale);
            pauseTimer = duration;
            currentPauseTimeScale = targetTimeScale;
            isPaused = true;
            return;
        }

        // 已经处于停顿中时：
        // 1. 更强（timeScale 更小）的覆盖当前停顿强度
        // 2. 更长的时长覆盖当前剩余时长
        if (targetTimeScale < currentPauseTimeScale)
        {
            ApplyTimeScale(targetTimeScale);
            currentPauseTimeScale = targetTimeScale;
        }

        pauseTimer = Mathf.Max(pauseTimer, duration);
    }

    private void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * scale;
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        currentPauseTimeScale = 1f;
        pauseTimer = 0f;
        isPaused = false;

        if (debugLog)
        {
            Debug.Log("[HitFeedbackController] 时间缩放已恢复");
        }
    }
}
