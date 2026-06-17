/// <summary>
/// 实现功能：封装开局动画播放，包括开场动画、退场动画和三轮龟壳动画，并提供稳定的等待完成逻辑。
/// </summary>
using System.Collections;
using UnityEngine;

public class OpeningAnimationPlayer : MonoBehaviour
{
    [Header("开场动画")]
    [Tooltip("播放开场动画1的 Animator。")]
    [SerializeField] private Animator introAnimator;

    [Tooltip("开场动画1 Trigger 名称。为空时跳过真实播放。")]
    [SerializeField] private string introTriggerName = "Intro";

    [Tooltip("播放开场动画2的 Animator。")]
    [SerializeField] private Animator outroAnimator;

    [Tooltip("开场动画2 Trigger 名称。为空时跳过真实播放。")]
    [SerializeField] private string outroTriggerName = "Outro";

    [Tooltip("开场动画2播放完成后是否隐藏 outroAnimator 所在物体。")]
    [SerializeField] private bool deactivateOutroAnimatorObjectAfterComplete = true;

    [Header("龟壳动画")]
    [Tooltip("龟壳 Animator。")]
    [SerializeField] private Animator shellAnimator;

    [Tooltip("三轮龟壳前进动画 Trigger，索引 0/1/2 对应第 1/2/3 轮。")]
    [SerializeField] private string[] shellForwardTriggers = new string[3] { "Shell1_1", "Shell2_1", "Shell3_1" };

    [Tooltip("三轮龟壳复位动画 Trigger，索引 0/1/2 对应第 1/2/3 轮。")]
    [SerializeField] private string[] shellBackTriggers = new string[3] { "Shell1_2", "Shell2_2", "Shell3_2" };

    [Header("等待")]
    [Tooltip("等待 Animator 时是否使用不受 Time.timeScale 影响的时间。")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("等待单个动画完成的最长时间，避免 Animator 配置异常导致流程卡死。")]
    [Min(0.1f)]
    [SerializeField] private float maxAnimationWait = 10f;

    [Header("调试")]
    [Tooltip("是否输出动画播放日志。")]
    [SerializeField] private bool debugLog = true;

    public IEnumerator PlayIntro()
    {
        yield return PlayTriggerAndWait(introAnimator, introTriggerName, "开场动画1");
    }

    public IEnumerator PlayOutro()
    {
        yield return PlayTriggerAndWait(outroAnimator, outroTriggerName, "开场动画2");

        if (deactivateOutroAnimatorObjectAfterComplete && outroAnimator != null)
        {
            outroAnimator.gameObject.SetActive(false);
        }
    }

    public IEnumerator PlayShellForward(int roundIndex)
    {
        string triggerName = GetRoundTrigger(shellForwardTriggers, roundIndex);
        yield return PlayTriggerAndWait(shellAnimator, triggerName, $"龟壳动画{roundIndex}-1");
    }

    public IEnumerator PlayShellBack(int roundIndex)
    {
        string triggerName = GetRoundTrigger(shellBackTriggers, roundIndex);
        yield return PlayTriggerAndWait(shellAnimator, triggerName, $"龟壳动画{roundIndex}-2");
    }

    public void StopAllOpeningAnimations()
    {
        ResetKnownTriggers(introAnimator, introTriggerName);
        ResetKnownTriggers(outroAnimator, outroTriggerName);
        ResetKnownTriggers(shellAnimator, shellForwardTriggers);
        ResetKnownTriggers(shellAnimator, shellBackTriggers);
    }

    private IEnumerator PlayTriggerAndWait(Animator animator, string triggerName, string label)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
        {
            if (debugLog)
            {
                Debug.Log($"[OpeningAnimationPlayer] 跳过动画播放 | object:{name} | label:{label} | animator:{GetAnimatorName(animator)} | trigger:{triggerName}");
            }

            yield break;
        }

        if (!animator.gameObject.activeSelf)
        {
            animator.gameObject.SetActive(true);
        }

        if (!animator.enabled)
        {
            animator.enabled = true;
        }

        animator.updateMode = useUnscaledTime ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);

        if (debugLog)
        {
            Debug.Log($"[OpeningAnimationPlayer] 播放动画 | object:{name} | label:{label} | animator:{animator.name} | trigger:{triggerName}");
        }

        yield return WaitAnimatorStateComplete(animator, label);
    }

    private IEnumerator WaitAnimatorStateComplete(Animator animator, string label)
    {
        if (animator == null || !animator.isActiveAndEnabled)
            yield break;

        float elapsed = 0f;
        bool hasPlayableState = false;
        int playableStateHash = 0;

        while (animator != null && animator.isActiveAndEnabled && elapsed < maxAnimationWait)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool canUseState = !animator.IsInTransition(0) && !state.loop && state.normalizedTime < 1f;

            if (canUseState)
            {
                playableStateHash = state.fullPathHash;
                hasPlayableState = true;
                break;
            }

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        if (!hasPlayableState)
        {
            Debug.LogWarning($"[OpeningAnimationPlayer] 未检测到可等待的非循环动画状态，继续开局流程 | object:{name} | label:{label} | wait:{elapsed:F2}");
            yield break;
        }

        while (animator != null && animator.isActiveAndEnabled && elapsed < maxAnimationWait)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool changedToAnotherStateAfterComplete = state.fullPathHash != playableStateHash && !animator.IsInTransition(0);
            bool completedNonLoopState = state.fullPathHash == playableStateHash && !state.loop && !animator.IsInTransition(0) && state.normalizedTime >= 1f;

            if (changedToAnotherStateAfterComplete || completedNonLoopState)
            {
                if (debugLog)
                {
                    Debug.Log($"[OpeningAnimationPlayer] 动画完成 | object:{name} | label:{label} | wait:{elapsed:F2}");
                }

                yield break;
            }

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"[OpeningAnimationPlayer] 等待动画完成超时，继续开局流程 | object:{name} | label:{label} | wait:{elapsed:F2}");
    }

    private static string GetRoundTrigger(string[] triggers, int roundIndex)
    {
        int index = roundIndex - 1;
        if (triggers == null || index < 0 || index >= triggers.Length)
            return string.Empty;

        return triggers[index];
    }

    private static void ResetKnownTriggers(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
            return;

        animator.ResetTrigger(triggerName);
    }

    private static void ResetKnownTriggers(Animator animator, string[] triggerNames)
    {
        if (animator == null || triggerNames == null)
            return;

        for (int i = 0; i < triggerNames.Length; i++)
        {
            ResetKnownTriggers(animator, triggerNames[i]);
        }
    }

    private static string GetAnimatorName(Animator animator)
    {
        return animator != null ? animator.name : "None";
    }
}
