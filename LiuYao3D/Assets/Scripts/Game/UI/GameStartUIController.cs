/// <summary>
/// 实现功能：管理游戏开始前 UI 表现，点击后自动播放开始 UI 根物体上的 Animator/Animation，并在动画结束后通知游戏流程正式开始。
/// </summary>
using System.Collections;
using UnityEngine;

public class GameStartUIController : MonoBehaviour
{
    [Header("流程引用")]
    [Tooltip("游戏流程控制器。为空时自动使用 GameFlowController.Instance。")]
    [SerializeField] private GameFlowController flowController;

    [Header("输入")]
    [Tooltip("是否监听鼠标左键触发开始流程。")]
    [SerializeField] private bool startOnLeftMouseClick = true;

    [Tooltip("场景中存在 OpeningFlowController 时，是否停用旧点击开局流程。")]
    [SerializeField] private bool disableWhenOpeningFlowExists = true;

    [Header("开始 UI")]
    [Tooltip("开始 UI 的根物体。Animator 或 Animation 组件应挂在这个根物体上。")]
    [SerializeField] private GameObject startUiRoot;

    [Tooltip("播放完成后是否隐藏开始 UI 根物体。")]
    [SerializeField] private bool hideAfterAnimation = true;

    [Header("等待")]
    [Tooltip("等待动画时是否使用不受 Time.timeScale 影响的时间。")]
    [SerializeField] private bool useUnscaledWait = true;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private bool isStarting;
    private bool disabledByOpeningFlow;
    private Coroutine startCoroutine;

    private void Awake()
    {
        ResolveFlowController();
        disabledByOpeningFlow = ShouldDisableForOpeningFlow();
        UIAnimationRootPlayer.PrepareWithoutPlaying(startUiRoot);
    }

    private void Update()
    {
        if (disabledByOpeningFlow || !startOnLeftMouseClick || isStarting)
            return;

        if (flowController == null)
        {
            ResolveFlowController();
        }

        if (flowController == null || flowController.State != GameFlowState.WaitingToStart)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            BeginStartSequence();
        }
    }

    public void BeginStartSequence()
    {
        if (ShouldDisableForOpeningFlow())
        {
            disabledByOpeningFlow = true;

            if (debugLog)
            {
                Debug.Log($"[GameStartUIController] 检测到 OpeningFlowController，旧开局入口已停用 | object:{name}");
            }

            return;
        }

        if (isStarting)
            return;

        ResolveFlowController();
        if (flowController == null)
        {
            Debug.LogWarning($"[GameStartUIController] 未找到 GameFlowController，无法开始游戏 | object:{name}");
            return;
        }

        if (!flowController.RequestStartSequence())
            return;

        isStarting = true;

        if (startCoroutine != null)
        {
            StopCoroutine(startCoroutine);
        }

        startCoroutine = StartCoroutine(PlayStartAnimationCoroutine());
    }

    private IEnumerator PlayStartAnimationCoroutine()
    {
        bool played = UIAnimationRootPlayer.Play(startUiRoot, useUnscaledWait, nameof(GameStartUIController));

        if (debugLog)
        {
            Debug.Log($"[GameStartUIController] 播放开始 UI 动画 | object:{name} | root:{GetRootName(startUiRoot)} | played:{played}");
        }

        if (played)
        {
            yield return UIAnimationRootPlayer.WaitUntilComplete(startUiRoot, useUnscaledWait, nameof(GameStartUIController));
        }

        if (hideAfterAnimation && startUiRoot != null)
        {
            startUiRoot.SetActive(false);
        }

        CompleteStartSequence();
    }

    private void CompleteStartSequence()
    {
        isStarting = false;
        startCoroutine = null;

        if (flowController == null)
        {
            ResolveFlowController();
        }

        if (flowController != null)
        {
            flowController.CompleteStartSequence();
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

    private bool ShouldDisableForOpeningFlow()
    {
        if (!disableWhenOpeningFlowExists)
            return false;

        OpeningFlowController openingFlow = FindObjectOfType<OpeningFlowController>();
        return openingFlow != null;
    }

    private static string GetRootName(GameObject root)
    {
        return root != null ? root.name : "None";
    }
}

public static class UIAnimationRootPlayer
{
    public static void PrepareWithoutPlaying(GameObject root)
    {
        if (root == null)
            return;

        Animator animator = root.GetComponent<Animator>();
        if (animator != null)
        {
            if (root.activeInHierarchy && animator.isActiveAndEnabled)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            animator.enabled = false;
            return;
        }

        Animation legacyAnimation = root.GetComponent<Animation>();
        if (legacyAnimation != null)
        {
            legacyAnimation.Stop();
            legacyAnimation.enabled = false;
        }
    }

    public static bool Play(GameObject root, bool useUnscaledTime, string logOwner)
    {
        if (root == null)
        {
            Debug.LogWarning($"[{logOwner}] UI 根物体为空，跳过动画播放");
            return false;
        }

        root.SetActive(true);

        Animator animator = root.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            animator.updateMode = useUnscaledTime ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
            animator.Rebind();
            animator.Update(0f);
            return true;
        }

        Animation legacyAnimation = root.GetComponent<Animation>();
        if (legacyAnimation != null)
        {
            legacyAnimation.enabled = true;
            legacyAnimation.Stop();
            legacyAnimation.Play();
            return true;
        }

        Debug.LogWarning($"[{logOwner}] UI 根物体上没有 Animator 或 Animation 组件 | root:{root.name}");
        return false;
    }

    public static IEnumerator WaitUntilComplete(GameObject root, bool useUnscaledTime, string logOwner)
    {
        if (root == null)
            yield break;

        Animator animator = root.GetComponent<Animator>();
        if (animator != null)
        {
            yield return WaitAnimatorUntilComplete(animator, useUnscaledTime, logOwner);
            yield break;
        }

        Animation legacyAnimation = root.GetComponent<Animation>();
        if (legacyAnimation != null)
        {
            while (legacyAnimation.isPlaying)
            {
                yield return null;
            }
        }
    }

    private static IEnumerator WaitAnimatorUntilComplete(Animator animator, bool useUnscaledTime, string logOwner)
    {
        yield return null;

        if (animator == null || !animator.isActiveAndEnabled)
            yield break;

        AnimatorStateInfo firstState = animator.GetCurrentAnimatorStateInfo(0);
        int firstStateHash = firstState.fullPathHash;
        float loopFallbackDuration = firstState.length > 0f ? firstState.length : 10f;
        float elapsed = 0f;

        while (animator != null && animator.isActiveAndEnabled)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool changedToAnotherState = state.fullPathHash != firstStateHash && !animator.IsInTransition(0);
            bool completedNonLoopState = !state.loop && !animator.IsInTransition(0) && state.normalizedTime >= 1f;
            bool completedOneLoop = state.loop && elapsed >= loopFallbackDuration;

            if (changedToAnotherState || completedNonLoopState || completedOneLoop)
                yield break;

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"[{logOwner}] 等待 Animator 动画结束时 Animator 失效 | object:{animator.name}");
    }
}
