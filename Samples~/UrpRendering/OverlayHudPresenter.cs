using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// A plain Screen Space - Overlay presenter, deliberately configured at the LOWEST
	/// <c>UiConfig.Layer</c> of the three, to demonstrate that it still draws on top of the stacked one.
	/// </summary>
	public class OverlayHudPresenter : UiPresenter
	{
		[SerializeField] private Button _closeButton;
		[SerializeField] private TMP_Text _body;

		private void Awake()
		{
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(() => Close(false));
			}
		}

		protected override void OnOpened()
		{
			if (_body != null)
			{
				_body.text = "Overlay HUD at Layer 0 -- the LOWEST layer in this sample.\n\n" +
					"It still covers the stacked presenter at Layer 20. UiConfig.Layer is not a total " +
					"order once render modes mix, and there is no runtime fix.";
			}
		}
	}
}
