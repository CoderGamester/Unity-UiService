using GameLovers.UiService;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example implementation using time-based delays with UI Toolkit.
	/// Demonstrates how to combine TimeDelayFeature and UiToolkitPresenterFeature.
	/// 
	/// Setup:
	/// 1. Attach this component to a GameObject
	/// 2. Add TimeDelayFeature component (configure delay times in inspector)
	/// 3. Add UIDocument component
	/// 4. Create a UXML file with elements: Title (Label), Status (Label), CloseButton (Button)
	/// 5. Assign the UXML to the UIDocument
	/// </summary>
	[RequireComponent(typeof(TimeDelayFeature))]
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	public class TimeDelayedUiToolkitPresenter : UiPresenter
	{
		[SerializeField] private TimeDelayFeature _delayFeature;
		[SerializeField] private UiToolkitPresenterFeature _toolkitFeature;

		private Label _titleLabel;
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
			base.OnInitialized();
			Debug.Log("TimeDelayedUiToolkitPresenter: Initialized");

			// Subscribe to visual tree attachment to safely query elements
			_toolkitFeature.AddVisualTreeAttachedListener(SetupUI);
		}

		private void SetupUI(VisualElement root)
		{
			// Query UI Toolkit elements
			_titleLabel = root.Q<Label>("Title");
			_statusLabel = root.Q<Label>("Message");
			_closeButton = root.Q<Button>("CloseButton");

			// Setup UI elements
			if (_titleLabel != null)
			{
				_titleLabel.text = "Time-Delayed UI Toolkit";
			}

			// Set up event handlers using RegisterCallback for more control
			_closeButton?.RegisterCallback<ClickEvent>(OnCloseButtonClicked);
		}

		/// <inheritdoc />
		protected override void OnOpened()
		{
			base.OnOpened();
			
			if (_statusLabel != null && _delayFeature != null)
			{
				_statusLabel.text = $"Opening with {_delayFeature.OpenDelayInSeconds}s delay...";
			}
		}

		/// <inheritdoc />
		protected override void OnOpenTransitionCompleted()
		{
			base.OnOpenTransitionCompleted();
			
			// Update UI after delay
			if (_statusLabel != null)
			{
				_statusLabel.text = "Ready!";
			}

			if (_titleLabel != null)
			{
				_titleLabel.text = "Time-Delayed UI Toolkit - Ready!";
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
