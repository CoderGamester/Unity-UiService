using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
		[SerializeField] private TMP_Text _titleText;
		[SerializeField] private TMP_Text _messageText;
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _backgroundButton;

		private string _instanceAddress;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
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
			OnCloseRequested.Invoke();
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

			OnCloseRequested.RemoveAllListeners();
		}
	}
}

