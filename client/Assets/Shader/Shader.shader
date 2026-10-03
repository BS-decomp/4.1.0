// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/DirtyLens_01_GeoMobile.shaderlab (compiled ShaderLab taken from
// sharedassets3.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "DirtyLens_01_GeoMobile" {
Properties {
	_FlareMultiplier ("_FlareMultiplier", Float) = 10
	_Flare ("_Flare", 2D) = "black" {}
	_DirtyLensTexture ("_DirtyLensTexture", 2D) = "black" {}
}
SubShader {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Pass {
		Name "FORWARD"
		Tags { "LightMode"="ForwardBase" "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Fog { Color (0,0,0,0) }
		Blend One One
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_fwdbase
		#include "UnityCG.cginc"
		#include "Lighting.cginc"

		sampler2D _Flare;
		float4 _Flare_ST;
		sampler2D _DirtyLensTexture;
		float _FlareMultiplier;

		struct appdata_t { float4 vertex : POSITION; float3 normal : NORMAL; float2 texcoord : TEXCOORD0; };
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 screen : TEXCOORD1;
			float3 lightDirWS : TEXCOORD2;
			float3 sh : TEXCOORD3;
			float3 normalWS : TEXCOORD4;
		};

		// The original is a surface shader compiled for ForwardBase. The GLES
		// program computes, in tangent space:
		//   albedo = tex2D(_Flare, uv) * _FlareMultiplier * tex2D(_DirtyLensTexture, screenUV)
		//   c.rgb  = albedo * (_LightColor0.rgb * max(0, N.L)) * 2 + albedo * SH
		// which is what this pass reproduces in world space (identical maths,
		// the shader has no normal map so tangent space only rotated N and L).
		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _Flare);
			o.screen = ComputeScreenPos(o.pos);
			float3 n = UnityObjectToWorldNormal(v.normal);
			o.normalWS = n;
			o.lightDirWS = normalize(_WorldSpaceLightPos0.xyz);
			o.sh = ShadeSH9(float4(n, 1.0));
			return o;
		}

		fixed4 frag (v2f i) : SV_Target {
			float2 screenUV = i.screen.xy / i.screen.w;
			fixed3 albedo = (tex2D(_Flare, i.uv) * _FlareMultiplier * tex2D(_DirtyLensTexture, screenUV)).rgb;
			fixed ndotl = max(0.0, dot(normalize(i.normalWS), i.lightDirWS));
			fixed3 col = albedo * (_LightColor0.rgb * ndotl) * 2.0;
			col += albedo * i.sh;
			return fixed4(col, 1.0);
		}
		ENDCG
	}
}
}
