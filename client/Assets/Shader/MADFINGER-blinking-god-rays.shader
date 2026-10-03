// Rebuilt for the Unity 2021 port of Block Strike 4.1.0.
// Ground truth: tools/shader-extract/MADFINGER-Transparent-Blinking GodRays.shaderlab (compiled ShaderLab taken from
// sharedassets3.assets inside com.rexetstudio.blockstrike-780.apk).
// Name, properties, tags and render state are copied verbatim from it; the
// programs below are transcribed from the GLES/GLES3 code in the same text.
// Regenerate with tools/rebuild_shaders.py, check with tools/verify_shaders.py.
Shader "MADFINGER/Transparent/Blinking GodRays" {
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
