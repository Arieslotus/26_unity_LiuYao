/// <summary>
/// 实现功能：进入场景后自动请求播放指定 BGM，用于主界面和游戏场景的背景音乐切换。
/// </summary>
using UnityEngine;

public class SceneBGMPlayer : MonoBehaviour
{
    [Header("BGM")]
    [Tooltip("当前场景要播放的背景音乐。")]
    [SerializeField] private BGMType bgmType = BGMType.GameScene;

    [Tooltip("物体启用时是否自动播放。")]
    [SerializeField] private bool playOnStart = true;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    [ContextMenu("播放当前场景 BGM")]
    public void Play()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[SceneBGMPlayer] 播放 BGM 失败：场景中没有 AudioManager | object:{name} | bgm:{bgmType}");
            return;
        }

        AudioManager.Instance.PlayBGM(bgmType);

        if (debugLog)
        {
            Debug.Log($"[SceneBGMPlayer] 请求播放场景 BGM | object:{name} | bgm:{bgmType}");
        }
    }
}
