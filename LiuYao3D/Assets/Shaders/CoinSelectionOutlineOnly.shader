/// <summary>
/// Outline-only shader for selected coin visualization.
/// </summary>
Shader "Custom/Coin Selection Outline Only"
{
    Properties
    {
        [Header(Selection Outline)]
        [HDR] _OutlineColor ("Outline Color", Color) = (1,0.78,0.22,1)
        _OutlineWidth ("Outline Width", Range(0, 20)) = 4
        [Toggle] _OutlineEnabled ("Outline Enabled", Float) = 1

        [Header(Outline Glow)]
        [Toggle] _OutlineGlowEnabled ("Outline Glow Enabled", Float) = 1
        _OutlineGlowIntensity ("Outline Glow Intensity", Range(0, 10)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "STENCIL_MASK"
            Tags { "LightMode" = "Always" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            ColorMask 0

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return 0;
            }
            ENDCG
        }

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "Always" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineEnabled;
            float _OutlineGlowEnabled;
            float _OutlineGlowIntensity;

            float2 SafeNormalize2(float2 value, float2 fallback)
            {
                float lenSq = dot(value, value);
                return lenSq > 0.000001 ? value * rsqrt(lenSq) : fallback;
            }

            v2f vert(appdata v)
            {
                v2f o;

                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                viewNormal.z = -0.5;

                float4 clipPos = UnityObjectToClipPos(v.vertex);
                float2 outlineDirection = SafeNormalize2(
                    TransformViewToProjection(normalize(viewNormal).xy),
                    float2(0, 1)
                );
                float2 pixelSize = 2.0 / _ScreenParams.xy;
                clipPos.xy += outlineDirection * _OutlineWidth * pixelSize * clipPos.w * _OutlineEnabled;

                o.pos = clipPos;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _OutlineColor;
                col.rgb *= lerp(1.0, _OutlineGlowIntensity, saturate(_OutlineGlowEnabled));
                col.a *= saturate(_OutlineEnabled);

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
