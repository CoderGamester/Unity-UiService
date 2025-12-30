using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating basic UI flow: loading, opening, closing, unloading.
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// </summary>
	public class BasicUiFlowExample : MonoBehaviour
	{
		[SerializeField] private UiConfigs _uiConfigs;

		[Header("Sample Prefabs")]
		[SerializeField] private GameObject[] _presenterPrefabs;
		
		[Header("UI Buttons")]
		[SerializeField] private Button _loadButton;
		[SerializeField] private Button _openButton;
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _unloadButton;
		[SerializeField] private Button _loadAndOpenButton;
		
		private IUiServiceInit _uiService;

		private void Start()
		{
			// Initialize UI Service
			var loader = new SampleUiAssetLoader();
			foreach (var prefab in _presenterPrefabs)
			{
				var presenter = prefab.GetComponent<UiPresenter>();
				loader.RegisterPrefab(presenter.GetType().Name, prefab);
			}

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_loadButton?.onClick.AddListener(LoadUi);
			_openButton?.onClick.AddListener(OpenUi);
			_closeButton?.onClick.AddListener(CloseUi);
			_unloadButton?.onClick.AddListener(UnloadUi);
			_loadAndOpenButton?.onClick.AddListener(LoadAndOpenUi);
			
			Debug.Log("=== Basic UI Flow Example Started ===");
			Debug.Log("Use the UI buttons to interact with the example:");
			Debug.Log("  Load: Load UI into memory (not visible)");
			Debug.Log("  Open: Make UI visible");
			Debug.Log("  Close: Hide UI (keep in memory)");
			Debug.Log("  Unload: Destroy UI from memory");
			Debug.Log("  Load & Open: Combined operation");
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
			Debug.Log("Loading BasicUiExamplePresenter...");
			_uiService.LoadUiAsync<BasicUiExamplePresenter>().Forget();
			Debug.Log("UI Loaded (but not visible yet)");
		}

		/// <summary>
		/// Opens (shows) the UI
		/// </summary>
		public void OpenUi()
		{
			Debug.Log("Opening BasicUiExamplePresenter...");
			_uiService.OpenUiAsync<BasicUiExamplePresenter>().Forget();
			Debug.Log("UI Opened and visible");
		}

		/// <summary>
		/// Closes the UI but keeps it in memory
		/// </summary>
		public void CloseUi()
		{
			Debug.Log("Closing BasicUiExamplePresenter (destroy: false)...");
			_uiService.CloseUi<BasicUiExamplePresenter>(destroy: false);
			Debug.Log("UI Closed but still in memory");
		}

		/// <summary>
		/// Unloads (destroys) the UI from memory
		/// </summary>
		public void UnloadUi()
		{
			Debug.Log("Unloading BasicUiExamplePresenter...");
			_uiService.UnloadUi<BasicUiExamplePresenter>();
			Debug.Log("UI Destroyed and removed from memory");
		}

		/// <summary>
		/// Loads and opens the UI in one call
		/// </summary>
		public void LoadAndOpenUi()
		{
			Debug.Log("Loading and Opening BasicUiExamplePresenter (combined)...");
			_uiService.LoadUiAsync<BasicUiExamplePresenter>(openAfter: true).Forget();
			Debug.Log("UI Loaded and Opened in one call");
		}
	}
}
