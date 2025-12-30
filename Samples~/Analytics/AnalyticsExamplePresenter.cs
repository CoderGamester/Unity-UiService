using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Simple presenter for the analytics demo.
	/// </summary>
	public class AnalyticsExamplePresenter : UiPresenter
	{
		[SerializeField] private Text _titleText;
		[SerializeField] private Button _closeButton;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(() => Close(destroy: false));
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			if (_titleText != null)
			{
				_titleText.text = "Analytics Example";
			}
		}
	}
}

