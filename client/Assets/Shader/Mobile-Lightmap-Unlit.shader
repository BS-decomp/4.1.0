// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/Mobile-Unlit (Supports Lightmap).shaderlab (compiled ShaderLab taken from
// sharedassets2.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "Mobile/Unlit (Supports Lightmap)" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	LOD 100
	Tags { "RenderType"="Opaque" }

	// The APK ships three fixed-function passes: "Vertex" (no lightmap),
	// "VertexLM" (unity_Lightmap * texture, doubled) and "VertexLMRGBM"
	// (the same with an RGBM-encoded lightmap, quadrupled).
	// `unity_LightmapMatrix` and the fixed-function combiners are gone in modern
	// Unity, so the three are folded into one pass with Unity's own LIGHTMAP_ON
	// keyword and unity_LightmapST, decoded with DecodeLightmap() — that covers
	// both the dLDR and the RGBM encodings on every platform/renderer.
	Pass {
		Tags { "LightMode"="Vertex" "RenderType"="Opaque" }
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile _ LIGHTMAP_ON
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;

		struct appdata_t {
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			#ifdef LIGHTMAP_ON
			float2 lmuv : TEXCOORD1;
			#endif
		};

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			#ifdef LIGHTMAP_ON
			o.lmuv = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
			#endif
			return o;
		}

		fixed4 frag (v2f i) : SV_Target {
			fixed4 col = tex2D(_MainTex, i.uv);
			#ifdef LIGHTMAP_ON
			fixed3 lm = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmuv));
			col.rgb *= lm;
			#endif
			return col;
		}
		ENDCG
	}
}
}
