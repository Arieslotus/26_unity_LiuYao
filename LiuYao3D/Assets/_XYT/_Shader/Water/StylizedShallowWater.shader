Shader "Custom/StylizedShallowWater"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.4,0.8,1,0.7)
        _DeepColor ("Deep Color", Color) = (0,0.15,0.3,0.85)

        _MainTex ("Normal Noise", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
_NormalSpeedX ("Normal Speed X", Float) = 0.05
_NormalSpeedY ("Normal Speed Y", Float) = 0.03

_NormalStrength ("Normal Strength", Range(0,2)) = 1

        _WaveSpeed ("Wave Speed", Float) = 0.05
        _WaveStrength ("Wave Strength", Float) = 0.05

        _DepthDistance ("Depth Distance", Float) = 3

        _FoamColor ("Foam Color", Color) = (1,1,1,1)
        _FoamDistance ("Foam Distance", Float) = 0.5

        _RefractionStrength ("Refraction", Float) = 0.03

        _FresnelPower ("Fresnel Power", Float) = 5
        _FresnelStrength ("Fresnel Strength", Float) = 1

        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
_SpecularPower ("Specular Power", Range(1,256)) = 64
_SpecularStrength ("Specular Strength", Range(0,5)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        GrabPass
        {
            "_WaterGrab"
        }

        Pass
        {
        Tags { "LightMode"="ForwardBase" }

            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            sampler2D _NormalMap;
            float _NormalSpeedX;
float _NormalSpeedY;

float _NormalStrength;
            sampler2D _CameraDepthTexture;
            sampler2D _WaterGrab;

            float4 _MainTex_ST;

            fixed4 _ShallowColor;
            fixed4 _DeepColor;
            fixed4 _FoamColor;

            float _WaveSpeed;
            float _WaveStrength;

            float _DepthDistance;
            float _FoamDistance;

            float _RefractionStrength;

            float _FresnelPower;
            float _FresnelStrength;

            fixed4 _SpecularColor;
float _SpecularPower;
float _SpecularStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;

                float2 uv : TEXCOORD0;

                float4 screenPos : TEXCOORD1;

                float3 worldPos : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.screenPos = ComputeScreenPos(o.pos);

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv1 = i.uv + _Time.y * _WaveSpeed;
                float2 uv2 = i.uv - _Time.y * _WaveSpeed * 0.7;

                float2 noise1 = tex2D(_MainTex, uv1).rg;
                float2 noise2 = tex2D(_MainTex, uv2).rg;

                float2 waveOffset = (noise1 + noise2 - 1) * _WaveStrength;
float2 normalUV =
    i.uv +
    float2(
        _Time.y * _NormalSpeedX,
        _Time.y * _NormalSpeedY
    );

float3 normalTex =
    UnpackNormal(
        tex2D(_NormalMap, normalUV)
    );

normalTex.xy *= _NormalStrength;


                //--------------------------------
                // 深度
                //--------------------------------

                float2 screenUV =
                    i.screenPos.xy / i.screenPos.w;

                float sceneDepth =
                    LinearEyeDepth(
                        SAMPLE_DEPTH_TEXTURE(
                            _CameraDepthTexture,
                            screenUV
                        )
                    );

                float waterDepth =
                    LinearEyeDepth(i.screenPos.z / i.screenPos.w);

                float depthDiff = sceneDepth - waterDepth;

                float depth01 =
                    saturate(depthDiff / _DepthDistance);

                //--------------------------------
                // 水颜色
                //--------------------------------

                fixed4 waterColor =
                    lerp(_ShallowColor, _DeepColor, depth01);

                //--------------------------------
                // 泡沫
                //--------------------------------

                float foam =
                    1 - saturate(depthDiff / _FoamDistance);

                waterColor.rgb =
                    lerp(
                                            waterColor.rgb,
                        _FoamColor.rgb,

                        foam
                    );

                //--------------------------------
                // 折射
                //--------------------------------

                float2 refractUV =
                    screenUV +
                    normalTex.xy * _RefractionStrength;

                fixed3 refractCol =
                    tex2D(_WaterGrab, refractUV).rgb;

                //--------------------------------
                // Fresnel
                //--------------------------------

                float3 viewDir =
                    normalize(
                        _WorldSpaceCameraPos.xyz - i.worldPos
                    );

                float fresnel =
                    pow(
                        1 - saturate(dot(viewDir, i.worldNormal)),
                        _FresnelPower
                    );

                fresnel *= _FresnelStrength;

                //--------------------------------
// Specular
//--------------------------------

float3 lightDir =
    normalize(_WorldSpaceLightPos0.xyz);

float3 halfDir =
    normalize(lightDir + viewDir);

float3 worldNormal =
    normalize(i.worldNormal + normalTex);
float3 NdL = saturate(dot(worldNormal, halfDir));
float spec =
    pow(
        NdL,
        _SpecularPower
    );

spec *= _SpecularStrength;

float3 specular =
    spec * _SpecularColor.rgb;

                //--------------------------------
                // 最终颜色
                //--------------------------------

                fixed3 finalCol = //waterColor;
                    lerp(refractCol, waterColor.rgb, 0.5);

                finalCol += fresnel;
finalCol += specular;

                //return fixed4(specular,1);
                return fixed4(finalCol, waterColor.a);
            }

            ENDCG
        }
    }
}