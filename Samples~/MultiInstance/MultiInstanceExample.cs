using System.Collections.Generic;
using System.Text;
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
			_listActiveButton?.onClick.AddListener(ListActivePopups);
			
			UpdateStatus("Ready");
		}

		private void OnDestroy()
		{
			_activePopupIds.Clear();

			_spawnPopupButton?.onClick.RemoveListener(SpawnNewPopupWrapper);
			_closeRecentButton?.onClick.RemoveListener(CloseRecentPopup);
			_listActiveButton?.onClick.RemoveListener(ListActivePopups);
			
			_uiService?.Dispose();
		}

		/// <summary>
		/// Spawn a new popup with a unique instance address
		/// </summary>
		public async UniTaskVoid SpawnNewPopup()
		{
			if (_uiService == null) return;

			_popupCounter++;
			var instanceAddress = $"popup_{_popupCounter}";
			var data = new NotificationData
			{
				Title = $"Notification #{_popupCounter}",
				Message = $"This is popup instance '{instanceAddress}'.\nClick to close or use Close Recent button.",
				InstanceAddress = instanceAddress
			};
			
			// Load with a specific instance address
			// This allows multiple instances of the same UI type
			var presenter = await _uiService.LoadUiAsync(typeof(NotificationPopupPresenter), instanceAddress, openAfter: false);
			
			// Subscribe to close events
			var popup = (NotificationPopupPresenter)presenter;
			popup.OnCloseRequested.AddListener(() => OnPopupClosed(instanceAddress));
			
			// Open with instance address and data
			await _uiService.OpenUiAsync(typeof(NotificationPopupPresenter), instanceAddress, data);
			
			_activePopupIds.Add(instanceAddress);
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
			
			// Close and unload with instance address.
			_uiService.CloseUi(typeof(NotificationPopupPresenter), instanceAddress, destroy: true);
			
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
			
			// Close each popup by its instance address.
			// As of v1.2.0, destroy:true is safe for multi-instance presenters (no ambiguity).
			foreach (var instanceAddress in _activePopupIds)
			{
				_uiService.CloseUi(typeof(NotificationPopupPresenter), instanceAddress, destroy: true);
			}

			UpdateStatus($"Closed all {_activePopupIds.Count} popups...");
			_activePopupIds.Clear();
		}

		/// <summary>
		/// List all active popup instances
		/// </summary>
		public void ListActivePopups()
		{
			var sb = new StringBuilder("=== Active Popup Instances ===");

			UpdateStatus("Check console for active popup instances list.");

			foreach (var instanceAddress in _activePopupIds)
			{
				var instanceId = new UiInstanceId(typeof(NotificationPopupPresenter), instanceAddress);
				var isVisible = _uiService.IsVisible<NotificationPopupPresenter>(instanceAddress);

				sb.Append($"\n  - {instanceId} (visible: {isVisible})");
			}

			sb.Append($"\nTotal: {_activePopupIds.Count} popups");
			Debug.Log(sb.ToString());
		}

		/// <summary>
		/// Called by popups when they want to close themselves
		/// </summary>
		public void OnPopupClosed(string instanceAddress)
		{
			_activePopupIds.Remove(instanceAddress);

			UpdateStatus($"Popup '{instanceAddress}' self-closed. Remaining: {_activePopupIds.Count}");
		}
		
		private void SpawnNewPopupWrapper()
		{
			SpawnNewPopup().Forget();
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
