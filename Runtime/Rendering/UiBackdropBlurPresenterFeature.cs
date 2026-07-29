using UnityEngine;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Requests a blurred backdrop behind this presenter while it is open, served by a
	/// <see cref="UiBackdropBlurFeature"/> on the project's active URP Renderer asset.
	/// </summary>
	public class UiBackdropBlurPresenterFeature : PresenterFeatureBase
	{
		[SerializeField] private int _downsample = 1;
		[SerializeField] private int _iterations = 2;
		[SerializeField] private float _spread = 1f;
		[SerializeField] private Color _tint = Color.white;
		[SerializeField] private int _sortKey;

		private bool _registered;

		private void OnDisable()
		{
			// Not OnPresenterClosed: that fires before the close transition finishes and before
			// SetActive(false), which would pop the blur off while the presenter is still visible.
			if (!_registered)
			{
				return;
			}

			UiBackdropBlurRequests.Unregister(this);
			_registered = false;
		}

		/// <inheritdoc />
		public override void OnPresenterOpening()
		{
			base.OnPresenterOpening();
			Register();
		}

		internal void ConfigureForTest(int downsample, int iterations, float spread, Color tint, int sortKey)
		{
			_downsample = downsample;
			_iterations = iterations;
			_spread = spread;
			_tint = tint;
			_sortKey = sortKey;
		}

		private void Register()
		{
			if (_registered)
			{
				return;
			}

			UiBackdropBlurRequests.Register(this, new UiBackdropBlurSettings(_downsample, _iterations, _spread, _tint), _sortKey);
			_registered = true;
		}
	}
}
