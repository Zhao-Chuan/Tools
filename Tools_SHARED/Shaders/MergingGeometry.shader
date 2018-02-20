﻿
Shader "Terrain/MergingGeometry" {
	Properties{
		[NoScaleOffset]_MainTex("Geometry Texture (RGB)", 2D) = "white" {}
		[NoScaleOffset]_BumpMapC("Geometry Combined Maps (RGB)", 2D) = "white" {}
		_Merge("_Merge", Range(0.01,2)) = 1
	}


		Category{
		Tags{ "RenderType" = "Opaque"
		"LightMode" = "ForwardBase"
		"Queue" = "Geometry"
	}
		LOD 200
		ColorMask RGBA


		SubShader{
		Pass{



		CGPROGRAM

#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
//#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "VertexDataProcessInclude.cginc"

#pragma multi_compile_fwdbase //nolightmap nodirlightmap nodynlightmap novertexlight
#pragma multi_compile  ___ MODIFY_BRIGHTNESS 
#pragma multi_compile  ___ COLOR_BLEED


	sampler2D _MainTex;
	sampler2D _BumpMapC;


	struct v2f {
		float4 pos : POSITION;

		UNITY_FOG_COORDS(1)
		float3 viewDir : TEXCOORD2;
		float3 wpos : TEXCOORD3;
		float3 tc_Control : TEXCOORD4;
		float3 fwpos : TEXCOORD5;
		SHADOW_COORDS(6)
		//float3 normal : TEXCOORD11;
		float2 texcoord : TEXCOORD7;
		half3 tspace0 : TEXCOORD8; // tangent.x, bitangent.x, normal.x
		half3 tspace1 : TEXCOORD9; // tangent.y, bitangent.y, normal.y
		half3 tspace2 : TEXCOORD10; // tangent.z, bitangent.z, normal.z
	};

	v2f vert(appdata_full v) {
		v2f o;

		float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
		o.tc_Control.xyz = (worldPos.xyz - _mergeTeraPosition.xyz) / _mergeTerrainScale.xyz;

		// The portion below is to preview editing.
		/*	float4 height = tex2Dlod(_mergeTerrainHeight, float4(o.tc_Control.xy, 0, 0));
		worldPos.y = _mergeTeraPosition.y + height.a*_mergeTerrainScale.y;
		v.vertex = mul(unity_WorldToObject, float4(worldPos.xyz, v.vertex.w));*/
		// end of preview, can be commented out for build

		o.pos = UnityObjectToClipPos(v.vertex);
		o.wpos = worldPos;//mul (unity_ObjectToWorld, v.vertex).xyz;
		o.viewDir.xyz = (WorldSpaceViewDir(v.vertex));

		//o.tc_Control.zw = 0;
		o.texcoord = v.texcoord;
		UNITY_TRANSFER_FOG(o, o.pos);
		TRANSFER_SHADOW(o);

		float3 worldNormal = UnityObjectToWorldNormal(v.normal);

		o.fwpos = foamStuff(o.wpos);

		half3 wNormal = worldNormal;//UnityObjectToWorldNormal(v.normal);
		half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
		// compute bitangent from cross product of normal and tangent
		half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
		half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
		// output the tangent space matrix
		o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
		o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
		o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);


		return o;
	}


	float4 frag(v2f i) : COLOR{
		i.viewDir.xyz = normalize(i.viewDir.xyz);
		float dist = length(i.wpos.xyz - _WorldSpaceCameraPos.xyz);

	float far = min(1, dist*0.01);
	float deFar = 1 - far;


	float4 cont = tex2D(_mergeControl, i.tc_Control.xz);
	float4 height = tex2D(_mergeTerrainHeight, i.tc_Control.xz + _mergeTerrainScale.w);
	float3 bump = (height.rgb - 0.5) * 2;



	float4 geocol = tex2D(_MainTex, i.texcoord.xy);
	float4 bumpMap = tex2D(_BumpMapC, i.texcoord.xy);
	bumpMap.rg = bumpMap.rg - 0.5;// *2 - 1;
	float3 tnormal = float3(bumpMap.r, bumpMap.g, 1);
	float3 worldNormal;
	worldNormal.x = dot(i.tspace0, tnormal);
	worldNormal.y = dot(i.tspace1, tnormal);
	worldNormal.z = dot(i.tspace2, tnormal);


	float aboveTerrainBump = ((((i.wpos.y - _mergeTeraPosition.y) - height.a*_mergeTerrainScale.y ))); 
	float aboveTerrainBump01 = saturate(aboveTerrainBump);
	float deAboveBump = 1 - aboveTerrainBump01;
	bump = (bump * deAboveBump + worldNormal * aboveTerrainBump01);


	float2 tiled = i.tc_Control.xz*_mergeTerrainTiling.xy + _mergeTerrainTiling.zw;
	float tiledY = i.tc_Control.y * _mergeTeraPosition.w*2;//*_mergeTerrainTiling.x;


	float2 lowtiled = i.tc_Control.xz*_mergeTerrainTiling.xy*0.1;

	float4 splat0 = tex2D(_mergeSplat_0, lowtiled)*far + tex2D(_mergeSplat_0, tiled)*deFar;
	float4 splat1 = tex2D(_mergeSplat_1, lowtiled)*far + tex2D(_mergeSplat_1, tiled)*deFar;
	float4 splat2 = tex2D(_mergeSplat_2, lowtiled)*far + tex2D(_mergeSplat_2, tiled)*deFar;
	float4 splat3 = tex2D(_mergeSplat_3, lowtiled)*far + tex2D(_mergeSplat_3, tiled)*deFar;

	float4 splat0N = tex2D(_mergeSplatN_0, lowtiled)*far + tex2D(_mergeSplatN_0, tiled)*deFar;
	float4 splat1N = tex2D(_mergeSplatN_1, lowtiled)*far + tex2D(_mergeSplatN_1, tiled)*deFar;
	float4 splat2N = tex2D(_mergeSplatN_2, lowtiled)*far + tex2D(_mergeSplatN_2, tiled)*deFar;
	float4 splat3N = tex2D(_mergeSplatN_3, lowtiled)*far + tex2D(_mergeSplatN_3, tiled)*deFar;

	const float edge = MERGE_POWER;

	float4 terrain = geocol;
	float4 terrainN = float4(0.5,0.5, bumpMap.b, bumpMap.a);


	float maxheight = ( geocol.a);//*abs(bump.y);

	float tripMaxH = maxheight;
	float3 tmpbump = bump;
	float triplanarY = max(0, tmpbump.y) * 2; // Recalculate it based on previously sampled bump

	float newHeight = cont.r * triplanarY + splat0.a;
	float adiff = max(0, (newHeight - maxheight));
	float alpha = min(1, adiff*(1 + edge*terrainN.b*splat0N.b));
	float dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat0*alpha;
	terrainN = terrainN*(dAlpha)+splat0N*alpha;
	maxheight += adiff;


	newHeight = cont.g*triplanarY + splat1.a;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1,adiff*(1 + edge*terrainN.b*splat1N.b));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat1*alpha;
	terrainN = terrainN*(dAlpha)+splat1N*alpha;
	maxheight += adiff;


	newHeight = cont.b*triplanarY + splat2.a;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1,adiff*(1 + edge*terrainN.b*splat2N.b));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat2*alpha;
	terrainN = terrainN*(dAlpha)+splat2N*alpha;
	maxheight += adiff;

	newHeight = cont.a*triplanarY + splat3.a;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1,adiff*(1 + edge*terrainN.b*splat3N.b));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat3*alpha;
	terrainN = terrainN*(dAlpha)+splat3N*alpha;
	maxheight += adiff;

	terrainN.rg = terrainN.rg * 2 - 1;

	adiff = max(0, (tripMaxH + 0.5 - maxheight));
	alpha = min(1, adiff * 2);

	float aboveTerrain = saturate((aboveTerrainBump / _Merge + geocol.a -maxheight - 1) * 4); // MODIFIED
	float deAboveTerrain = 1 - aboveTerrain;

	alpha*=deAboveTerrain;
	bump = tmpbump*alpha + (1 - alpha)*bump;


	cont = geocol* aboveTerrain +terrain*deAboveTerrain;

	float wetSection = saturate(_foamParams.w - i.fwpos.y - (cont.a)*_foamParams.w)*(1 - terrainN.b);
	i.fwpos.y += cont.a;

	worldNormal = normalize(bump 
	+float3(terrainN.r, 0, terrainN.g)*deAboveTerrain
	);

	terrainN.ba = terrainN.ba * deAboveTerrain +
		aboveTerrain*bumpMap.ba; // temporary default value

	float dotprod = max(0,dot(worldNormal,  i.viewDir.xyz));
	float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*worldNormal);

	float2 foamA_W = foamAlphaWhite(i.fwpos);
	float water = max(0.5, min(i.fwpos.y + 2 - (foamA_W.x) * 2, 1)); // MODIFIED
	float under = (water - 0.5) * 2;

	terrainN.b = max(terrainN.b, wetSection*under); // MODIFIED

	float fernel = 1.5 - dotprod;

	float smoothness = (pow(terrainN.b, (3 - fernel) * 2));  //terrainN.b*terrainN.b;//+((1 - dotprod)*(1 - terrainN.b)));
	float deSmoothness = (1 - smoothness);

	float ambientBlock = (1 - terrainN.a)*dotprod; // MODIFIED

	float shadow = saturate((SHADOW_ATTENUATION(i) * 2 - ambientBlock));

	float3 teraBounce = _LightColor0.rgb*TERABOUNCE;
	float4 terrainAmbient = tex2D(_TerrainColors, i.tc_Control.xz);
	terrainAmbient.rgb *= teraBounce;
	terrainAmbient.a *= terrainN.a;

	float4 terrainLight = tex2D(_TerrainColors, i.tc_Control.xz - reflected.xz*terrainN.b*terrainAmbient.a*0.1);
	terrainLight.rgb *= teraBounce;


	float diff = saturate((dot(worldNormal, _WorldSpaceLightPos0.xyz)));
	diff = saturate(diff - ambientBlock * 4 * (1 - diff));
	float direct = diff*shadow;

	float3 ambientRefl = ShadeSH9(float4(reflected, 1))*terrainAmbient.a;

	float4 col;
	col.a = water; // NEW
	col.rgb = (cont.rgb* (_LightColor0*direct + (terrainAmbient.rgb
		)*fernel)*deSmoothness*terrainAmbient.a + foamA_W.y*(0.5 + shadow)*(under));

	float power = smoothness * 1024;

	float3 reflResult = (
		((pow(max(0.01, dot(_WorldSpaceLightPos0, -reflected)), power)* direct	*(_LightColor0)*power)) +

		terrainLight.rgb +
		ambientRefl.rgb

		)* terrainN.b * fernel;

	col.rgb += reflResult*under;

	col.rgb *= 1 - saturate((_foamParams.z - i.wpos.y)*0.1);  // NEW

	float4 fogged = col;
	UNITY_APPLY_FOG(i.fogCoord, fogged);
	float fogging = (32 - max(0,i.wpos.y - _foamParams.z)) / 32;

	fogging = min(1,pow(max(0,fogging),2));
	col.rgb = fogged.rgb * fogging + col.rgb *(1 - fogging);


#if	MODIFY_BRIGHTNESS
	col.rgb *= _lightControl.a;
#endif

#if COLOR_BLEED
	float3 mix = col.gbr + col.brg;
	col.rgb += mix*mix*_lightControl.r;
#endif

	//col.rgb = bump.rgb;
	//col.rgb = worldNormal;
	//col.rg = abs(reflected.xz);
	//col.b = 0;
	//terrainLight.rgb *= cont.rgb;
	return
		//ambientPower;
		//micro;
		//power;//
		//terrainLight;//*(1 - smoothness);
		//smoothness;
		//cont;
		//power;
		//splat0N;
		//terrainAmbient;
		//fernel;
		//diff;
		//aboveTerrainBump;
		//aboveTerrain;
		col;
	//dotprod;
	//terrainAmbient;
	}


		ENDCG
	}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	}
}