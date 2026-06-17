/// <summary>
/// 实现功能：运行时统一为场景中的 Unity UI Button 绑定点击音效，减少逐个按钮手动接音效的维护成本。
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIButtonClickSFXBinder : MonoBehaviour
{
    [Header("音效")]
    [Tooltip("所有 UI Button 点击时播放的音效。")]
    [SerializeField] private SFXType clickSfx = SFXType.UI_ClickButton;

    [Header("扫描")]
    [Tooltip("是否包含未激活物体上的按钮。弹窗初始隐藏时建议开启。")]
    [SerializeField] private bool includeInactiveButtons = true;

    [Tooltip("动态 UI 的补绑扫描间隔。小于等于 0 时只在启用和切场景时绑定一次。")]
    [Min(0f)]
    [SerializeField] private float refreshInterval = 0.5f;

    [Header("调试")]
    [SerializeField] private bool debugLog;

    private readonly List<Button> boundButtons = new List<Button>();
    private Coroutine refreshRoutine;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindAllButtons();

        if (refreshInterval > 0f)
        {
            refreshRoutine = StartCoroutine(RefreshBindingLoop());
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }

        UnbindAllButtons();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RemoveDestroyedButtons();
        BindAllButtons();
    }

    private IEnumerator RefreshBindingLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(refreshInterval);

        while (true)
        {
            yield return wait;
            RemoveDestroyedButtons();
            BindAllButtons();
        }
    }

    private void BindAllButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(includeInactiveButtons);
        int newBindCount = 0;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || boundButtons.Contains(button))
                continue;

            button.onClick.AddListener(PlayClickSFX);
            boundButtons.Add(button);
            newBindCount++;
        }

        if (debugLog && newBindCount > 0)
        {
            Debug.Log($"[UIButtonClickSFXBinder] 绑定 UI 点击音效 | object:{name} | new:{newBindCount} | total:{boundButtons.Count}");
        }
    }

    private void UnbindAllButtons()
    {
        for (int i = 0; i < boundButtons.Count; i++)
        {
            Button button = boundButtons[i];
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSFX);
            }
        }

        boundButtons.Clear();
    }

    private void RemoveDestroyedButtons()
    {
        for (int i = boundButtons.Count - 1; i >= 0; i--)
        {
            if (boundButtons[i] == null)
            {
                boundButtons.RemoveAt(i);
            }
        }
    }

    private void PlayClickSFX()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFX(clickSfx);
    }
}
