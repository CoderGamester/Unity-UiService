using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using GameLovers.UiService;
using TMPro;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating multi-instance UI support.
	/// Shows how to create multiple instances of the same UI type (e.g., multiple popups, notifications).
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// 
	/// Key concepts:
	/// - UiInstanceId: Combines Type + InstanceAddress for unique identification
	/// - Instance address: A string that distinguishes instances of the same type
	/// - Default instance: When instanceAddress is null/empty, it's the "singleton" instance
	/// </summary>
	public class MultiInstanceExample : MonoBehaviour
	{
		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;

		[Header("UI Buttons")]
		[SerializeField] private Button _spawnPopupButton;
		[SerializeField] private Button _closeRecentButton;
		[SerializeField] private Button _closeAllButton;
		[SerializeField] private Button _listActiveButton;

		[Header("UI Elements")]
		[SerializeField] private TMP_Text _explanationText;
		[SerializeField] private TMP_Text _statusText;
		
		private UiService _uiService;
		private int _popupCounter = 0;
		private readonly List<string> _activePopupIds = new List<string>();

		private void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_spawnPopupButton?.onClick.AddListener(SpawnNewPopupWrapper);
			_closeRecentButton?.onClick.AddListener(CloseRecentPopup);
			_closeAllButton?.onClick.AddListener(CloseAllPopups);
			_listActiveButton?.onClick.AddListener(ListActivePopups);
			
			UpdateUiVisibility(false);
			UpdateStatus("Ready");
		}

		private void OnDestroy()
		{
			_spawnPopupButton?.onClick.RemoveListener(SpawnNewPopupWrapper);
			_closeRecentButton?.onClick.RemoveListener(CloseRecentPopup);
			_closeAllButton?.onClick.RemoveListener(CloseAllPopups);
			_listActiveButton?.onClick.RemoveListener(ListActivePopups);
			
			_uiService?.Dispose();
		}

		/// <summary>
		/// Wrapper to call async spawn method from button
		/// </summary>
		private void SpawnNewPopupWrapper()
		{
			SpawnNewPopup().Forget();
		}

		/// <summary>
		/// Spawn a new popup with a unique instance address
		/// </summary>
		public async UniTaskVoid SpawnNewPopup()
		{
			_popupCounter++;
			var instanceAddress = $"popup_{_popupCounter}";
			
			UpdateStatus($"Spawning popup with instance address: '{instanceAddress}'");
			
			// Load with a specific instance address
			// This allows multiple instances of the same UI type
			var presenter = await _uiService.LoadUiAsync(
				typeof(NotificationPopupPresenter), 
				instanceAddress, 
				openAfter: false
			);
			
			// Subscribe to close events
			presenter.OnCloseRequested.AddListener(() => OnPopupClosed(instanceAddress));
			
			// Set data for this specific popup
			var popup = presenter as NotificationPopupPresenter;
			if (popup != null)
			{
				popup.SetNotification(
					$"Notification #{_popupCounter}",
					$"This is popup instance '{instanceAddress}'.\nClick to close or use Close Recent button.",
					instanceAddress
				);
			}
			
			// Open with instance address
			await _uiService.OpenUiAsync(typeof(NotificationPopupPresenter), instanceAddress);
			
			_activePopupIds.Add(instanceAddress);
			UpdateUiVisibility(true);
			UpdateStatus($"Popup '{instanceAddress}' opened. Total active: {_activePopupIds.Count}");
		}

		/// <summary>
		/// Close the most recently opened popup
		/// </summary>
		public void CloseRecentPopup()
		{
			if (_activePopupIds.Count == 0)
			{
				UpdateStatus("No active popups to close.");
				return;
			}
			
			var instanceAddress = _activePopupIds[^1]; // Last one
			_activePopupIds.RemoveAt(_activePopupIds.Count - 1);
			
			UpdateStatus($"Closing popup: '{instanceAddress}'");
			
			// Close and unload with instance address
			_uiService.CloseUi(typeof(NotificationPopupPresenter), instanceAddress, destroy: true);
			_uiService.UnloadUi(typeof(NotificationPopupPresenter), instanceAddress);
			
			if (_activePopupIds.Count == 0)
			{
				UpdateUiVisibility(false);
			}
			UpdateStatus($"Popup closed. Remaining: {_activePopupIds.Count}");
		}

		/// <summary>
		/// Close all active popups
		/// </summary>
		public void CloseAllPopups()
		{
			if (_activePopupIds.Count == 0)
			{
				UpdateStatus("No active popups to close.");
				return;
			}
			
			UpdateStatus($"Closing all {_activePopupIds.Count} popups...");
			
			// Close each popup by its instance address
			foreach (var instanceAddress in _activePopupIds)
			{
				_uiService.CloseUi(typeof(NotificationPopupPresenter), instanceAddress, destroy: true);
				_uiService.UnloadUi(typeof(NotificationPopupPresenter), instanceAddress);
			}
			
			_activePopupIds.Clear();
			UpdateUiVisibility(false);
			UpdateStatus("All popups closed.");
		}

		/// <summary>
		/// List all active popup instances
		/// </summary>
		public void ListActivePopups()
		{
			UpdateStatus("Check console for active popup instances list.");
			Debug.Log("=== Active Popup Instances ===");
			
			if (_activePopupIds.Count == 0)
			{
				Debug.Log("No active popups.");
				return;
			}
			
			foreach (var instanceAddress in _activePopupIds)
			{
				var instanceId = new UiInstanceId(typeof(NotificationPopupPresenter), instanceAddress);
				var isVisible = _uiService.IsVisible<NotificationPopupPresenter>(instanceAddress);
				Debug.Log($"  - {instanceId} (visible: {isVisible})");
			}
			
			Debug.Log($"Total: {_activePopupIds.Count} popups");
		}

		/// <summary>
		/// Called by popups when they want to close themselves
		/// </summary>
		public void OnPopupClosed(string instanceAddress)
		{
			_activePopupIds.Remove(instanceAddress);
			
			// Unload the specific instance
			try
			{
				_uiService.UnloadUi(typeof(NotificationPopupPresenter), instanceAddress);
			}
			catch (KeyNotFoundException)
			{
				// Already unloaded
			}
			
			if (_activePopupIds.Count == 0)
			{
				UpdateUiVisibility(false);
			}
			UpdateStatus($"Popup '{instanceAddress}' self-closed. Remaining: {_activePopupIds.Count}");
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

