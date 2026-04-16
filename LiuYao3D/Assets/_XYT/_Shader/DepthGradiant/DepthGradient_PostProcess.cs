using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class DepthGradient_PostProcess : MonoBehaviour
{
    [Header("渐变设置")]
    public Texture2D rampTexture;                    // 渐变纹理
    [Range(0.1f, 50f)]
    public float gradientStart = 5f;
    [Range(0.1f, 50f)]
    public float gradientEnd = 10f;
    [Range(0.1f, 5f)]
    public float gradientPower = 1f;
    [Range(0f, 1f)]
    public float intensity = 1f;

    [Header("径向渐变设置")]
    [Range(0, 360)]
    public float radialAngle = 0f;                   // 线性渐变方向角度
    [Range(0, 1)]
    public float radialCenterX = 0.5f;               // 径向中心 X
    [Range(0, 1)]
    public float radialCenterY = 0.5f;               // 径向中心 Y
    [Range(0, 1)]
    public float radialType = 0f;                    // 0=线性渐变, 1=径向渐变

    //[Header("颜色过渡")]
    //public Color gradientColor = Color.red;           // 备用颜色（当没有渐变纹理时使用）

    [Header("调试")]
    public bool enableEffect = true;

    [SerializeField] Shader effectShader;

    private Material effectMaterial;
    private Coroutine colorTransitionCoroutine;
    public bool isTransitioningColor => colorTransitionCoroutine != null;

    void OnEnable()
    {
        // 确保相机深度纹理已开启
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

        // 创建材质
        CreateMaterial();
    }

    void OnDisable()
    {
        if (effectMaterial != null)
        {
            DestroyImmediate(effectMaterial);
        }
    }

    void CreateMaterial()
    {
        if (effectShader == null)
        {
            effectShader = Shader.Find("Custom/DepthGradientPostEffect");
        }

        if (effectShader != null && effectMaterial == null)
        {
            effectMaterial = new Material(effectShader);
            effectMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!enableEffect || effectMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // 设置 Shader 参数
        if (rampTexture != null)
        {
            effectMaterial.SetTexture("_RampTex", rampTexture);
        }

        effectMaterial.SetFloat("_GradientStart", gradientStart);
        effectMaterial.SetFloat("_GradientEnd", gradientEnd);
        effectMaterial.SetFloat("_GradientPower", gradientPower);
        effectMaterial.SetFloat("_Intensity", intensity);

        // 径向渐变参数
        effectMaterial.SetFloat("_RadialAngle", radialAngle);
        effectMaterial.SetFloat("_RadialCenterX", radialCenterX);
        effectMaterial.SetFloat("_RadialCenterY", radialCenterY);
        effectMaterial.SetFloat("_RadialType", radialType);

        // 备用颜色（Shader 中需要添加 _GradientColor 属性才能用）
        // effectMaterial.SetColor("_GradientColor", gradientColor);

        // 应用后处理
        Graphics.Blit(source, destination, effectMaterial);
    }

    void OnValidate()
    {
        // 确保参数有效性
        gradientStart = Mathf.Max(0.1f, gradientStart);
        gradientEnd = Mathf.Max(gradientStart + 0.1f, gradientEnd);

        radialAngle = Mathf.Clamp(radialAngle, 0, 360);
        radialCenterX = Mathf.Clamp01(radialCenterX);
        radialCenterY = Mathf.Clamp01(radialCenterY);
        radialType = Mathf.Clamp01(radialType);

        // 重新创建材质（在编辑器模式下）
        if (Application.isEditor && !Application.isPlaying)
        {
            CreateMaterial();
        }
    }


 
    /// <summary>
    /// 设置径向渐变模式
    /// </summary>
    public void SetRadialMode(bool isRadial)
    {
        radialType = isRadial ? 1f : 0f;
    }

    /// <summary>
    /// 设置线性渐变角度
    /// </summary>
    public void SetRadialAngle(float angle)
    {
        radialAngle = Mathf.Clamp(angle, 0, 360);
    }

    /// <summary>
    /// 设置径向中心点
    /// </summary>
    public void SetRadialCenter(Vector2 center)
    {
        radialCenterX = Mathf.Clamp01(center.x);
        radialCenterY = Mathf.Clamp01(center.y);
    }

    /// <summary>
    /// 重置所有参数为默认值
    /// </summary>
    public void ResetParameters()
    {
        gradientStart = 5f;
        gradientEnd = 10f;
        gradientPower = 1f;
        intensity = 1f;
        radialAngle = 0f;
        radialCenterX = 0.5f;
        radialCenterY = 0.5f;
        radialType = 0f;
        //gradientColor = Color.red;
    }
}