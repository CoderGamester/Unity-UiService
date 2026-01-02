using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;
using TMPro;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating data-driven UI presenters.
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
			_showWarriorButton?.onClick.AddListener(ShowWarriorData);
			_showMageButton?.onClick.AddListener(ShowMageData);
			_showRogueButton?.onClick.AddListener(ShowRogueData);
			_updateLowHealthButton?.onClick.AddListener(UpdateToLowHealth);
			
			// Pre-load presenter and subscribe to close events
			var presenter = await _uiService.LoadUiAsync<DataUiExamplePresenter>();
			presenter.OnCloseRequested.AddListener(() => UpdateUiVisibility(false));

			UpdateUiVisibility(false);
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
			await _uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(data);
			UpdateUiVisibility(true);
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
			await _uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(data);
			UpdateUiVisibility(true);
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
			await _uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(data);
			UpdateUiVisibility(true);
		}

		/// <summary>
		/// Updates the UI to show low health state
		/// </summary>
		public async void UpdateToLowHealth()
		{
			var data = new PlayerData
			{
				PlayerName = "Wounded Hero",
				Level = 15,
				Score = 3500,
				HealthPercentage = 0.15f
			};
			
			UpdateStatus("Updating to low health data...");
			await _uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(data);
			UpdateUiVisibility(true);
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

