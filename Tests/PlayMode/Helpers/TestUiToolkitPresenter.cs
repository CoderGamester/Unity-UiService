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

		// Create PanelSettings for test environment (required for panel attachment)
		if (document.panelSettings == null)
		{
			document.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
		}

		ToolkitFeature = GetComponent<UiToolkitPresenterFeature>();
		if (ToolkitFeature == null)
		{
			ToolkitFeature = gameObject.AddComponent<UiToolkitPresenterFeature>();
		}

		// Set document reference via reflection
		var docField = typeof(UiToolkitPresenterFeature).GetField("_document",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		docField?.SetValue(ToolkitFeature, document);
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

