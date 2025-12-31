using UnityEngine;
using GameLovers.UiService;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Simple presenter for demonstrating different loading strategies.
	/// </summary>
	public class ExamplePresenter : UiPresenter
	{
		[SerializeField] private Text _titleText;
		[SerializeField] private Button _closeButton;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log($"[{nameof(ExamplePresenter)}] Initialized");

			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(() => Close(destroy: false));
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			Debug.Log($"[{nameof(ExamplePresenter)}] Opened");

			if (_titleText != null)
			{
				_titleText.text = "Asset Loading Example";
			}
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			Debug.Log($"[{nameof(ExamplePresenter)}] Closed");
		}
	}
}

