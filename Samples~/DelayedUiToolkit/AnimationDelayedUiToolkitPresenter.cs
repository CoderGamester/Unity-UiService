using GameLovers.UiService;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Examples
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
		private Button _closeButton;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		/// <inheritdoc />
		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log("AnimationDelayedUiToolkitPresenter: Initialized");

			_toolkitFeature.AddVisualTreeAttachedListener(SetupUI);
		}

		private void SetupUI(VisualElement root)
		{
			Debug.Log("AnimationDelayedUiToolkitPresenter: Visual tree ready, setting up UI elements");

			// Unregister from old elements (may be stale after close/reopen)
			_closeButton?.UnregisterCallback<ClickEvent>(OnCloseButtonClicked);
			
			// Query fresh elements (UI Toolkit may recreate them on activate)
			_titleLabel = root.Q<Label>("Title");
			_messageLabel = root.Q<Label>("Message");
			_scoreLabel = root.Q<Label>("Score");
			_closeButton = root.Q<Button>("CloseButton");

			// Register callbacks on current elements
			_closeButton?.RegisterCallback<ClickEvent>(OnCloseButtonClicked);

			OnSetData();
		}

		/// <inheritdoc />
		protected override void OnSetData()
		{
			base.OnSetData();

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
		protected override void OnOpenTransitionCompleted()
		{
			base.OnOpenTransitionCompleted();
			
			// Update UI after animation
			if (_messageLabel != null)
			{
				_messageLabel.text = Data.Message + " (Ready)";
			}
		}

		private void OnCloseButtonClicked(ClickEvent evt)
		{
			OnCloseRequested.Invoke();
			Close(destroy: false);
		}

		private void OnDestroy()
		{
			_closeButton?.UnregisterCallback<ClickEvent>(OnCloseButtonClicked);
			OnCloseRequested.RemoveAllListeners();
		}
	}
}
