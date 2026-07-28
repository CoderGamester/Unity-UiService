using UnityEngine;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Requests a blurred backdrop behind this presenter while it is open, via
	/// <see cref="UiBackdropBlurFeature"/> on the consumer's active URP Renderer asset.
	/// </summary>
	/// <remarks>
	/// Because every canvas in this package's samples is Screen Space - Overlay (which composites
	/// after all URP rendering), the simplest correct design is also the one this feature
	/// implements: blur the camera's own color target in place, before post-processing. That
	/// blurs everything behind the UI with zero canvas changes -- no backdrop element, no extra
	/// wiring, on any presenter that adds this feature.
	/// </remarks>
	public class UiBackdropBlurPresenterFeature : PresenterFeatureBase
	{
		[SerializeField] private int _downsample = 1;
		[SerializeField] private int _iterations = 2;
		[SerializeField] private float _spread = 1f;
		[SerializeField] private Color _tint = Color.white;
		[SerializeField] private int _sortKey;

		private bool _registered;

		/// <inheritdoc />
		public override void OnPresenterOpening()
		{
			base.OnPresenterOpening();
			Register();
		}

		private void Register()
		{
			if (_registered)
			{
				return;
			}

			var settings = new UiBackdropBlurSettings(_downsample, _iterations, _spread, _tint);
			UiBackdropBlurRequests.Register(this, settings, _sortKey);
			_registered = true;
		}

		private void OnDisable()
		{
			// Same reasoning as UiCameraStackFeature: OnPresenterClosed fires before the close
			// transition finishes and before SetActive(false), so unregistering there would pop
			// the blur off while the presenter is still visually closing. OnDisable fires exactly
			// when SetActive(false) lands, and also on destroy/scene-unload.
			if (!_registered)
			{
				return;
			}

			UiBackdropBlurRequests.Unregister(this);
			_registered = false;
		}

		/// <summary>Test-only configuration hook (package tests have InternalsVisibleTo access).</summary>
		internal void ConfigureForTest(int downsample, int iterations, float spread, Color tint, int sortKey)
		{
			_downsample = downsample;
			_iterations = iterations;
			_spread = spread;
			_tint = tint;
			_sortKey = sortKey;
		}
	}
}
