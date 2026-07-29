using GameLovers.UiService.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// A panel whose backdrop is blurred by <c>UiBackdropBlurPresenterFeature</c> on the same prefab, with
	/// buttons that step the blur's iteration count so the effect can be compared live.
	/// </summary>
	/// <remarks>
	/// Nothing here switches the blur on: the feature holds it open for as long as this presenter is visible.
	/// The +/- buttons drive <see cref="UiBackdropBlurRendererFeature.IterationsOverride"/>, which exists so a
	/// runtime tuning UI does not have to write to the renderer asset and persist the change to disk.
	/// </remarks>
	public class BlurredModalPresenter : UiPresenter
	{
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _moreBlurButton;
		[SerializeField] private Button _lessBlurButton;
		[SerializeField] private TMP_Text _body;
		[SerializeField] private TMP_Text _blurStatus;

		private void Awake()
		{
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(() => Close(false));
			}

			if (_moreBlurButton != null)
			{
				_moreBlurButton.onClick.AddListener(() => StepBlur(1));
			}

			if (_lessBlurButton != null)
			{
				_lessBlurButton.onClick.AddListener(() => StepBlur(-1));
			}
		}

		protected override void OnOpened()
		{
			if (_body != null)
			{
				_body.text = "Everything behind this panel is blurred by the URP renderer feature.\n\n" +
					"If it is NOT blurred, UiBackdropBlurRendererFeature is missing from your Renderer " +
					"asset -- check the Console for the error naming the setup step.";
			}

			RefreshStatus();
		}

		private void StepBlur(int delta)
		{
			// Steps from whatever is in effect, so the first press moves off the authored value rather than
			// jumping to the bottom of the range. Clamped here rather than relying on the setter: the setter
			// treats zero as "clear the override", so decrementing into it would snap back to the asset value
			// instead of stopping at the minimum.
			var next = Mathf.Clamp(
				UiBackdropBlurRendererFeature.EffectiveIterations + delta,
				UiBackdropBlurRendererFeature.MinIterations,
				UiBackdropBlurRendererFeature.MaxIterations);

			UiBackdropBlurRendererFeature.IterationsOverride = next;
			RefreshStatus();
		}

		private void RefreshStatus()
		{
			if (_blurStatus == null)
			{
				return;
			}

			var effective = UiBackdropBlurRendererFeature.EffectiveIterations;
			var authored = UiBackdropBlurRendererFeature.AuthoredIterations;
			var suffix = UiBackdropBlurRendererFeature.IterationsOverride > 0 ? string.Empty : " (asset default)";

			_blurStatus.text = $"Blur passes: {effective}{suffix}    " +
				$"range {UiBackdropBlurRendererFeature.MinIterations}-{UiBackdropBlurRendererFeature.MaxIterations}, " +
				$"asset authored {authored}";
		}
	}
}
