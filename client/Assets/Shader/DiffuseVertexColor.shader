// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/ProBuilder-Diffuse Vertex Color.shaderlab (compiled ShaderLab taken from
// sharedassets4.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "ProBuilder/Diffuse Vertex Color" {
Properties {
	_MainTex ("Texture", 2D) = "white" {}
}
SubShader {
	Tags { "RenderType"="Opaque" }

	// The APK ships the four passes a Lambert surface shader generates
	// (ForwardBase, ForwardAdd, PrePassBase, PrePassFinal) and falls back to
	// "Diffuse"; the albedo is texture * vertex colour.
	CGPROGRAM
	#pragma surface surf Lambert

	sampler2D _MainTex;
	struct Input { float2 uv_MainTex; float4 color : COLOR; };

	void surf (Input IN, inout SurfaceOutput o) {
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb;
		o.Alpha = c.a;
	}
	ENDCG
}
Fallback "Legacy Shaders/Diffuse"
}
