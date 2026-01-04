using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example data structure for Notification UI
	/// </summary>
	public struct NotificationData
	{
		public string Title;
		public string Message;
		public string InstanceAddress;
	}

	/// <summary>
	/// Example popup that can have multiple instances.
	/// Each instance has a unique instance address for identification.
	/// </summary>
	public class NotificationPopupPresenter : UiPresenter<NotificationData>
	{
		[SerializeField] private TMP_Text _titleText;
		[SerializeField] private TMP_Text _messageText;
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _backgroundButton;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

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

		protected override void OnSetData()
		{
			base.OnSetData();
			
			if (_titleText != null)
			{
				_titleText.text = Data.Title;
			}
			
			if (_messageText != null)
			{
				_messageText.text = Data.Message;
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			Debug.Log($"[Popup:{Data.InstanceAddress}] Opened");
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			Debug.Log($"[Popup:{Data.InstanceAddress}] Closed");
		}

		private void OnCloseClicked()
		{
			OnCloseRequested.Invoke();
			// Close ourselves (but don't destroy - let the manager handle that)
			Close(destroy: false);
		}
	}
}
