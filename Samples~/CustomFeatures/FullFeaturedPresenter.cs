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
		[SerializeField] private Button _closeButton;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		private void OnDestroy()
		{
			_closeButton?.onClick.RemoveListener(OnCloseButtonClicked);
			OnCloseRequested.RemoveAllListeners();
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();

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
	}
}

