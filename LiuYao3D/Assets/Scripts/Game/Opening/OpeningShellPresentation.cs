/// <summary>
/// 实现功能：负责开局龟壳的入场、退场、显隐和位移中断，供开局流程总控调用。
/// </summary>
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class OpeningShellPresentation : MonoBehaviour
{
    [Header("龟壳引用")]
    [Tooltip("龟壳根物体。为空时使用当前物体。")]
    [SerializeField] private Transform shellRoot;

    [Header("入场")]
    [Tooltip("龟壳入场起点。")]
    [SerializeField] private Transform enterStartPoint;

    [Tooltip("龟壳入场终点，也是摇卦展示点。")]
    [SerializeField] private Transform activePoint;

    [Min(0.01f)]
    [Tooltip("龟壳入场位移时长。")]
    [SerializeField] private float enterDuration = 0.45f;

    [Tooltip("龟壳入场缓动。")]
    [SerializeField] private Ease enterEase = Ease.OutCubic;

    [Header("退场")]
    [Tooltip("龟壳退场终点。")]
    [SerializeField] private Transform exitEndPoint;

    [Min(0.01f)]
    [Tooltip("龟壳退场位移时长。")]
    [SerializeField] private float exitDuration = 0.45f;

    [Tooltip("龟壳退场缓动。")]
    [SerializeField] private Ease exitEase = Ease.InCubic;

    [Header("调试")]
    [Tooltip("是否输出龟壳表现日志。")]
    [SerializeField] private bool debugLog = true;

    private Tween activeTween;

    private void Awake()
    {
        if (shellRoot == null)
        {
            shellRoot = transform;
        }
    }

    public IEnumerator PlayEnter()
    {
        if (shellRoot == null)
            yield break;

        KillActiveTween(false);
        shellRoot.gameObject.SetActive(true);

        if (enterStartPoint != null)
        {
            shellRoot.position = enterStartPoint.position;
            shellRoot.rotation = enterStartPoint.rotation;
        }

        Vector3 targetPosition = activePoint != null ? activePoint.position : shellRoot.position;
        Quaternion targetRotation = activePoint != null ? activePoint.rotation : shellRoot.rotation;

        bool completed = false;
        Sequence sequence = DOTween.Sequence();
        sequence.Join(shellRoot.DOMove(targetPosition, enterDuration).SetEase(enterEase));
        sequence.Join(shellRoot.DORotateQuaternion(targetRotation, enterDuration).SetEase(enterEase));
        sequence.OnComplete(() => completed = true);
        activeTween = sequence;

        if (debugLog)
        {
            Debug.Log($"[OpeningShellPresentation] 龟壳入场开始 | object:{name} | shell:{shellRoot.name}");
        }

        while (!completed && activeTween != null && activeTween.IsActive())
        {
            yield return null;
        }

        activeTween = null;

        if (debugLog)
        {
            Debug.Log($"[OpeningShellPresentation] 龟壳入场完成 | object:{name} | shell:{shellRoot.name}");
        }
    }

    public IEnumerator PlayExit()
    {
        if (shellRoot == null)
            yield break;

        KillActiveTween(false);

        Vector3 targetPosition = exitEndPoint != null ? exitEndPoint.position : shellRoot.position;
        Quaternion targetRotation = exitEndPoint != null ? exitEndPoint.rotation : shellRoot.rotation;

        bool completed = false;
        Sequence sequence = DOTween.Sequence();
        sequence.Join(shellRoot.DOMove(targetPosition, exitDuration).SetEase(exitEase));
        sequence.Join(shellRoot.DORotateQuaternion(targetRotation, exitDuration).SetEase(exitEase));
        sequence.OnComplete(() => completed = true);
        activeTween = sequence;

        if (debugLog)
        {
            Debug.Log($"[OpeningShellPresentation] 龟壳退场开始 | object:{name} | shell:{shellRoot.name}");
        }

        while (!completed && activeTween != null && activeTween.IsActive())
        {
            yield return null;
        }

        activeTween = null;

        if (shellRoot != null)
        {
            shellRoot.gameObject.SetActive(false);
        }

        if (debugLog)
        {
            Debug.Log($"[OpeningShellPresentation] 龟壳退场完成 | object:{name}");
        }
    }

    public void HideImmediate()
    {
        KillActiveTween(false);

        if (shellRoot != null)
        {
            shellRoot.gameObject.SetActive(false);
        }
    }

    public void KillActiveTween(bool complete)
    {
        if (activeTween != null)
        {
            activeTween.Kill(complete);
            activeTween = null;
        }

        if (shellRoot != null)
        {
            shellRoot.DOKill(complete);
        }
    }
}
