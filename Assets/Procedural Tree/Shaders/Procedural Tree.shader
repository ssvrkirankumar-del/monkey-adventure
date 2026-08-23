Shader "Nature/Procedural Tree"
{
    Properties
    {
        _BarkMap("Bark Texture", 2D) = "white" {}
        _BarkColor("Bark Color", Color) = (1,1,1,1)
        _BarkSmoothness("Bark Smoothness", Range(0,1)) = 0.12

        _LeafMap("Leaf Texture", 2D) = "white" {}
        _LeafColor("Leaf Color", Color) = (1,1,1,1)
        _LeafBottomTint("Leaf Bottom Tint", Color) = (0.24,0.42,0.20,1)
        _LeafTopTint("Leaf Top Tint", Color) = (0.58,0.82,0.34,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.4

        _LeafTranslucency("Leaf Translucency", Range(0,2)) = 0.4
        _LeafSmoothness("Leaf Smoothness", Range(0,1)) = 0.05

        _WindStrength("Wind Strength", Range(0,0.5)) = 0.025
        _WindSpeed("Wind Speed", Range(0,10)) = 1.6
        _WindScale("Wind Scale", Range(0.1,10)) = 1.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

        LOD 300
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BarkColor;
            float4 _LeafColor;
            float4 _LeafBottomTint;
            float4 _LeafTopTint;
            float _BarkSmoothness;
            float _LeafSmoothness;
            float _LeafTranslucency;
            float _Cutoff;
            float _WindStrength;
            float _WindSpeed;
            float _WindScale;
        CBUFFER_END

        TEXTURE2D(_BarkMap);
        SAMPLER(sampler_BarkMap);
        TEXTURE2D(_LeafMap);
        SAMPLER(sampler_LeafMap);

        float3 ApplyTreeWind(float3 positionOS, float4 color, float2 uv)
        {
            float leafMask = saturate(color.a);
            float inheritedWind = saturate(color.b);
            float leafFlutter = saturate(uv.y) * leafMask;

            float timeVal = _Time.y * _WindSpeed;
            float waveA = sin((positionOS.x + positionOS.z) * _WindScale + timeVal);
            float waveB = cos(positionOS.z * (_WindScale * 1.37) + timeVal * 1.19);
            float waveC = sin(positionOS.y * (_WindScale * 0.73) + timeVal * 0.81);
            float sway = (waveA + waveB + waveC) * 0.33333334;

            float3 pos = positionOS;
            pos.x += sway * (_WindStrength * 0.55) * inheritedWind + sway * _WindStrength * leafFlutter;
            pos.z += waveA * (_WindStrength * 0.28) * inheritedWind + waveA * (_WindStrength * 0.45) * leafFlutter;
            pos.y += waveB * (_WindStrength * 0.08) * inheritedWind + waveB * (_WindStrength * 0.18) * leafFlutter;
            return pos;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 posOS = ApplyTreeWind(input.positionOS.xyz, input.color, input.uv);
                VertexPositionInputs vertexInputs = GetVertexPositionInputs(posOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInputs.positionCS;
                output.positionWS = vertexInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.color = input.color;
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input, half facing : VFACE) : SV_Target
            {
                float leafMask = saturate(input.color.a);

                half4 barkSample = SAMPLE_TEXTURE2D(_BarkMap, sampler_BarkMap, input.uv) * _BarkColor;
                half4 leafSample = SAMPLE_TEXTURE2D(_LeafMap, sampler_LeafMap, input.uv) * _LeafColor;

                // Alpha Cutout
                half alpha = lerp(1.0, leafSample.a, leafMask);
                clip(alpha - _Cutoff);

                half3 leafGradient = lerp(_LeafBottomTint.rgb, _LeafTopTint.rgb, saturate(input.uv.y));
                half leafVariation = lerp(0.9, 1.1, saturate(input.color.r));

                half3 barkAlbedo = barkSample.rgb;
                half3 leafAlbedo = leafSample.rgb * leafGradient * leafVariation;
                half3 albedo = lerp(barkAlbedo, leafAlbedo, leafMask);

                half smoothness = lerp(_BarkSmoothness, _LeafSmoothness, leafMask);

                // Lighting
                half3 normalWS = normalize(input.normalWS);
                if (facing < 0.0) normalWS = -normalWS;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 directLight = mainLight.color * (NdotL * mainLight.shadowAttenuation);

                // Subsurface / Translucency
                half backLight = saturate(dot(-normalWS, mainLight.direction));
                half3 translucency = leafAlbedo * backLight * _LeafTranslucency * leafMask * mainLight.color;

                half3 ambient = SampleSH(normalWS) * 0.8;
                half3 finalColor = albedo * (directLight + ambient) + translucency;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile_shadowcaster

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 posOS = ApplyTreeWind(input.positionOS.xyz, input.color, input.uv);
                float3 positionWS = TransformObjectToWorld(posOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                output.positionCS = positionCS;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float leafMask = saturate(input.color.a);
                if (leafMask > 0.5)
                {
                    half4 leafSample = SAMPLE_TEXTURE2D(_LeafMap, sampler_LeafMap, input.uv);
                    clip(leafSample.a - _Cutoff);
                }
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 posOS = ApplyTreeWind(input.positionOS.xyz, input.color, input.uv);
                output.positionCS = TransformObjectToHClip(posOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float leafMask = saturate(input.color.a);
                if (leafMask > 0.5)
                {
                    half4 leafSample = SAMPLE_TEXTURE2D(_LeafMap, sampler_LeafMap, input.uv);
                    clip(leafSample.a - _Cutoff);
                }
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}