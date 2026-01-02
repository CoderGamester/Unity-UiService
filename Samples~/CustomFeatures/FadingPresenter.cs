using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
		[SerializeField] private TMP_Text _titleText;
		[SerializeField] private Button _closeButton;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(OnCloseButtonClicked);
			}
			
			Debug.Log("[FadingPresenter] Initialized with FadeFeature");
		}

		private void OnDestroy()
		{
			_closeButton?.onClick.RemoveListener(OnCloseButtonClicked);
			OnCloseRequested.RemoveAllListeners();
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
				_titleText.text = "Fading Presenter";
			}
		}

		protected override void OnOpenTransitionCompleted()
		{
			base.OnOpenTransitionCompleted();
			Debug.Log("[FadingPresenter] Fade in animation completed!");
			// You can enable interactions here if needed
		}
	}
}

