Shader "SLG/MarchLine"
{
        Properties
        {
            _MainTex("Sprite Texture", 2D) = "white" {}
            _Color("Tint", Color) = (1,1,1,1)
            _SpeedU("Speed U", Range(0, 1000)) = 0
            _SpeedV("Speed V", Range(0, 1000)) = 0
            _SpaceV("Space V", Float) = 0
            _Split("Split Point", Float) = 0.5
            _AddWidth("Add Width", Float) = 0
            _Width("Width", Float) = 0.08
            _Percent("Percent", Range(0, 1)) = 1

            [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
            [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0
        	_Stencil("Stencil ID", Float) = 10

        }

		SubShader
		{
		    LOD 100
			Tags
			{
				"Queue" = "Geometry"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"CanUseSpriteAtlas" = "True"
			}

			Cull Off
			Lighting Off
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			
			Blend SrcAlpha OneMinusSrcAlpha

			Pass
			{
                Stencil {
                    Ref [_Stencil]
                    Comp Greater
                    Pass keep
                    Fail keep
                    ZFail keep
                }
				CGPROGRAM
				#pragma vertex SpriteVertEx
				#pragma fragment SpriteFragEx
				#pragma target 2.0
				
				//#include "UnityCG.cginc"
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"


			    #pragma multi_compile MULTIPLY OVERLAY

                CBUFFER_START(UnityPerMaterial)
                sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _Color;
				float _SpeedU;
				float _SpeedV;
				float _Split;
                float _AddWidth;
                float _Width;
                float _SpaceV;
                float _Percent;
                CBUFFER_END

                struct appdata
                {
                    float4 vertex   : POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                    float4 normal  : NORMAL;
                };

                struct v2f
                {
                    float4 vertex   : SV_POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                    float2 originTexCoord : TEXCOORD1;
                };
                
                fixed3 Overlay(fixed3 baseColor, fixed3 topColor)
                {
                	if (Luminance(baseColor) < 0.5)
                	{
                		return 2.0 * baseColor * topColor;
                	}
                    else
                	{
                		return 1.0 - 2.0 * (1.0 - baseColor) * (1.0 - topColor);
                	}
                }

				v2f SpriteVertEx(appdata IN)
				{
					v2f OUT;
                    OUT.originTexCoord = IN.texcoord;
                    IN.vertex += IN.normal * _AddWidth;
					OUT.vertex = UnityObjectToClipPos(IN.vertex);
                    IN.texcoord.y /= (1 + _AddWidth * 2 / _Width);
					OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
					OUT.color = SRGBToLinear(IN.color) * _Color;

                    float time = frac(_Time.x) * 20; // _Time.y 游戏开时间久了会有精度问题，用 _Time.x 试试，如果 _Time.x 也是用 _Time.y 算出来的话就不会改善
                    OUT.texcoord = OUT.texcoord - float2(time * _SpeedU, time * _SpeedV);
					return OUT;
				}

				fixed4 SpriteFragEx(v2f IN) : SV_Target
				{
					IN.texcoord.y = clamp(fmod(IN.texcoord.y, 1 + _SpaceV), -1, 1);
					fixed4 color = tex2D(_MainTex, IN.texcoord);
					
#ifdef MULTIPLY
                    color = color * IN.color;
#elif OVERLAY
                    color.rgb = Overlay(color.rgb, IN.color.rgb);
#endif
                    color.a = step(_Percent, IN.originTexCoord.y) * color.a;
					return color * IN.color;
				}
				ENDCG
			}
		}
		
	Fallback "Sprites/Default"
}