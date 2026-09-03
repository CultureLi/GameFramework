Shader "BundleRes/Foliage"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.45
        _WindStrength ("Wind Strength", Range(0,1)) = 0.15
        _WindSpeed ("Wind Speed", Range(0,10)) = 1.5
        _WindFreq ("Wind Frequency", Range(0,10)) = 1.2
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        LOD 200
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float  _Cutoff;
            float  _WindStrength;
            float  _WindSpeed;
            float  _WindFreq;
        CBUFFER_END

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

        float3 ApplyWind(float3 posWS, float baseY)
        {
            float t = _Time.y * _WindSpeed;
            float w = sin(t + posWS.x * _WindFreq * 0.7 + posWS.z * _WindFreq * 0.9);
            float mask = saturate((posWS.y - baseY) * 0.5);
            posWS.x += w * _WindStrength * mask;
            posWS.z += cos(t * 0.7 + posWS.x * 0.4) * _WindStrength * mask * 0.6;
            return posWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; float4 shadowCoord : TEXCOORD3; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                float baseY = vpi.positionWS.y - IN.positionOS.y;
                vpi.positionWS = ApplyWind(vpi.positionWS, baseY);
                OUT.positionWS = vpi.positionWS;
                OUT.positionHCS = TransformWorldToHClip(vpi.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = TransformWorldToShadowCoord(vpi.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN, float facing : VFACE) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                clip(texel.a - _Cutoff);
                half4 baseCol = texel * _BaseColor;

                float3 N = normalize(IN.normalWS) * (facing >= 0 ? 1 : -1);
                Light mainLight = GetMainLight(IN.shadowCoord);
                half3 L = mainLight.direction;
                half ndl = saturate(dot(N, L));
                half3 lit = baseCol.rgb * (mainLight.color * (ndl * mainLight.shadowAttenuation) + SampleSH(N) * 0.65);
                return half4(lit, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex vertS
            #pragma fragment fragS
            struct AS { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct VS { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            VS vertS (AS IN)
            {
                VS OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float baseY = posWS.y - IN.positionOS.y;
                posWS = ApplyWind(posWS, baseY);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }
            half4 fragS (VS IN) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a;
                clip(a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex vertD
            #pragma fragment fragD
            struct AD { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct VD { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            VD vertD (AD IN)
            {
                VD OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }
            half4 fragD (VD IN) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a;
                clip(a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
