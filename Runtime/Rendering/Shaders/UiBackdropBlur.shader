// 5-tap box blur, applied repeatedly (per UiBackdropBlurSettings.Iterations) at a downsampled
// resolution -- a repeated box blur approximates a Gaussian by the Central Limit Theorem, which
// is simple and cheap enough to run once per iteration without a full separable two-pass kernel.
Shader "Hidden/GameLovers/UiService/UiBackdropBlur"
{
	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" }
		ZTest Always
		ZWrite Off
		Cull Off

		Pass
		{
			Name "UiBackdropBlur"

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			float _Spread;
			half4 _Tint;

			half4 Frag(Varyings input) : SV_Target
			{
				float2 texelSize = _BlitTexture_TexelSize.xy * _Spread;
				float2 uv = input.texcoord;

				half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel) * 4.0;
				color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( texelSize.x,  texelSize.y), _BlitMipLevel);
				color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-texelSize.x,  texelSize.y), _BlitMipLevel);
				color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( texelSize.x, -texelSize.y), _BlitMipLevel);
				color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-texelSize.x, -texelSize.y), _BlitMipLevel);
				color /= 8.0;

				return color * _Tint;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
