// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/Unlit-Texture.shaderlab (compiled ShaderLab taken from
// sharedassets2.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "Unlit/Texture" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	LOD 100
	Tags { "RenderType"="Opaque" }
	Pass {
		Tags { "RenderType"="Opaque" }
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		struct appdata_t { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; };
		struct v2f { float4 pos : SV_POSITION; float2 texcoord : TEXCOORD0; };

		sampler2D _MainTex;
		float4 _MainTex_ST;

		// GLES: xlv_TEXCOORD0 = uv0 * _MainTex_ST.xy + _MainTex_ST.zw
		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
			return o;
		}
		// GLES: gl_FragData[0] = texture2D(_MainTex, uv)
		fixed4 frag (v2f i) : SV_Target { return tex2D(_MainTex, i.texcoord); }
		ENDCG
	}
}
}
