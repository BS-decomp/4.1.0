// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/Mobile-VertexLit.shaderlab (compiled ShaderLab taken from
// sharedassets2.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "Mobile/VertexLit" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	LOD 80
	Tags { "RenderType"="Opaque" }

	// Same situation as Mobile/Unlit (Supports Lightmap): the legacy
	// Vertex/VertexLM/VertexLMRGBM trio becomes one ForwardBase pass.
	// Unlit case = ShadeVertexLights (what the fixed-function Material/Lighting
	// block compiled into: ambient + per-vertex lights, no extra doubling),
	// lightmapped case = x2 dLDR decode of the original VertexLM pass.
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

		struct appdata_t {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			#ifdef LIGHTMAP_ON
			float2 lmuv : TEXCOORD1;
			#else
			fixed3 vlight : TEXCOORD1;
			#endif
			UNITY_FOG_COORDS(2)
		};

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			#ifdef LIGHTMAP_ON
			o.lmuv = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
			#else
			o.vlight = ShadeVertexLights(v.vertex, v.normal);
			#endif
			UNITY_TRANSFER_FOG(o, o.pos);
			return o;
		}

		fixed4 frag (v2f i) : SV_Target {
			fixed4 col = tex2D(_MainTex, i.uv);
			#ifdef LIGHTMAP_ON
			col.rgb *= 2.0 * UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmuv).rgb;
			#else
			col.rgb *= i.vlight;
			#endif
			UNITY_APPLY_FOG(i.fogCoord, col);
			return col;
		}
		ENDCG
	}
}
Fallback "Legacy Shaders/VertexLit"
}
