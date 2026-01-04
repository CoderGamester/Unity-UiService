using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example presenter that uses the ScaleFeature.
	/// Shows how to attach a single custom feature with animation curves.
	/// </summary>
	[RequireComponent(typeof(ScaleFeature))]
	public class ScalingPresenter : UiPresenter
	{
		[SerializeField] private ScaleFeature _scaleFeature;
		[SerializeField] private TMP_Text _titleText;
		[SerializeField] private Button _closeButton;

		/// <summary>
		/// Event invoked when the close button is clicked, before the close transition begins.
		/// Subscribe to this event to react to the presenter's close request.
		/// </summary>
		public UnityEvent OnCloseRequested { get; } = new UnityEvent();

		private void OnDestroy()
		{
			_closeButton?.onClick.RemoveListener(OnCloseButtonClicked);
			OnCloseRequested.RemoveAllListeners();
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(OnCloseButtonClicked);
			}
			
			Debug.Log("[ScalingPresenter] Initialized with ScaleFeature");
		}

		private void OnCloseButtonClicked()
		{
			OnCloseRequested.Invoke();
			Close(destroy: false);
		}
	}
}

