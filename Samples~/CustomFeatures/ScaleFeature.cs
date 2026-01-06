using System.Collections;
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
		private Coroutine _scaleCoroutine;

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
			StartScale(isOpening: true);
		}

		public override void OnPresenterClosing()
		{
			if (!Presenter || !Presenter.gameObject) return;
			StartScale(isOpening: false);
		}

		private void StartScale(bool isOpening)
		{
			if (_targetTransform == null) return;

			StopScaleCoroutine();

			Vector3 targetScale = isOpening ? _endScale : _startScale;
			float duration = isOpening ? _scaleInDuration : _scaleOutDuration;
			AnimationCurve curve = isOpening ? _scaleInCurve : _scaleOutCurve;
			var completionSource = new UniTaskCompletionSource();

			// If we are interrupting the opposite direction, ensure it doesn't block the presenter lifecycle.
			if (isOpening)
			{
				_closeTransitionCompletion?.TrySetResult();
				_openTransitionCompletion = completionSource;
			}
			else
			{
				_openTransitionCompletion?.TrySetResult();
				_closeTransitionCompletion = completionSource;
			}

			if (duration <= 0f)
			{
				_targetTransform.localScale = targetScale;
				completionSource.TrySetResult();
				return;
			}

			Vector3 startScale = _targetTransform.localScale;
			_scaleCoroutine = StartCoroutine(ScaleRoutine(startScale, targetScale, duration, curve, completionSource));
		}

		private IEnumerator ScaleRoutine(
			Vector3 startScale,
			Vector3 targetScale,
			float duration,
			AnimationCurve curve,
			UniTaskCompletionSource completionSource)
		{
			float elapsed = 0f;

			while (elapsed < duration)
			{
				if (!this || !gameObject || _targetTransform == null) break;

				elapsed += Time.deltaTime;
				float normalized = Mathf.Clamp01(elapsed / duration);
				float t = curve != null ? curve.Evaluate(normalized) : normalized;
				_targetTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
				yield return null;
			}

			if (this && gameObject && _targetTransform != null)
			{
				_targetTransform.localScale = targetScale;
			}

			completionSource?.TrySetResult();
			_scaleCoroutine = null;
		}

		private void StopScaleCoroutine()
		{
			if (_scaleCoroutine == null) return;

			StopCoroutine(_scaleCoroutine);
			_scaleCoroutine = null;
		}
	}
}
