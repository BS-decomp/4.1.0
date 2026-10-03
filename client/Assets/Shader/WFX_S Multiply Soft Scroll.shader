// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/WFX-Scroll-Multiply Soft Tint.shaderlab (compiled ShaderLab taken from
// sharedassets5.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "WFX/Scroll/Multiply Soft Tint" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Texture", 2D) = "white" {}
	_ScrollSpeed ("Scroll Speed", Float) = 2
}
SubShader {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Pass {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Cull Off
		Blend DstColor SrcColor
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		fixed4 _TintColor;
		float _ScrollSpeed;

		struct appdata_t { float4 vertex : POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };
		struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.color = v.color;
			o.texcoord = v.texcoord;
			return o;
		}

		// GLES: mask = tex(uv).a * color.a; uv.y -= frac(_Time.x * _ScrollSpeed);
		//       tex.rgb = scrolled.rgb * (color.rgb * _TintColor.rgb); tex.a = scrolled.a
		//       out = lerp(0.5, tex, mask)
		fixed4 frag (v2f i) : SV_Target {
			fixed mask = tex2D(_MainTex, i.texcoord).a * i.color.a;
			float2 uv = i.texcoord;
			uv.y -= frac(_Time.x * _ScrollSpeed);
			fixed4 scrolled = tex2D(_MainTex, uv);
			fixed4 col;
			col.rgb = scrolled.rgb * (i.color.rgb * _TintColor.rgb);
			col.a = scrolled.a;
			return lerp(fixed4(0.5, 0.5, 0.5, 0.5), col, mask);
		}
		ENDCG
	}
}
}
