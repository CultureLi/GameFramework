Shader "SLG/CityGrid"
{
    Properties
	{
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Alpha ("Alpha", Range(0, 1)) = .5
        _LineWidth ("Line Width", Range(0, 1)) = 0.1
        _CellCount ("Cell Count", Float) = 10.0
		_CornerRange ("Corner Range", Range(0, 1)) = 0
        _MooreFator("Moore Fator",float) = 0
	    [Toggle(USEVERTCOLOR)] _USEVERTCOLOR("Use Vert Color", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
    }
     
    SubShader
	{
        Tags { "Queue" = "Transparent" }
     
        Pass
		{
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest [_ZTest]
     
            CGPROGRAM
     
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local __ USEVERTCOLOR
     	    //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            uniform float4 _Color;
            uniform float _Alpha;
            uniform float _LineWidth;
            uniform float _CellCount;
            uniform float _CornerRange;
            uniform float _MooreFator;
            CBUFFER_END

            struct a2v
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
            };
 
            struct v2f
			{
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 posWS: TEXCOORD1;
                float4 color: COLOR;
            };
     
            v2f vert(a2v i)
			{
                v2f o;
                o.pos = UnityObjectToClipPos(i.vertex);
                o.posWS = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.uv = i.uv;
                #ifdef USEVERTCOLOR
			    o.color = i.color;
			    #endif

                return o;
            }

			float OutOfCorner(float2 uv)
			{
				return -max(max(_CornerRange - uv.x - uv.y, _CornerRange + uv.x - uv.y - 1.0),
					max(_CornerRange + uv.y - uv.x - 1.0, _CornerRange + uv.x + uv.y - 2.0));
			}

			float OnLine(float2 uv,float fator)
			{
				float x = uv.x * _CellCount;
				x = abs(x - round(x));
				float y = uv.y * _CellCount;
				y = abs(y - round(y));
				return max(_LineWidth*fator - x, _LineWidth*fator - y);
			}
 
            half4 frag(v2f i) : SV_Target
			{
                half fator = distance(_WorldSpaceCameraPos.xyz,i.posWS.xyz)*_MooreFator;
                // return half4(fator,fator,fator,1);

			    half4 col = _Color;
			    #ifdef USEVERTCOLOR
			    col *= i.color;
			    #endif

				if(min(OutOfCorner(i.uv), OnLine(i.uv,fator)) - 0.0001 < 0)
                    col.a *= _Alpha;
				return col;
            }
			ENDCG
		}
    }
}
