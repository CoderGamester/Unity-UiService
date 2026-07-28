using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Add this to the project's active Universal Renderer asset (Inspector: the Renderer Features
	/// list) to enable <see cref="UiBackdropBlurPresenterFeature"/> requests. One instance affects
	/// every camera using that renderer; <see cref="UiBackdropBlurInjection.ShouldInject"/> decides
	/// per-camera, per-frame whether this actually runs.
	/// </summary>
	/// <remarks>
	/// Ships its own shader (<c>Shaders/UiBackdropBlur.shader</c>) via a serialized
	/// <see cref="Shader"/> reference rather than <c>Resources.Load</c> (unstrippable, forces the
	/// shader into every build) or <c>Shader.Find</c> (silently renders pink in a build unless the
	/// shader happens to be in Always Included Shaders) -- a direct reference makes it a real,
	/// correctly variant-compiled build dependency through Graphics Settings -> Renderer asset ->
	/// this feature -> the shader. The Editor-only fallback in <see cref="Create"/> auto-loads it
	/// by path so a fresh drag-and-drop isn't required for the common case.
	/// </remarks>
	public class UiBackdropBlurFeature : ScriptableRendererFeature
	{
		private const string ShaderPath = "Packages/com.gamelovers.uiservice/Runtime/Rendering/Shaders/UiBackdropBlur.shader";

		[SerializeField] private Shader _blurShader;

		private Material _blurMaterial;
		private UiBackdropBlurPass _pass;

#if UNITY_EDITOR
		private void Reset()
		{
			if (_blurShader == null)
			{
				_blurShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
			}
		}
#endif

		public override void Create()
		{
#if UNITY_EDITOR
			if (_blurShader == null)
			{
				_blurShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
			}
#endif

			if (_blurShader == null)
			{
				Debug.LogError($"{nameof(UiBackdropBlurFeature)} '{name}': no blur shader assigned." +
					" Assign Shaders/UiBackdropBlur.shader in the Inspector.", this);
				return;
			}

			CoreUtils.Destroy(_blurMaterial);
			_blurMaterial = CoreUtils.CreateEngineMaterial(_blurShader);
			_pass = new UiBackdropBlurPass(_blurMaterial)
			{
				renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
			};
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (_pass == null || _blurMaterial == null)
			{
				return;
			}

			renderer.EnqueuePass(_pass);
		}

		protected override void Dispose(bool disposing)
		{
			CoreUtils.Destroy(_blurMaterial);
			_blurMaterial = null;
		}
	}
}
