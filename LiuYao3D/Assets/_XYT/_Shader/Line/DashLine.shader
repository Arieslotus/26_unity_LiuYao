Shader "Custom/DashLine"
{
    Properties
    {
    _DashTex ("Dash Mask", 2D) = "white" {}

        [HDR]_MainColor1 ("Main Color", Color) = (0,1,1,1)
        [HDR]_MainColor2 ("Main Color", Color) = (1,0,0,1)
        _GradientPos ("Gradient Position", Range(0,1)) = 0.5
_GradientSmooth ("Gradient Smooth", Range(0.001,1)) = 0.2


        //_DashSize ("Dash Size", Float) = 8
        //_Gap ("Gap", Range(0.01,0.99)) = 0.5

        _ScrollSpeed ("Scroll Speed", Float) = 1

        _FadePower ("Tail Fade", Float) = 1.5
        _FlowLong ("Fade Long", Float) = 0.5

        [HDR]_FlowColor ("Flow Color", Color) = (1,1,1,1)

        _FlowWidth ("Flow Width", Float) = 0.15
        _FlowSpeed ("Flow Speed", Float) = 2

        _FlowStrength ("Flow Strength", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
sampler2D _DashTex;
float4 _DashTex_ST;

            fixed4 _MainColor1;
            fixed4 _MainColor2;
            float _GradientPos;
float _GradientSmooth;


            //float _DashSize;
            //float _Gap;

            float _ScrollSpeed;

            float _FadePower;
            float _FlowLong;

            fixed4 _FlowColor;

            float _FlowWidth;
            float _FlowSpeed;
            float _FlowStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //--------------------------------
                // 沿线滚动UV
                //--------------------------------

                float lineUV =
                    i.uv.x +
                    _Time.y * _ScrollSpeed;

                ////--------------------------------
                //// 虚线
                ////--------------------------------

                //float dash =
                //    frac(lineUV * _DashSize);

                //float dashMask =
                //    step(_Gap, dash);

////--------------------------------
//// 虚化虚线
////--------------------------------

//float dash =
//    frac(lineUV * _DashSize);

//// 中心化
//dash = abs(dash - 0.5);

//// 圆角宽度
//float dashMask =
//    smoothstep(
//        0.5,
//        _Gap,
//        dash
//    );
//--------------------------------
// Dash Mask Texture
//--------------------------------

float2 dashUV =
    TRANSFORM_TEX(i.uv, _DashTex);

dashUV.x += _Time.y * _ScrollSpeed;

float dashMask =
    tex2D(_DashTex, dashUV).r;
                //--------------------------------
                // 尾部渐隐
                //--------------------------------

                float tailFade =
                    pow(
                        saturate(1 - i.uv.x + _FlowLong),
                        _FadePower
                    );

                //--------------------------------
                // 流光
                //--------------------------------

                float flowPos =
                    frac(
                        i.uv.x -
                        _Time.y * _FlowSpeed
                    );

                float flow =
                    smoothstep(
                        _FlowWidth,
                        0,
                        abs(flowPos - 0.5)
                    );
                flow *= dashMask;

//--------------------------------
// 基础渐变色
//--------------------------------

float gradient =
    smoothstep(
        _GradientPos - _GradientSmooth,
        _GradientPos + _GradientSmooth,
        i.uv.x
    );

fixed4 gradientColor =
    lerp(
        _MainColor1,
        _MainColor2,
        gradient
    );

fixed3 col = gradientColor.rgb;

                //--------------------------------
                // 叠加流光
                //--------------------------------

                col +=
                    _FlowColor.rgb *
                    flow *
                    _FlowStrength;

                //--------------------------------
                // 最终alpha
                //--------------------------------
float alpha =
    dashMask *
    tailFade *
    gradientColor.a;

                return fixed4(col, alpha);
            }

            ENDCG
        }
    }
}