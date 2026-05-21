/// <summary>
/// 实现功能：控制硬币当前可操作提示物体的显示与隐藏，适用于 Plane、模型或 UI 子物体。
/// </summary>
using UnityEngine;

public class CoinControlHint : MonoBehaviour
{
    [Header("提示物体")]
    [Tooltip("用于提示当前硬币可操作的物体，例如 Plane。")]
    [SerializeField] private GameObject hintObject;

    private void Reset()
    {
        if (transform.childCount > 0)
        {
            hintObject = transform.GetChild(0).gameObject;
        }
    }

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        CacheHintObject();

        if (hintObject == null)
        {
            Debug.LogWarning($"[CoinControlHint] {name} 未绑定提示物体，无法显示当前操作提示。");
            return;
        }

        hintObject.SetActive(true);
    }

    public void Hide()
    {
        CacheHintObject();

        if (hintObject == null)
            return;

        hintObject.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            Show();
            return;
        }

        Hide();
    }

    private void CacheHintObject()
    {
        if (hintObject != null)
            return;

        if (transform.childCount > 0)
        {
            hintObject = transform.GetChild(0).gameObject;
        }
    }
}
