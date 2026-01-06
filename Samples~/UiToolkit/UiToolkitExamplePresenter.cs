using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example UI Presenter using Unity's UI Toolkit with the self-contained UiToolkitPresenterFeature.
	/// Demonstrates how to work with UI Toolkit elements using the feature composition pattern.
	/// </summary>
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	public class UiToolkitExamplePresenter : UiPresenter
	{
		[SerializeField] private UiToolkitPresenterFeature _toolkitFeature;

		private Button _incrementButton;
		private Button _closeButton;
		private Label _counterLabel;
		
		private int _counter = 0;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log("[UiToolkitExample] UI Initialized");
			
			_toolkitFeature.AddVisualTreeAttachedListener(SetupUI);
		}

		private void SetupUI(VisualElement root)
		{
			Debug.Log("[UiToolkitExample] Visual tree ready, setting up UI elements");
			
			// Unregister from old elements (may be stale after close/reopen)
			_incrementButton?.UnregisterCallback<ClickEvent>(OnIncrementClicked);
			_closeButton?.UnregisterCallback<ClickEvent>(OnCloseButtonClicked);
			
			// Query fresh elements (UI Toolkit may recreate them on activate)
			_incrementButton = root.Q<Button>("IncrementButton");
			_closeButton = root.Q<Button>("CloseButton");
			_counterLabel = root.Q<Label>("CounterLabel");
			
			// Register callbacks on current elements
			_incrementButton?.RegisterCallback<ClickEvent>(OnIncrementClicked);
			_closeButton?.RegisterCallback<ClickEvent>(OnCloseButtonClicked);

			UpdateCounter();
		}

		private void OnCloseButtonClicked(ClickEvent evt)
		{
			OnCloseRequested.Invoke();
			Close(destroy: false);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			Debug.Log("[UiToolkitExample] UI Opened");
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			Debug.Log("[UiToolkitExample] UI Closed");
		}

		private void OnIncrementClicked(ClickEvent evt)
		{
			_counter++;
			UpdateCounter();
			Debug.Log($"[UiToolkitExample] Counter incremented to {_counter}");
		}

		private void UpdateCounter()
		{
			if (_counterLabel != null)
			{
				_counterLabel.text = $"Count: {_counter}";
			}
		}

		private void OnDestroy()
		{
			// Clean up event handlers
			_incrementButton?.UnregisterCallback<ClickEvent>(OnIncrementClicked);
			_closeButton?.UnregisterCallback<ClickEvent>(OnCloseButtonClicked);

			OnCloseRequested.RemoveAllListeners();
		}
	}
}
