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

	// APK passes: LIGHTMODE=Vertex (plain texture), VertexLM (lightmap * texture,
	// "double" = dLDR x2) and VertexLMRGBM (RGBM, "quad"). Unity 5 removed
	// unity_LightmapMatrix and the fixed-function combiners, and a CG pass tagged
	// "Vertex" never receives the LIGHTMAP_ON keyword, so the trio is folded into
	// one ForwardBase pass with the LIGHTMAP_ON variant; x2 reproduces "double".
	Pass {
		Tags { "LightMode"="ForwardBase" "RenderType"="Opaque" }
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile _ LIGHTMAP_ON
		#pragma multi_compile_fog
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		// Set per renderer by BSLegacyLightmaps through a MaterialPropertyBlock.
		// Unity resets Renderer.lightmapIndex in the editor whenever it decides a
		// scene is "not baked" (no LightingData asset), which is exactly our case,
		// so the baked map is also delivered through these two uniforms. They cost
		// nothing when unused: _BSLightmapST stays (0,0,0,0) and the branch is off.
		sampler2D _BSLightmap;
		float4 _BSLightmapST;

		struct appdata_t {
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float2 lmuv : TEXCOORD1;
			UNITY_FOG_COORDS(2)
		};

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			#ifdef LIGHTMAP_ON
			o.lmuv = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
			#else
			o.lmuv = v.texcoord1.xy * _BSLightmapST.xy + _BSLightmapST.zw;
			#endif
			UNITY_TRANSFER_FOG(o, o.pos);
			return o;
		}

		fixed4 frag (v2f i) : SV_Target {
			fixed4 col = tex2D(_MainTex, i.uv);
			#ifdef LIGHTMAP_ON
			col.rgb *= 2.0 * UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmuv).rgb;
			#else
			if (any(_BSLightmapST.xy))
			{
				col.rgb *= 2.0 * tex2D(_BSLightmap, i.lmuv).rgb;
			}
			#endif
			UNITY_APPLY_FOG(i.fogCoord, col);
			return col;
		}
		ENDCG
	}
}
}
