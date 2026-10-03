// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/Mobile-VertexLit.shaderlab (compiled ShaderLab taken from
// sharedassets2.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "Mobile/VertexLit" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	// Recovery additions (not in the APK shader, documented deviation — see
	// docs/lightmaps-410.md): a MaterialPropertyBlock can only address
	// properties declared in this block, so the BSLegacyLightmaps delivery
	// needs these two here.
	[HideInInspector] _BSLightmap ("BS legacy lightmap (recovery)", 2D) = "black" {}
	[HideInInspector] _BSLightmapST ("BS legacy lightmap scale/offset (recovery)", Vector) = (0, 0, 0, 0)
}
SubShader {
	LOD 80
	Tags { "RenderType"="Opaque" }

	// Same situation as Mobile/Unlit (Supports Lightmap): the fixed-function
	// Vertex/VertexLM/VertexLMRGBM trio becomes one untagged CG pass that
	// takes the bake from the BSLegacyLightmaps MaterialPropertyBlock.
	// Unlit case = ShadeVertexLights (what the APK's Material/Lighting block
	// compiled into: ambient + per-vertex lights, no extra doubling);
	// lightmapped case = x2 dLDR decode of the original VertexLM pass.
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_fog
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		sampler2D _BSLightmap;
		float4 _BSLightmapST;
		float _BSDebugMode;

		struct appdata_t {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float2 bsuv : TEXCOORD1;
			fixed3 vlight : TEXCOORD2;
			UNITY_FOG_COORDS(3)
		};

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			o.bsuv = v.texcoord1.xy * _BSLightmapST.xy + _BSLightmapST.zw;
			o.vlight = ShadeVertexLights(v.vertex, v.normal);
			UNITY_TRANSFER_FOG(o, o.pos);
			return o;
		}

		fixed4 frag (v2f i) : SV_Target {
			fixed4 col = tex2D(_MainTex, i.uv);
			// A delivered bake replaces lighting entirely (VertexLM behaviour);
			// without one the shader falls back to per-vertex lights (Vertex).
			fixed3 lm = lerp(i.vlight,
			                 2.0 * tex2D(_BSLightmap, i.bsuv).rgb,
			                 step(0.0001, _BSLightmapST.x));
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
Fallback "Legacy Shaders/VertexLit"
}
