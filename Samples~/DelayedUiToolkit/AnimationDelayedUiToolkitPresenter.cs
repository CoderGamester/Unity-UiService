using GameLovers.UiService;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GameLovers.UiServiceExamples
{
	/// <summary>
	/// Data structure for the animated UI Toolkit example
	/// </summary>
	public struct UiToolkitExampleData
	{
		public string Title;
		public string Message;
		public int Score;
	}

	/// <summary>
	/// Example implementation using animation-based delays with UI Toolkit and data.
	/// Demonstrates how to combine AnimationDelayFeature, UiToolkitPresenterFeature, and presenter data.
	/// 
	/// Setup:
	/// 1. Attach this component to a GameObject
	/// 2. Add AnimationDelayFeature component
	/// 3. Add Animation component and assign intro/outro animation clips
	/// 4. Add UIDocument component
	/// 5. Create a UXML file with elements: Title, Message, Score (Labels), CloseButton (Button)
	/// 6. Assign the UXML to the UIDocument
	/// </summary>
	[RequireComponent(typeof(AnimationDelayFeature))]
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	public class AnimationDelayedUiToolkitPresenter : UiPresenter<UiToolkitExampleData>
	{
		[SerializeField] private AnimationDelayFeature _animationFeature;
		[SerializeField] private UiToolkitPresenterFeature _toolkitFeature;

		private Label _titleLabel;
		private Label _messageLabel;
		private Label _scoreLabel;
		private Label _statusLabel;
		private Button _closeButton;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		/// <inheritdoc />
		protected override void OnInitialized()
		{
			Debug.Log("AnimationDelayedUiToolkitPresenter: Initialized");

			// Query UI Toolkit elements
			var root = _toolkitFeature.Root;
			_titleLabel = root.Q<Label>("Title");
			_messageLabel = root.Q<Label>("Message");
			_scoreLabel = root.Q<Label>("Score");
			_statusLabel = root.Q<Label>("Status");
			_closeButton = root.Q<Button>("CloseButton");

			// Setup button event
			if (_closeButton != null)
			{
				_closeButton.clicked += OnCloseButtonClicked;
			}
		}

		/// <inheritdoc />
		protected override void OnSetData()
		{
			Debug.Log($"AnimationDelayedUiToolkitPresenter: Data set - {Data.Title}");

			// Update UI elements with the provided data
			if (_titleLabel != null)
			{
				_titleLabel.text = Data.Title;
			}

			if (_messageLabel != null)
			{
				_messageLabel.text = Data.Message;
			}

			if (_scoreLabel != null)
			{
				_scoreLabel.text = $"Score: {Data.Score}";
			}
		}

		/// <inheritdoc />
		protected override void OnOpened()
		{
			Debug.Log("AnimationDelayedUiToolkitPresenter: Opened, playing intro animation...");
			
			if (_statusLabel != null && _animationFeature != null)
			{
				var duration = _animationFeature.OpenDelayInSeconds;
				_statusLabel.text = $"Playing animation ({duration:F2}s)...";
			}
		}

		/// <inheritdoc />
		protected override void OnOpenTransitionCompleted()
		{
			Debug.Log("AnimationDelayedUiToolkitPresenter: Opening animation completed!");
			
			// Update UI after animation
			if (_statusLabel != null)
			{
				_statusLabel.text = "Animation Complete!";
			}

			if (_messageLabel != null)
			{
				_messageLabel.text = Data.Message + " (Ready)";
			}
		}

		private void OnCloseButtonClicked()
		{
			Debug.Log("Close button clicked, closing UI with animation...");
			OnCloseRequested.Invoke();
			Close(false);
		}

		private void OnDestroy()
		{
			if (_closeButton != null)
			{
				_closeButton.clicked -= OnCloseButtonClicked;
			}
			OnCloseRequested.RemoveAllListeners();
		}
	}
}
