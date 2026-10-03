// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/Mobile-VertexLit.shaderlab (compiled ShaderLab taken from
// sharedassets2.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "Mobile/VertexLit" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	LOD 80
	Tags { "RenderType"="Opaque" }

	// The three passes below are the original fixed-function passes, copied
	// verbatim from the APK text (Unity compiled them to GLES at build time).
	// The SHADOWCASTER/SHADOWCOLLECTOR passes of the 4.x shader are replaced by
	// the fallback: SHADOWCOLLECTOR no longer exists in Unity 2021.
	Pass {
		Tags { "LightMode"="Vertex" "RenderType"="Opaque" }
		Lighting On
		Material {
			Ambient (1,1,1,1)
			Diffuse (1,1,1,1)
		}
		SetTexture [_MainTex] { combine texture * primary double, texture alpha * primary alpha }
	}
	Pass {
		Tags { "LightMode"="VertexLM" "RenderType"="Opaque" }
		BindChannels {
			Bind "vertex", Vertex
			Bind "normal", Normal
			Bind "texcoord1", TexCoord0
			Bind "texcoord", TexCoord1
		}
		SetTexture [unity_Lightmap] { Matrix [unity_LightmapMatrix] combine texture }
		SetTexture [_MainTex] { combine texture * previous double, texture alpha * primary alpha }
	}
	Pass {
		Tags { "LightMode"="VertexLMRGBM" "RenderType"="Opaque" }
		BindChannels {
			Bind "vertex", Vertex
			Bind "normal", Normal
			Bind "texcoord1", TexCoord0
			Bind "texcoord", TexCoord1
		}
		SetTexture [unity_Lightmap] { Matrix [unity_LightmapMatrix] combine texture * texture alpha double }
		SetTexture [_MainTex] { combine texture * previous quad, texture alpha * primary alpha }
	}
}
Fallback "Legacy Shaders/VertexLit"
}
