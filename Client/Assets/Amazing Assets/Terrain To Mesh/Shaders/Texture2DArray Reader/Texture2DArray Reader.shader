// Terrain To Mesh <http://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
Shader "Hidden/Amazing Assets/Terrain To Mesh/Texture2DArray Reader"
{
	Properties 
	{
		_Color("", Color) = (1, 1, 1, 1)
		_MainTex("", 2D) = "white" {}
	}
	 

	CGINCLUDE

	#include "UnityCG.cginc"

	float4 _Color;
	sampler2D _MainTex;

	UNITY_DECLARE_TEX2DARRAY(_TextureArray);
	int _TextureArrayIndex;
	int _IncludeAlpha;

	float4 fragTexture2DArrayToRGB(v2f_img i) : SV_Target 
	{
		float4 c = UNITY_SAMPLE_TEX2DARRAY(_TextureArray, float3(i.uv, _TextureArrayIndex));
		
		return float4(c.rgb, _IncludeAlpha > 0.5 ? c.a : 1);
	} 

	ENDCG 
	

	SubShader    
	{	
		Pass
	    {
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert_img
	    	#pragma fragment fragTexture2DArrayToRGB
			ENDCG

		}
	}	 
} 
