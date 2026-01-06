using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;
using TMPro;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating data-driven UI presenters using <see cref="UiPresenter{T}"/>.
	/// Shows two patterns for passing data:
	/// <list type="bullet">
	/// <item>Initial data via <c>OpenUiAsync&lt;T, TData&gt;(data)</c> when opening the UI</item>
	/// <item>Dynamic updates via the public <c>Data</c> property setter (triggers <c>OnSetData()</c> automatically)</item>
	/// </list>
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// </summary>
	public class DataPresenterExample : MonoBehaviour
	{
		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;

		[Header("UI Buttons")]
		[SerializeField] private Button _showWarriorButton;
		[SerializeField] private Button _showMageButton;
		[SerializeField] private Button _showRogueButton;
		[SerializeField] private Button _updateLowHealthButton;

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
			_showWarriorButton?.onClick.AddListener(ShowWarriorData);
			_showMageButton?.onClick.AddListener(ShowMageData);
			_showRogueButton?.onClick.AddListener(ShowRogueData);
			_updateLowHealthButton?.onClick.AddListener(UpdateToLowHealth);
			_updateLowHealthButton?.gameObject.SetActive(false);
			
			// Pre-load presenter and subscribe to close events
			var presenter = await _uiService.LoadUiAsync<DataUiExamplePresenter>();
			presenter.OnCloseRequested.AddListener(UpdateCloseButtonStatus);

			UpdateStatus("Ready");
		}

		private void OnDestroy()
		{
			_showWarriorButton?.onClick.RemoveListener(ShowWarriorData);
			_showMageButton?.onClick.RemoveListener(ShowMageData);
			_showRogueButton?.onClick.RemoveListener(ShowRogueData);
			_updateLowHealthButton?.onClick.RemoveListener(UpdateToLowHealth);
		}

		/// <summary>
		/// Shows the warrior character data
		/// </summary>
		public async void ShowWarriorData()
		{
			var data = new PlayerData
			{
				PlayerName = "Thor the Warrior",
				Level = 45,
				Score = 12500,
				HealthPercentage = 0.85f
			};
			
			UpdateStatus("Opening UI with Warrior data...");
			await OpenOrUpdateUi(data);
		}

		/// <summary>
		/// Shows the mage character data
		/// </summary>
		public async void ShowMageData()
		{
			var data = new PlayerData
			{
				PlayerName = "Gandalf the Mage",
				Level = 99,
				Score = 50000,
				HealthPercentage = 0.60f
			};
			
			UpdateStatus("Opening UI with Mage data...");
			await OpenOrUpdateUi(data);
		}

		/// <summary>
		/// Shows the rogue character data
		/// </summary>
		public async void ShowRogueData()
		{
			var data = new PlayerData
			{
				PlayerName = "Shadow the Rogue",
				Level = 33,
				Score = 8900,
				HealthPercentage = 1.0f
			};
			
			UpdateStatus("Opening UI with Rogue data...");
			await OpenOrUpdateUi(data);
		}

		/// <summary>
		/// Updates the UI to show low health state.
		/// Demonstrates direct Data property assignment which triggers OnSetData() automatically.
		/// </summary>
		public void UpdateToLowHealth()
		{
			UpdateStatus("Updating data directly...");
			
			// Get the presenter and update its Data property directly.
			// Setting Data automatically triggers OnSetData() to refresh the UI.
			var presenter = _uiService.GetUi<DataUiExamplePresenter>();
			var data = presenter.Data;

			data.HealthPercentage = 0.15f;
			presenter.Data = data;
		}

		private void UpdateCloseButtonStatus()
		{
			_updateLowHealthButton.gameObject.SetActive(false);
			UpdateStatus("UI Closed but still in memory");
		}

		private async UniTask OpenOrUpdateUi(PlayerData data)
		{
			if(_uiService.IsVisible<DataUiExamplePresenter>())
			{
				var presenter = _uiService.GetUi<DataUiExamplePresenter>();
				presenter.Data = data;
			}
			else
			{
				await _uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(data);

				_updateLowHealthButton.gameObject.SetActive(true);
			}
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

