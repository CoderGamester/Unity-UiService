using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Downsamples the active camera color, applies N iterations of a box blur, then blits the
	/// result back over the full-resolution color target -- blurring everything behind whatever
	/// UI composites after this pass (every sample canvas in this package is Screen Space -
	/// Overlay, which composites after all URP rendering, so this needs zero canvas changes to
	/// produce a blurred backdrop).
	/// </summary>
	internal class UiBackdropBlurPass : ScriptableRenderPass
	{
		// _BlitTexture / _BlitScaleBias match Core RP's own Blit.hlsl conventions (see
		// Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl); the cached
		// UnityEngine.Rendering.Universal.ShaderPropertyId equivalents are internal to URP's own
		// assembly and not visible from here, so these are defined locally.
		private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
		private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
		private static readonly int SpreadId = Shader.PropertyToID("_Spread");
		private static readonly int TintId = Shader.PropertyToID("_Tint");

		private static readonly MaterialPropertyBlock s_PropertyBlock = new();

		private readonly Material _material;

		public UiBackdropBlurPass(Material material)
		{
			_material = material;
			profilingSampler = new ProfilingSampler(nameof(UiBackdropBlurPass));
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			if (_material == null)
			{
				return;
			}

			var resourceData = frameData.Get<UniversalResourceData>();
			var cameraData = frameData.Get<UniversalCameraData>();

			var isOverlay = cameraData.renderType == CameraRenderType.Overlay;
			var hasTargetTexture = cameraData.camera != null && cameraData.camera.targetTexture != null;

			if (!UiBackdropBlurInjection.ShouldInject(cameraData.isGameCamera, isOverlay, hasTargetTexture, UiBackdropBlurRequests.ActiveCount > 0))
			{
				return;
			}

			if (!UiBackdropBlurRequests.TryGetActive(out var settings))
			{
				return;
			}

			// The backbuffer cannot be sampled as a texture -- there is nothing to blur into it from.
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
			var downsampleFactor = 1 << settings.Downsample;
			descriptor.width = Mathf.Max(1, descriptor.width / downsampleFactor);
			descriptor.height = Mathf.Max(1, descriptor.height / downsampleFactor);
			descriptor.name = "_UiBackdropBlurWork";
			descriptor.clearBuffer = false;

			var downsampled = renderGraph.CreateTexture(descriptor);
			renderGraph.AddBlitPass(source, downsampled, Vector2.one, Vector2.zero, passName: "UiBackdropBlur_Downsample");

			var current = downsampled;
			for (var i = 0; i < settings.Iterations; i++)
			{
				var next = renderGraph.CreateTexture(descriptor);
				// Tint only on the final iteration -- the same pass runs Iterations times, so
				// applying it every time would compound it multiplicatively (invisible at the
				// default white tint, but wrong the moment a consumer sets a real one).
				var isLastIteration = i == settings.Iterations - 1;
				var tint = isLastIteration ? settings.Tint : Color.white;
				AddBlurIterationPass(renderGraph, current, next, settings.Spread, tint);
				current = next;
			}

			renderGraph.AddBlitPass(current, source, Vector2.one, Vector2.zero, passName: "UiBackdropBlur_Composite");
		}

		private void AddBlurIterationPass(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, float spread, Color tint)
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
				s_PropertyBlock.Clear();
				s_PropertyBlock.SetTexture(BlitTextureId, data.source);
				s_PropertyBlock.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
				s_PropertyBlock.SetFloat(SpreadId, data.spread);
				s_PropertyBlock.SetColor(TintId, data.tint);

				context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1, s_PropertyBlock);
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
