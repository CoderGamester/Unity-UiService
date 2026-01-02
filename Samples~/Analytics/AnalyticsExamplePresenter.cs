using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Simple presenter for the analytics demo.
	/// </summary>
	public class AnalyticsExamplePresenter : UiPresenter
	{
		[SerializeField] private TMP_Text _titleText;
		[SerializeField] private Button _closeButton;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		protected override void OnInitialized()
		{
			base.OnInitialized();
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(OnCloseButtonClicked);
			}
		}

		private void OnDestroy()
		{
			_closeButton?.onClick.RemoveListener(OnCloseButtonClicked);
			OnCloseRequested.RemoveAllListeners();
		}

		private void OnCloseButtonClicked()
		{
			OnCloseRequested.Invoke();
			Close(destroy: false);
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

