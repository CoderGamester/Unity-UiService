using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating data-driven UI presenters.
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// </summary>
	public class DataPresenterExample : MonoBehaviour
	{
		[SerializeField] private UiConfigs _uiConfigs;
		
		[Header("UI Buttons")]
		[SerializeField] private Button _showWarriorButton;
		[SerializeField] private Button _showMageButton;
		[SerializeField] private Button _showRogueButton;
		[SerializeField] private Button _updateLowHealthButton;
		
		private IUiServiceInit _uiService;

		private void Start()
		{
			// Initialize UI Service
			_uiService = new UiService();
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_showWarriorButton?.onClick.AddListener(ShowWarriorData);
			_showMageButton?.onClick.AddListener(ShowMageData);
			_showRogueButton?.onClick.AddListener(ShowRogueData);
			_updateLowHealthButton?.onClick.AddListener(UpdateToLowHealth);
			
			Debug.Log("=== Data Presenter Example Started ===");
			Debug.Log("Use the UI buttons to show different character data:");
			Debug.Log("  Warrior: High health warrior character");
			Debug.Log("  Mage: High level mage character");
			Debug.Log("  Rogue: Full health rogue character");
			Debug.Log("  Low Health: Update to wounded state");
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
		public void ShowWarriorData()
		{
			var data = new PlayerData
			{
				PlayerName = "Thor the Warrior",
				Level = 45,
				Score = 12500,
				HealthPercentage = 0.85f
			};
			
			Debug.Log("Opening UI with Warrior data...");
			_uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(data).Forget();
		}

		/// <summary>
		/// Shows the mage character data
		/// </summary>
		public void ShowMageData()
		{
			var data = new PlayerData
			{
				PlayerName = "Gandalf the Mage",
				Level = 99,
				Score = 50000,
				HealthPercentage = 0.60f
			};
			
			Debug.Log("Opening UI with Mage data...");
			_uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(data).Forget();
		}

		/// <summary>
		/// Shows the rogue character data
		/// </summary>
		public void ShowRogueData()
		{
			var data = new PlayerData
			{
				PlayerName = "Shadow the Rogue",
				Level = 33,
				Score = 8900,
				HealthPercentage = 1.0f
			};
			
			Debug.Log("Opening UI with Rogue data...");
			_uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(data).Forget();
		}

		/// <summary>
		/// Updates the UI to show low health state
		/// </summary>
		public void UpdateToLowHealth()
		{
			var data = new PlayerData
			{
				PlayerName = "Wounded Hero",
				Level = 15,
				Score = 3500,
				HealthPercentage = 0.15f
			};
			
			Debug.Log("Updating to low health data...");
			_uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(data).Forget();
		}
	}
}
