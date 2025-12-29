using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example UI Presenter with time-based delays using the new self-contained TimeDelayFeature.
	/// Demonstrates how to configure delays and respond to delay completion via presenter lifecycle hooks.
	/// </summary>
	[RequireComponent(typeof(TimeDelayFeature))]
	public class DelayedUiExamplePresenter : UiPresenter
	{
		[SerializeField] private TimeDelayFeature _delayFeature;
		[SerializeField] private Text _titleText;
		[SerializeField] private Text _statusText;
		[SerializeField] private Button _closeButton;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			Debug.Log("[DelayedUiExample] UI Initialized");
			
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(() => Close(destroy: false));
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			Debug.Log("[DelayedUiExample] UI Opened, starting delay...");
			
			if (_titleText != null)
			{
				_titleText.text = "Delayed UI Example";
			}
			
			if (_statusText != null)
			{
				_statusText.text = $"Opening with {_delayFeature.OpenDelayInSeconds}s delay...";
			}
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			Debug.Log($"[DelayedUiExample] UI Closed after {_delayFeature.CloseDelayInSeconds}s delay");
		}

		protected override void OnOpenTransitionCompleted()
		{
			Debug.Log("[DelayedUiExample] Opening delay completed!");
			
			if (_statusText != null)
			{
				_statusText.text = $"Opened successfully after {_delayFeature.OpenDelayInSeconds}s!";
			}
		}

		protected override void OnCloseTransitionCompleted()
		{
			Debug.Log("[DelayedUiExample] Closing delay completed!");
		}
	}
}
