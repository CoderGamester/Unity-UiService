using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using GameLovers.UiService;
using TMPro;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating UI Sets - grouping multiple UIs that are loaded/opened/closed together.
	/// Common use case: Game HUD with multiple elements (health bar, currency, minimap, etc.)
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// </summary>
	public class UiSetsExample : MonoBehaviour
	{
		/// <summary>
		/// Define your UI Set IDs as an enum for type safety
		/// </summary>
		public enum UiSetId
		{
			GameHud = 0,
			PauseMenu = 1,
			SettingsPanel = 2
		}

		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;

		[Header("UI Buttons")]
		[SerializeField] private Button _loadSetButton;
		[SerializeField] private Button _openSetButton;
		[SerializeField] private Button _closeSetButton;
		[SerializeField] private Button _unloadSetButton;
		[SerializeField] private Button _loadAndOpenButton;
		[SerializeField] private Button _listSetsButton;

		[Header("UI Elements")]
		[SerializeField] private TMP_Text _explanationText;
		[SerializeField] private TMP_Text _statusText;
		
		private IUiServiceInit _uiService;

		private async void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_loadSetButton?.onClick.AddListener(LoadUiSetWrapper);
			_openSetButton?.onClick.AddListener(OpenUiSetWrapper);
			_closeSetButton?.onClick.AddListener(CloseUiSetExample);
			_unloadSetButton?.onClick.AddListener(UnloadUiSetExample);
			_loadAndOpenButton?.onClick.AddListener(LoadAndOpenUiSetWrapper);
			_listSetsButton?.onClick.AddListener(ListUiSets);
			
			// Pre-load the set and subscribe to close events for each presenter in it
			var loadTasks = _uiService.LoadUiSetAsync((int)UiSetId.GameHud);
			foreach (var task in loadTasks)
			{
				var presenter = await task;
				presenter.OnCloseRequested.AddListener(() => UpdateUiVisibility(false));
			}

			UpdateUiVisibility(false);
			UpdateStatus("Ready");
		}

		private void OnDestroy()
		{
			_loadSetButton?.onClick.RemoveListener(LoadUiSetWrapper);
			_openSetButton?.onClick.RemoveListener(OpenUiSetWrapper);
			_closeSetButton?.onClick.RemoveListener(CloseUiSetExample);
			_unloadSetButton?.onClick.RemoveListener(UnloadUiSetExample);
			_loadAndOpenButton?.onClick.RemoveListener(LoadAndOpenUiSetWrapper);
			_listSetsButton?.onClick.RemoveListener(ListUiSets);
			
			_uiService?.Dispose();
		}

		private void LoadUiSetWrapper() => LoadUiSetExample().Forget();
		private void OpenUiSetWrapper() => OpenUiSetExample().Forget();
		private void LoadAndOpenUiSetWrapper() => LoadAndOpenUiSetExample().Forget();

		/// <summary>
		/// Load all UIs in a set (but don't show them yet)
		/// This is useful for preloading UIs to avoid hitches when opening
		/// </summary>
		public async UniTaskVoid LoadUiSetExample()
		{
			UpdateStatus($"Loading UI Set: GameHud...");
			
			// LoadUiSetAsync returns a list of tasks, one per UI
			var loadTasks = _uiService.LoadUiSetAsync((int)UiSetId.GameHud);
			
			UpdateStatus($"  Started loading {loadTasks.Count} UIs...");
			
			// Wait for all UIs to load
			foreach (var task in loadTasks)
			{
				var presenter = await task;
				UpdateStatus($"  Loaded: {presenter.GetType().Name}");
			}
			
			UpdateStatus("UI Set loaded! UIs are in memory but not visible.");
		}

		/// <summary>
		/// Open all UIs in a set (loads them first if needed)
		/// </summary>
		public async UniTaskVoid OpenUiSetExample()
		{
			UpdateStatus($"Opening UI Set: GameHud...");
			
			// First ensure all UIs are loaded
			var loadTasks = _uiService.LoadUiSetAsync((int)UiSetId.GameHud);
			
			// Wait for all to load, then open each one
			foreach (var task in loadTasks)
			{
				var presenter = await task;
				await _uiService.OpenUiAsync(presenter.GetType());
				UpdateStatus($"  Opened: {presenter.GetType().Name}");
			}
			
			UpdateUiVisibility(true);
			UpdateStatus("UI Set opened! All UIs are now visible.");
		}

		/// <summary>
		/// Close all UIs in a set (keeps them in memory)
		/// </summary>
		public void CloseUiSetExample()
		{
			UpdateStatus($"Closing UI Set: GameHud...");
			
			// CloseAllUiSet hides all UIs in the set but keeps them loaded
			_uiService.CloseAllUiSet((int)UiSetId.GameHud);
			
			UpdateUiVisibility(false);
			UpdateStatus("UI Set closed! UIs are hidden but still in memory.");
		}

		/// <summary>
		/// Unload all UIs in a set (destroys them)
		/// </summary>
		public void UnloadUiSetExample()
		{
			UpdateStatus($"Unloading UI Set: GameHud...");
			
			try
			{
				// UnloadUiSet destroys all UIs in the set
				_uiService.UnloadUiSet((int)UiSetId.GameHud);
				UpdateUiVisibility(false);
				UpdateStatus("UI Set unloaded! All UIs have been destroyed.");
			}
			catch (KeyNotFoundException)
			{
				UpdateStatus("UI Set not loaded. Load it first!");
			}
		}

		/// <summary>
		/// Combined load and open for convenience
		/// </summary>
		public async UniTaskVoid LoadAndOpenUiSetExample()
		{
			UpdateStatus("Loading and opening UI Set: GameHud...");
			
			var loadTasks = _uiService.LoadUiSetAsync((int)UiSetId.GameHud);
			
			// Load and immediately open each UI
			var openTasks = new List<UniTask>();
			foreach (var loadTask in loadTasks)
			{
				openTasks.Add(LoadAndOpenSingle(loadTask));
			}
			
			await UniTask.WhenAll(openTasks);
			
			UpdateUiVisibility(true);
			UpdateStatus("UI Set loaded and opened!");
		}

		private async UniTask LoadAndOpenSingle(UniTask<UiPresenter> loadTask)
		{
			var presenter = await loadTask;
			await _uiService.OpenUiAsync(presenter.GetType());
		}

		/// <summary>
		/// List all configured UI Sets
		/// </summary>
		public void ListUiSets()
		{
			UpdateStatus("Check console for configured UI Sets list.");
			Debug.Log("=== Configured UI Sets ===");
			
			foreach (var kvp in _uiService.UiSets)
			{
				var setId = kvp.Key;
				var setConfig = kvp.Value;
				
				Debug.Log($"Set {setId}:");
				foreach (var instanceId in setConfig.UiInstanceIds)
				{
					Debug.Log($"  - {instanceId}");
				}
			}
			
			if (_uiService.UiSets.Count == 0)
			{
				Debug.Log("No UI Sets configured. Add sets in your UiConfigs asset.");
			}
		}

		private void UpdateUiVisibility(bool isPresenterActive)
		{
			if (_explanationText != null)
			{
				_explanationText.gameObject.SetActive(!isPresenterActive);
			}
		}

		private void UpdateStatus(string message)
		{
			if (_statusText != null)
			{
				_statusText.text = message;
			}
			Debug.Log(message);
		}
	}
}

