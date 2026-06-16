using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 在这里编辑要添加的bgm序号
public enum BGMType
{
    Start = 0, // 封面
    Game = 1,   // 游戏内
}

// 在这里编辑要添加的sfx序号
public enum SFXType
{
    UI_ClickButton = 0,
    UI_ClickCoin = 1,
    

    Game_Coin = 10,
    Game_CoinFlip = 11,     // 硬币翻面
    Game_CoinCollide = 12, // 硬币碰撞
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

    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public List<BGMEntry> bgms = new(); // 在面板配置游戏内的音乐
    public List<SFXEntry> sfxs = new();

    Dictionary<BGMType, AudioClip> bgmDict;
    Dictionary<SFXType, AudioClip> sfxDict;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        bgmDict = new Dictionary<BGMType, AudioClip>();
        sfxDict = new Dictionary<SFXType, AudioClip>();

        foreach (var item in bgms)
            bgmDict[item.type] = item.clip;

        foreach (var item in sfxs)
            sfxDict[item.type] = item.clip;
    }

    //----------------------------------
    // BGM
    //----------------------------------

    //public void PlayBGM(BGMType type)
    //{
    //    if (!bgmDict.TryGetValue(type, out var clip))
    //        return;

    //    if (bgmSource.clip == clip)
    //        return;

    //    bgmSource.clip = clip;
    //    bgmSource.loop = true;
    //    bgmSource.Play();
    //}

    // ** 播放bgm AudioManager.instance.PlayBGM
    public void PlayBGM(BGMType type)
    {
        if (!bgmDict.TryGetValue(type, out var clip))
            return;

        if (bgmSource.clip == clip)
            return;

        StopAllCoroutines();
        StartCoroutine(FadeSwitchBGM(clip));
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }
    IEnumerator FadeSwitchBGM(AudioClip newClip)
    {
        float fadeTime = 1f;
        // Fade Out
        float startVolume = bgmSource.volume;

        float t = 0;

        while (t < fadeTime)
        {
            t += Time.deltaTime;

            bgmSource.volume =
                Mathf.Lerp(
                    startVolume,
                    0,
                    t / fadeTime
                );

            yield return null;
        }

        // Switch

        bgmSource.Stop();

        bgmSource.clip = newClip;

        bgmSource.loop = true;

        bgmSource.Play();
        // Fade In
        t = 0;

        while (t < fadeTime)
        {
            t += Time.deltaTime;

            bgmSource.volume =
                Mathf.Lerp(
                    0,
                    startVolume,
                    t / fadeTime
                );

            yield return null;
        }

        bgmSource.volume = startVolume;
    }


    //----------------------------------
    // SFX
    //----------------------------------

    // ** 播放音效 AudioManager.instance.PlaySFX
    public void PlaySFX(SFXType type)
    {
        if (!sfxDict.TryGetValue(type, out var clip))
            return;

        sfxSource.PlayOneShot(clip);
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }


    // 测试
    [ContextMenu("1")]
    public void Test1()
    {
        PlayBGM(BGMType.Game);
    }
    [ContextMenu("2")]
    public void Test2()
    {
        PlayBGM(BGMType.Start);
    }

    [ContextMenu("3")]
    public void Test3()
    {
        PlaySFX(SFXType.Game_Coin);
    }
    [ContextMenu("4")]
    public void Test4()
    {
        PlaySFX(SFXType.Game_CoinCollide);
    }
}
