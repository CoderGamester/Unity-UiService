using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log("[DataUiExample] UI Initialized");
			
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(OnCloseButtonClicked);
			}
		}

		private void OnDestroy()
		{
			_closeButton?.onClick.RemoveListener(OnCloseButtonClicked);
			OnCloseRequested.RemoveAllListeners();
		}

		private void OnCloseButtonClicked()
		{
			OnCloseRequested.Invoke();
			Close(destroy: false);
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

