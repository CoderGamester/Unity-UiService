using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example UI Presenter demonstrating basic UI lifecycle
	/// </summary>
	public class BasicUiExamplePresenter : UiPresenter
	{
		[SerializeField] private TMP_Text _titleText;
		[SerializeField] private Button _closeButton;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log("[BasicUiExample] UI Initialized");
			
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(() => Close(destroy: false));
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			Debug.Log("[BasicUiExample] UI Opened");
			
			if (_titleText != null)
			{
				_titleText.text = "Basic UI Example";
			}
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			Debug.Log("[BasicUiExample] UI Closed");
		}
	}
}

