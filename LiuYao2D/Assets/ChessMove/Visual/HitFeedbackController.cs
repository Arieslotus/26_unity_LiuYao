/// <summary>
/// 实现功能：监听棋子的命中反馈事件，并触发简单的全局慢动作/短暂停顿效果。
/// 挂在场景中的任意管理物体上
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class HitFeedbackController : MonoBehaviour
{
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

    private void Awake()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("[HitFeedbackController] 未绑定 HitFeedbackConfig，反馈系统无法正常工作。");
            return;
        }

        RegisterAllPieces();
    }

    private void OnDestroy()
    {
        UnregisterAllPieces();
        RestoreTimeScale();
    }

    private void Update()
    {
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

    private void RegisterAllPieces()
    {
        HashSet<ChessPiece> set = new HashSet<ChessPiece>();

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] != null)
                set.Add(pieces[i]);
        }

        if (autoFindPiecesOnStart)
        {
#if UNITY_2023_1_OR_NEWER
            ChessPiece[] foundPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
#else
            ChessPiece[] foundPieces = FindObjectsOfType<ChessPiece>();
#endif
            for (int i = 0; i < foundPieces.Length; i++)
            {
                if (foundPieces[i] != null)
                    set.Add(foundPieces[i]);
            }
        }

        pieces.Clear();
        pieces.AddRange(set);

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null)
                continue;

            pieces[i].ImpactFeedbackRequested -= OnImpactFeedbackRequested;
            pieces[i].ImpactFeedbackRequested += OnImpactFeedbackRequested;
        }

        if (debugLog)
        {
            Debug.Log($"[HitFeedbackController] 完成订阅 | 棋子数量:{pieces.Count}");
        }
    }

    private void UnregisterAllPieces()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null)
                continue;

            pieces[i].ImpactFeedbackRequested -= OnImpactFeedbackRequested;
        }
    }

    private void OnImpactFeedbackRequested(
    ChessPiece piece,
    CollisionType targetType,
    bool isFlip,
    float strength,
    Vector2 hitPoint)
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