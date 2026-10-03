#!/usr/bin/env python3
"""Replace the AssetRipper shader placeholders with shaders rebuilt from the APK.

For every placeholder in `client/Assets/Shader` (`//DummyShaderTextExporter`)
this writes a real shader whose name, properties, tags and render state are
taken verbatim from the compiled ShaderLab stored in the APK
(`tools/shader-extract/*.shaderlab`, produced by `extract_apk_shaders.py`), and
whose programs are transcribed from the GLES/GLES3 code in that same text.

The `.meta` files (and therefore the GUIDs every material points at) are left
untouched — only the shader body changes.

    python3 tools/rebuild_shaders.py --dry-run
    python3 tools/rebuild_shaders.py
    python3 tools/verify_shaders.py          # checks the result against the APK
"""

from __future__ import annotations

import argparse
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SHADER_DIR = ROOT / "client" / "Assets" / "Shader"
EXTRACT_DIR = ROOT / "tools" / "shader-extract"
DUMMY_MARKERS = ("DummyShaderTextExporter", "Shader created for shader asset")

HEADER = """// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: {extract} (compiled ShaderLab taken from
// {source} inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
"""

# --------------------------------------------------------------------------- #
# Shared snippets
# --------------------------------------------------------------------------- #

SOFT_PARTICLE_DECL = """            sampler2D _CameraDepthTexture;
            float _InvFade;
"""

SHADERS = {}


def shader(file_name, shader_name, extract):
    def wrap(body):
        SHADERS[file_name] = {"name": shader_name, "extract": extract, "body": body}
        return body
    return wrap


# --------------------------------------------------------------------------- #
# 1. Unlit/Texture
# --------------------------------------------------------------------------- #
shader("Unlit-Normal.shader", "Unlit/Texture", "Unlit-Texture.shaderlab")("""Shader "Unlit/Texture" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	LOD 100
	Tags { "RenderType"="Opaque" }
	Pass {
		Tags { "RenderType"="Opaque" }
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		struct appdata_t { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; };
		struct v2f { float4 pos : SV_POSITION; float2 texcoord : TEXCOORD0; };

		sampler2D _MainTex;
		float4 _MainTex_ST;

		// GLES: xlv_TEXCOORD0 = uv0 * _MainTex_ST.xy + _MainTex_ST.zw
		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
			return o;
		}
		// GLES: gl_FragData[0] = texture2D(_MainTex, uv)
		fixed4 frag (v2f i) : SV_Target { return tex2D(_MainTex, i.texcoord); }
		ENDCG
	}
}
}
""")

# --------------------------------------------------------------------------- #
# 2. Unlit/Transparent
# --------------------------------------------------------------------------- #
shader("Unlit-Alpha.shader", "Unlit/Transparent", "Unlit-Transparent.shaderlab")("""Shader "Unlit/Transparent" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
}
SubShader {
	LOD 100
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Pass {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		struct appdata_t { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; };
		struct v2f { float4 pos : SV_POSITION; float2 texcoord : TEXCOORD0; };

		sampler2D _MainTex;
		float4 _MainTex_ST;

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
			return o;
		}
		fixed4 frag (v2f i) : SV_Target { return tex2D(_MainTex, i.texcoord); }
		ENDCG
	}
}
}
""")

# --------------------------------------------------------------------------- #
# 3. ProBuilder/Unlit Solid Color
# --------------------------------------------------------------------------- #
shader("UnlitSolidColor.shader", "ProBuilder/Unlit Solid Color", "ProBuilder-Unlit Solid Color.shaderlab")("""Shader "ProBuilder/Unlit Solid Color" {
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
""")

# --------------------------------------------------------------------------- #
# 4. ProBuilder/UnlitVertexColor
# --------------------------------------------------------------------------- #
shader("pb_UnlitVertexColor.shader", "ProBuilder/UnlitVertexColor", "ProBuilder-UnlitVertexColor.shaderlab")("""Shader "ProBuilder/UnlitVertexColor" {
SubShader {
	Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Pass {
		Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		struct appdata_t { float4 vertex : POSITION; fixed4 color : COLOR; };
		struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; };

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.color = v.color;
			return o;
		}
		// "AlphaTest Greater 0.25" in the original fixed-function pass.
		fixed4 frag (v2f i) : SV_Target { clip(i.color.a - 0.25); return i.color; }
		ENDCG
	}
}
}
""")

# --------------------------------------------------------------------------- #
# 5. Hidden/ProBuilder/HideVertices
# --------------------------------------------------------------------------- #
shader("pb_HideVertices.shader", "Hidden/ProBuilder/HideVertices", "Hidden-ProBuilder-HideVertices.shaderlab")("""Shader "Hidden/ProBuilder/HideVertices" {
SubShader {
	Tags { "IgnoreProjector"="True" "RenderType"="Geometry" }
	Pass {
		Tags { "IgnoreProjector"="True" "RenderType"="Geometry" }
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		struct v2f { float4 pos : SV_POSITION; };
		// GLES: gl_Position = vec4(0.0); gl_FragData[0] = vec4(0.0);
		v2f vert (float4 vertex : POSITION) { v2f o; o.pos = float4(0,0,0,0); return o; }
		fixed4 frag (v2f i) : SV_Target { return fixed4(0,0,0,0); }
		ENDCG
	}
}
}
""")

# --------------------------------------------------------------------------- #
# 6. Particles/Additive
# --------------------------------------------------------------------------- #
shader("Particle Add.shader", "Particles/Additive", "Particles-Additive.shaderlab")("""Shader "Particles/Additive" {
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
""")

# --------------------------------------------------------------------------- #
# 7. WFX/Additive Alpha8
# --------------------------------------------------------------------------- #
shader("WFX_S Particle Add A8.shader", "WFX/Additive Alpha8", "WFX-Additive Alpha8.shaderlab")("""Shader "WFX/Additive Alpha8" {
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
		Blend One One
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

		// GLES: out = ((2 * color) * _TintColor) * ((tex.a * color.a) * 2)
		fixed4 frag (v2f i) : SV_Target {
			#ifdef SOFTPARTICLES_ON
			float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
			i.color.a *= saturate(_InvFade * (sceneZ - i.projPos.z));
			#endif
			fixed alpha = tex2D(_MainTex, i.texcoord).a;
			return ((2.0f * i.color) * _TintColor) * ((alpha * i.color.a) * 2.0f);
		}
		ENDCG
	}
}
}
""")

# --------------------------------------------------------------------------- #
# 8. WFX/Additive (Soft) Alpha8
# --------------------------------------------------------------------------- #
shader("WFX_S Particle AddSoft A8.shader", "WFX/Additive (Soft) Alpha8", "WFX-Additive (Soft) Alpha8.shaderlab")("""Shader "WFX/Additive (Soft) Alpha8" {
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
""")

# --------------------------------------------------------------------------- #
# 9. WFX/Scroll/Additive
# --------------------------------------------------------------------------- #
shader("WFX_S Add Scroll.shader", "WFX/Scroll/Additive", "WFX-Scroll-Additive.shaderlab")("""Shader "WFX/Scroll/Additive" {
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
""")

# --------------------------------------------------------------------------- #
# 10. WFX/Scroll/Multiply Soft Tint
# --------------------------------------------------------------------------- #
shader("WFX_S Multiply Soft Scroll.shader", "WFX/Scroll/Multiply Soft Tint", "WFX-Scroll-Multiply Soft Tint.shaderlab")("""Shader "WFX/Scroll/Multiply Soft Tint" {
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
""")

# --------------------------------------------------------------------------- #
# 11. WFX/Scroll/Smoke
# --------------------------------------------------------------------------- #
shader("WFX_S Smoke Scroll.shader", "WFX/Scroll/Smoke", "WFX-Scroll-Smoke.shaderlab")("""Shader "WFX/Scroll/Smoke" {
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
		Blend DstColor SrcAlpha
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
		//       tex.rgb = scrolled.rgb * (color.rgb * _TintColor.rgb); tex.a = mask
		//       out = lerp(0.5, tex, mask)
		fixed4 frag (v2f i) : SV_Target {
			fixed mask = tex2D(_MainTex, i.texcoord).a * i.color.a;
			float2 uv = i.texcoord;
			uv.y -= frac(_Time.x * _ScrollSpeed);
			fixed4 col;
			col.rgb = tex2D(_MainTex, uv).rgb * (i.color.rgb * _TintColor.rgb);
			col.a = mask;
			return lerp(fixed4(0.5, 0.5, 0.5, 0.5), col, mask);
		}
		ENDCG
	}
}
}
""")

# --------------------------------------------------------------------------- #
# 12. MADFINGER/Transparent/GodRays
# --------------------------------------------------------------------------- #
shader("MADFINGER-god-rays.shader", "MADFINGER/Transparent/GodRays", "MADFINGER-Transparent-GodRays.shaderlab")("""Shader "MADFINGER/Transparent/GodRays" {
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
""")

# --------------------------------------------------------------------------- #
# 13. MADFINGER/Transparent/Blinking GodRays
# --------------------------------------------------------------------------- #
shader("MADFINGER-blinking-god-rays.shader", "MADFINGER/Transparent/Blinking GodRays", "MADFINGER-Transparent-Blinking GodRays.shaderlab")("""Shader "MADFINGER/Transparent/Blinking GodRays" {
Properties {
	_MainTex ("Base texture", 2D) = "white" {}
	_FadeOutDistNear ("Near fadeout dist", Float) = 10
	_FadeOutDistFar ("Far fadeout dist", Float) = 10000
	_Multiplier ("Color multiplier", Float) = 1
	_Bias ("Bias", Float) = 0
	_TimeOnDuration ("ON duration", Float) = 0.5
	_TimeOffDuration ("OFF duration", Float) = 0.5
	_BlinkingTimeOffsScale ("Blinking time offset scale (seconds)", Float) = 5
	_SizeGrowStartDist ("Size grow start dist", Float) = 5
	_SizeGrowEndDist ("Size grow end dist", Float) = 50
	_MaxGrowSize ("Max grow size", Float) = 2.5
	_NoiseAmount ("Noise amount (when zero, pulse wave is used)", Range(0,0.5)) = 0
	_Color ("Color", Color) = (1,1,1,1)
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
		float _Bias;
		float _TimeOnDuration;
		float _TimeOffDuration;
		float _BlinkingTimeOffsScale;
		float _SizeGrowStartDist;
		float _SizeGrowEndDist;
		float _MaxGrowSize;
		float _NoiseAmount;
		fixed4 _Color;

		struct appdata_t {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			fixed4 color : COLOR;
			float4 texcoord : TEXCOORD0;
		};
		struct v2f { float4 pos : SV_POSITION; float2 texcoord : TEXCOORD0; fixed4 color : TEXCOORD1; };

		// Transcribed 1:1 from the GLES vertex program (see
		// tools/shader-extract/MADFINGER-Transparent-Blinking GodRays.shaderlab):
		//   t       = _Time.y + _BlinkingTimeOffsScale * color.b
		//   period  = _TimeOnDuration + _TimeOffDuration; tp = fmod(t, period)
		//   pulse   = smoothstep(0, _TimeOnDuration*0.25, tp) * (1 - smoothstep(_TimeOnDuration*0.75, _TimeOnDuration, tp))
		//   noise   = _NoiseAmount * (sin(w) * (0.5*cos(w*0.6366 + 56.7272) + 0.5)) + (1 - _NoiseAmount), w = t * 2PI/_TimeOnDuration
		//   blink   = _NoiseAmount < 0.01 ? pulse : noise
		//   grow    = min(max(d - _SizeGrowStartDist, 0) / _SizeGrowEndDist, 1)
		//   pos     = vertex + grow^2 * _MaxGrowSize * color.a * normalize(normal)
		//   col     = (near^2)^2 * far^2 * _Color * _Multiplier * (blink + _Bias)
		v2f vert (appdata_t v) {
			v2f o;
			float3 nrm = normalize(v.normal);
			float t = _Time.y + (_BlinkingTimeOffsScale * v.color.b);
			float3 viewPos = mul(UNITY_MATRIX_MV, v.vertex).xyz;
			float d = length(viewPos);
			float nearF = saturate(d / _FadeOutDistNear);
			float farF = 1.0 - saturate(max(d - _FadeOutDistFar, 0.0) * 0.2);

			float period = _TimeOnDuration + _TimeOffDuration;
			float tp = fmod(t, period);
			float up = smoothstep(0.0, _TimeOnDuration * 0.25, tp);
			float down = smoothstep(_TimeOnDuration * 0.75, _TimeOnDuration, tp);
			float pulse = up * (1.0 - down);

			float w = t * (6.28319 / _TimeOnDuration);
			float noise = (_NoiseAmount * (sin(w) * ((0.5 * cos((w * 0.6366) + 56.7272)) + 0.5)))
			            + (1.0 - _NoiseAmount);
			float blink = (_NoiseAmount < 0.01) ? pulse : noise;

			float grow = min(max(d - _SizeGrowStartDist, 0.0) / _SizeGrowEndDist, 1.0);
			float4 pos = v.vertex;
			pos.xyz += ((grow * grow) * _MaxGrowSize * v.color.a) * nrm;

			nearF = nearF * nearF;
			o.pos = UnityObjectToClipPos(pos);
			o.texcoord = v.texcoord.xy;
			o.color = (((nearF * nearF) * (farF * farF)) * _Color) * _Multiplier * (blink + _Bias);
			return o;
		}
		fixed4 frag (v2f i) : SV_Target { return tex2D(_MainTex, i.texcoord) * i.color; }
		ENDCG
	}
}
}
""")

# --------------------------------------------------------------------------- #
# 14. DirtyLens_01_GeoMobile
# --------------------------------------------------------------------------- #
shader("Shader.shader", "DirtyLens_01_GeoMobile", "DirtyLens_01_GeoMobile.shaderlab")("""Shader "DirtyLens_01_GeoMobile" {
Properties {
	_FlareMultiplier ("_FlareMultiplier", Float) = 10
	_Flare ("_Flare", 2D) = "black" {}
	_DirtyLensTexture ("_DirtyLensTexture", 2D) = "black" {}
}
SubShader {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Pass {
		Name "FORWARD"
		Tags { "LightMode"="ForwardBase" "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Fog { Color (0,0,0,0) }
		Blend One One
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_fwdbase
		#include "UnityCG.cginc"
		#include "Lighting.cginc"

		sampler2D _Flare;
		float4 _Flare_ST;
		sampler2D _DirtyLensTexture;
		float _FlareMultiplier;

		struct appdata_t { float4 vertex : POSITION; float3 normal : NORMAL; float2 texcoord : TEXCOORD0; };
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 screen : TEXCOORD1;
			float3 lightDirWS : TEXCOORD2;
			float3 sh : TEXCOORD3;
			float3 normalWS : TEXCOORD4;
		};

		// The original is a surface shader compiled for ForwardBase. The GLES
		// program computes, in tangent space:
		//   albedo = tex2D(_Flare, uv) * _FlareMultiplier * tex2D(_DirtyLensTexture, screenUV)
		//   c.rgb  = albedo * (_LightColor0.rgb * max(0, N.L)) * 2 + albedo * SH
		// which is what this pass reproduces in world space (identical maths,
		// the shader has no normal map so tangent space only rotated N and L).
		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _Flare);
			o.screen = ComputeScreenPos(o.pos);
			float3 n = UnityObjectToWorldNormal(v.normal);
			o.normalWS = n;
			o.lightDirWS = normalize(_WorldSpaceLightPos0.xyz);
			o.sh = ShadeSH9(float4(n, 1.0));
			return o;
		}

		fixed4 frag (v2f i) : SV_Target {
			float2 screenUV = i.screen.xy / i.screen.w;
			fixed3 albedo = (tex2D(_Flare, i.uv) * _FlareMultiplier * tex2D(_DirtyLensTexture, screenUV)).rgb;
			fixed ndotl = max(0.0, dot(normalize(i.normalWS), i.lightDirWS));
			fixed3 col = albedo * (_LightColor0.rgb * ndotl) * 2.0;
			col += albedo * i.sh;
			return fixed4(col, 1.0);
		}
		ENDCG
	}
}
}
""")

# --------------------------------------------------------------------------- #
# --------------------------------------------------------------------------- #
# 18. Mobile/Unlit (Supports Lightmap) — the main map shader
# --------------------------------------------------------------------------- #
shader("Mobile-Lightmap-Unlit.shader", "Mobile/Unlit (Supports Lightmap)", "Mobile-Unlit (Supports Lightmap).shaderlab")("""Shader "Mobile/Unlit (Supports Lightmap)" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	// Recovery additions (not in the APK shader, documented deviation — see
	// docs/lightmaps-410.md): a MaterialPropertyBlock can only address
	// properties declared in this block, so the BSLegacyLightmaps delivery
	// needs these two here. [HideInInspector] keeps the inspector clean; the
	// defaults (black texture / zero scale) leave the pass as plain unlit.
	[HideInInspector] _BSLightmap ("BS legacy lightmap (recovery)", 2D) = "black" {}
	[HideInInspector] _BSLightmapST ("BS legacy lightmap scale/offset (recovery)", Vector) = (0, 0, 0, 0)
}
SubShader {
	LOD 100
	Tags { "RenderType"="Opaque" }

	// The APK ships fixed-function Vertex / VertexLM / VertexLMRGBM passes.
	// Unity 5 removed unity_LightmapMatrix and the fixed-function combiners.
	// A CG pass tagged "ForwardBase" with multi_compile LIGHTMAP_ON also turned
	// out to be at the mercy of the editor keyword stripping and lightmap
	// bookkeeping for scenes without a LightingData asset (which is every
	// scene of this export). This port therefore uses ONE untagged CG pass —
	// the same pass shape as the fixed-function shaders that render correctly
	// on the target editor — and takes the bake exclusively from the
	// per-renderer MaterialPropertyBlock set by BSLegacyLightmaps. The x2
	// reproduces the dLDR "double" decode of the original VertexLM pass.
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_fog
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		// Set per renderer by BSLegacyLightmaps through a MaterialPropertyBlock.
		sampler2D _BSLightmap;
		float4 _BSLightmapST;
		// Debug switch driven by Shader.SetGlobalFloat from
		// Tools > Block Strike > Lighting: debug view. Deliberately NOT a
		// material property: a declared property would shadow the global.
		// 0 = normal, 1 = albedo only, 2 = lightmap only, 3 = UV1 as colour.
		float _BSDebugMode;

		struct appdata_t {
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float2 bsuv : TEXCOORD1;
			UNITY_FOG_COORDS(2)
		};

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			o.bsuv = v.texcoord1.xy * _BSLightmapST.xy + _BSLightmapST.zw;
			UNITY_TRANSFER_FOG(o, o.pos);
			return o;
		}

		fixed4 frag (v2f i) : SV_Target {
			fixed4 col = tex2D(_MainTex, i.uv);
			// _BSLightmapST.x is 0 when nothing is delivered -> plain unlit
			// albedo, exactly the APK's plain "Vertex" pass.
			fixed3 lm = lerp(fixed3(1, 1, 1),
			                 2.0 * tex2D(_BSLightmap, i.bsuv).rgb,
			                 step(0.0001, _BSLightmapST.x));
			// Safety floor: baked shade may be dark, never pitch black.
			lm = max(lm, fixed3(0.02, 0.02, 0.02));
			if (_BSDebugMode > 0.5)
			{
				if (_BSDebugMode < 1.5) { return fixed4(col.rgb, 1); }
				if (_BSDebugMode < 2.5) { return fixed4(lm, 1); }
				return fixed4(frac(i.bsuv), 0, 1);
			}
			col.rgb *= lm;
			UNITY_APPLY_FOG(i.fogCoord, col);
			return col;
		}
		ENDCG
	}
}
}
""")
# --------------------------------------------------------------------------- #
shader("Mobile-VertexLit.shader", "Mobile/VertexLit", "Mobile-VertexLit.shaderlab")("""Shader "Mobile/VertexLit" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	// Recovery additions (not in the APK shader, documented deviation — see
	// docs/lightmaps-410.md): a MaterialPropertyBlock can only address
	// properties declared in this block, so the BSLegacyLightmaps delivery
	// needs these two here.
	[HideInInspector] _BSLightmap ("BS legacy lightmap (recovery)", 2D) = "black" {}
	[HideInInspector] _BSLightmapST ("BS legacy lightmap scale/offset (recovery)", Vector) = (0, 0, 0, 0)
}
SubShader {
	LOD 80
	Tags { "RenderType"="Opaque" }

	// Same situation as Mobile/Unlit (Supports Lightmap): the fixed-function
	// Vertex/VertexLM/VertexLMRGBM trio becomes one untagged CG pass that
	// takes the bake from the BSLegacyLightmaps MaterialPropertyBlock.
	// Unlit case = ShadeVertexLights (what the APK's Material/Lighting block
	// compiled into: ambient + per-vertex lights, no extra doubling);
	// lightmapped case = x2 dLDR decode of the original VertexLM pass.
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_fog
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		sampler2D _BSLightmap;
		float4 _BSLightmapST;
		float _BSDebugMode;

		struct appdata_t {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float2 bsuv : TEXCOORD1;
			fixed3 vlight : TEXCOORD2;
			UNITY_FOG_COORDS(3)
		};

		v2f vert (appdata_t v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			o.bsuv = v.texcoord1.xy * _BSLightmapST.xy + _BSLightmapST.zw;
			o.vlight = ShadeVertexLights(v.vertex, v.normal);
			UNITY_TRANSFER_FOG(o, o.pos);
			return o;
		}

		fixed4 frag (v2f i) : SV_Target {
			fixed4 col = tex2D(_MainTex, i.uv);
			// A delivered bake replaces lighting entirely (VertexLM behaviour);
			// without one the shader falls back to per-vertex lights (Vertex).
			fixed3 lm = lerp(i.vlight,
			                 2.0 * tex2D(_BSLightmap, i.bsuv).rgb,
			                 step(0.0001, _BSLightmapST.x));
			lm = max(lm, fixed3(0.02, 0.02, 0.02));
			if (_BSDebugMode > 0.5)
			{
				if (_BSDebugMode < 1.5) { return fixed4(col.rgb, 1); }
				if (_BSDebugMode < 2.5) { return fixed4(lm, 1); }
				return fixed4(frac(i.bsuv), 0, 1);
			}
			col.rgb *= lm;
			UNITY_APPLY_FOG(i.fogCoord, col);
			return col;
		}
		ENDCG
	}
}
Fallback "Legacy Shaders/VertexLit"
}
""")
# --------------------------------------------------------------------------- #
# 16. Mobile/VertexLit (Only Directional Lights)
# --------------------------------------------------------------------------- #
shader("Mobile-VertexLit-OnlyDirectionalLights.shader", "Mobile/VertexLit (Only Directional Lights)",
       "Mobile-VertexLit (Only Directional Lights).shaderlab")("""Shader "Mobile/VertexLit (Only Directional Lights)" {
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
""")

# --------------------------------------------------------------------------- #
# 17. ProBuilder/Diffuse Vertex Color
# --------------------------------------------------------------------------- #
shader("DiffuseVertexColor.shader", "ProBuilder/Diffuse Vertex Color", "ProBuilder-Diffuse Vertex Color.shaderlab")("""Shader "ProBuilder/Diffuse Vertex Color" {
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
""")


# --------------------------------------------------------------------------- #


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--force", action="store_true",
                        help="also rewrite shaders that are no longer placeholders")
    args = parser.parse_args(argv)

    written, skipped, problems = 0, 0, []
    for file_name, spec in sorted(SHADERS.items()):
        target = SHADER_DIR / file_name
        extract = EXTRACT_DIR / spec["extract"]
        if not target.exists():
            problems.append("missing shader asset: %s" % file_name)
            continue
        if not extract.exists():
            problems.append("missing ground truth: %s" % spec["extract"])
            continue
        ground = extract.read_text(encoding="utf-8")
        declared = re.match(r'Shader "([^"]+)"', ground)
        if not declared or declared.group(1) != spec["name"]:
            problems.append("%s: ground truth declares %r, expected %r" % (
                file_name, declared.group(1) if declared else None, spec["name"]))
            continue
        current = target.read_text(encoding="utf-8", errors="replace")
        if not any(marker in current for marker in DUMMY_MARKERS) and not args.force:
            skipped += 1
            continue
        source_file = "unknown"
        index = EXTRACT_DIR / "index.json"
        if index.exists():
            import json

            for item in json.loads(index.read_text(encoding="utf-8")):
                if item["shader_name"] == spec["name"]:
                    source_file = item["source_file"]
                    break
        text = HEADER.format(extract="tools/shader-extract/" + spec["extract"], source=source_file) + spec["body"]
        if not args.dry_run:
            target.write_text(text, encoding="utf-8")
        written += 1
        print("%-48s <- %s" % (file_name, spec["extract"]))

    print("\nrebuilt %d, already real %d%s" % (written, skipped, " (dry run)" if args.dry_run else ""))
    if problems:
        print("PROBLEMS:")
        for p in problems:
            print("  - " + p)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
