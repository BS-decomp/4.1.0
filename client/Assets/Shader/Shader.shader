Shader "DirtyLens_01_GeoMobile" {
Properties {
 _FlareMultiplier ("_FlareMultiplier", Float) = 10
 _Flare ("_Flare", 2D) = "black" {}
 _DirtyLensTexture ("_DirtyLensTexture", 2D) = "black" {}
}
	//DummyShaderTextExporter
	
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Lambert
#pragma target 3.0
		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};
		void surf(Input IN, inout SurfaceOutput o)
		{
			float4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
		}
		ENDCG
	}
}