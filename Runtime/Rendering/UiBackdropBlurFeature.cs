using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Enables <see cref="UiBackdropBlurPresenterFeature"/> requests for every camera using the
	/// Universal Renderer asset this is added to.
	/// </summary>
	/// <remarks>
	/// The shader is a serialized reference rather than a <c>Resources.Load</c> or <c>Shader.Find</c>
	/// lookup so it resolves through Graphics Settings as a real build dependency with correctly
	/// compiled variants; the alternatives render pink in a player unless always-included.
	/// </remarks>
	public class UiBackdropBlurFeature : ScriptableRendererFeature
	{
		private const string ShaderPath = "Packages/com.gamelovers.uiservice/Runtime/Rendering/Shaders/UiBackdropBlur.shader";

		[SerializeField] private Shader _blurShader;

		private Material _blurMaterial;
		private UiBackdropBlurPass _pass;

		/// <inheritdoc />
		public override void Create()
		{
			TryLoadShaderInEditor();

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

		/// <inheritdoc />
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (_pass == null || _blurMaterial == null)
			{
				return;
			}

			renderer.EnqueuePass(_pass);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			// The feature asset outlives play mode, so the material leaks without this.
			CoreUtils.Destroy(_blurMaterial);
			_blurMaterial = null;
		}

		private void Reset()
		{
			TryLoadShaderInEditor();
		}

		private void TryLoadShaderInEditor()
		{
#if UNITY_EDITOR
			if (_blurShader == null)
			{
				_blurShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
			}
#endif
		}
	}
}
