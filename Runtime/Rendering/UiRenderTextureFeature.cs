using UnityEngine;
using UnityEngine.Events;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Renders a presenter's <b>uGUI <see cref="Canvas"/></b> into a <see cref="RenderTexture"/> instead
	/// of the screen -- for 3D-in-world UI, portals, or minimap-style composition.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>uGUI only, deliberately.</b> There is no UI Toolkit path here. UI Toolkit's own
	/// <c>PanelSettings.renderMode = PanelRenderMode.WorldSpace</c> renders a <c>UIDocument</c> directly
	/// in world space -- positioned per-document via <c>UIDocument.position</c> / <c>pivot</c> /
	/// <c>worldSpaceSize</c> -- with no render texture, no intermediate quad, and no per-presenter
	/// <c>PanelSettings</c> mutation. Use that instead; routing UI Toolkit through a texture here would
	/// be strictly worse. See <c>docs/urp-rendering.md</c>.
	/// </para>
	/// <para>
	/// <b>Screen Space - Overlay cannot render into a RenderTexture.</b> Every sample canvas in this
	/// package ships as Overlay, so this path is net-new: it switches the canvas to
	/// Screen Space - Camera and points a dedicated render <see cref="Camera"/> (a prefab child,
	/// assigned via the inspector) at the texture. Without that camera assigned, this feature logs
	/// an error rather than silently doing nothing.
	/// </para>
	/// <para>
	/// <b>Depth/stencil is not optional.</b> uGUI <c>Mask</c>/<c>RectMask2D</c> needs a stencil buffer;
	/// a texture created without one renders masks as no-ops or garbage. The default depth bits (24)
	/// allocates depth+stencil via the legacy <see cref="RenderTexture"/> constructor.
	/// </para>
	/// <para>
	/// <b>Color space is a known open question.</b> In a Linear project, UI rendered to a texture and
	/// then sampled by a world-space material can double-apply or drop the sRGB transform, depending on
	/// the sampling material's own color space handling. This has not been verified against a real
	/// rendered frame -- treat it as a known gap to check visually before shipping a world-space
	/// consumer of this feature's output, not as settled behaviour.
	/// </para>
	/// </remarks>
	public class UiRenderTextureFeature : PresenterFeatureBase
	{
		[SerializeField] private RenderTexture _presetTexture;
		[SerializeField] private bool _matchScreenSize;
		[SerializeField] private Vector2Int _size = new(1280, 720);
		[SerializeField] private int _depthBits = 24;
		[SerializeField] private Camera _renderCamera;

		private Canvas _canvas;
		private RenderTexture _activeTexture;
		private bool _ownsTexture;
		private bool _resolutionListenerRegistered;

		/// <summary>Invoked whenever the active target texture changes (including the first assignment).</summary>
		public readonly UnityEvent<RenderTexture> TargetChanged = new();

		/// <summary>The texture currently receiving this presenter's UI output, or null if not yet applied.</summary>
		public RenderTexture Target => _activeTexture;

		/// <summary>True if this feature allocated <see cref="Target"/> itself (vs. a pre-authored asset it does not own).</summary>
		public bool OwnsTarget => _ownsTexture;

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);

			_canvas = GetComponent<Canvas>();

			if (_renderCamera == null)
			{
				Debug.LogError($"{nameof(UiRenderTextureFeature)} on '{name}': requires a render Camera to" +
					" be assigned -- Screen Space - Overlay canvases cannot render into a RenderTexture.", this);
			}
			else if (_canvas == null)
			{
				Debug.LogError($"{nameof(UiRenderTextureFeature)} on '{name}': requires a {nameof(Canvas)} on" +
					" the same GameObject. For UI Toolkit presenters use" +
					" PanelSettings.renderMode = PanelRenderMode.WorldSpace instead.", this);
			}
		}

		/// <inheritdoc />
		public override void OnPresenterOpening()
		{
			base.OnPresenterOpening();

			EnsureTexture();
			ApplyTarget();

			if (_matchScreenSize && !_resolutionListenerRegistered)
			{
				UiService.OnResolutionChanged.AddListener(OnResolutionChanged);
				_resolutionListenerRegistered = true;
			}
		}

		private void EnsureTexture()
		{
			if (_presetTexture != null)
			{
				_activeTexture = _presetTexture;
				_ownsTexture = false;
				return;
			}

			if (_activeTexture != null && _ownsTexture)
			{
				return; // already allocated from a previous open
			}

			_activeTexture = CreateTexture(_size);
			_ownsTexture = true;
		}

		private RenderTexture CreateTexture(Vector2Int size)
		{
			var texture = new RenderTexture(Mathf.Max(1, size.x), Mathf.Max(1, size.y), _depthBits, RenderTextureFormat.Default)
			{
				name = name + "_UiRenderTexture"
			};
			texture.Create();
			return texture;
		}

		private void ApplyTarget()
		{
			if (_canvas != null && _renderCamera != null)
			{
				_canvas.renderMode = RenderMode.ScreenSpaceCamera;
				_canvas.worldCamera = _renderCamera;
				_renderCamera.targetTexture = _activeTexture;
			}

			TargetChanged.Invoke(_activeTexture);
		}

		private void OnResolutionChanged(Vector2 previous, Vector2 current)
		{
			if (_presetTexture != null || !_ownsTexture)
			{
				return; // a pre-authored asset's size is the consumer's choice, not ours to change
			}

			ReleaseOwnedTexture();
			_activeTexture = CreateTexture(new Vector2Int(Mathf.RoundToInt(current.x), Mathf.RoundToInt(current.y)));
			_ownsTexture = true;
			ApplyTarget();
		}

		private void ReleaseOwnedTexture()
		{
			// Unclaim the camera's targetTexture regardless of ownership -- whoever wired the
			// camera to a texture is responsible for unwiring it, independent of whether this
			// feature also owns disposal of the texture asset itself. Skipping this for a
			// non-owned (preset) texture would leave the camera pointing at a texture the
			// consumer may then legitimately Release() elsewhere, which logs "Releasing render
			// texture that is set as Camera.targetTexture!".
			if (_renderCamera != null && _activeTexture != null && _renderCamera.targetTexture == _activeTexture)
			{
				_renderCamera.targetTexture = null;
			}

			if (_activeTexture != null && _ownsTexture)
			{
				_activeTexture.Release();
				Object.Destroy(_activeTexture);
			}

			_activeTexture = null;
		}

		private void OnDestroy()
		{
			// UiService never clears its static event's listener list -- this component must
			// unsubscribe itself or leak into a static event that outlives it.
			if (_resolutionListenerRegistered)
			{
				UiService.OnResolutionChanged.RemoveListener(OnResolutionChanged);
				_resolutionListenerRegistered = false;
			}

			ReleaseOwnedTexture();
		}

		/// <summary>Test-only configuration hook (package tests have InternalsVisibleTo access).</summary>
		internal void ConfigureForTest(RenderTexture presetTexture = null,
			bool matchScreenSize = false, Vector2Int? size = null, int depthBits = 24, Camera renderCamera = null)
		{
			_presetTexture = presetTexture;
			_matchScreenSize = matchScreenSize;
			_size = size ?? new Vector2Int(1280, 720);
			_depthBits = depthBits;
			_renderCamera = renderCamera;
		}
	}
}
