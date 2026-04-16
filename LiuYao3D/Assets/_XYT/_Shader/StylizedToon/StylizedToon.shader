Shader "Custom/StylizedToonFull"
{
    Properties
    {
        _RampTex ("Gradient Ramp", 2D) = "white" {}

        // 固有色
        [Header(Albedo_Mask)]
        _MainTex ("Albedo Tex", 2D) = "white" {}
        _ChannelMask ("Channel Mask", Range(0, 4)) = 4     // 0=R, 1=G, 2=B, 3=A, 4=Max(RGB)
        _Brightness ("Mask Brightness", Range(-1, 1)) = 0
        _Contrast ("Mask Contrast", Range(0, 5)) = 1

        // 改变 固有色 使用多少渐变
        [Header(Albedo_Gradient)]
        _AlbedoGradientInetsity("Albedo Use Gradient Intensity", Range(0,1)) = 0.7
        _LightAlbedoIntensity("Light Part Use Albedo Intensity", Range(0,1)) = 0
        _DarkAlbedoIntensity("Dark Part Use Albedo Intensity", Range(0,1)) = 1

        // 基础色 二分
        [Header(Toon_Mask)]
        _Threshold ("Light Threshold", Range(0,1)) = 0.5
        _Smooth ("Edge Smooth", Range(0.001,0.2)) = 0.05

        [Header(Toon_Color)]
        _LightColor ("Light Color", Color) = (1,1,1,1)
        _LightIntensity("Light Intensity", Range(1,3)) = 1.4
        _DarkIntensity("Dark Intensity", Range(0.001,1)) = 0.4
        _LightDarkColorMul ("Color Mutiply", Color) = (1,1,1,1)

        [Space(10)]
        _IsCustomizeDarkColor("is Customize Dark Color", Range(0,1)) = 0
        _DarkColor ("Dark Color", Color) = (0.2,0.2,0.2,1)

        // 亮暗部 使用多少渐变
        [Header(Toon_Gradient)]
        _LightGradientIntensity("Light Gradient Intensity", Range(0,1)) = 0.5 // 亮部的 渐变应用程度
        _DarkGradientIntensity("Dark Gradient Intensity", Range(0,1)) = 0.05 // 暗部的 渐变应用程度
        _ShadowEdgeGradientIntensity("Shadow Edge Gradient Intensity", Range(0,1)) = 0.5 // 明暗交界线的 渐变应用程度

        [Space(10)]
        _GradientPower ("Gradient Power", Range(0.1, 20)) = 1         // 渐变对比度/强弱
        
        _GradientType ("Gradient Type", Range(0, 1)) = 0             // 0=线性渐变, 1=径向渐变
        _GradientAxis ("Gradient Axis", Range(0, 2)) = 0             // 0=X, 1=Y, 2=Z
        _GradientAngle ("Gradient Angle", Range(0, 360)) = 0           // 渐变方向角度

        _GradientOffsetX ("Linear Offset X", Float) = 0            // 线性偏移 X
        _GradientOffsetY ("Linear Offset Y", Float) = 0            // 线性偏移 Y
        _RadialStretchX ("Radial Stretch X", Range(0.1, 5)) = 1      // 径向椭圆拉伸 X
        _RadialStretchY ("Radial Stretch Y", Range(0.1, 5)) = 1      // 径向椭圆拉伸 Y

        // 阴影边缘
        [Header(ShadowEdge)]
        _ShadowEdgeColor ("Shadow Edge Color", Color) = (1,0.4,0.2,1)
        _ShadowEdgeMulColor ("Shadow Edge Mul Color", Color) = (1,1,1,1)
        _ShadowEdgeWidth ("Shadow Edge Width", Range(0.0,0.5)) = 0.1

        _BrushTex ("Brush Tex", 2D) = "white" {}
        _BrushStrength ("Brush Strength", Range(0,1)) = 0.5

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        // ======================
        // 主 Pass（卡通渲染）
        // ======================
        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            
            //UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _RampTex;
            sampler2D _BrushTex;

            float4 _LightColor;
            float4 _DarkColor;
            float4 _LightDarkColorMul;
            float _IsCustomizeDarkColor;
            float _DarkIntensity, _LightIntensity;

            float _Threshold;
            float _Smooth;

            float4 _ShadowEdgeColor,_ShadowEdgeMulColor;
            float _ShadowEdgeWidth;

            float _BrushStrength;

            float _GradientAngle;
            float _LightGradientIntensity, _DarkGradientIntensity, _ShadowEdgeGradientIntensity;
            float _GradientOffsetX;
            float _GradientOffsetY;
            float _GradientPower;
            float _GradientAxis;
            float _GradientType;
            float _RadialStretchX;
            float _RadialStretchY;

float _ChannelMask;
float _Brightness;
float _Contrast;
float _AlbedoGradientInetsity;
float _DarkAlbedoIntensity,_LightAlbedoIntensity;

		float _reflectionFactor;


            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD3;
                float3 objectPos : TEXCOORD4;  // 传递物体空间坐标
                float4 pos : SV_POSITION;

                SHADOW_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.objectPos = v.vertex.xyz;  // 物体空间坐标
                o.uv = v.uv;
                TRANSFER_SHADOW(o);
                return o;
            }


//// 不需要 sampler2D 参数，直接使用 Unity 内置的阴影采样
//float sampleShadowMapWithPCF(float4 shadowCoord, float2 offset) 
//{
//    // 使用 UNITY_SAMPLE_SHADOW 宏，它会自动处理一切
//    // 注意：变量名是 _ShadowCoord（大写S）
//    return UNITY_SAMPLE_SHADOW(_ShadowMapTexture, float3(shadowCoord.xy + offset, shadowCoord.z));
//}

//float getSoftShadow(float4 shadowCoord, float blurRadius) 
//{
//    float shadow = 0.0;
//    float samples = 0.0;
    
//    // 获取纹素大小，用于正确的偏移量
//    float2 texelSize = _ShadowMapTexture_TexelSize.xy;
    
//    for (float x = -blurRadius; x <= blurRadius; x += blurRadius) 
//    {
//        for (float y = -blurRadius; y <= blurRadius; y += blurRadius) 
//        {
//            float2 offset = float2(x, y) * texelSize;
//            shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, float3(shadowCoord.xy + offset, shadowCoord.z));
//            samples++;
//        }
//    }
    
//    return shadow / samples;
//}
// 2D 旋转函数
float2 Rotate2D(float2 v, float angleDegrees)
{
    float rad = radians(angleDegrees);
    float s = sin(rad);
    float c = cos(rad);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

// 计算线性渐变的 U 值
float GetLinearGradient(float3 objectPos)
{
    float2 gradCoord;
    
    // 根据选择的轴确定渐变坐标
    if (_GradientAxis < 0.5)      // X 轴
    {
        gradCoord = float2(objectPos.x, 0);
    }
    else if (_GradientAxis < 1.5) // Y 轴
    {
        gradCoord = float2(objectPos.y, 0);
    }
    else if (_GradientAxis < 2.5) // Z 轴
    {
        gradCoord = float2(objectPos.z, 0);
    }
    //else                          // 自定义方向（XZ 平面，可旋转）
    //{
    //    gradCoord = float2(objectPos.x, objectPos.z);
    //}
    
    // 应用偏移
    gradCoord -= float2(_GradientOffsetX, _GradientOffsetY);
    
    // 应用旋转
    gradCoord = Rotate2D(gradCoord, _GradientAngle);
    
    // 线性渐变：使用 X 轴方向
    float u = gradCoord.x;
    
    return u;
}

// 计算径向渐变的 U 值
float GetRadialGradient(float3 objectPos)
{
    // 获取径向坐标
    float2 radialCoord = 0;

    if (_GradientAxis < 0.5)      // X 轴
    {
        radialCoord = float2(objectPos.y, objectPos.z);
    }
    else if (_GradientAxis < 1.5) // Y 轴
    {
        radialCoord = float2(objectPos.x, objectPos.z);
    }
    else if (_GradientAxis < 2.5) // Z 轴
    {
        radialCoord = float2(objectPos.x, objectPos.y);
    }
    
    // 应用偏移
    radialCoord -= float2(_GradientOffsetX, _GradientOffsetY);
    
    // 应用椭圆拉伸（先拉伸再计算距离）
    radialCoord.x /= max(0.01, _RadialStretchX);
    radialCoord.y /= max(0.01, _RadialStretchY);
    
    // 应用旋转（让椭圆可以旋转）
    radialCoord = Rotate2D(radialCoord, _GradientAngle);
    
    // 计算到中心点的距离（0 到 1 范围）
    // 距离越远，值越大（从中心向外渐变）
    float dist = length(radialCoord);
    
    // 控制渐变范围（默认半径 5 个单位，可调整）
    float radius = 5.0;
    float u = dist / radius;
    
    return u;
}
// 主渐变函数
float GetObjectSpaceGradient(float3 objectPos)
{
    float u;
    
    // 根据类型选择渐变
    if (_GradientType < 0.5)  // 线性渐变
    {
        u = GetLinearGradient(objectPos);
    }
    else  // 径向渐变
    {
        u = GetRadialGradient(objectPos);
    }
    
    // 映射到 0-1 范围
    // 假设线性渐变范围在 [-5, 5] 内，径向半径 5
    float range = 5.0;
    float t = (u + range) / (2.0 * range);
    t = saturate(t);
    
    // 应用渐变强弱（对比度）
    t = pow(t, _GradientPower);
    
    return t;
}



// 提取指定通道的函数
float ExtractChannel(float4 color, float channel)
{
    float result = 0;
    
    if (channel < 0.5)           // R 通道
        result = color.r;
    else if (channel < 1.5)      // G 通道
        result = color.g;
    else if (channel < 2.5)      // B 通道
        result = color.b;
    else if (channel < 3.5)      // A 通道
        result = color.a;
    else                         // Max(RGB) - 取 RGB 最大值
        result = max(color.r, max(color.g, color.b));
    
    return result;
}

// 亮度/对比度调节函数
float AdjustBrightnessContrast(float value, float brightness, float contrast)
{
    // 亮度：加法
    value += brightness;
    // 对比度：以 0.5 为基准拉伸
    value = (value - 0.5) * contrast + 0.5;
    // 钳制到有效范围
    return saturate(value);
}



            fixed4 frag (v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);

                float ndl = dot(N, L);
                float shadow = SHADOW_ATTENUATION(i);
                ndl *= shadow;

                // ===== 扁平化（二分色）=====
                // mask
                float lightMask = smoothstep(_Threshold - _Smooth, _Threshold + _Smooth, ndl);

                // light color
                float3 lightCol = _LightColor.rgb * _LightIntensity;
                lightCol *= _LightDarkColorMul;

                // dark color
                float3 darkCol = _LightColor.rgb * _DarkIntensity * (1-_IsCustomizeDarkColor) + 
                                _DarkColor.rgb * _IsCustomizeDarkColor;
                darkCol *= _LightDarkColorMul;

                // ==== 固有色 ====
                float4 baseTex = tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
                float albedoMask = ExtractChannel(baseTex, _ChannelMask);
                albedoMask = AdjustBrightnessContrast(albedoMask, _Brightness, _Contrast);

                // * ramp 最下面的渐变
                float3 rampAlbedoColor = tex2D(_RampTex, float2(albedoMask, 0)).rgb;
                rampAlbedoColor = lerp(baseTex, rampAlbedoColor, _AlbedoGradientInetsity);
                

//return float4(rampDarkCol.xyz,1);

                // ===== 渐变 =====

                // 计算渐变 U 值
                float rampU = GetObjectSpaceGradient(i.objectPos); //* _RampScale + _RampOffset
    
                // 采样渐变纹理
                // * ramp 最上面的渐变
                float3 rampColor = tex2D(_RampTex, float2(rampU, 1)).rgb;

                // how to blend : ramp color / albedo == light dark color
                float4 rampLightCol = 0;
                float4 rampDarkCol = 0;

                rampLightCol = float4(lerp(lightCol.rgb, rampColor, _LightGradientIntensity), 1);
                rampDarkCol = float4(lerp(darkCol.rgb, rampColor, _DarkGradientIntensity), 1);

                rampLightCol = float4(lerp(rampLightCol.rgb, rampAlbedoColor, _LightAlbedoIntensity), 1);
                rampDarkCol = float4(lerp(rampDarkCol.rgb, rampAlbedoColor, _DarkAlbedoIntensity), 1);


//return float4(rampDarkCol.xyz,1);

                //

                float4 toon = lerp(rampDarkCol, rampLightCol, lightMask);


//return float4(rampDarkCol.xxx,1);



                // ===== ② 阴影投影边缘色 =====
                float e1 = 1 - smoothstep(_Threshold, _Threshold + _ShadowEdgeWidth, ndl);
                float e2 = smoothstep(_Threshold - _ShadowEdgeWidth, _Threshold, ndl);
                float edgeMask = e1 * e2;
                float3 edgeColor = 
                    lerp(_ShadowEdgeColor.rgb, rampColor, _ShadowEdgeGradientIntensity) 
                    * _ShadowEdgeMulColor;

// bug: shadow edge is thinner???
//float softShadow = getSoftShadow(i._ShadowCoord,1);
//pow(shadow, 9);//smoothstep(0, 1, shadow);
//float shadowEdgeWidth = _ShadowEdgeWidth - 0.5;
//float sThres = 0.5;
//float es1 = 1 - smoothstep(sThres , sThres + shadowEdgeWidth, shadow);
//float es2 = smoothstep(sThres - shadowEdgeWidth, sThres , shadow);
//float shadowEdgeMask = es1 * es2;
//edgeMask += edge_shadow;

                toon.rgb = lerp(toon.rgb, edgeColor, edgeMask);

return float4(toon.xyz,1);


                // ===== ⑤ 笔触叠加（只在暗部）=====
                float brush = tex2D(_BrushTex, i.uv * 3).r;
                float shadowMask = 1 - lightMask;
                toon.rgb = lerp(toon.rgb, toon.rgb * brush, shadowMask * _BrushStrength);

                toon.a = (1.0 - _reflectionFactor);

                return toon;
                //return float4(shadow.xxx,1);
            }
            ENDCG
        }

        // ======================
        // ④ 描边 Pass（法线外扩）
        // ======================
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="Always" }

            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 normal = UnityObjectToWorldNormal(v.normal);
                float3 pos = mul(unity_ObjectToWorld, v.vertex).xyz;

                pos += normal * _OutlineWidth;

                o.pos = UnityWorldToClipPos(pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }

            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}