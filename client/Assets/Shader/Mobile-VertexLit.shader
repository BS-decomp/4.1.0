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
		// Set per renderer by BSLegacyLightmaps through a MaterialPropertyBlock.
		// Unity resets Renderer.lightmapIndex in the editor whenever it decides a
		// scene is "not baked" (no LightingData asset), which is exactly our case,
		// so the baked map is also delivered through these two uniforms. They cost
		// nothing when unused: _BSLightmapST stays (0,0,0,0) and the branch is off.
		sampler2D _BSLightmap;
		float4 _BSLightmapST;

		// One place that decides where the baked light comes from:
		//  * Unity's own lightmap when the engine provides one (LIGHTMAP_ON),
		//  * otherwise the texture BSLegacyLightmaps pushes per renderer.
		// Both are decoded with the x2 of the original dLDR "double" pass.
		// A fully black sample means "nothing is actually bound" (Unity hands out a
		// black default texture), so the surface stays unlit instead of going black.
		// Debug switch driven by Tools > Block Strike > Lighting: debug view.
		// 0 = normal, 1 = albedo only, 2 = lightmap only, 3 = UV1 as colour.
		float _BSDebugMode;

		fixed3 BSSampleLightmap(float2 unityUV, float2 bsUV)
		{
			fixed3 lm = fixed3(1, 1, 1);
			#ifdef LIGHTMAP_ON
			lm = 2.0 * UNITY_SAMPLE_TEX2D(unity_Lightmap, unityUV).rgb;
			#else
			if (any(_BSLightmapST.xy))
			{
				lm = 2.0 * tex2D(_BSLightmap, bsUV).rgb;
			}
			#endif
			return (lm.r + lm.g + lm.b) < 0.01 ? fixed3(1, 1, 1) : lm;
		}

		struct appdata_t {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float2 lmuv : TEXCOORD1;
			float2 bsuv : TEXCOORD2;
			fixed3 vlight : TEXCOORD3;
			UNITY_FOG_COORDS(4)
		};

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			#ifdef LIGHTMAP_ON
			o.lmuv = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
			#else
			o.lmuv = v.texcoord1.xy;
			#endif
			o.bsuv = v.texcoord1.xy * _BSLightmapST.xy + _BSLightmapST.zw;
			o.vlight = ShadeVertexLights(v.vertex, v.normal);
			UNITY_TRANSFER_FOG(o, o.pos);
			return o;
		}

		fixed4 frag (v2f i) : SV_Target {
			fixed4 col = tex2D(_MainTex, i.uv);
			fixed3 lm = BSSampleLightmap(i.lmuv, i.bsuv);
			if (_BSDebugMode > 0.5)
			{
				if (_BSDebugMode < 1.5) { return fixed4(col.rgb, 1); }
				if (_BSDebugMode < 2.5) { return fixed4(lm, 1); }
				return fixed4(frac(i.bsuv), 0, 1);
			}
			// Priority, matching the original VertexLM behaviour: the engine
			// lightmap (LIGHTMAP_ON, play mode) replaces lighting entirely; the
			// BSLegacyLightmaps property block is the editor/fallback source;
			// only a renderer with neither falls back to vertex lights.
			#ifdef LIGHTMAP_ON
			col.rgb *= lm;
			#else
			col.rgb *= any(_BSLightmapST.xy) ? lm : i.vlight;
			#endif
			UNITY_APPLY_FOG(i.fogCoord, col);
			return col;
		}
		ENDCG
	}
}
Fallback "Legacy Shaders/VertexLit"
}
