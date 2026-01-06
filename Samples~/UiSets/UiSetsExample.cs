using System.Collections.Generic;
using System.Linq;
using System.Text;
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
	/// 
	/// Note: The <see cref="UiSetId"/> enum is defined in a separate file (UiSetId.cs) so it can be
	/// referenced by both runtime code and editor scripts (for the custom UiConfigs editor).
	/// </summary>
	public class UiSetsExample : MonoBehaviour
	{
		[SerializeField] private UiSetsSampleConfigs _uiConfigs;

		[Header("UI Buttons")]
		[SerializeField] private Button _loadSetButton;
		[SerializeField] private Button _openSetButton;
		[SerializeField] private Button _closeSetButton;
		[SerializeField] private Button _unloadSetButton;
		[SerializeField] private Button _listSetsButton;

		[Header("UI Elements")]
		[SerializeField] private TMP_Text _statusText;
		
		private IUiServiceInit _uiService;

		private void Start()
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
			_listSetsButton?.onClick.AddListener(ListUiSets);

			UpdateStatus("Ready");
		}

		private void OnDestroy()
		{
			_loadSetButton?.onClick.RemoveListener(LoadUiSetWrapper);
			_openSetButton?.onClick.RemoveListener(OpenUiSetWrapper);
			_closeSetButton?.onClick.RemoveListener(CloseUiSetExample);
			_unloadSetButton?.onClick.RemoveListener(UnloadUiSetExample);
			_listSetsButton?.onClick.RemoveListener(ListUiSets);
			
			_uiService?.Dispose();
		}

		/// <summary>
		/// Load all UIs in a set (but don't show them yet)
		/// This is useful for preloading UIs to avoid hitches when opening
		/// </summary>
		public async UniTaskVoid LoadUiSetExample()
		{
			// LoadUiSetAsync returns a list of tasks, one per UI
			var loadTasks = _uiService.LoadUiSetAsync((int)UiSetId.GameHud);
			
			UpdateStatus($"  Started loading {loadTasks.Count} UIs...");
			
			// Wait for all UIs to load
			var presenters = await UniTask.WhenAll(loadTasks);
			
			var names = string.Join(", ", presenters.Select(p => p.GetType().Name));
			UpdateStatus($"UI Set loaded ({names})! UIs are in memory but not visible.");
		}

		/// <summary>
		/// Open all UIs in a set (loads them if not already loaded)
		/// </summary>
		public async UniTaskVoid OpenUiSetExample()
		{
			UpdateStatus($"Opening UI Set: {UiSetId.GameHud}...");

			try
			{
				// OpenUiSetAsync handles loading (if needed) and opening all UIs in the set
				// with proper address handling, ensuring CloseAllUiSet and UnloadUiSet work correctly
				// Returns all opened presenters in parallel
				var presenters = await _uiService.OpenUiSetAsync((int)UiSetId.GameHud);
				
				var names = string.Join(", ", presenters.Select(p => p.GetType().Name));
				UpdateStatus($"UI Set opened ({names})! All UIs are now visible.");
			}
			catch (KeyNotFoundException)
			{
				UpdateStatus("UI Set not configured!");
			}
		}

		/// <summary>
		/// Close all UIs in a set (keeps them in memory)
		/// </summary>
		public void CloseUiSetExample()
		{
			// CloseAllUiSet hides all UIs in the set but keeps them loaded
			_uiService.CloseAllUiSet((int)UiSetId.GameHud);
			
			if (_uiService.VisiblePresenters.Count > 0)
			{
				UpdateStatus("<color=red>UI Set not closed! UIs are still visible.</color>");
			}
			else
			{
				UpdateStatus("UI Set closed! UIs are hidden but still in memory.");
			}
		}

		/// <summary>
		/// Unload all UIs in a set (destroys them)
		/// </summary>
		public void UnloadUiSetExample()
		{
			try
			{
				// UnloadUiSet destroys all UIs in the set
				_uiService.UnloadUiSet((int)UiSetId.GameHud);
				UpdateStatus("UI Set unloaded! All UIs have been destroyed.");
			}
			catch (KeyNotFoundException)
			{
				UpdateStatus("UI Set not loaded. Load it first!");
			}
		}

		/// <summary>
		/// List all configured UI Sets
		/// </summary>
		public void ListUiSets()
		{
			UpdateStatus("Check console for configured UI Sets list.");

			var sb = new StringBuilder();
			sb.AppendLine("=== Configured UI Sets ===");
			
			foreach (var kvp in _uiService.UiSets)
			{
				var setId = kvp.Key;
				var setConfig = kvp.Value;
				
				sb.AppendLine($"Set {setId}:");
				foreach (var instanceId in setConfig.UiInstanceIds)
				{
					sb.AppendLine($"  - {instanceId}");
				}
			}
			
			Debug.Log(sb.ToString());
		}

		private void LoadUiSetWrapper() => LoadUiSetExample().Forget();
		private void OpenUiSetWrapper() => OpenUiSetExample().Forget();

		private void UpdateStatus(string message)
		{
			if (_statusText != null)
			{
				_statusText.text = message;
			}
		}
	}
}

