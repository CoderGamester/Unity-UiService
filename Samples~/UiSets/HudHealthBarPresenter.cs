using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example HUD element: Health Bar
	/// Part of the GameHud UI Set
	/// </summary>
	public class HudHealthBarPresenter : UiPresenter
	{
		[SerializeField] private Slider _healthSlider;
		[SerializeField] private TMP_Text _healthText;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		private float _currentHealth = 100f;
		private float _maxHealth = 100f;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log("[HealthBar] Initialized");
		}

		private void OnDestroy()
		{
			OnCloseRequested.RemoveAllListeners();
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			var health = Random.Range(0f, 100f);
			SetHealth(health, 100f);
			Debug.Log("[HealthBar] Opened");
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			Debug.Log("[HealthBar] Closed");
		}

		/// <summary>
		/// Call this to update the health display
		/// </summary>
		public void SetHealth(float current, float max)
		{
			_currentHealth = current;
			_maxHealth = max;
			UpdateHealthDisplay();
		}

		private void UpdateHealthDisplay()
		{
			if (_healthSlider != null)
			{
				_healthSlider.value = _currentHealth / _maxHealth;
			}

			if (_healthText != null)
			{
				_healthText.text = $"{_currentHealth:F0}/{_maxHealth:F0}";
			}
		}
	}
}

