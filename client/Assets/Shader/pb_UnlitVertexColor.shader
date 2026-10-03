// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/ProBuilder-UnlitVertexColor.shaderlab (compiled ShaderLab taken from
// 84f04ba86cd824249bb1c2029be83101 inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "ProBuilder/UnlitVertexColor" {
SubShader {
	Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Pass {
		Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		struct appdata_t { float4 vertex : POSITION; fixed4 color : COLOR; };
		struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; };

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.color = v.color;
			return o;
		}
		// "AlphaTest Greater 0.25" in the original fixed-function pass.
		fixed4 frag (v2f i) : SV_Target { clip(i.color.a - 0.25); return i.color; }
		ENDCG
	}
}
}
