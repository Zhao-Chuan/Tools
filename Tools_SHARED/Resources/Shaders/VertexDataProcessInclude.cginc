/*
struct v2fvd {
				float4 pos : POSITION;
				float4 texcoord : TEXCOORD0;  // xy - Main texture UV ; zw - UV from world Pos   
				float4 vcol : COLOR1;  // rgb for border, a - for shadow
				float4 viewDir: TEXCOORD1; // w - distance
				float3 normal: TEXCOORD2;
				float3 snormal: TEXCOORD3;
				float3 scenepos: TEXCOORD4;
				float4 bshad : TEXCOORD5;  //Baked Shadow	
				float4 rendMapUV : TEXCOORD6; // xy - small, zw - big
				float4 bC : TEXCOORD7; // xy - small, zw - big
				};*/


				/*

				o.normal.xyz = UnityObjectToWorldNormal(v.tangent.xyz);//v.tangent.xyz;//v.normal;
				o.snormal.xyz = UnityObjectToWorldNormal(v.normal.xyz);
				o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

*/

 #include "UnityCG.cginc"
 

static const float MERGE_POWER = 512;
static const float TERABOUNCE = 0.5;

float4 _lightControl;
sampler2D _mergeTerrainHeight;
float4 _mergeTerrainHeight_TexelSize;
float4 _wrldOffset;

sampler2D _TerrainColors;
sampler2D _mergeControl;

sampler2D _mergeSplat_0;
sampler2D _mergeSplat_1;
sampler2D _mergeSplat_2;
sampler2D _mergeSplat_3;
sampler2D _mergeSplat_4;

sampler2D _mergeSplatN_0;
sampler2D _mergeSplatN_1;
sampler2D _mergeSplatN_2;
sampler2D _mergeSplatN_3;
sampler2D _mergeSplatN_4;

float4 _Control_ST;
float4 _mergeTerrainTiling;
float4 _foamParams;
float4 _foamDynamics;
float4 _mergeTeraPosition;
float4 _mergeTerrainScale;
float _Merge;



inline void atlasedTexture(float _AtlasTextures, float atlasNumber, float _TexelSizeX, out float4 atlasedUV) {
	

	float atY = floor(atlasNumber / _AtlasTextures);
	float atX = atlasNumber - atY*_AtlasTextures;
	float edge = _TexelSizeX;

	atlasedUV.xy = float2(atX, atY) / _AtlasTextures;				//+edge;
	atlasedUV.z = edge;										//(1) / _AtlasTextures - edge * 2;
	atlasedUV.w = 1 / _AtlasTextures;
}


inline void applyTangent (inout float3 normal, float3 tnormal, float4 wTangent){
	float3 wBitangent = cross(normal, wTangent.xyz) * wTangent.w;

	float3 tspace0 = float3(wTangent.x, wBitangent.x, normal.x);
	float3 tspace1 = float3(wTangent.y, wBitangent.y, normal.y);
	float3 tspace2 = float3(wTangent.z, wBitangent.z, normal.z);																												  //normal = i.normal.xyz;

	normal.x = dot(tspace0, tnormal);
	normal.y = dot(tspace1, tnormal);
	normal.z = dot(tspace2, tnormal);
}

inline void normalAndPositionToUV (float3 worldNormal, float3 scenepos, out float4 tang, out float2 uv){

	scenepos += _wrldOffset.xyz;

	worldNormal = abs(worldNormal);
	float znorm = saturate((worldNormal.x-worldNormal.z)*55555);
	float xnorm = saturate(((worldNormal.z+worldNormal.y) - worldNormal.x)*55555);
	float ynorm = saturate((worldNormal.y-0.8)*55555);
					
	float x = (scenepos.x)*(xnorm)+(scenepos.z)*(1-xnorm);
	float y = (scenepos.y)*(1-ynorm)+(scenepos.z)*(ynorm);

	uv.x = x;
	uv.y = y;

	float dey = 1-ynorm;

	tang.w = xnorm*ynorm;
	tang.x = tang.w + (1-znorm)*dey;
	tang.y = dey; 
	tang.z = znorm*dey; 

}

inline void normalAndPositionToUV(float3 worldNormal, float3 scenepos, out float2 uv) {

	

	scenepos += _wrldOffset.xyz;

	worldNormal = abs(worldNormal);
	float znorm = saturate((worldNormal.x - worldNormal.z) * 55555);
	float xnorm = saturate(((worldNormal.z + worldNormal.y) - worldNormal.x) * 55555);
	float ynorm = saturate((worldNormal.y - 0.8) * 55555);

	float x = (scenepos.x)*(xnorm)+(scenepos.z)*(1 - xnorm);
	float y = (scenepos.y)*(1 - ynorm) + (scenepos.z)*(ynorm);

	uv.x = x;
	uv.y = y;

}


inline void applyTangentNonNormalized (float4 tang, inout float3 normal, float2 bump){

	normal.xyz+=float3(bump.x*tang.x, bump.y*tang.y, bump.x*tang.z + bump.y*tang.w); 


}

inline void rotate ( inout float2 uv, float angle){

		float sinX = sin ( angle );
		float cosX = cos ( angle );
		float sinY = sin ( angle );
		float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);

		uv =  mul ( uv, rotationMatrix );

}

void smoothedPixelsSampling (inout float2 texcoord, float4 texelsSize, float dist){

float2 perfTex = (floor(texcoord.xy*texelsSize.z) + 0.5) * texelsSize.x;
		float2 off = (texcoord.xy - perfTex);

		float n = max(4,30 - dist); 

		float2 offset = saturate((abs(off) * texelsSize.z)*(n*2+2) - n);

		off = off * offset;

		texcoord.xy = perfTex  + off;

}

inline float2 DetectEdge(float4 vcol){

				vcol = max(0, vcol - 0.965);
				vcol.a = min(1, vcol.a * 28);

				float allof = vcol.r+vcol.g+vcol.b;
				float border = min(1,(allof)*(28)+ vcol.a); 

				return float2(border, vcol.a);

				// use vcol.a to apply color

}

inline float3 DetectSmoothEdge(float4 edge, float3 junkNorm, float3 sharpNorm, float3 edge0, float3 edge1, float3 edge2, out float weight) {

	edge = max(0, edge - 0.965) * 28;

	//edge = max(0, edge - 0.93) * 14;
	float border = max(max(edge.r, edge.g), edge.b);

	float3 edgeN = edge0*edge.r + edge1*edge.g + edge2*edge.b;

	float junk = min(1, (edge.g*edge.b + edge.r*edge.b + edge.r*edge.g)*2)* border;

	weight = (edge.w)*border;

	return normalize((sharpNorm*(1 - border) + border*edgeN)*(1 - junk) + junk*(junkNorm));

}

inline float3 DetectSmoothEdge(float thickness ,float4 edge, float3 junkNorm, float3 sharpNorm, float3 edge0, float3 edge1, float3 edge2, out float weight) {

	thickness = thickness*thickness*0.5;
	//0.00125

	edge = saturate((edge - 1 + thickness)/thickness);

	//edge = max(0, edge - 0.93) * 14;
	float border = max(max(edge.r, edge.g), edge.b);

	float3 edgeN = edge0*edge.r + edge1*edge.g + edge2*edge.b;

	float junk = min(1, (edge.g*edge.b + edge.r*edge.b + edge.r*edge.g) * 2)* border;

	weight = (edge.w)*border;

	return normalize((sharpNorm*(1 - border) + border*edgeN)*(1 - junk) + junk*(junkNorm));

}



inline float3 reflectedVector (float3 normal, float3 viewDir){

	float dotprod = dot(normal.xyz, viewDir.xyz);
	return  normalize(viewDir.xyz - 2*(dotprod)*normal.xyz); 

}


inline void leakColors (inout float4 col){

	float3 flow = (col.gbr + col.brg);
	flow *= flow;
	col.rgb += flow*0.02;

}

inline float2 foamAlphaWhite(float3 fwpos) {
	//fwpos += _wrldOffset.xyz;

	float l = cos(_foamParams.x + fwpos.x) - fwpos.y;
	float dl = max(0, 0.2 - abs(l));

	float l1 = sin(_foamParams.y + fwpos.z) - fwpos.y;
	float dl1 = max(0, (0.3 - abs(l1))*max(0, 1 - l));

	float foamAlpha = (dl + dl1);
	foamAlpha = max(0, l) + max(0, l1) - max(0, foamAlpha)*(_foamDynamics.w);

	float foamWhite;
	foamWhite = saturate(max(l, l1) * 8);



	return float2(foamAlpha, foamWhite);
}

inline float3 foamStuff(float3 wpos) {
	float3 fwpos;
	fwpos = wpos;
	fwpos.xz += _wrldOffset.xz;
	fwpos.y -= _foamParams.z;
	fwpos.y *= _foamDynamics.x;
	fwpos.y += 128 / _foamDynamics.x;
	fwpos.xz *= _foamDynamics.y;
	fwpos /= _foamDynamics.z;
	fwpos.y += 1;

	return fwpos;
}