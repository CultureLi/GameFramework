Shader "BundleRes/SimpleBuilding"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float  _Smoothness;
        CBUFFER_END

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; float4 shadowCoord : TEXCOORD3; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionWS = vpi.positionWS;
                OUT.positionHCS = vpi.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = TransformWorldToShadowCoord(vpi.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float3 N = normalize(IN.normalWS);
                Light mainLight = GetMainLight(IN.shadowCoord);
                half3 L = mainLight.direction;
                half3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                half ndl = saturate(dot(N, L));
                half3 diffuse = texel.rgb * (mainLight.color * (ndl * mainLight.shadowAttenuation) + SampleSH(N) * 0.75);
                half3 H = normalize(L + V);
                half ndh = saturate(dot(N, H));
                half spec = pow(ndh, lerp(8, 128, _Smoothness));
                diffuse += mainLight.color * spec * mainLight.shadowAttenuation * _Smoothness;

            #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < pixelLightCount; ++i)
                {
                    Light al = GetAdditionalLight(i, IN.positionWS);
                    half aNdl = saturate(dot(N, al.direction));
                    diffuse += al.color * texel.rgb * (aNdl * al.distanceAttenuation * al.shadowAttenuation);
                }
            #endif
                return half4(diffuse, 1);
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
            struct AS { float4 positionOS : POSITION; };
            struct VS { float4 positionHCS : SV_POSITION; };
            VS vertS (AS IN) { VS OUT; OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz); return OUT; }
            half4 fragS (VS IN) : SV_Target { return 0; }
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
            struct AD { float4 positionOS : POSITION; };
            struct VD { float4 positionHCS : SV_POSITION; };
            VD vertD (AD IN) { VD OUT; OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz); return OUT; }
            half4 fragD (VD IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
