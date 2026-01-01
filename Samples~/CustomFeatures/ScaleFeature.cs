using Cysharp.Threading.Tasks;
using UnityEngine;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Custom feature that adds scale in/out effects with easing.
	/// Demonstrates a custom transition feature with configurable animation curves.
	/// 
	/// Usage:
	/// 1. Add this component to your presenter prefab
	/// 2. Configure scale settings and curves in the inspector
	/// 
	/// The feature will automatically:
	/// - Start at minimum scale when opening
	/// - Scale up with the configured curve
	/// - Scale down when closing
	/// - The presenter waits for transitions via ITransitionFeature before completing lifecycle
	/// </summary>
	public class ScaleFeature : PresenterFeatureBase, ITransitionFeature
	{
		[Header("Scale Settings")]
		[SerializeField] private float _scaleInDuration = 0.25f;
		[SerializeField] private float _scaleOutDuration = 0.15f;
		[SerializeField] private Vector3 _startScale = new Vector3(0.8f, 0.8f, 1f);
		[SerializeField] private Vector3 _endScale = Vector3.one;
		
		[Header("Easing")]
		[SerializeField] private AnimationCurve _scaleInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		[SerializeField] private AnimationCurve _scaleOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		
		[Header("Target (optional)")]
		[SerializeField] private Transform _targetTransform;

		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;

		/// <inheritdoc />
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		private void OnValidate()
		{
			// Use this transform if no target specified
			if (_targetTransform == null)
			{
				_targetTransform = transform;
			}
		}

		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			
			if (_targetTransform == null)
			{
				_targetTransform = transform;
			}
		}

		public override void OnPresenterOpening()
		{
			// Start at minimum scale
			_targetTransform.localScale = _startScale;
		}

		public override void OnPresenterOpened()
		{
			if (_scaleInDuration > 0)
			{
				ScaleInAsync().Forget();
			}
		}

		public override void OnPresenterClosing()
		{
			if (_scaleOutDuration > 0 && Presenter && Presenter.gameObject)
			{
				ScaleOutAsync().Forget();
			}
		}

		private async UniTask ScaleInAsync()
		{
			if (_targetTransform == null) return;
			
			_openTransitionCompletion = new UniTaskCompletionSource();
			
			float elapsed = 0f;
			Vector3 startScale = _targetTransform.localScale;
			
			while (elapsed < _scaleInDuration)
			{
				if (!this || !gameObject) break;
				
				elapsed += Time.deltaTime;
				float t = _scaleInCurve.Evaluate(elapsed / _scaleInDuration);
				_targetTransform.localScale = Vector3.LerpUnclamped(startScale, _endScale, t);
				await UniTask.Yield();
			}
			
			if (this && gameObject && _targetTransform != null)
			{
				_targetTransform.localScale = _endScale;
			}
			
			_openTransitionCompletion?.TrySetResult();
		}

		private async UniTask ScaleOutAsync()
		{
			if (_targetTransform == null) return;
			
			_closeTransitionCompletion = new UniTaskCompletionSource();
			
			float elapsed = 0f;
			Vector3 startScale = _targetTransform.localScale;
			
			while (elapsed < _scaleOutDuration)
			{
				if (!this || !gameObject) break;
				
				elapsed += Time.deltaTime;
				float t = _scaleOutCurve.Evaluate(elapsed / _scaleOutDuration);
				_targetTransform.localScale = Vector3.LerpUnclamped(startScale, _startScale, t);
				await UniTask.Yield();
			}
			
			if (this && gameObject && _targetTransform != null)
			{
				_targetTransform.localScale = _startScale;
			}
			
			_closeTransitionCompletion?.TrySetResult();
		}
	}
}
