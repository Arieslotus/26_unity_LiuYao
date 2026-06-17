/// <summary>
/// 实现功能：管理开局摇卦流程骨架，以五个大阶段编排开场、抽币、选择、退场、镜头切换和正式游戏开始。
/// </summary>
using System.Collections;
using UnityEngine;

public class OpeningFlowController : MonoBehaviour
{
    [Header("流程引用")]
    [Tooltip("游戏流程控制器。为空时自动使用 GameFlowController.Instance。")]
    [SerializeField] private GameFlowController flowController;

    [Header("模块引用")]
    [Tooltip("开局镜头控制器，负责 Cinemachine2 / Cinemachine1 的优先级切换和混合等待。")]
    [SerializeField] private OpeningCameraController cameraController;

    [Tooltip("开局动画播放器，负责开场动画、退场动画和龟壳动画播放等待。")]
    [SerializeField] private OpeningAnimationPlayer animationPlayer;

    [Tooltip("龟壳表现控制器，负责入场、退场和跳过隐藏。")]
    [SerializeField] private OpeningShellPresentation shellPresentation;

    [Tooltip("摇卦输入控制器，负责鼠标晃动判定和龟壳轻微跟随。")]
    [SerializeField] private OpeningShellShakeInput shellShakeInput;

    [Header("抽币数据")]
    [Tooltip("开局抽币使用的币池配置。骨架阶段可以为空，为空时会使用模拟硬币占位。")]
    [SerializeField] private CoinDrawConfig drawConfig;

    [Tooltip("币池不足或未配置时，是否使用模拟硬币占位跑通 9/3/6 的数据链路。")]
    [SerializeField] private bool allowPlaceholderCoins = true;

    [Header("骨架模拟")]
    [Tooltip("是否在 Start 时自动启动开局骨架流程。")]
    [SerializeField] private bool autoStartOnStart = true;

    [Tooltip("每个骨架步骤之间的模拟等待时间。")]
    [Min(0f)]
    [SerializeField] private float stepDelay = 0.25f;

    [Tooltip("等待时是否使用不受 Time.timeScale 影响的时间。")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("调试")]
    [Tooltip("是否输出骨架流程日志。")]
    [SerializeField] private bool debugLog = true;

    private OpeningState state = OpeningState.None;
    private OpeningCoinDraftService draftService;
    private Coroutine flowRoutine;
    private bool isRunning;
    private bool isSkipRequested;
    private int flowVersion;
    private int currentRoundIndex;

    public OpeningState State => state;
    public OpeningCoinDraft Draft => draftService != null ? draftService.Draft : null;
    public int CurrentRoundIndex => currentRoundIndex;
    public bool IsRunning => isRunning;
    public bool IsCompleted => state == OpeningState.Finished;
    public bool IsSkipRequested => isSkipRequested;

    private void Awake()
    {
        ResolveFlowController();
        ResolveModuleReferences();
        RebuildDraftService();
    }

    private void Start()
    {
        if (autoStartOnStart)
        {
            BeginOpening();
        }
    }

    [ContextMenu("启动开局骨架流程")]
    public void BeginOpening()
    {
        if (isRunning)
        {
            Debug.LogWarning($"[OpeningFlowController] 开局流程已经在运行，忽略重复启动 | object:{name} | state:{state}");
            return;
        }

        ResolveFlowController();
        if (flowController == null)
        {
            Debug.LogWarning($"[OpeningFlowController] 未找到 GameFlowController，无法启动开局流程 | object:{name}");
            return;
        }

        if (!flowController.RequestStartSequence())
        {
            Debug.LogWarning($"[OpeningFlowController] GameFlowController 拒绝开始请求 | object:{name} | gameState:{flowController.State}");
            return;
        }

        RebuildDraftService();
        flowVersion++;
        isRunning = true;
        isSkipRequested = false;
        currentRoundIndex = 0;
        SetState(OpeningState.IntroCinematic);

        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
        }

        flowRoutine = StartCoroutine(RunOpeningSkeleton(flowVersion));
    }

    [ContextMenu("跳过开局骨架流程")]
    public void SkipOpening()
    {
        if (!isRunning || isSkipRequested || state == OpeningState.Finished)
            return;

        isSkipRequested = true;
        flowVersion++;

        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        flowRoutine = StartCoroutine(RunSkipSkeleton(flowVersion));
    }

    private IEnumerator RunOpeningSkeleton(int version)
    {
        yield return RunIntroCinematic(version);
        if (!IsVersionValid(version)) yield break;

        yield return RunDrawingCoins(version);
        if (!IsVersionValid(version)) yield break;

        yield return RunSelectingCoins(version);
        if (!IsVersionValid(version)) yield break;

        yield return RunOutroCinematic(version);
    }

    private IEnumerator RunIntroCinematic(int version)
    {
        SetState(OpeningState.IntroCinematic);

        LogNormal(1, "进入主游戏场景，准备开场流程");
        yield return WaitStep();
        if (!IsVersionValid(version)) yield break;

        if (cameraController != null)
        {
            cameraController.ActivateOpeningCamera();
        }

        LogNormal(2, "激活开场镜头 Cinemachine2");
        yield return WaitStep();
        if (!IsVersionValid(version)) yield break;

        LogNormal(3, "播放开场动画1");
        if (animationPlayer != null)
        {
            yield return animationPlayer.PlayIntro();
        }
        else
        {
            yield return WaitStep();
        }

        if (!IsVersionValid(version)) yield break;

        LogNormal(4, "开场动画1结束，激活龟壳、跳过UI、提示文字");
        if (shellPresentation != null)
        {
            yield return shellPresentation.PlayEnter();
        }
        else
        {
            yield return WaitStep();
        }
    }

    private IEnumerator RunDrawingCoins(int version)
    {
        SetState(OpeningState.DrawingCoins);

        for (int roundIndex = 1; roundIndex <= OpeningCoinDraftRules.RoundCount; roundIndex++)
        {
            currentRoundIndex = roundIndex;
            int firstLogIndex = 5 + (roundIndex - 1) * 5;

            LogNormal(firstLogIndex, $"进入第{roundIndex}轮摇卦交互");
            if (shellShakeInput != null)
            {
                yield return shellShakeInput.WaitForShakeSuccess(roundIndex);
            }
            else
            {
                yield return WaitStep();
            }

            if (!IsVersionValid(version)) yield break;

            LogNormal(firstLogIndex + 1, $"第{roundIndex}轮摇卦成功");
            yield return WaitStep();
            if (!IsVersionValid(version)) yield break;

            LogNormal(firstLogIndex + 2, $"播放龟壳动画{roundIndex}-1");
            if (animationPlayer != null)
            {
                yield return animationPlayer.PlayShellForward(roundIndex);
            }
            else
            {
                yield return WaitStep();
            }

            if (!IsVersionValid(version)) yield break;

            if (draftService != null)
            {
                draftService.DrawRound(roundIndex);
            }

            LogNormal(firstLogIndex + 3, $"龟壳动画{roundIndex}-1到达出币点，抽取第{roundIndex}组三枚硬币并滑出 | rolled:{GetRolledCount()}");
            yield return WaitStep();
            if (!IsVersionValid(version)) yield break;

            LogNormal(firstLogIndex + 4, $"播放龟壳动画{roundIndex}-2复位");
            if (animationPlayer != null)
            {
                yield return animationPlayer.PlayShellBack(roundIndex);
            }
            else
            {
                yield return WaitStep();
            }

            if (!IsVersionValid(version)) yield break;
        }
    }

    private IEnumerator RunSelectingCoins(int version)
    {
        SetState(OpeningState.SelectingCoins);

        LogNormal(20, $"九枚硬币抽取完成，进入选择阶段 | rolled:{GetRolledCount()}");
        yield return WaitStep();
        if (!IsVersionValid(version)) yield break;

        if (draftService != null)
        {
            draftService.AutoSelectFirstCoins();
        }

        LogNormal(21, $"玩家已选择三枚硬币，点击确认（骨架阶段自动选择前三枚） | selected:{GetSelectedCount()} | inventory:{GetInventoryCount()}");
        yield return WaitStep();
    }

    private IEnumerator RunOutroCinematic(int version)
    {
        SetState(OpeningState.OutroCinematic);

        LogNormal(22, "关闭开场UI根物体，播放开场动画2");
        if (animationPlayer != null)
        {
            yield return animationPlayer.PlayOutro();
        }
        else
        {
            yield return WaitStep();
        }

        if (!IsVersionValid(version)) yield break;

        if (cameraController != null)
        {
            cameraController.ActivateGameplayCamera();
        }

        LogNormal(23, "开场动画2结束，切换回正式游戏镜头 Cinemachine1");
        if (cameraController != null)
        {
            yield return cameraController.WaitForGameplayCameraBlend();
        }
        else
        {
            yield return WaitStep();
        }

        if (!IsVersionValid(version)) yield break;

        if (shellPresentation != null)
        {
            yield return shellPresentation.PlayExit();
        }
        else
        {
            yield return WaitStep();
        }

        if (!IsVersionValid(version)) yield break;

        LogNormal(24, "龟壳和硬币退场完成");
        LogNormal(25, "开场流程结束，通知游戏开始");
        CompleteOpening();
    }

    private IEnumerator RunSkipSkeleton(int version)
    {
        LogSkip(1, "玩家点击跳过开场");
        yield return WaitStep();
        if (!IsVersionValid(version)) yield break;

        LogSkip(2, "停止当前开场交互、动画、硬币滑动");
        if (animationPlayer != null)
        {
            animationPlayer.StopAllOpeningAnimations();
        }

        if (shellPresentation != null)
        {
            shellPresentation.KillActiveTween(false);
        }

        if (shellShakeInput != null)
        {
            shellShakeInput.StopShake();
            shellShakeInput.ResetPoseImmediate();
        }

        yield return WaitStep();
        if (!IsVersionValid(version)) yield break;

        if (draftService != null)
        {
            draftService.CompleteMissingRolls();
        }

        LogSkip(3, $"如果未抽满九枚，自动补齐硬币 | rolled:{GetRolledCount()}");
        yield return WaitStep();
        if (!IsVersionValid(version)) yield break;

        if (draftService != null)
        {
            draftService.AutoSelectFirstCoins();
        }

        LogSkip(4, $"自动确定三枚开局硬币 | selected:{GetSelectedCount()} | inventory:{GetInventoryCount()}");
        yield return WaitStep();
        if (!IsVersionValid(version)) yield break;

        LogSkip(5, "关闭开场UI根物体、隐藏龟壳和硬币");
        if (shellPresentation != null)
        {
            shellPresentation.HideImmediate();
        }

        yield return WaitStep();
        if (!IsVersionValid(version)) yield break;

        SetState(OpeningState.OutroCinematic);

        LogSkip(6, "播放开场动画2");
        if (animationPlayer != null)
        {
            yield return animationPlayer.PlayOutro();
        }
        else
        {
            yield return WaitStep();
        }

        if (!IsVersionValid(version)) yield break;

        if (cameraController != null)
        {
            cameraController.ActivateGameplayCamera();
        }

        LogSkip(7, "开场动画2结束，切换回正式游戏镜头 Cinemachine1");
        if (cameraController != null)
        {
            yield return cameraController.WaitForGameplayCameraBlend();
        }
        else
        {
            yield return WaitStep();
        }

        if (!IsVersionValid(version)) yield break;

        LogSkip(8, "开场流程结束，通知游戏开始");
        CompleteOpening();
    }

    private IEnumerator WaitStep()
    {
        if (stepDelay <= 0f)
        {
            yield return null;
            yield break;
        }

        if (useUnscaledTime)
        {
            yield return new WaitForSecondsRealtime(stepDelay);
        }
        else
        {
            yield return new WaitForSeconds(stepDelay);
        }
    }

    private void CompleteOpening()
    {
        SetState(OpeningState.Finished);
        isRunning = false;
        isSkipRequested = false;
        currentRoundIndex = 0;
        flowRoutine = null;

        if (flowController == null)
        {
            ResolveFlowController();
        }

        if (flowController != null)
        {
            flowController.CompleteStartSequence();
        }
    }

    private void RebuildDraftService()
    {
        draftService = new OpeningCoinDraftService(drawConfig, allowPlaceholderCoins, debugLog, name);
    }

    private bool IsVersionValid(int version)
    {
        return version == flowVersion;
    }

    private void SetState(OpeningState newState)
    {
        if (state == newState)
            return;

        state = newState;

        if (debugLog)
        {
            Debug.Log($"[OpeningFlowController] 开局阶段切换 | object:{name} | state:{state}");
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

    private void ResolveModuleReferences()
    {
        if (cameraController == null)
        {
            cameraController = GetComponent<OpeningCameraController>();
        }

        if (cameraController == null)
        {
            cameraController = FindObjectOfType<OpeningCameraController>();
        }

        if (animationPlayer == null)
        {
            animationPlayer = GetComponent<OpeningAnimationPlayer>();
        }

        if (animationPlayer == null)
        {
            animationPlayer = FindObjectOfType<OpeningAnimationPlayer>();
        }

        if (shellPresentation == null)
        {
            shellPresentation = GetComponent<OpeningShellPresentation>();
        }

        if (shellPresentation == null)
        {
            shellPresentation = FindObjectOfType<OpeningShellPresentation>();
        }

        if (shellShakeInput == null)
        {
            shellShakeInput = GetComponent<OpeningShellShakeInput>();
        }

        if (shellShakeInput == null)
        {
            shellShakeInput = FindObjectOfType<OpeningShellShakeInput>();
        }
    }

    private int GetRolledCount()
    {
        return Draft != null ? Draft.RolledCount : 0;
    }

    private int GetSelectedCount()
    {
        return Draft != null ? Draft.SelectedCount : 0;
    }

    private int GetInventoryCount()
    {
        return Draft != null ? Draft.InventoryCount : 0;
    }

    private void LogNormal(int index, string message)
    {
        if (!debugLog)
            return;

        Debug.Log($"【骨架{index}】{message} | object:{name} | state:{state} | round:{currentRoundIndex}");
    }

    private void LogSkip(int index, string message)
    {
        if (!debugLog)
            return;

        Debug.Log($"【骨架S{index}】{message} | object:{name} | state:{state} | round:{currentRoundIndex}");
    }
}
