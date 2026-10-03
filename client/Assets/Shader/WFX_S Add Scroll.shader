// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/WFX-Scroll-Additive.shaderlab (compiled ShaderLab taken from
// sharedassets5.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "WFX/Scroll/Additive" {
Properties {
	_MainTex ("Looped Texture + Alpha Mask", 2D) = "white" {}
	_InvFade ("Soft Particles Factor", Range(0.01,3)) = 1
	_ScrollSpeed ("Scroll Speed", Float) = 2
}
SubShader {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Pass {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Cull Off
		Blend One One
		ColorMask RGB
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_particles
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		float _ScrollSpeed;
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
			o.texcoord = v.texcoord;   // GLES passes uv0 through unscaled
			return o;
		}

		// GLES: mask = tex(uv).a * fadedAlpha; uv.y -= frac(_Time.x * _ScrollSpeed);
		//       out.rgb = mask * (tex(scrolled).rgb * color.rgb); out.a = mask
		fixed4 frag (v2f i) : SV_Target {
			fixed alpha = i.color.a;
			#ifdef SOFTPARTICLES_ON
			float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
			alpha *= saturate(_InvFade * (sceneZ - i.projPos.z));
			#endif
			fixed mask = tex2D(_MainTex, i.texcoord).a * alpha;
			float2 uv = i.texcoord;
			uv.y -= frac(_Time.x * _ScrollSpeed);
			fixed4 col = fixed4(mask, mask, mask, mask);
			col.rgb *= tex2D(_MainTex, uv).rgb * i.color.rgb;
			return col;
		}
		ENDCG
	}
}
}
