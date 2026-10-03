// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/Hidden-ProBuilder-HideVertices.shaderlab (compiled ShaderLab taken from
// 99ab5b57a637bdb4192babafc1298bee inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "Hidden/ProBuilder/HideVertices" {
SubShader {
	Tags { "IgnoreProjector"="True" "RenderType"="Geometry" }
	Pass {
		Tags { "IgnoreProjector"="True" "RenderType"="Geometry" }
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		struct v2f { float4 pos : SV_POSITION; };
		// GLES: gl_Position = vec4(0.0); gl_FragData[0] = vec4(0.0);
		v2f vert (float4 vertex : POSITION) { v2f o; o.pos = float4(0,0,0,0); return o; }
		fixed4 frag (v2f i) : SV_Target { return fixed4(0,0,0,0); }
		ENDCG
	}
}
}
