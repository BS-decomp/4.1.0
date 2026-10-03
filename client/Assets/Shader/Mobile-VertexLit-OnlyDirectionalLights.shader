// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/Mobile-VertexLit (Only Directional Lights).shaderlab (compiled ShaderLab taken from
// sharedassets5.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "Mobile/VertexLit (Only Directional Lights)" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	LOD 80
	Tags { "RenderType"="Opaque" }

	// The APK ships exactly one pass (LIGHTMODE=ForwardBase, SHADOWSUPPORT) with
	// variants for LIGHTMAP_ON/OFF, i.e. a Lambert surface shader with
	// noforwardadd. The GLES program for LIGHTMAP_ON is
	//   c.rgb = tex.rgb * 2 * lightmap.rgb
	// and for LIGHTMAP_OFF it is the standard Lambert + SH, which is what the
	// surface compiler below generates.
	CGPROGRAM
	#pragma surface surf Lambert noforwardadd

	sampler2D _MainTex;
	struct Input { float2 uv_MainTex; };

	void surf (Input IN, inout SurfaceOutput o) {
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
		o.Albedo = c.rgb;
		o.Alpha = c.a;
	}
	ENDCG
}
Fallback "Mobile/VertexLit"
}
