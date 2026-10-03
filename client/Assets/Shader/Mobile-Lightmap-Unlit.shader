// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/Mobile-Unlit (Supports Lightmap).shaderlab (compiled ShaderLab taken from
// sharedassets2.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "Mobile/Unlit (Supports Lightmap)" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	// Recovery additions (not in the APK shader, documented deviation — see
	// docs/lightmaps-410.md): a MaterialPropertyBlock can only address
	// properties declared in this block, so the BSLegacyLightmaps delivery
	// needs these two here. [HideInInspector] keeps the inspector clean; the
	// defaults (black texture / zero scale) leave the pass as plain unlit.
	[HideInInspector] _BSLightmap ("BS legacy lightmap (recovery)", 2D) = "black" {}
	[HideInInspector] _BSLightmapST ("BS legacy lightmap scale/offset (recovery)", Vector) = (0, 0, 0, 0)
}
SubShader {
	LOD 100
	Tags { "RenderType"="Opaque" }

	// The APK ships fixed-function Vertex / VertexLM / VertexLMRGBM passes.
	// Unity 5 removed unity_LightmapMatrix and the fixed-function combiners.
	// A CG pass tagged "ForwardBase" with multi_compile LIGHTMAP_ON also turned
	// out to be at the mercy of the editor keyword stripping and lightmap
	// bookkeeping for scenes without a LightingData asset (which is every
	// scene of this export). This port therefore uses ONE untagged CG pass —
	// the same pass shape as the fixed-function shaders that render correctly
	// on the target editor — and takes the bake exclusively from the
	// per-renderer MaterialPropertyBlock set by BSLegacyLightmaps. The x2
	// reproduces the dLDR "double" decode of the original VertexLM pass.
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_fog
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		// Set per renderer by BSLegacyLightmaps through a MaterialPropertyBlock.
		sampler2D _BSLightmap;
		float4 _BSLightmapST;
		// Debug switch driven by Shader.SetGlobalFloat from
		// Tools > Block Strike > Lighting: debug view. Deliberately NOT a
		// material property: a declared property would shadow the global.
		// 0 = normal, 1 = albedo only, 2 = lightmap only, 3 = UV1 as colour.
		float _BSDebugMode;

		struct appdata_t {
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float2 bsuv : TEXCOORD1;
			UNITY_FOG_COORDS(2)
		};

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			o.bsuv = v.texcoord1.xy * _BSLightmapST.xy + _BSLightmapST.zw;
			UNITY_TRANSFER_FOG(o, o.pos);
			return o;
		}

		fixed4 frag (v2f i) : SV_Target {
			fixed4 col = tex2D(_MainTex, i.uv);
			// _BSLightmapST.x is 0 when nothing is delivered -> plain unlit
			// albedo, exactly the APK's plain "Vertex" pass.
			fixed3 lm = lerp(fixed3(1, 1, 1),
			                 2.0 * tex2D(_BSLightmap, i.bsuv).rgb,
			                 step(0.0001, _BSLightmapST.x));
			// Safety floor: baked shade may be dark, never pitch black.
			lm = max(lm, fixed3(0.02, 0.02, 0.02));
			if (_BSDebugMode > 0.5)
			{
				if (_BSDebugMode < 1.5) { return fixed4(col.rgb, 1); }
				if (_BSDebugMode < 2.5) { return fixed4(lm, 1); }
				return fixed4(frac(i.bsuv), 0, 1);
			}
			col.rgb *= lm;
			UNITY_APPLY_FOG(i.fogCoord, col);
			return col;
		}
		ENDCG
	}
}
}
