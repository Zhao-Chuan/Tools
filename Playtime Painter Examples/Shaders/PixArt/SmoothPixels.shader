﻿Shader "Playtime Painter/Pixel Art/SmoothPixels" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		struct Input {
			float2 uv_MainTex;
		};

		float _Glossiness;
		float _Metallic;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		//	UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		//UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			
			float2 perfTex = (floor(IN.uv_MainTex.xy*_MainTex_TexelSize.z) + 0.5) * _MainTex_TexelSize.x;
			float2 off = (IN.uv_MainTex.xy - perfTex);
			off = off *saturate((abs(off) * _MainTex_TexelSize.z)*40 - 19);
			perfTex  += off;

			float4 c = tex2D(_MainTex, perfTex);
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
