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

		private Label _titleLabel;
		private Label _descriptionLabel;
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
			
			// Get UI elements from the VisualElement root
			var root = _toolkitFeature.Root;
			_titleLabel = root.Q<Label>("TitleLabel");
			_descriptionLabel = root.Q<Label>("DescriptionLabel");
			_incrementButton = root.Q<Button>("IncrementButton");
			_closeButton = root.Q<Button>("CloseButton");
			_counterLabel = root.Q<Label>("CounterLabel");
			
			// Set up event handlers
			if (_incrementButton != null)
			{
				_incrementButton.clicked += OnIncrementClicked;
			}
			
			if (_closeButton != null)
			{
				_closeButton.clicked += OnCloseButtonClicked;
			}
		}

		private void OnCloseButtonClicked()
		{
			OnCloseRequested.Invoke();
			Close(destroy: false);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			Debug.Log("[UiToolkitExample] UI Opened");
			
			if (_titleLabel != null)
			{
				_titleLabel.text = "UI Toolkit Example";
			}
			
			if (_descriptionLabel != null)
			{
				_descriptionLabel.text = "This UI uses Unity's UI Toolkit with feature composition";
			}
			
			UpdateCounter();
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			Debug.Log("[UiToolkitExample] UI Closed");
		}

		private void OnIncrementClicked()
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
			if (_incrementButton != null)
			{
				_incrementButton.clicked -= OnIncrementClicked;
			}

			if (_closeButton != null)
			{
				_closeButton.clicked -= OnCloseButtonClicked;
			}

			OnCloseRequested.RemoveAllListeners();
		}
	}
}
