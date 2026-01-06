using UnityEngine;
using UnityEngine.UIElements;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating UI Toolkit integration with UiService.
	/// Uses UI Toolkit for both the example controls and the presenter.
	/// </summary>
	public class UiToolkitExample : MonoBehaviour
	{
		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;
		[SerializeField] private UIDocument _uiDocument;

		private IUiServiceInit _uiService;
		private Label _statusLabel;

		private async void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners from UIDocument
			var root = _uiDocument.rootVisualElement;
			
			_statusLabel = root.Q<Label>("status-label");
			
			root.Q<Button>("load-button")?.RegisterCallback<ClickEvent>(_ => LoadUiToolkit());
			root.Q<Button>("open-button")?.RegisterCallback<ClickEvent>(_ => OpenUiToolkit());
			root.Q<Button>("unload-button")?.RegisterCallback<ClickEvent>(_ => UnloadUiToolkit());
			
			// Pre-load the presenter and subscribe to close events
			var presenter = await _uiService.LoadUiAsync<UiToolkitExamplePresenter>();
			presenter.OnCloseRequested.AddListener(() => UpdateStatus("UI Closed but still in memory"));

			UpdateStatus("Ready");
		}

		private void OnDestroy()
		{
		}

		/// <summary>
		/// Loads the UI Toolkit presenter into memory without showing it
		/// </summary>
		public void LoadUiToolkit()
		{
			_uiService.LoadUiAsync<UiToolkitExamplePresenter>().Forget();
			UpdateStatus("UI Loaded (but not visible yet)");
		}

		/// <summary>
		/// Opens (shows) the UI Toolkit presenter
		/// </summary>
		public async void OpenUiToolkit()
		{
			await _uiService.OpenUiAsync<UiToolkitExamplePresenter>();
			UpdateStatus("UI Opened");
		}

		/// <summary>
		/// Unloads (destroys) the UI Toolkit presenter from memory
		/// </summary>
		public void UnloadUiToolkit()
		{
			_uiService.UnloadUi<UiToolkitExamplePresenter>();
			UpdateStatus("UI Destroyed and removed from memory");
		}

		private void UpdateStatus(string message)
		{
			if (_statusLabel != null)
			{
				_statusLabel.text = message;
			}
		}
	}
}

