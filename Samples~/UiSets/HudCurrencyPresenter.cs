using UnityEngine;
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
		[SerializeField] private Text _goldText;
		[SerializeField] private Text _gemsText;

		private int _gold = 1000;
		private int _gems = 50;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log("[Currency] Initialized");
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			UpdateCurrencyDisplay();
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
				_goldText.text = FormatNumber(_gold);
			}

			if (_gemsText != null)
			{
				_gemsText.text = FormatNumber(_gems);
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

