// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/ProBuilder-Unlit Solid Color.shaderlab (compiled ShaderLab taken from
// sharedassets2.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "ProBuilder/Unlit Solid Color" {
Properties {
	_Color ("Color Tint", Color) = (1,1,1,1)
}
SubShader {
	Tags { "IgnoreProjector"="True" "RenderType"="Geometry" }
	Pass {
		Tags { "IgnoreProjector"="True" "RenderType"="Geometry" }
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		struct v2f { float4 pos : SV_POSITION; };
		fixed4 _Color;

		v2f vert (float4 vertex : POSITION) { v2f o; o.pos = UnityObjectToClipPos(vertex); return o; }
		// GLES: gl_FragData[0] = _Color;  original pass used fixed-function
		// "AlphaTest Greater 0.25", reproduced here with clip().
		fixed4 frag (v2f i) : SV_Target { clip(_Color.a - 0.25); return _Color; }
		ENDCG
	}
}
}
