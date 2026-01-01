using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example data structure for UI Presenter
	/// </summary>
	public struct PlayerData
	{
		public string PlayerName;
		public int Level;
		public int Score;
		public float HealthPercentage;
	}

	/// <summary>
	/// Example UI Presenter demonstrating data-driven UI
	/// </summary>
	public class DataUiExamplePresenter : UiPresenter<PlayerData>
	{
		[SerializeField] private TMP_Text _playerNameText;
		[SerializeField] private TMP_Text _levelText;
		[SerializeField] private TMP_Text _scoreText;
		[SerializeField] private Slider _healthSlider;
		[SerializeField] private Button _closeButton;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log("[DataUiExample] UI Initialized");
			
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(() => Close(destroy: false));
			}
		}

		protected override void OnSetData()
		{
			base.OnSetData();
			Debug.Log($"[DataUiExample] Data Set: {Data.PlayerName}, Level {Data.Level}, Score {Data.Score}");
			
			UpdateUI();
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			Debug.Log("[DataUiExample] UI Opened");
		}

		private void UpdateUI()
		{
			if (_playerNameText != null)
			{
				_playerNameText.text = $"Player: {Data.PlayerName}";
			}
			
			if (_levelText != null)
			{
				_levelText.text = $"Level: {Data.Level}";
			}
			
			if (_scoreText != null)
			{
				_scoreText.text = $"Score: {Data.Score}";
			}
			
			if (_healthSlider != null)
			{
				_healthSlider.value = Data.HealthPercentage;
			}
		}
	}
}

