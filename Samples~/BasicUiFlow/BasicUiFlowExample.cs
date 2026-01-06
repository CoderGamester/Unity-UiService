using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;
using TMPro;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating basic UI flow: loading, opening, closing, unloading.
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// </summary>
	public class BasicUiFlowExample : MonoBehaviour
	{
		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;

		[Header("UI Buttons")]
		[SerializeField] private Button _loadButton;
		[SerializeField] private Button _openButton;
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _unloadButton;
		[SerializeField] private Button _loadAndOpenButton;

		[Header("UI Elements")]
		[SerializeField] private TMP_Text _statusText;
		
		private IUiServiceInit _uiService;

		private async void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_loadButton?.onClick.AddListener(LoadUi);
			_openButton?.onClick.AddListener(OpenUi);
			_closeButton?.onClick.AddListener(CloseUi);
			_unloadButton?.onClick.AddListener(UnloadUi);
			_loadAndOpenButton?.onClick.AddListener(LoadAndOpenUi);
			
			// Pre-load the presenter and subscribe to close events
			var presenter = await _uiService.LoadUiAsync<BasicUiExamplePresenter>();
			presenter.OnCloseRequested.AddListener(() => UpdateStatus("UI Closed but still in memory"));

			UpdateStatus("Ready");
		}

		private void OnDestroy()
		{
			_loadButton?.onClick.RemoveListener(LoadUi);
			_openButton?.onClick.RemoveListener(OpenUi);
			_closeButton?.onClick.RemoveListener(CloseUi);
			_unloadButton?.onClick.RemoveListener(UnloadUi);
			_loadAndOpenButton?.onClick.RemoveListener(LoadAndOpenUi);
		}

		/// <summary>
		/// Loads the UI into memory without showing it
		/// </summary>
		public void LoadUi()
		{
			_uiService.LoadUiAsync<BasicUiExamplePresenter>().Forget();
			UpdateStatus("UI Loaded (but not visible yet)");
		}

		/// <summary>
		/// Opens (shows) the UI
		/// </summary>
		public async void OpenUi()
		{
			await _uiService.OpenUiAsync<BasicUiExamplePresenter>();
			UpdateStatus("Ui Opened");
		}

		/// <summary>
		/// Closes the UI but keeps it in memory
		/// </summary>
		public void CloseUi()
		{
			_uiService.CloseUi<BasicUiExamplePresenter>(destroy: false);
			UpdateStatus("UI Closed but still in memory");
		}

		/// <summary>
		/// Unloads (destroys) the UI from memory
		/// </summary>
		public void UnloadUi()
		{
			_uiService.UnloadUi<BasicUiExamplePresenter>();
			UpdateStatus("UI Destroyed and removed from memory");
		}

		/// <summary>
		/// Loads and opens the UI in one call
		/// </summary>
		public async void LoadAndOpenUi()
		{
			await _uiService.LoadUiAsync<BasicUiExamplePresenter>(openAfter: true);
			UpdateStatus("Ui Loaded and Opened");
		}

		private void UpdateStatus(string message)
		{
			if (_statusText != null)
			{
				_statusText.text = message;
			}
		}
	}
}

