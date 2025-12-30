using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using GameLovers.UiService;

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

		[SerializeField] private UiConfigs _uiConfigs;
		
		[Header("UI Buttons")]
		[SerializeField] private Button _loadSetButton;
		[SerializeField] private Button _openSetButton;
		[SerializeField] private Button _closeSetButton;
		[SerializeField] private Button _unloadSetButton;
		[SerializeField] private Button _loadAndOpenButton;
		[SerializeField] private Button _listSetsButton;
		
		private UiService _uiService;

		private void Start()
		{
			// Initialize UI Service
			_uiService = new UiService();
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_loadSetButton?.onClick.AddListener(LoadUiSetWrapper);
			_openSetButton?.onClick.AddListener(OpenUiSetWrapper);
			_closeSetButton?.onClick.AddListener(CloseUiSetExample);
			_unloadSetButton?.onClick.AddListener(UnloadUiSetExample);
			_loadAndOpenButton?.onClick.AddListener(LoadAndOpenUiSetWrapper);
			_listSetsButton?.onClick.AddListener(ListUiSets);
			
			Debug.Log("=== UI Sets Example Started ===");
			Debug.Log("UI Sets allow you to group multiple UIs and manage them together.");
			Debug.Log("");
			Debug.Log("Use the UI buttons to manage UI Sets:");
			Debug.Log("  Load: Load all HUD elements into memory");
			Debug.Log("  Open: Show all HUD elements");
			Debug.Log("  Close: Hide all HUD elements (keep in memory)");
			Debug.Log("  Unload: Destroy all HUD elements");
			Debug.Log("  Load & Open: Combined operation");
			Debug.Log("  List Sets: Show all configured UI Sets");
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
			Debug.Log($"Loading UI Set: GameHud...");
			
			// LoadUiSetAsync returns a list of tasks, one per UI
			var loadTasks = _uiService.LoadUiSetAsync((int)UiSetId.GameHud);
			
			Debug.Log($"  Started loading {loadTasks.Count} UIs...");
			
			// Wait for all UIs to load
			foreach (var task in loadTasks)
			{
				var presenter = await task;
				Debug.Log($"  Loaded: {presenter.GetType().Name}");
			}
			
			Debug.Log("UI Set loaded! UIs are in memory but not visible.");
		}

		/// <summary>
		/// Open all UIs in a set (loads them first if needed)
		/// </summary>
		public async UniTaskVoid OpenUiSetExample()
		{
			Debug.Log($"Opening UI Set: GameHud...");
			
			// First ensure all UIs are loaded
			var loadTasks = _uiService.LoadUiSetAsync((int)UiSetId.GameHud);
			
			// Wait for all to load, then open each one
			foreach (var task in loadTasks)
			{
				var presenter = await task;
				await _uiService.OpenUiAsync(presenter.GetType());
				Debug.Log($"  Opened: {presenter.GetType().Name}");
			}
			
			Debug.Log("UI Set opened! All UIs are now visible.");
		}

		/// <summary>
		/// Close all UIs in a set (keeps them in memory)
		/// </summary>
		public void CloseUiSetExample()
		{
			Debug.Log($"Closing UI Set: GameHud...");
			
			// CloseAllUiSet hides all UIs in the set but keeps them loaded
			_uiService.CloseAllUiSet((int)UiSetId.GameHud);
			
			Debug.Log("UI Set closed! UIs are hidden but still in memory.");
		}

		/// <summary>
		/// Unload all UIs in a set (destroys them)
		/// </summary>
		public void UnloadUiSetExample()
		{
			Debug.Log($"Unloading UI Set: GameHud...");
			
			try
			{
				// UnloadUiSet destroys all UIs in the set
				_uiService.UnloadUiSet((int)UiSetId.GameHud);
				Debug.Log("UI Set unloaded! All UIs have been destroyed.");
			}
			catch (KeyNotFoundException)
			{
				Debug.Log("UI Set not loaded. Load it first!");
			}
		}

		/// <summary>
		/// Combined load and open for convenience
		/// </summary>
		public async UniTaskVoid LoadAndOpenUiSetExample()
		{
			Debug.Log("Loading and opening UI Set: GameHud...");
			
			var loadTasks = _uiService.LoadUiSetAsync((int)UiSetId.GameHud);
			
			// Load and immediately open each UI
			var openTasks = new List<UniTask>();
			foreach (var loadTask in loadTasks)
			{
				openTasks.Add(LoadAndOpenSingle(loadTask));
			}
			
			await UniTask.WhenAll(openTasks);
			
			Debug.Log("UI Set loaded and opened!");
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
	}
}
