/// <summary>
/// 实现功能：为硬币展示架提供统一起点入场、统一终点退场的顺序动画，并支持在硬币入场前、退场后触发 shelfRoot 的 Animator 状态。
/// </summary>
using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class CoinShelfSequenceAnimator : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private CoinModelShelf shelf;

    [Header("根节点 Animator")]
    [Tooltip("与硬币 item 独立的展示根节点 Animator。")]
    [SerializeField] private Animator shelfRootAnimator;

    [Tooltip("入场前是否触发 shelfRootAnimator。")]
    [SerializeField] private bool playShelfRootBeforeEnter = true;

    [Tooltip("退场后是否触发 shelfRootAnimator。")]
    [SerializeField] private bool playShelfRootAfterExit = true;

    [Tooltip("进场 Trigger 名称。")]
    [SerializeField] private string enterTriggerName = "Enter";

    [Tooltip("退场 Trigger 名称。")]
    [SerializeField] private string exitTriggerName = "Exit";

    [Tooltip("是否使用非缩放时间等待 Animator 状态结束。")]
    [SerializeField] private bool useUnscaledWait = false;

    [Header("入场")]
    [SerializeField] private Transform enterStartAnchor;
    [Min(0.01f)]
    [SerializeField] private float enterDuration = 0.35f;
    [Min(0f)]
    [SerializeField] private float enterInterval = 0.08f;
    [SerializeField] private Ease enterEase = Ease.OutBack;
    [SerializeField] private Vector3 enterStartScale = new Vector3(0.8f, 0.8f, 0.8f);

    [Header("退场")]
    [SerializeField] private Transform exitEndAnchor;
    [Min(0.01f)]
    [SerializeField] private float exitDuration = 0.25f;
    [Min(0f)]
    [SerializeField] private float exitInterval = 0.06f;
    [SerializeField] private Ease exitEase = Ease.InBack;
    [SerializeField] private Vector3 exitEndScale = new Vector3(0.8f, 0.8f, 0.8f);

    [Header("交互")]
    [SerializeField] private bool restoreHoverAfterAnimation = true;

    [Header("调试")]
    [SerializeField] private bool debugLog;

    private Sequence activeSequence;
    private Coroutine activeRoutine;
    private bool originalHoverEnabled = true;
    private bool originalSelectionEnabled;
    private bool hasCachedInteractionState;

    public bool IsPlaying =>
        (activeSequence != null && activeSequence.IsActive() && activeSequence.IsPlaying()) ||
        activeRoutine != null;

    private void Awake()
    {
        if (shelf == null)
        {
            shelf = GetComponent<CoinModelShelf>();
        }
    }

    public void PlayEnter(Action onComplete = null)
    {
        KillActiveSequence();
        activeRoutine = StartCoroutine(PlayEnterRoutine(onComplete));
    }

    public void PlayExit(Action onComplete = null)
    {
        KillActiveSequence();
        activeRoutine = StartCoroutine(PlayExitRoutine(onComplete));
    }

    public void KillActiveSequence()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        if (activeSequence != null)
        {
            activeSequence.Kill();
            activeSequence = null;
        }

        if (shelf != null)
        {
            var items = shelf.SpawnedItems;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].transform.DOKill();
                }
            }
        }

        RestoreInteraction();
    }

    private IEnumerator PlayEnterRoutine(Action onComplete)
    {
        if (shelf == null)
        {
            activeRoutine = null;
            onComplete?.Invoke();
            yield break;
        }

        CacheAndDisableInteraction();

        PrepareItemsAtEnterStart();

        if (playShelfRootBeforeEnter)
        {
            yield return PlayShelfRootAnimator(enterTriggerName);
        }

        var items = shelf.SpawnedItems;
        if (items.Count == 0)
        {
            RestoreInteraction();
            activeRoutine = null;
            onComplete?.Invoke();
            yield break;
        }

        bool sequenceCompleted = false;
        activeSequence = DOTween.Sequence();

        for (int i = 0; i < items.Count; i++)
        {
            CoinReplacementModelItem item = items[i];
            if (item == null)
                continue;

            Transform itemTransform = item.transform;
            Vector3 targetPosition = item.TargetLocalPosition;

            itemTransform.DOKill();

            activeSequence.Insert(
                i * enterInterval,
                itemTransform.DOLocalMove(targetPosition, enterDuration).SetEase(enterEase)
            );
            activeSequence.Insert(
                i * enterInterval,
                itemTransform.DOScale(Vector3.one, enterDuration).SetEase(enterEase)
            );
        }

        activeSequence.OnComplete(() => sequenceCompleted = true);

        while (!sequenceCompleted && activeSequence != null)
        {
            yield return null;
        }

        activeSequence = null;
        RestoreInteraction();
        activeRoutine = null;

        if (debugLog)
        {
            Debug.Log($"[CoinShelfSequenceAnimator] 入场完成 | object:{name}");
        }

        onComplete?.Invoke();
    }

    private IEnumerator PlayExitRoutine(Action onComplete)
    {
        if (shelf == null)
        {
            activeRoutine = null;
            onComplete?.Invoke();
            yield break;
        }

        CacheAndDisableInteraction();

        var items = shelf.SpawnedItems;
        if (items.Count > 0)
        {
            bool sequenceCompleted = false;
            activeSequence = DOTween.Sequence();

            for (int i = 0; i < items.Count; i++)
            {
                CoinReplacementModelItem item = items[i];
                if (item == null)
                    continue;

                Transform itemTransform = item.transform;
                Vector3 endPosition = exitEndAnchor != null
                    ? itemTransform.parent.InverseTransformPoint(exitEndAnchor.position)
                    : itemTransform.localPosition;

                itemTransform.DOKill();

                activeSequence.Insert(
                    i * exitInterval,
                    itemTransform.DOLocalMove(endPosition, exitDuration).SetEase(exitEase)
                );
                activeSequence.Insert(
                    i * exitInterval,
                    itemTransform.DOScale(exitEndScale, exitDuration).SetEase(exitEase)
                );
            }

            activeSequence.OnComplete(() => sequenceCompleted = true);

            while (!sequenceCompleted && activeSequence != null)
            {
                yield return null;
            }

            activeSequence = null;
        }

        if (playShelfRootAfterExit)
        {
            yield return PlayShelfRootAnimator(exitTriggerName);
        }

        RestoreInteraction();
        activeRoutine = null;

        if (debugLog)
        {
            Debug.Log($"[CoinShelfSequenceAnimator] 退场完成 | object:{name}");
        }

        onComplete?.Invoke();
    }

    private IEnumerator PlayShelfRootAnimator(string triggerName)
    {
        if (shelfRootAnimator == null || string.IsNullOrEmpty(triggerName))
            yield break;

        if (!shelfRootAnimator.isActiveAndEnabled)
        {
            shelfRootAnimator.enabled = true;
        }

        shelfRootAnimator.updateMode = useUnscaledWait ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
        shelfRootAnimator.ResetTrigger(enterTriggerName);
        shelfRootAnimator.ResetTrigger(exitTriggerName);
        shelfRootAnimator.SetTrigger(triggerName);

        yield return null;

        AnimatorStateInfo firstState = shelfRootAnimator.GetCurrentAnimatorStateInfo(0);
        int firstStateHash = firstState.fullPathHash;
        float loopFallbackDuration = firstState.length > 0f ? firstState.length : 10f;
        float elapsed = 0f;

        while (shelfRootAnimator != null && shelfRootAnimator.isActiveAndEnabled)
        {
            AnimatorStateInfo state = shelfRootAnimator.GetCurrentAnimatorStateInfo(0);
            bool changedToAnotherState = state.fullPathHash != firstStateHash && !shelfRootAnimator.IsInTransition(0);
            bool completedNonLoopState = !state.loop && !shelfRootAnimator.IsInTransition(0) && state.normalizedTime >= 1f;
            bool completedOneLoop = state.loop && elapsed >= loopFallbackDuration;

            if (changedToAnotherState || completedNonLoopState || completedOneLoop)
                yield break;

            elapsed += useUnscaledWait ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        if (debugLog && shelfRootAnimator != null)
        {
            Debug.LogWarning($"[CoinShelfSequenceAnimator] 等待 shelfRoot Animator 结束时 Animator 失效 | object:{shelfRootAnimator.name}");
        }
    }

    private void CacheAndDisableInteraction()
    {
        if (shelf == null)
            return;

        originalHoverEnabled = shelf.EnableHover;
        originalSelectionEnabled = shelf.EnableSelection;
        hasCachedInteractionState = true;
        shelf.SetInteractionMode(false, false);
    }

    private void RestoreInteraction()
    {
        if (shelf == null || !hasCachedInteractionState)
            return;

        shelf.SetInteractionMode(
            restoreHoverAfterAnimation ? originalHoverEnabled : false,
            originalSelectionEnabled
        );
        hasCachedInteractionState = false;
    }

    private void PrepareItemsAtEnterStart()
    {
        if (shelf == null)
            return;

        var items = shelf.SpawnedItems;
        for (int i = 0; i < items.Count; i++)
        {
            CoinReplacementModelItem item = items[i];
            if (item == null)
                continue;

            Transform itemTransform = item.transform;
            Vector3 targetPosition = itemTransform.localPosition;
            Vector3 startPosition = enterStartAnchor != null
                ? itemTransform.parent.InverseTransformPoint(enterStartAnchor.position)
                : targetPosition;

            itemTransform.DOKill();
            itemTransform.localPosition = startPosition;
            itemTransform.localScale = enterStartScale;
        }
    }
}
