using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example HUD element: Currency Display
	/// Part of the GameHud UI Set
	/// </summary>
	public class HudCurrencyPresenter : UiPresenter
	{
		[SerializeField] private TMP_Text _goldText;
		[SerializeField] private TMP_Text _gemsText;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		private int _gold = 1000;
		private int _gems = 50;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log("[Currency] Initialized");
		}

		private void OnDestroy()
		{
			OnCloseRequested.RemoveAllListeners();
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			var gold = Random.Range(0, 1000000);
			var gems = Random.Range(0, 1000);
			SetCurrency(gold, gems);
			Debug.Log("[Currency] Opened");
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			Debug.Log("[Currency] Closed");
		}

		/// <summary>
		/// Call this to update the currency display
		/// </summary>
		public void SetCurrency(int gold, int gems)
		{
			_gold = gold;
			_gems = gems;
			UpdateCurrencyDisplay();
		}

		private void UpdateCurrencyDisplay()
		{
			if (_goldText != null)
			{
				_goldText.text = "Gold: " + FormatNumber(_gold);
			}

			if (_gemsText != null)
			{
				_gemsText.text = "Gems: " + FormatNumber(_gems);
			}
		}

		private string FormatNumber(int value)
		{
			if (value >= 1000000)
			{
				return $"{value / 1000000f:F1}M";
			}
			if (value >= 1000)
			{
				return $"{value / 1000f:F1}K";
			}
			return value.ToString();
		}
	}
}

