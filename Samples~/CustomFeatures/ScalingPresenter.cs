using UnityEngine;
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
		[SerializeField] private Text _titleText;
		[SerializeField] private Button _closeButton;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			// Subscribe to feature events if needed
			if (_scaleFeature != null)
			{
				_scaleFeature.OnScaleInComplete += OnScaleInComplete;
			}
			
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(() => Close(destroy: false));
			}
			
			Debug.Log("[ScalingPresenter] Initialized with ScaleFeature");
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			if (_titleText != null)
			{
				_titleText.text = "Scaling Presenter";
			}
		}

		private void OnScaleInComplete()
		{
			Debug.Log("[ScalingPresenter] Scale in animation completed!");
		}

		private void OnDestroy()
		{
			if (_scaleFeature != null)
			{
				_scaleFeature.OnScaleInComplete -= OnScaleInComplete;
			}
		}
	}
}

