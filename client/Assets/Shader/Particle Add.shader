// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/Particles-Additive.shaderlab (compiled ShaderLab taken from
// 0000000000000000f000000000000000 inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "Particles/Additive" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture", 2D) = "white" {}
	_InvFade ("Soft Particles Factor", Range(0.01,3)) = 1
}
SubShader {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Pass {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Cull Off
		Blend SrcAlpha One
		ColorMask RGB
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_particles
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		fixed4 _TintColor;

		struct appdata_t { float4 vertex : POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };
		struct v2f {
			float4 pos : SV_POSITION;
			fixed4 color : COLOR;
			float2 texcoord : TEXCOORD0;
			#ifdef SOFTPARTICLES_ON
			float4 projPos : TEXCOORD1;
			#endif
		};

		sampler2D_float _CameraDepthTexture;
		float _InvFade;

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

		// GLES: col.w *= saturate(_InvFade * (sceneZ - partZ));
		//       out = (2 * col) * _TintColor * tex2D(_MainTex, uv)
		fixed4 frag (v2f i) : SV_Target {
			#ifdef SOFTPARTICLES_ON
			float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
			float partZ = i.projPos.z;
			float fade = saturate(_InvFade * (sceneZ - partZ));
			i.color.a *= fade;
			#endif
			fixed4 col = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
			clip(col.a - 0.01);
			return col;
		}
		ENDCG
	}
}
}
