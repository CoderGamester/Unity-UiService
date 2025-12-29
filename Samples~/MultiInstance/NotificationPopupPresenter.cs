using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example popup that can have multiple instances.
	/// Each instance has a unique instance address for identification.
	/// </summary>
	public class NotificationPopupPresenter : UiPresenter
	{
		[SerializeField] private Text _titleText;
		[SerializeField] private Text _messageText;
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _backgroundButton;

		private string _instanceAddress;
		private MultiInstanceExample _example;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			// Find the example manager (in a real game, you'd use dependency injection)
			_example = Object.FindFirstObjectByType<MultiInstanceExample>();
			
			// Setup close buttons
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(OnCloseClicked);
			}
			
			if (_backgroundButton != null)
			{
				_backgroundButton.onClick.AddListener(OnCloseClicked);
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			Debug.Log($"[Popup:{_instanceAddress}] Opened");
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			Debug.Log($"[Popup:{_instanceAddress}] Closed");
		}

		/// <summary>
		/// Set the notification content and instance address
		/// </summary>
		public void SetNotification(string title, string message, string instanceAddress)
		{
			_instanceAddress = instanceAddress;
			
			if (_titleText != null)
			{
				_titleText.text = title;
			}
			
			if (_messageText != null)
			{
				_messageText.text = message;
			}
		}

		private void OnCloseClicked()
		{
			// Notify the example manager that this popup is closing
			if (_example != null)
			{
				_example.OnPopupClosed(_instanceAddress);
			}
			
			// Close ourselves (but don't destroy - let the manager handle that)
			Close(destroy: false);
		}

		private void OnDestroy()
		{
			// Clean up button listeners
			if (_closeButton != null)
			{
				_closeButton.onClick.RemoveListener(OnCloseClicked);
			}
			
			if (_backgroundButton != null)
			{
				_backgroundButton.onClick.RemoveListener(OnCloseClicked);
			}
		}
	}
}

