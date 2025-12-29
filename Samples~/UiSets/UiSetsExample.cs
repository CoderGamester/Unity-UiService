using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating UI Sets - grouping multiple UIs that are loaded/opened/closed together.
	/// Common use case: Game HUD with multiple elements (health bar, currency, minimap, etc.)
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
		
		private UiService _uiService;

		private async void Start()
		{
			// Initialize UI Service
			_uiService = new UiService();
			_uiService.Init(_uiConfigs);
			
			Debug.Log("=== UI Sets Example Started ===");
			Debug.Log("UI Sets allow you to group multiple UIs and manage them together.");
			Debug.Log("");
			Debug.Log("Press 1: Load Game HUD Set (loads all HUD elements)");
			Debug.Log("Press 2: Open Game HUD Set (shows all HUD elements)");
			Debug.Log("Press 3: Close Game HUD Set (hides all HUD elements)");
			Debug.Log("Press 4: Unload Game HUD Set (destroys all HUD elements)");
			Debug.Log("Press 5: Load & Open HUD Set (combined)");
			Debug.Log("");
			Debug.Log("Press L: List all configured UI Sets");
		}

		private void OnDestroy()
		{
			_uiService?.Dispose();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				LoadUiSetExample().Forget();
			}
			else if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				OpenUiSetExample().Forget();
			}
			else if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				CloseUiSetExample();
			}
			else if (Input.GetKeyDown(KeyCode.Alpha4))
			{
				UnloadUiSetExample();
			}
			else if (Input.GetKeyDown(KeyCode.Alpha5))
			{
				LoadAndOpenUiSetExample().Forget();
			}
			else if (Input.GetKeyDown(KeyCode.L))
			{
				ListUiSets();
			}
		}

		/// <summary>
		/// Load all UIs in a set (but don't show them yet)
		/// This is useful for preloading UIs to avoid hitches when opening
		/// </summary>
		private async UniTaskVoid LoadUiSetExample()
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
		private async UniTaskVoid OpenUiSetExample()
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
		private void CloseUiSetExample()
		{
			Debug.Log($"Closing UI Set: GameHud...");
			
			// CloseAllUiSet hides all UIs in the set but keeps them loaded
			_uiService.CloseAllUiSet((int)UiSetId.GameHud);
			
			Debug.Log("UI Set closed! UIs are hidden but still in memory.");
		}

		/// <summary>
		/// Unload all UIs in a set (destroys them)
		/// </summary>
		private void UnloadUiSetExample()
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
		private async UniTaskVoid LoadAndOpenUiSetExample()
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
		private void ListUiSets()
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

