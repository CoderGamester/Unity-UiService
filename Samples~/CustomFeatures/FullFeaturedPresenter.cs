using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example presenter that combines multiple custom features.
	/// Demonstrates feature composition - mixing and matching features freely.
	/// 
	/// This presenter has:
	/// - FadeFeature: For fade in/out effects
	/// - ScaleFeature: For scale in/out effects
	/// - SoundFeature: For open/close sounds
	/// 
	/// All features work together automatically!
	/// </summary>
	[RequireComponent(typeof(FadeFeature))]
	[RequireComponent(typeof(ScaleFeature))]
	[RequireComponent(typeof(SoundFeature))]
	public class FullFeaturedPresenter : UiPresenter
	{
		[Header("Features")]
		[SerializeField] private FadeFeature _fadeFeature;
		[SerializeField] private ScaleFeature _scaleFeature;
		[SerializeField] private SoundFeature _soundFeature;
		
		[Header("UI Elements")]
		[SerializeField] private TMP_Text _titleText;
		[SerializeField] private TMP_Text _descriptionText;
		[SerializeField] private Button _closeButton;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			// Subscribe to feature completion events
			if (_fadeFeature != null)
			{
				_fadeFeature.OnFadeInComplete += OnAllAnimationsComplete;
			}
			
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(OnCloseButtonClicked);
			}
			
			Debug.Log("[FullFeaturedPresenter] Initialized with 3 features: Fade + Scale + Sound");
		}

		private void OnCloseButtonClicked()
		{
			OnCloseRequested.Invoke();
			Close(destroy: false);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			if (_titleText != null)
			{
				_titleText.text = "Full Featured Presenter";
			}
			
			if (_descriptionText != null)
			{
				_descriptionText.text = "This presenter combines:\n" +
					"• FadeFeature (fade in/out)\n" +
					"• ScaleFeature (scale in/out)\n" +
					"• SoundFeature (open/close sounds)\n\n" +
					"All features work together automatically!";
			}
		}

		private void OnAllAnimationsComplete()
		{
			Debug.Log("[FullFeaturedPresenter] All entrance animations completed!");
			// Good place to enable interaction or trigger other logic
		}

		private void OnDestroy()
		{
			if (_fadeFeature != null)
			{
				_fadeFeature.OnFadeInComplete -= OnAllAnimationsComplete;
			}

			_closeButton?.onClick.RemoveListener(OnCloseButtonClicked);
			OnCloseRequested.RemoveAllListeners();
		}
	}
}

