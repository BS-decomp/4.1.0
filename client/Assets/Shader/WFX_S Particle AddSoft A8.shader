// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/WFX-Additive (Soft) Alpha8.shaderlab (compiled ShaderLab taken from
// sharedassets3.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "WFX/Additive (Soft) Alpha8" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture (alpha)", 2D) = "white" {}
	_InvFade ("Soft Particles Factor", Range(0.01,3)) = 1
}
SubShader {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Pass {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Cull Off
		Blend OneMinusDstColor One
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_particles
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		fixed4 _TintColor;
		sampler2D_float _CameraDepthTexture;
		float _InvFade;

		struct appdata_t { float4 vertex : POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };
		struct v2f {
			float4 pos : SV_POSITION;
			fixed4 color : COLOR;
			float2 texcoord : TEXCOORD0;
			#ifdef SOFTPARTICLES_ON
			float4 projPos : TEXCOORD1;
			#endif
		};

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			#ifdef SOFTPARTICLES_ON
			o.projPos = ComputeScreenPos(o.pos);
			COMPUTE_EYEDEPTH(o.projPos.z);
			#endif
			o.color = v.color;
			o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
			return o;
		}

		// GLES: out = ((2 * color) * _TintColor) * tex.a
		fixed4 frag (v2f i) : SV_Target {
			#ifdef SOFTPARTICLES_ON
			float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
			i.color.a *= saturate(_InvFade * (sceneZ - i.projPos.z));
			#endif
			return ((2.0f * i.color) * _TintColor) * tex2D(_MainTex, i.texcoord).a;
		}
		ENDCG
	}
}
}
