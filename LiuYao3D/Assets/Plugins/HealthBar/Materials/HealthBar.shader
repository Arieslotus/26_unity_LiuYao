Shader "CustomShaders/HealthBar"
{
    Properties
    {
        [KeywordEnum(Circle,Box,Rhombus)] _shape("Shape",Float) = 0
        _healthNormalized("Health Normalized", Range(0,1)) = 0.0
        _lowHealthThreshold("Low Health Threshold", Range(0,1)) = 0.2
        _fillColor("Fill Color", Color) = (0,0,0,1)

        _waveAmp("Wave Amp", float) = 0.01
        _waveFreq("Wave Freq", float) = 8
        _waveSpeed("Wave Speed", float) = 0.5

        _backgroundColor("Background Color", Color) = (0,0,0,0.25)
        _borderWidth("Border Width", Range(0,0.4)) = 0
        _borderColor("Border Color", Color) = (0.1,0.1,0.1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _SHAPE_CIRCLE _SHAPE_BOX _SHAPE_RHOMBUS

            #include "UnityCG.cginc"
            #include "MyFunctions.hlsl"

            float _healthNormalized;
            float _lowHealthThreshold;

            float4 _fillColor;
            float4 _backgroundColor;
            float4 _borderColor;

            float _borderWidth;
            float _waveAmp;
            float _waveFreq;
            float _waveSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 posOS : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.posOS = v.vertex.xyz;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 objectScale = GetObjectScale();

                float minScale = min(objectScale.x, objectScale.y);
                float margin = minScale * 0.1;

                float3 elong = (objectScale - minScale) * 0.5;

                float3 p = i.posOS * objectScale;
                float3 q = Elongate(p, elong);

                float halfSize = minScale * 0.5 - margin;

                float healthBarSDF;

                #if _SHAPE_CIRCLE
                    healthBarSDF = CircleSDF(q, halfSize);
                #elif _SHAPE_BOX
                    healthBarSDF = BoxSDF(q, halfSize);
                #elif _SHAPE_RHOMBUS
                    healthBarSDF = RhombusSDF(q, float2(halfSize, halfSize));
                #endif

                float healthBarMask = GetSmoothMask(healthBarSDF);

                // ²¨ÀË
                float waveOffset = _waveAmp * cos(_waveFreq * (i.uv.x + _Time.y * _waveSpeed)) 
                                 * min(1.3 * sin(UNITY_PI * _healthNormalized), 1);

                float marginY = margin / objectScale.y;
                float fillOffset = marginY + _borderWidth;

                float healthMapped = lerp(fillOffset - 0.01, 1 - fillOffset, _healthNormalized);

                float fillSDF = i.uv.y - healthMapped + waveOffset;
                float fillMask = GetSmoothMask(fillSDF);

                // ±ß¿ò
                float borderSDF = healthBarSDF + _borderWidth * objectScale.y;
                float borderMask = 1 - GetSmoothMask(borderSDF);

                float4 col =
                    fillMask * (1 - borderMask) * _fillColor +
                    (1 - fillMask) * (1 - borderMask) * _backgroundColor +
                    borderMask * _borderColor;

                float4 outColor = healthBarMask * col;

                // ÖÐÐÄ¸ß¹â
float3 highlight = 2 - healthBarSDF / (minScale * 0.5);
outColor *= float4(highlight, 1);

                // µÍÑªÉÁË¸
                if (_healthNormalized < _lowHealthThreshold)
                {
                    float flash = 0.1 * cos(6 * _Time.y) + 0.1;
                    outColor.rgb += flash;
                }

                return outColor;
            }

            ENDCG
        }
    }
}