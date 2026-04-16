Shader "Custom/DepthGradientPostEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RampTex ("Ramp Texture", 2D) = "white" {}      // 渐变纹理
        _GradientStart ("Gradient Start", Float) = 5.0
        _GradientEnd ("Gradient End", Float) = 10.0
        _GradientPower ("Gradient Power", Range(0.1, 5)) = 1.0
        _Intensity ("Intensity", Range(0, 1)) = 1.0
        
        // 径向渐变相关参数
        _RadialAngle ("Radial Angle", Range(0, 360)) = 0      // 渐变方向角度
        _RadialCenterX ("Radial Center X", Range(0, 1)) = 0.5 // 径向中心 X (0-1)
        _RadialCenterY ("Radial Center Y", Range(0, 1)) = 0.5 // 径向中心 Y (0-1)
        _RadialType ("Radial Type", Range(0, 1)) = 0          // 0=线性渐变, 1=径向渐变
    }
    
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #pragma require depth
            #pragma multi_compile _ UNITY_CAMERA_DEPTH_TEXTURE

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler2D _RampTex;
            
            float _GradientStart;
            float _GradientEnd;
            float _GradientPower;
            float _Intensity;
            
            // 径向渐变参数
            float _RadialAngle;
            float _RadialCenterX;
            float _RadialCenterY;
            float _RadialType;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            // 根据角度旋转UV坐标
            float2 RotateUV(float2 uv, float angleDegrees, float2 center)
            {
                // 将角度转换为弧度
                float rad = radians(angleDegrees);
                float s = sin(rad);
                float c = cos(rad);
                
                // 将UV坐标偏移到原点
                float2 uvCentered = uv - center;
                
                // 旋转
                float2 uvRotated;
                uvRotated.x = uvCentered.x * c - uvCentered.y * s;
                uvRotated.y = uvCentered.x * s + uvCentered.y * c;
                
                // 偏移回原位置
                return uvRotated + center;
            }
            
            // 计算渐变强度值 (0-1)
            float CalculateDepthStrength(float depth, float2 uv)
            {
                float strength = 0;
                
                // 获取深度方向的渐变值
                float depthStrength = 0;
                if (depth > _GradientStart)
                {
                    if (depth >= _GradientEnd)
                    {
                        depthStrength = 1.0;
                    }
                    else
                    {
                        float range = _GradientEnd - _GradientStart;
                        float normalizedDepth = (depth - _GradientStart) / range;
                        depthStrength = pow(normalizedDepth, _GradientPower);
                    }
                }

                strength = depthStrength;
                

                
                return saturate(strength);
            }

            float CalculateGradientStrength(float depthStrength, float2 uv){
                
                float strength = 0;

                // 根据 _RadialType 选择渐变模式
                if (_RadialType > 0.5) // 径向渐变
                {
                    // 计算径向距离 (中心点到当前UV的距离)
                    float2 radialCenter = float2(_RadialCenterX, _RadialCenterY);
                    float radialDist = distance(uv, radialCenter);
                    
                    // 径向强度: 中心为0，边缘为1
                    // 最大距离约为0.707 (从中心到角落)
                    float radialStrength = saturate(radialDist / 0.707);
                    
                    // 深度强度可以控制径向渐变的"进度"
                    // 当深度强度为0时，只显示中心; 为1时，显示完整径向渐变
                    strength = radialStrength;
                }
                else // 线性渐变 (带角度)
                {
                    // 旋转UV坐标
                    float2 rotatedUV = RotateUV(uv, _RadialAngle, float2(0.5, 0.5));
                    
                    // 线性渐变强度: 从左到右 (0-1)
                    // 也可以根据需要改为从上到下，这里使用旋转后的X轴
                    float linearStrength = saturate((rotatedUV.x - 0.5) / 0.5 + 0.5);
                    
                    // 可选: 使用反向渐变
                    // float linearStrength = 1 - saturate((rotatedUV.x - 0.5) / 0.5 + 0.5);
                    
                    strength = linearStrength;
                }

                return saturate(strength);
            }


            fixed4 frag (v2f i) : SV_Target
            {
                // 采样原颜色
                fixed4 originalColor = tex2D(_MainTex, i.uv);
                
                // 采样深度图并转换为线性深度
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float sceneDepth = LinearEyeDepth(rawDepth);
                
                // 计算深度渐变强度
                float gradientStrength = CalculateDepthStrength(sceneDepth, i.uv);            
                gradientStrength *= _Intensity;


                // 渐变色
                float gradientMask = CalculateGradientStrength(gradientStrength, i .uv);
                
                // 使用渐变纹理采样颜色 (基于强度值)
                // 可选: 使用深度值来采样渐变纹理，产生更丰富的效果
                // float rampColor = saturate((sceneDepth - _GradientStart) / (_GradientEnd - _GradientStart));
                
                fixed4 gradientColor = tex2D(_RampTex, float2(gradientMask, 0));
                
                // 混合最终颜色
                fixed4 finalColor = lerp(originalColor, gradientColor, gradientStrength);
                finalColor.a = 1;

                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Hidden/InternalErrorShader"
}