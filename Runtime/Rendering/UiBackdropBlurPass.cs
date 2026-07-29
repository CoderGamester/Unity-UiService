using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Downsamples the camera colour, blurs it, and blits the result back over the full-resolution
	/// target, so any UI compositing after this pass sits on a blurred backdrop.
	/// </summary>
	internal class UiBackdropBlurPass : ScriptableRenderPass
	{
		// URP's own cached ShaderPropertyId equivalents are internal to its assembly; these names
		// match Core RP's Blit.hlsl conventions.
		private static readonly int _blitTextureId = Shader.PropertyToID("_BlitTexture");
		private static readonly int _blitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
		private static readonly int _spreadId = Shader.PropertyToID("_Spread");
		private static readonly int _tintId = Shader.PropertyToID("_Tint");

		private static readonly MaterialPropertyBlock _propertyBlock = new();

		private readonly Material _material;
		private readonly UiBackdropBlurRendererFeature _settings;

		public UiBackdropBlurPass(Material material, UiBackdropBlurRendererFeature settings)
		{
			_material = material;
			_settings = settings;
			profilingSampler = new ProfilingSampler(nameof(UiBackdropBlurPass));
		}

		/// <inheritdoc />
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			if (_material == null || _settings == null)
			{
				return;
			}

			var resourceData = frameData.Get<UniversalResourceData>();
			var cameraData = frameData.Get<UniversalCameraData>();
			var isOverlay = cameraData.renderType == CameraRenderType.Overlay;
			var hasTargetTexture = cameraData.camera != null && cameraData.camera.targetTexture != null;

			if (!ShouldInject(cameraData.isGameCamera, isOverlay, hasTargetTexture, UiBackdropBlurPresenterFeature.AnyOpen))
			{
				return;
			}

			// The backbuffer cannot be sampled as a texture, so there is nothing to blur from.
			if (resourceData.isActiveTargetBackBuffer)
			{
				return;
			}

			var source = resourceData.activeColorTexture;
			if (!source.IsValid())
			{
				return;
			}

			var descriptor = renderGraph.GetTextureDesc(source);
			var downsampleFactor = 1 << Mathf.Clamp(_settings.Downsample, 0, 3);
			descriptor.width = Mathf.Max(1, descriptor.width / downsampleFactor);
			descriptor.height = Mathf.Max(1, descriptor.height / downsampleFactor);
			descriptor.name = "_UiBackdropBlurWork";
			descriptor.clearBuffer = false;

			var downsampled = renderGraph.CreateTexture(descriptor);
			renderGraph.AddBlitPass(source, downsampled, Vector2.one, Vector2.zero, passName: "UiBackdropBlur_Downsample");

			var iterations = Mathf.Max(1, _settings.Iterations);
			var current = downsampled;

			for (var i = 0; i < iterations; i++)
			{
				var next = renderGraph.CreateTexture(descriptor);
				// Tinting every iteration would compound the tint multiplicatively.
				var isLastIteration = i == iterations - 1;
				AddBlurIterationPass(renderGraph, current, next, Mathf.Max(0f, _settings.Spread),
					isLastIteration ? _settings.Tint : Color.white);
				current = next;
			}

			renderGraph.AddBlitPass(current, source, Vector2.one, Vector2.zero, passName: "UiBackdropBlur_Composite");
		}

		/// <summary>
		/// Whether the blur should run against a camera with the given traits.
		/// </summary>
		/// <remarks>
		/// Scene and Preview cameras would blur the Editor's own views; a URP overlay camera would blur
		/// the stacked UI rather than the world behind it; a render-texture camera would blur the UI
		/// being captured.
		/// </remarks>
		internal static bool ShouldInject(bool isGameCamera, bool isOverlayCamera, bool hasTargetTexture, bool anyPresenterOpen)
		{
			return isGameCamera && !isOverlayCamera && !hasTargetTexture && anyPresenterOpen;
		}

		private void AddBlurIterationPass(RenderGraph renderGraph, TextureHandle source, TextureHandle destination,
			float spread, Color tint)
		{
			using var builder = renderGraph.AddRasterRenderPass<PassData>("UiBackdropBlur_Iteration", out var passData, profilingSampler);

			passData.material = _material;
			passData.source = source;
			passData.spread = spread;
			passData.tint = tint;

			builder.UseTexture(source, AccessFlags.Read);
			builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

			builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
			{
				_propertyBlock.Clear();
				_propertyBlock.SetTexture(_blitTextureId, data.source);
				_propertyBlock.SetVector(_blitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
				_propertyBlock.SetFloat(_spreadId, data.spread);
				_propertyBlock.SetColor(_tintId, data.tint);

				context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1, _propertyBlock);
			});
		}

		private class PassData
		{
			internal Material material;
			internal TextureHandle source;
			internal float spread;
			internal Color tint;
		}
	}
}
