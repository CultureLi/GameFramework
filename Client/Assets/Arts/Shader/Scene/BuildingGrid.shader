Shader "Custom/BuildingGrid"
{
    Properties
    {
        _LineWidth ("Line Width (UV)", Range(0.005, 0.3)) = 0.055
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.16
        _LineAlpha ("Line Alpha", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Grid"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _LineWidth;
                float _FillAlpha;
                float _LineAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 每个格子单独一张 quad，uv 在格子内是 0..1，靠近边缘即画线。
                float2 edge = min(IN.uv, 1.0 - IN.uv);
                float distanceToEdge = min(edge.x, edge.y);
                float edgeMask = 1.0 - smoothstep(_LineWidth, _LineWidth * 1.6, distanceToEdge);

                half4 color = IN.color;
                color.rgb = lerp(color.rgb, saturate(color.rgb * 1.4 + 0.15), edgeMask);
                color.a = lerp(_FillAlpha, _LineAlpha, edgeMask);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
