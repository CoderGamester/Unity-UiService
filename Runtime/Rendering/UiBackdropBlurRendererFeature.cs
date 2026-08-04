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

		/// <summary>Inclusive bounds for <see cref="IterationsOverride"/>, matching the authored field's range.</summary>
		public const int MinIterations = 1;

		public const int MaxIterations = 8;

		private static bool _isInstalled;
		private static int _iterationsOverride;
		private static int _authoredIterations = 2;

		[SerializeField] private Shader _blurShader;
		[SerializeField, Range(0, 3)] private int _downsample = 1;
		[SerializeField, Range(1, 8)] private int _iterations = 2;
		[SerializeField, Min(0f)] private float _spread = 1f;
		[SerializeField] private Color _tint = Color.white;

		private Material _blurMaterial;
		private UiBackdropBlurPass _pass;

		/// <summary>True once an instance exists on a loaded Renderer asset, letting presenters warn when it is absent.</summary>
		internal static bool IsInstalled => _isInstalled;

		/// <summary>
		/// Overrides the authored iteration count at runtime, for quality tiers, accessibility settings, or a
		/// tuning UI. Zero or less clears the override. Clamped to [<see cref="MinIterations"/>,
		/// <see cref="MaxIterations"/>].
		/// </summary>
		/// <remarks>
		/// Deliberately not a write to the serialized field: this feature is a <c>ScriptableObject</c> asset,
		/// so assigning to it at runtime would persist to disk in the Editor and change the authored look for
		/// the whole project. Static because the asset instance is owned by the Renderer and has no reference
		/// path back from game code.
		/// </remarks>
		public static int IterationsOverride
		{
			get => _iterationsOverride;
			set => _iterationsOverride = value <= 0 ? 0 : Mathf.Clamp(value, MinIterations, MaxIterations);
		}

		/// <summary>The iteration count authored on the asset, readable so a tuning UI can step from it.</summary>
		public static int AuthoredIterations => _authoredIterations;

		/// <summary>The iteration count currently in effect, override included.</summary>
		public static int EffectiveIterations => _iterationsOverride > 0 ? _iterationsOverride : _authoredIterations;

		/// <summary>Authored render-target downsample factor; higher is cheaper and blurrier.</summary>
		internal int Downsample => _downsample;

		/// <summary>Blur pass count actually used this frame, the runtime override taking precedence.</summary>
		internal int Iterations => _iterationsOverride > 0 ? _iterationsOverride : _iterations;

		/// <summary>Authored sample offset per blur pass, in pixels.</summary>
		internal float Spread => _spread;

		/// <summary>Authored colour multiplied over the blurred result.</summary>
		internal Color Tint => _tint;

		/// <summary>Exposes the created blur material for tests; production code has no need to reach it.</summary>
		internal Material BlurMaterialInternal => _blurMaterial;

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
			_authoredIterations = _iterations;
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

		// Statics survive between play sessions when Domain Reload is disabled.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			_iterationsOverride = 0;
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
