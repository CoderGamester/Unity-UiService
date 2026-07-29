using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Renders a blurred backdrop for every camera using the Universal Renderer asset this is added to,
	/// for as long as any <see cref="UiBackdropBlurPresenterFeature"/> presenter is open.
	/// </summary>
	/// <remarks>
	/// The blur's appearance is authored here rather than per presenter: it is one art decision for the
	/// whole project, and it lives on an asset a designer already has to open. The shader is a serialized
	/// reference rather than a <c>Resources.Load</c> or <c>Shader.Find</c> lookup so it resolves through
	/// Graphics Settings as a real build dependency with correctly compiled variants; the alternatives
	/// render pink in a player unless always-included.
	/// </remarks>
	public class UiBackdropBlurRendererFeature : ScriptableRendererFeature
	{
		private const string ShaderPath = "Packages/com.gamelovers.uiservice/Runtime/Rendering/Shaders/UiBackdropBlur.shader";

		private static bool _isInstalled;

		[SerializeField] private Shader _blurShader;
		[SerializeField, Range(0, 3)] private int _downsample = 1;
		[SerializeField, Range(1, 8)] private int _iterations = 2;
		[SerializeField, Min(0f)] private float _spread = 1f;
		[SerializeField] private Color _tint = Color.white;

		private Material _blurMaterial;
		private UiBackdropBlurPass _pass;

		/// <summary>True once an instance exists on a loaded Renderer asset, letting presenters warn when it is absent.</summary>
		internal static bool IsInstalled => _isInstalled;

		internal int Downsample => _downsample;

		internal int Iterations => _iterations;

		internal float Spread => _spread;

		internal Color Tint => _tint;

		/// <inheritdoc />
		public override void Create()
		{
			TryLoadShaderInEditor();

			if (_blurShader == null)
			{
				Debug.LogError($"{nameof(UiBackdropBlurRendererFeature)} '{name}': no blur shader assigned." +
					" Assign Shaders/UiBackdropBlur.shader in the Inspector.", this);
				return;
			}

			CoreUtils.Destroy(_blurMaterial);
			_blurMaterial = CoreUtils.CreateEngineMaterial(_blurShader);
			_pass = new UiBackdropBlurPass(_blurMaterial, this)
			{
				renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
			};
			_isInstalled = true;
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
