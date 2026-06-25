/// <summary>
/// Coin selected outline shader with Standard lighting, normal map, emission and adjustable outline.
/// </summary>
Shader "Custom/Coin Selected Outline"
{
    Properties
    {
        [Header(Base Maps)]
        _Color ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}

        [Header(Normal)]
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1

        [Header(Metallic Smoothness)]
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _MetallicGlossMap ("Metallic(R) Smoothness(A)", 2D) = "white" {}
        [Toggle(_METALLICGLOSSMAP)] _UseMetallicGlossMap ("Use Metallic Map", Float) = 0

        [Header(Occlusion)]
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1

        [Header(Body Emission)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        [Toggle(_EMISSION_ON)] _UseEmission ("Use Emission", Float) = 0
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 1

        [Header(Selection Outline)]
        [HDR] _OutlineColor ("Outline Color", Color) = (1,0.78,0.22,1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 2
        [Toggle] _OutlineEnabled ("Outline Enabled", Float) = 1

        [Header(Outline Glow)]
        [Toggle] _OutlineGlowEnabled ("Outline Glow Enabled", Float) = 1
        _OutlineGlowIntensity ("Outline Glow Intensity", Range(0, 10)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 300

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

            v2f vert(appdata v)
            {
                v2f o;

                float4 viewPos = mul(UNITY_MATRIX_MV, v.vertex);
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                viewNormal.z = -0.5;
                viewPos.xyz += normalize(viewNormal) * _OutlineWidth * 0.01 * _OutlineEnabled;

                o.pos = mul(UNITY_MATRIX_P, viewPos);
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

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma shader_feature_local _METALLICGLOSSMAP
        #pragma shader_feature_local _EMISSION_ON

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicGlossMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;

        fixed4 _Color;
        half _BumpScale;
        half _Metallic;
        half _Glossiness;
        half _OcclusionStrength;
        fixed4 _EmissionColor;
        half _EmissionIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_MetallicGlossMap;
            float2 uv_OcclusionMap;
            float2 uv_EmissionMap;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = albedo.rgb;
            o.Alpha = albedo.a;

            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_BumpMap), _BumpScale);

            #if defined(_METALLICGLOSSMAP)
                fixed4 metallicGloss = tex2D(_MetallicGlossMap, IN.uv_MetallicGlossMap);
                o.Metallic = metallicGloss.r * _Metallic;
                o.Smoothness = metallicGloss.a * _Glossiness;
            #else
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
            #endif

            half occlusion = tex2D(_OcclusionMap, IN.uv_OcclusionMap).g;
            o.Occlusion = lerp(1.0h, occlusion, _OcclusionStrength);

            #if defined(_EMISSION_ON)
                fixed3 emission = tex2D(_EmissionMap, IN.uv_EmissionMap).rgb * _EmissionColor.rgb;
                o.Emission = emission * _EmissionIntensity;
            #else
                o.Emission = 0;
            #endif
        }
        ENDCG
    }

    FallBack "Standard"
}
