using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example presenter that uses the FadeFeature.
	/// Shows how to attach a single custom feature.
	/// </summary>
	[RequireComponent(typeof(FadeFeature))]
	public class FadingPresenter : UiPresenter
	{
		[SerializeField] private FadeFeature _fadeFeature;
		[SerializeField] private Text _titleText;
		[SerializeField] private Button _closeButton;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			// Subscribe to feature events if needed
			if (_fadeFeature != null)
			{
				_fadeFeature.OnFadeInComplete += OnFadeInComplete;
			}
			
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(() => Close(destroy: false));
			}
			
			Debug.Log("[FadingPresenter] Initialized with FadeFeature");
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			if (_titleText != null)
			{
				_titleText.text = "Fading Presenter";
			}
		}

		private void OnFadeInComplete()
		{
			Debug.Log("[FadingPresenter] Fade in animation completed!");
			// You can enable interactions here if needed
		}

		private void OnDestroy()
		{
			if (_fadeFeature != null)
			{
				_fadeFeature.OnFadeInComplete -= OnFadeInComplete;
			}
		}
	}
}

