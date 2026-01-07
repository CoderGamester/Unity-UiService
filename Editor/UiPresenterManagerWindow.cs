using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// Unified editor window for managing UI presenters tracked by the UiService.
	/// Provides a single source of truth for loaded and visible presenters.
	/// </summary>
	public class UiPresenterManagerWindow : EditorWindow
	{
		private bool _autoRefresh = true;
		private double _lastRefreshTime;
		private const double RefreshInterval = 0.5; // seconds
		
		private ScrollView _scrollView;
		private Label _statsLabel;

		[MenuItem("Tools/UI Service/Presenter Manager")]
		public static void ShowWindow()
		{
			var window = GetWindow<UiPresenterManagerWindow>("UI Presenter Manager");
			window.minSize = new Vector2(500, 300);
			window.Show();
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
			if (rootVisualElement != null)
			{
				UpdateContent();
			}
		}

		private void CreateGUI()
		{
			var root = rootVisualElement;
			root.Clear();
			
			// Header
			var header = CreateHeader();
			root.Add(header);
			
			// Stats bar
			_statsLabel = new Label();
			_statsLabel.style.paddingLeft = 10;
			_statsLabel.style.paddingTop = 5;
			_statsLabel.style.paddingBottom = 5;
			_statsLabel.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
			_statsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			_statsLabel.enableRichText = true;
			root.Add(_statsLabel);
			
			// Column Headers
			var columnHeaders = CreateColumnHeaders();
			root.Add(columnHeaders);
			
			// Scroll view
			_scrollView = new ScrollView();
			_scrollView.style.flexGrow = 1;
			root.Add(_scrollView);
			
			// Footer
			var footer = CreateFooter();
			root.Add(footer);
			
			// Update content
			UpdateContent();
			
			// Schedule periodic updates
			root.schedule.Execute(() =>
			{
				if (_autoRefresh && Application.isPlaying && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
				{
					_lastRefreshTime = EditorApplication.timeSinceStartup;
					UpdateContent();
				}
			}).Every(100);
		}

		private VisualElement CreateHeader()
		{
			var header = new VisualElement();
			header.style.flexDirection = FlexDirection.Row;
			header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			header.style.paddingTop = 5;
			header.style.paddingBottom = 5;
			header.style.paddingLeft = 5;
			header.style.paddingRight = 5;
			
			var titleLabel = new Label("UI Presenter Manager");
			titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			titleLabel.style.flexGrow = 1;
			header.Add(titleLabel);
			
			var autoRefreshToggle = new Toggle("Auto Refresh") { value = _autoRefresh };
			autoRefreshToggle.RegisterValueChangedCallback(evt => _autoRefresh = evt.newValue);
			autoRefreshToggle.style.width = 110;
			header.Add(autoRefreshToggle);
			
			var refreshButton = new Button(() => UpdateContent()) { text = "Refresh" };
			refreshButton.style.width = 60;
			refreshButton.style.marginLeft = 5;
			header.Add(refreshButton);
			
			return header;
		}

		// Column definitions: (minWidth, flexGrow) - Priority: Type (highest) > Actions > Status/Instance (lowest)
		private static readonly (float minWidth, int flexGrow)[] ColumnDefs = 
		{
			(120, 6), // Type
			(60, 1),  // Status
			(140, 2), // Actions
			(120, 1)  // Instance
		};

		private static void ApplyColumnStyle(VisualElement element, int columnIndex)
		{
			var (minWidth, flexGrow) = ColumnDefs[columnIndex];
			element.style.minWidth = minWidth;
			element.style.flexGrow = flexGrow;
			element.style.flexShrink = 0;
			element.style.flexBasis = minWidth;
		}

		private VisualElement CreateColumnHeaders()
		{
			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
			row.style.paddingTop = 2;
			row.style.paddingBottom = 2;
			row.style.borderBottomWidth = 1;
			row.style.borderBottomColor = Color.black;
			
			row.Add(CreateHeaderLabel("Presenter Type", 0));
			row.Add(CreateHeaderLabel("Status", 1));
			row.Add(CreateHeaderLabel("Actions", 2));
			row.Add(CreateHeaderLabel("Instance", 3));
			
			return row;
		}

		private Label CreateHeaderLabel(string text, int columnIndex)
		{
			var label = new Label(text);
			ApplyColumnStyle(label, columnIndex);
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			label.style.paddingLeft = 10;
			return label;
		}

		private void UpdateContent()
		{
			if (_scrollView == null)
				return;
			
			_scrollView.Clear();
			
			if (!Application.isPlaying)
			{
				var helpBox = new HelpBox("UI Presenter Manager is only available in Play Mode.", HelpBoxMessageType.Info);
				_scrollView.Add(helpBox);
				_statsLabel.text = "Not in Play Mode";
				return;
			}

			var service = GameLovers.UiService.UiService.CurrentService;
			if (service == null)
			{
				var warningBox = new HelpBox("No UiService instance found.", HelpBoxMessageType.Warning);
				_scrollView.Add(warningBox);
				_statsLabel.text = "No Service Found";
				return;
			}

			var loadedPresenters = service.GetLoadedPresenters();
			var visiblePresenters = service.VisiblePresenters;
			
			var loadedCount = loadedPresenters.Count;
			var openCount = visiblePresenters.Count;
			var closedCount = loadedCount - openCount;
			
			_statsLabel.text = $"<b>Total Presenters:</b> {loadedCount} - <color=#88ff88><b>Opened:</b> {openCount}</color> | <color=#ff8888><b>Closed:</b> {closedCount}</color>";

			if (loadedPresenters.Count == 0)
			{
				var infoBox = new HelpBox("No presenters loaded in memory.", HelpBoxMessageType.Info);
				_scrollView.Add(infoBox);
				return;
			}

			int index = 0;
			foreach (var instance in loadedPresenters.OrderBy(i => i.Type.Name))
			{
				var isOpen = visiblePresenters.Any(id => id.PresenterType == instance.Type && id.InstanceAddress == instance.Address);
				_scrollView.Add(CreatePresenterRow(service, instance, isOpen, index++));
			}
		}

		private VisualElement CreatePresenterRow(GameLovers.UiService.UiService service, UiInstance instance, bool isOpen, int index)
		{
			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.style.paddingTop = 4;
			row.style.paddingBottom = 4;
			row.style.borderBottomWidth = 1;
			row.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);
			row.style.alignItems = Align.Center;

			if (index % 2 == 1)
			{
				row.style.backgroundColor = new Color(1, 1, 1, 0.03f);
			}

			// Type (column 0)
			var typeLabel = new Label(instance.Type.Name);
			ApplyColumnStyle(typeLabel, 0);
			typeLabel.style.paddingLeft = 10;
			row.Add(typeLabel);

			// Status (column 1)
			var statusContainer = new VisualElement();
			ApplyColumnStyle(statusContainer, 1);
			statusContainer.style.flexDirection = FlexDirection.Row;
			statusContainer.style.alignItems = Align.Center;
			statusContainer.style.paddingLeft = 10;

			var statusDot = new VisualElement();
			statusDot.style.width = 8;
			statusDot.style.height = 8;
			statusDot.style.borderTopLeftRadius = 4;
			statusDot.style.borderTopRightRadius = 4;
			statusDot.style.borderBottomLeftRadius = 4;
			statusDot.style.borderBottomRightRadius = 4;
			statusDot.style.backgroundColor = isOpen ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
			statusDot.style.marginRight = 6;
			statusContainer.Add(statusDot);

			var statusLabel = new Label(isOpen ? "Opened" : "Closed");
			statusLabel.style.color = isOpen ? Color.green : Color.red;
			statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			statusContainer.Add(statusLabel);
			row.Add(statusContainer);

			// Actions (column 2)
			var actionsContainer = new VisualElement();
			ApplyColumnStyle(actionsContainer, 2);
			actionsContainer.style.flexDirection = FlexDirection.Row;
			actionsContainer.style.paddingLeft = 5;

			if (isOpen)
			{
				var closeButton = new Button(() => service.CloseUi(instance.Type, instance.Address, false)) { text = "CLOSE" };
				closeButton.style.flexGrow = 1;
				closeButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
				closeButton.style.color = Color.white;
				actionsContainer.Add(closeButton);
			}
			else
			{
				var openButton = new Button(() => service.OpenUiAsync(instance.Type, instance.Address).Forget()) { text = "OPEN" };
				openButton.style.flexGrow = 1;
				openButton.style.backgroundColor = new Color(0.2f, 0.5f, 0.2f);
				openButton.style.color = Color.white;
				actionsContainer.Add(openButton);
				
				var unloadButton = new Button(() => {
					if (EditorUtility.DisplayDialog("Unload UI", $"Unload {instance.Type.Name} [{instance.Address}]?", "Yes", "Cancel"))
					{
						service.UnloadUi(instance.Type, instance.Address);
						UpdateContent();
					}
				}) { text = "Unload" };
				unloadButton.style.width = 60;
				unloadButton.style.marginLeft = 5;
				actionsContainer.Add(unloadButton);
			}
			row.Add(actionsContainer);

			// Instance (column 3)
			var instanceLabel = new Label(string.IsNullOrEmpty(instance.Address) ? "(default)" : instance.Address);
			ApplyColumnStyle(instanceLabel, 3);
			instanceLabel.style.paddingLeft = 10;
			instanceLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
			row.Add(instanceLabel);

			return row;
		}

		private VisualElement CreateFooter()
		{
			var footer = new VisualElement();
			footer.style.flexDirection = FlexDirection.Row;
			footer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			footer.style.paddingTop = 5;
			footer.style.paddingBottom = 5;
			footer.style.paddingLeft = 5;
			footer.style.paddingRight = 5;
			footer.style.marginTop = 5;
			
			var closeAllButton = new Button(() => {
				if (EditorUtility.DisplayDialog("Close All", "Close all visible UI presenters?", "Yes", "Cancel"))
				{
					GameLovers.UiService.UiService.CurrentService?.CloseAllUi();
					UpdateContent();
				}
			}) { text = "Close All" };
			closeAllButton.style.width = 100;
			footer.Add(closeAllButton);
			
			var unloadAllButton = new Button(() => {
				var service = GameLovers.UiService.UiService.CurrentService;
				if (service == null) return;
				
				var loaded = service.GetLoadedPresenters();
				if (loaded.Count == 0) return;
				
				if (EditorUtility.DisplayDialog("Unload All", $"Unload all {loaded.Count} presenters?", "Yes", "Cancel"))
				{
					foreach (var instance in loaded.ToList())
					{
						service.UnloadUi(instance.Type, instance.Address);
					}
					UpdateContent();
				}
			}) { text = "Unload All" };
			unloadAllButton.style.width = 100;
			unloadAllButton.style.marginLeft = 5;
			footer.Add(unloadAllButton);
			
			return footer;
		}
	}
}

