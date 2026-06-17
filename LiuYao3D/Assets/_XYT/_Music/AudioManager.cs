/// <summary>
/// 实现功能：集中管理 BGM、一次性音效和循环音效播放，支持 BGM 淡入淡出切换与循环音效渐停。
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 在这里编辑要添加的 BGM 序号
public enum BGMType
{
    MainMenu = 0, // 主界面
    GameScene = 1, // 游戏场景
}

// 在这里编辑要添加的 SFX 序号
public enum SFXType
{
    UI_ClickButton = 0,
    UI_ClickCoin = 1, // 保留旧配置，避免已序列化数据丢失

    Game_Coin = 10, // 保留旧配置
    Game_CoinFlip = 11, // 硬币翻面
    Game_CoinCollide = 12, // 保留旧配置：硬币碰撞
    Game_CoinCoinCollide = 12, // 硬币碰撞硬币
    Game_CoinEnemyCollide = 13, // 硬币碰撞敌人

    Opening_ShellShakeLoop = 20, // 摇龟壳循环
}

[System.Serializable]
public class BGMEntry
{
    public BGMType type;
    public AudioClip clip;
}

[System.Serializable]
public class SFXEntry
{
    public SFXType type;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("音频源")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource loopSfxSource;

    [Header("音频片段")]
    public List<BGMEntry> bgms = new List<BGMEntry>(); // 在面板配置游戏内的音乐
    public List<SFXEntry> sfxs = new List<SFXEntry>();

    private Dictionary<BGMType, AudioClip> bgmDict;
    private Dictionary<SFXType, AudioClip> sfxDict;
    private Coroutine bgmFadeRoutine;
    private Coroutine loopSfxFadeRoutine;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureLoopSfxSource();
        RebuildClipDictionaries();
    }

    private void RebuildClipDictionaries()
    {
        bgmDict = new Dictionary<BGMType, AudioClip>();
        sfxDict = new Dictionary<SFXType, AudioClip>();

        foreach (BGMEntry item in bgms)
        {
            if (item == null || item.clip == null)
                continue;

            bgmDict[item.type] = item.clip;
        }

        foreach (SFXEntry item in sfxs)
        {
            if (item == null || item.clip == null)
                continue;

            sfxDict[item.type] = item.clip;
        }
    }

    //----------------------------------
    // BGM
    //----------------------------------

    // 播放 BGM：AudioManager.Instance.PlayBGM(...)
    public void PlayBGM(BGMType type)
    {
        if (!TryGetBGM(type, out AudioClip clip))
            return;

        if (bgmSource == null)
        {
            Debug.LogWarning($"[AudioManager] 播放 BGM 失败：bgmSource 未绑定 | object:{name} | type:{type}");
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
        }

        bgmFadeRoutine = StartCoroutine(FadeSwitchBGM(type, clip));
    }

    public void StopBGM()
    {
        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = null;
        }

        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = volume;
        }
    }

    private IEnumerator FadeSwitchBGM(BGMType type, AudioClip newClip)
    {
        float fadeTime = 1f;
        float startVolume = bgmSource.volume;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.loop = true;
        bgmSource.Play();

        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, startVolume, t / fadeTime);
            yield return null;
        }

        bgmSource.volume = startVolume;
        bgmFadeRoutine = null;

        Debug.Log($"[AudioManager] BGM 切换完成 | object:{name} | type:{type} | clip:{newClip.name}");
    }

    //----------------------------------
    // SFX
    //----------------------------------

    // 播放一次性音效：AudioManager.Instance.PlaySFX(...)
    public void PlaySFX(SFXType type)
    {
        if (!TryGetSFX(type, out AudioClip clip))
            return;

        if (sfxSource == null)
        {
            Debug.LogWarning($"[AudioManager] 播放音效失败：sfxSource 未绑定 | object:{name} | type:{type}");
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    public void PlayLoopSFX(SFXType type)
    {
        if (!TryGetSFX(type, out AudioClip clip))
            return;

        EnsureLoopSfxSource();
        if (loopSfxSource == null)
        {
            Debug.LogWarning($"[AudioManager] 播放循环音效失败：loopSfxSource 未绑定 | object:{name} | type:{type}");
            return;
        }

        if (loopSfxFadeRoutine != null)
        {
            StopCoroutine(loopSfxFadeRoutine);
            loopSfxFadeRoutine = null;
        }

        loopSfxSource.volume = sfxSource != null ? sfxSource.volume : loopSfxSource.volume;

        if (loopSfxSource.isPlaying && loopSfxSource.clip == clip)
            return;

        loopSfxSource.clip = clip;
        loopSfxSource.loop = true;
        loopSfxSource.Play();
    }

    public void StopLoopSFX(float fadeTime = 0.25f)
    {
        EnsureLoopSfxSource();
        if (loopSfxSource == null || !loopSfxSource.isPlaying)
            return;

        if (loopSfxFadeRoutine != null)
        {
            StopCoroutine(loopSfxFadeRoutine);
        }

        loopSfxFadeRoutine = StartCoroutine(FadeStopLoopSFX(Mathf.Max(0f, fadeTime)));
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = volume;
        }

        if (loopSfxSource != null)
        {
            loopSfxSource.volume = volume;
        }
    }

    private IEnumerator FadeStopLoopSFX(float fadeTime)
    {
        float targetVolume = sfxSource != null ? sfxSource.volume : loopSfxSource.volume;
        float startVolume = loopSfxSource.volume;

        if (fadeTime <= 0f)
        {
            StopLoopSfxImmediate(targetVolume);
            yield break;
        }

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            loopSfxSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }

        StopLoopSfxImmediate(targetVolume);
    }

    private void StopLoopSfxImmediate(float restoreVolume)
    {
        loopSfxSource.Stop();
        loopSfxSource.clip = null;
        loopSfxSource.volume = restoreVolume;
        loopSfxFadeRoutine = null;
    }

    private bool TryGetBGM(BGMType type, out AudioClip clip)
    {
        if (bgmDict != null && bgmDict.TryGetValue(type, out clip) && clip != null)
            return true;

        Debug.LogWarning($"[AudioManager] 播放 BGM 失败：未配置音乐 | object:{name} | type:{type}");
        clip = null;
        return false;
    }

    private bool TryGetSFX(SFXType type, out AudioClip clip)
    {
        if (sfxDict != null && sfxDict.TryGetValue(type, out clip) && clip != null)
            return true;

        Debug.LogWarning($"[AudioManager] 播放音效失败：未配置音效 | object:{name} | type:{type}");
        clip = null;
        return false;
    }

    private void EnsureLoopSfxSource()
    {
        if (loopSfxSource != null)
            return;

        GameObject sourceObject = new GameObject("LoopSFXSource");
        sourceObject.transform.SetParent(transform);
        sourceObject.transform.localPosition = Vector3.zero;

        loopSfxSource = sourceObject.AddComponent<AudioSource>();
        loopSfxSource.playOnAwake = false;
        loopSfxSource.loop = true;

        if (sfxSource != null)
        {
            loopSfxSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            loopSfxSource.volume = sfxSource.volume;
            loopSfxSource.pitch = sfxSource.pitch;
            loopSfxSource.spatialBlend = sfxSource.spatialBlend;
        }
    }

    // 测试
    [ContextMenu("测试：游戏场景 BGM")]
    public void TestGameSceneBGM()
    {
        PlayBGM(BGMType.GameScene);
    }

    [ContextMenu("测试：主界面 BGM")]
    public void TestMainMenuBGM()
    {
        PlayBGM(BGMType.MainMenu);
    }

    [ContextMenu("测试：硬币碰撞敌人")]
    public void TestCoinEnemyCollideSFX()
    {
        PlaySFX(SFXType.Game_CoinEnemyCollide);
    }

    [ContextMenu("测试：硬币碰撞硬币")]
    public void TestCoinCoinCollideSFX()
    {
        PlaySFX(SFXType.Game_CoinCoinCollide);
    }
}
