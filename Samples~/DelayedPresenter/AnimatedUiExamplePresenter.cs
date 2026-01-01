using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example UI Presenter with animation-based delays using the self-contained AnimationDelayFeature.
	/// Demonstrates how animations automatically control timing via the unified virtual hooks pattern.
	/// No manual event subscription required - just override OnOpenTransitionCompleted/OnCloseTransitionCompleted.
	/// </summary>
	[RequireComponent(typeof(AnimationDelayFeature))]
	public class AnimatedUiExamplePresenter : UiPresenter
	{
		[SerializeField] private AnimationDelayFeature _animationFeature;
		[SerializeField] private TMP_Text _titleText;
		[SerializeField] private TMP_Text _statusText;
		[SerializeField] private Button _closeButton;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log("[AnimatedUiExample] UI Initialized");
			
			// Wire up the close button
			_closeButton.onClick.AddListener(OnCloseButtonClicked);
			_closeButton.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			_closeButton.onClick.RemoveListener(OnCloseButtonClicked);
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
			Debug.Log("[AnimatedUiExample] UI Opened, playing intro animation...");
			
			if (_titleText != null)
			{
				_titleText.text = "Animated UI Example";
			}
			
			if (_statusText != null && _animationFeature != null)
			{
				var duration = _animationFeature.OpenDelayInSeconds;
				_statusText.text = $"Playing intro animation ({duration:F2}s)...";
			}
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			_closeButton.gameObject.SetActive(false);
			Debug.Log("[AnimatedUiExample] UI Closing, playing outro animation...");
			
			if (_statusText != null && _animationFeature != null)
			{
				var duration = _animationFeature.CloseDelayInSeconds;
				_statusText.text = $"Playing outro animation ({duration:F2}s)...";
			}
		}

		/// <summary>
		/// Called automatically when the animation feature completes its open transition.
		/// No manual subscription needed - just override this virtual method.
		/// </summary>
		protected override void OnOpenTransitionCompleted()
		{
			_closeButton.gameObject.SetActive(true);
			Debug.Log("[AnimatedUiExample] Intro animation completed!");
			
			if (_statusText != null)
			{
				_statusText.text = "Animation complete - Ready!";
			}
		}

		/// <summary>
		/// Called automatically when the animation feature completes its close transition.
		/// No manual subscription needed - just override this virtual method.
		/// </summary>
		protected override void OnCloseTransitionCompleted()
		{
			Debug.Log("[AnimatedUiExample] Outro animation completed!");
			
			if (_statusText != null)
			{
				_statusText.text = "Outro animation complete - Closed!";
			}
		}
	}
}
