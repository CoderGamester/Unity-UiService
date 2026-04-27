using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Test presenter with UiToolkitPresenterFeature.
	/// Demonstrates the correct pattern for handling UI Toolkit element recreation on reopen.
	/// </summary>
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	[RequireComponent(typeof(UIDocument))]
	public class TestUiToolkitPresenter : UiPresenter
	{
		public UiToolkitPresenterFeature ToolkitFeature { get; private set; }
		public bool WasOpened { get; private set; }
		public int SetupCallbackCount { get; private set; }

		private Button _testButton;

	private void Awake()
	{
		// Ensure UIDocument exists
		var document = GetComponent<UIDocument>();
		if (document == null)
		{
			document = gameObject.AddComponent<UIDocument>();
		}

		// Create PanelSettings for test environment (required for panel attachment).
		// Assign an empty ThemeStyleSheet *before* setting panelSettings on the document
		// to suppress Unity's "No Theme Style Sheet set to PanelSettings" warning, which
		// would otherwise be logged on assignment and again on SetActive(true).
		if (document.panelSettings == null)
		{
			var panel = ScriptableObject.CreateInstance<PanelSettings>();
			panel.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
			document.panelSettings = panel;
		}

		ToolkitFeature = GetComponent<UiToolkitPresenterFeature>();
		if (ToolkitFeature == null)
		{
			ToolkitFeature = gameObject.AddComponent<UiToolkitPresenterFeature>();
		}

		ToolkitFeature.SetDocument(document);
	}

		protected override void OnInitialized()
		{
			// Register the callback to be invoked on each open
			ToolkitFeature.AddVisualTreeAttachedListener(SetupUI);
		}

		private void SetupUI(VisualElement root)
		{
			SetupCallbackCount++;

			// Correct pattern: unregister from old, query fresh, register on new
			_testButton?.UnregisterCallback<ClickEvent>(OnButtonClicked);
			_testButton = root?.Q<Button>("TestButton");
			_testButton?.RegisterCallback<ClickEvent>(OnButtonClicked);
		}

		private void OnButtonClicked(ClickEvent evt)
		{
			// Test click handler
		}

		/// <summary>
		/// Removes the setup listener for testing RemoveVisualTreeAttachedListener.
		/// </summary>
		public void RemoveSetupListener()
		{
			ToolkitFeature.RemoveVisualTreeAttachedListener(SetupUI);
		}

		protected override void OnOpened()
		{
			WasOpened = true;
		}

		private void OnDestroy()
		{
			_testButton?.UnregisterCallback<ClickEvent>(OnButtonClicked);
		}
	}
}

