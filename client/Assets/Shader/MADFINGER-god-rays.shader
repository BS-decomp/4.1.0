// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/MADFINGER-Transparent-GodRays.shaderlab (compiled ShaderLab taken from
// sharedassets4.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "MADFINGER/Transparent/GodRays" {
Properties {
	_MainTex ("Base texture", 2D) = "white" {}
	_FadeOutDistNear ("Near fadeout dist", Float) = 10
	_FadeOutDistFar ("Far fadeout dist", Float) = 10000
	_Multiplier ("Multiplier", Float) = 1
	_ContractionAmount ("Near contraction amount", Float) = 5
}
SubShader {
	LOD 100
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Pass {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Cull Off
		Fog { Color (0,0,0,0) }
		Blend One One
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float _FadeOutDistNear;
		float _FadeOutDistFar;
		float _Multiplier;
		float _ContractionAmount;

		struct appdata_t {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			fixed4 color : COLOR;
			float2 texcoord : TEXCOORD0;
		};
		struct v2f { float4 pos : SV_POSITION; float2 texcoord : TEXCOORD0; fixed4 color : TEXCOORD1; };

		// Transcribed 1:1 from the GLES vertex program:
		//   d     = length(mul(UNITY_MATRIX_MV, vertex).xyz)
		//   near  = saturate(d / _FadeOutDistNear)
		//   far   = 1 - saturate(max(d - _FadeOutDistFar, 0) * 0.2)
		//   f     = (near^2)^2 * far^2
		//   vpos  = vertex - normalize(normal) * saturate(1 - f) * color.a * _ContractionAmount
		//   col   = f * color * _Multiplier
		v2f vert (appdata_t v) {
			v2f o;
			float3 viewPos = mul(UNITY_MATRIX_MV, v.vertex).xyz;
			float d = length(viewPos);
			float nearF = saturate(d / _FadeOutDistNear);
			float farF = 1.0 - saturate(max(d - _FadeOutDistFar, 0.0) * 0.2);
			nearF = nearF * nearF;
			float f = (nearF * nearF) * (farF * farF);
			float4 vpos = v.vertex;
			vpos.xyz -= normalize(v.normal) * saturate(1.0 - f) * v.color.a * _ContractionAmount;
			o.pos = UnityObjectToClipPos(vpos);
			o.texcoord = v.texcoord;
			o.color = f * v.color * _Multiplier;
			return o;
		}
		fixed4 frag (v2f i) : SV_Target { return tex2D(_MainTex, i.texcoord) * i.color; }
		ENDCG
	}
}
}
