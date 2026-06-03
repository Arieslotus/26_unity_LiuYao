using UnityEngine;

[ExecuteInEditMode]
public class HealthBar : MonoBehaviour
{
    enum ShapeType
    {
        Circle, Box, Rhombus
    };

    [SerializeField] ShapeType _shape;
    [SerializeField, Range(0,1)] float _healthNormalized;
    [SerializeField, Range(0,1)] float _lowHealthThreshold;

    [Header("Fill")]
    [SerializeField] Gradient _lowToHighHealthTransition;

    [Header("Wave")]
    [SerializeField, Range(0,0.1f)] float _fillWaveAmplitude;
    [SerializeField, Range(0,100f)] float _fillWaveFrequency;
    [SerializeField, Range(0, 1f)] float _fillWaveSpeed;

    [Header("Background")]
    [SerializeField] Color _backgroundColor;

    [Header("Border")]
    [SerializeField, Range(0, 0.15f)] float _borderWidth;
    [SerializeField] Color _borderColor;

    Material _matInstance;
    bool _hasLoggedMissingGradient;
    bool _hasLoggedSetupFailure;

    /// <summary>
    /// Health value between 0 and 1
    /// </summary>
    public float HealthNormalized
    {
        get
        {
            return _healthNormalized;
        }
        set
        {
            value = Mathf.Clamp(value, 0, 1);
            if (value == _healthNormalized) return;

            _healthNormalized = value;
            SetupUniqueMaterial();
            SetMaterialData();
        }
    }

    void Start()
    {
        SetupUniqueMaterial();
        SetMaterialData();
    }

    void SetupUniqueMaterial()
    {
        if (_matInstance != null) return;

        Shader shader = Shader.Find("CustomShaders/HealthBar");
        if (shader == null)
        {
            LogSetupFailure($"[HealthBar] 未找到 Shader | object:{name} | shader:CustomShaders/HealthBar");
            return;
        }

        Renderer targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null)
        {
            LogSetupFailure($"[HealthBar] 缺少 Renderer，无法创建血条材质 | object:{name}");
            return;
        }

        Debug.Log("Setup Material", this.gameObject);
        _matInstance = new Material(shader);
        if (Application.isPlaying)
        {
            targetRenderer.material = _matInstance;
        }
        else
        {
            targetRenderer.sharedMaterial = _matInstance;
        }
    }

    void LogSetupFailure(string message)
    {
        if (_hasLoggedSetupFailure)
            return;

        _hasLoggedSetupFailure = true;
        Debug.LogError(message, this);
    }

    void SetMaterialData()
    {
        SetupUniqueMaterial();
        if (_matInstance == null) return;

        _matInstance.SetFloat("_healthNormalized", _healthNormalized);

        SetKeyword();

        _matInstance.SetFloat("_lowLifeThreshold", _lowHealthThreshold);

        _matInstance.SetFloat("_waveAmp", _fillWaveAmplitude);
        _matInstance.SetFloat("_waveFreq", _fillWaveFrequency);
        _matInstance.SetFloat("_waveSpeed", _fillWaveSpeed);

        _matInstance.SetColor("_fillColor", EvaluateFillColor());

        _matInstance.SetColor("_backgroundColor", _backgroundColor);
        _matInstance.SetFloat("_borderWidth", _borderWidth);
        _matInstance.SetColor("_borderColor", _borderColor);
    }

    Color EvaluateFillColor()
    {
        if (_lowToHighHealthTransition != null)
        {
            return _lowToHighHealthTransition.Evaluate(_healthNormalized);
        }

        if (!_hasLoggedMissingGradient)
        {
            _hasLoggedMissingGradient = true;
            Debug.LogWarning($"[HealthBar] 未配置血量颜色 Gradient，使用白色作为备用颜色 | object:{name}", this);
        }

        return Color.white;
    }

    void SetKeyword()
    {
        foreach (var kword in _matInstance.shaderKeywords)
        {
            _matInstance.DisableKeyword(kword);
        }
        if (_shape == ShapeType.Circle) _matInstance.EnableKeyword("_SHAPE_CIRCLE");
        else if (_shape == ShapeType.Box) _matInstance.EnableKeyword("_SHAPE_BOX");
        else if (_shape == ShapeType.Rhombus) _matInstance.EnableKeyword("_SHAPE_RHOMBUS");

        //Sync shader keywordEnum
        _matInstance.SetInt("_shape", (int)_shape);
    }

    //void OnValidate()
    //{
    //    SetMaterialData();
    //}

    private void Update()
    {
        SetMaterialData();
    }

    void OnDestroy()
    {
        if (_matInstance != null)
        {
            if (Application.isPlaying)
                Destroy(_matInstance);
            else
                DestroyImmediate(_matInstance);
        }
    }
}
