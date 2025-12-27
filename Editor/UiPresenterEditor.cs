using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using GameLovers.UiService;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// Custom editor for UiPresenter to show quick actions in the inspector
	/// </summary>
	[CustomEditor(typeof(UiPresenter), true)]
	public class UiPresenterEditor : Editor
	{
		private Label _statusLabel;
		private VisualElement _statusIndicator;
		private VisualElement _controlsContainer;

		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			
			// Draw default inspector
			InspectorElement.FillDefaultInspector(root, serializedObject, this);
			
			// Add spacing
			root.Add(CreateSpacer(10));
			
			// Controls section header
			var header = new Label("UI Presenter Controls");
			header.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.style.fontSize = 12;
			root.Add(header);
			
			root.Add(CreateSpacer(5));
			
			// Status display
			var statusRow = CreateStatusDisplay();
			root.Add(statusRow);
			
			root.Add(CreateSpacer(5));
			
			// Controls container (will be populated based on play mode)
			_controlsContainer = new VisualElement();
			root.Add(_controlsContainer);
			
			// Update controls based on play mode
			UpdateControls();
			
			// Schedule periodic updates in play mode
			root.schedule.Execute(() =>
			{
				if (target != null)
				{
					UpdateStatusDisplay();
				}
			}).Every(100);
			
			return root;
		}

		private VisualElement CreateStatusDisplay()
		{
			var container = new VisualElement();
			container.style.flexDirection = FlexDirection.Row;
			container.style.alignItems = Align.Center;
			
			var statusLabel = new Label("Status:");
			statusLabel.style.width = 100;
			container.Add(statusLabel);
			
			// Status indicator circle
			_statusIndicator = new VisualElement();
			_statusIndicator.style.width = 12;
			_statusIndicator.style.height = 12;
			_statusIndicator.style.borderTopLeftRadius = 6;
			_statusIndicator.style.borderTopRightRadius = 6;
			_statusIndicator.style.borderBottomLeftRadius = 6;
			_statusIndicator.style.borderBottomRightRadius = 6;
			_statusIndicator.style.marginRight = 5;
			container.Add(_statusIndicator);
			
			// Status text
			_statusLabel = new Label();
			_statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			container.Add(_statusLabel);
			
			UpdateStatusDisplay();
			
			return container;
		}

		private void UpdateStatusDisplay()
		{
			var presenter = (UiPresenter)target;
			if (presenter == null)
				return;
			
			var isOpen = presenter.IsOpen;
			_statusLabel.text = isOpen ? "OPEN" : "CLOSED";
			_statusIndicator.style.backgroundColor = isOpen ? new Color(0, 1, 0) : new Color(1, 0, 0);
		}

		private void UpdateControls()
		{
			_controlsContainer.Clear();
			
			var presenter = (UiPresenter)target;
			if (presenter == null)
				return;
			
			if (Application.isPlaying)
			{
				CreatePlayModeControls();
			}
			else
			{
				var helpBox = new HelpBox("UI controls are only available in Play Mode", HelpBoxMessageType.Info);
				_controlsContainer.Add(helpBox);
			}
		}

		private void CreatePlayModeControls()
		{
			var presenter = (UiPresenter)target;
			
			_controlsContainer.Add(CreateSpacer(5));
			
			// First row of buttons
			var row1 = new VisualElement();
			row1.style.flexDirection = FlexDirection.Row;
			
			var openButton = new Button(() => presenter.InternalOpen()) { text = "Open UI" };
			openButton.style.flexGrow = 1;
			openButton.style.height = 30;
			row1.Add(openButton);
			
			var closeButton = new Button(() => presenter.InternalClose(false)) { text = "Close UI" };
			closeButton.style.flexGrow = 1;
			closeButton.style.height = 30;
			closeButton.style.marginLeft = 5;
			row1.Add(closeButton);
			
			_controlsContainer.Add(row1);
			_controlsContainer.Add(CreateSpacer(5));
			
			// Destroy button
			var closeDestroyButton = new Button(() =>
			{
				if (EditorUtility.DisplayDialog("Destroy UI", 
					"Are you sure you want to destroy this UI?", "Yes", "Cancel"))
				{
					presenter.InternalClose(true);
				}
			}) { text = "Close & Destroy" };
			closeDestroyButton.style.height = 25;
			
			_controlsContainer.Add(closeDestroyButton);
		}

		private VisualElement CreateSpacer(int height)
		{
			var spacer = new VisualElement();
			spacer.style.height = height;
			return spacer;
		}

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (_controlsContainer != null)
			{
				UpdateControls();
			}
		}
	}
}

