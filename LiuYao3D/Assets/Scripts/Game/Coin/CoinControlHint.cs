/// <summary>
/// 实现功能：控制当前可操作硬币的提示特效，在硬币被激活操作时生成特效预制体，失去操作权时隐藏或销毁。
/// </summary>
using UnityEngine;

public class CoinControlHint : MonoBehaviour
{
    [Header("提示特效")]
    [Tooltip("当前硬币可操作时生成的提示特效预制体。")]
    [SerializeField] private GameObject controlEffectPrefab;

    [Tooltip("特效生成父节点。为空时使用当前 CoinControlHint 节点。")]
    [SerializeField] private Transform effectRoot;

    [Tooltip("特效生成后的本地位置。")]
    [SerializeField] private Vector3 localPosition = Vector3.zero;

    [Tooltip("特效生成后的本地旋转。")]
    [SerializeField] private Vector3 localEulerAngles = Vector3.zero;

    [Tooltip("特效生成后的本地缩放。")]
    [SerializeField] private Vector3 localScale = Vector3.one;

    [Tooltip("隐藏时是否销毁特效实例。关闭时会复用同一个实例，适合循环特效。")]
    [SerializeField] private bool destroyInstanceOnHide = false;

    private GameObject effectInstance;

    private void Awake()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (effectInstance != null)
        {
            Destroy(effectInstance);
            effectInstance = null;
        }
    }

    public void Show()
    {
        if (controlEffectPrefab == null)
        {
            Debug.LogWarning($"[CoinControlHint] {name} 未绑定操作提示特效预制体，无法显示当前操作提示。");
            return;
        }

        EnsureEffectInstance();

        if (effectInstance == null)
            return;

        effectInstance.SetActive(true);
    }

    public void Hide()
    {
        if (effectInstance == null)
            return;

        if (destroyInstanceOnHide)
        {
            Destroy(effectInstance);
            effectInstance = null;
            return;
        }

        effectInstance.SetActive(false);
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

    private void EnsureEffectInstance()
    {
        if (effectInstance != null)
            return;

        Transform root = effectRoot != null ? effectRoot : transform;
        effectInstance = Instantiate(controlEffectPrefab, root);
        effectInstance.transform.localPosition = localPosition;
        effectInstance.transform.localRotation = Quaternion.Euler(localEulerAngles);
        effectInstance.transform.localScale = localScale;
    }
}
