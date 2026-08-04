using UnityEngine;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Holds a blurred backdrop open behind this presenter while it is visible. The blur's appearance is
	/// configured on the <see cref="UiBackdropBlurRendererFeature"/> asset, not per presenter.
	/// </summary>
	public class UiBackdropBlurPresenterFeature : PresenterFeatureBase
	{
		private static int _openCount;

		private bool _counted;

		/// <summary>True while at least one presenter is holding a blurred backdrop open.</summary>
		internal static bool AnyOpen => _openCount > 0;

		private void OnDisable()
		{
			// Not OnPresenterClosed: that fires before the close transition finishes and before
			// SetActive(false), which would pop the blur off while the presenter is still visible.
			if (!_counted)
			{
				return;
			}

			_openCount--;
			_counted = false;
		}

		/// <inheritdoc />
		public override void OnPresenterOpening()
		{
			base.OnPresenterOpening();

			if (_counted)
			{
				return;
			}

			if (!UiBackdropBlurRendererFeature.IsInstalled)
			{
				Debug.LogError($"{nameof(UiBackdropBlurPresenterFeature)} on '{name}': no" +
					$" {nameof(UiBackdropBlurRendererFeature)} was found on the active URP Renderer asset," +
					" so nothing will render the blur. Select your Universal Renderer asset (not the" +
					" Pipeline asset) and use Add Renderer Feature.", this);
			}

			_openCount++;
			_counted = true;
		}

		// Statics survive between play sessions when Domain Reload is disabled.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			_openCount = 0;
		}
	}
}
