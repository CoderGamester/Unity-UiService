using System.Collections;
using UnityEngine;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Custom feature that adds scale in/out effects with easing.
	/// Demonstrates a custom feature with configurable animation curves.
	/// 
	/// Usage:
	/// 1. Add this component to your presenter prefab
	/// 2. Configure scale settings and curves in the inspector
	/// 
	/// The feature will automatically:
	/// - Start at minimum scale when opening
	/// - Scale up with the configured curve
	/// - Scale down when closing
	/// </summary>
	public class ScaleFeature : PresenterFeatureBase
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
		
		/// <summary>
		/// Event fired when scale in completes
		/// </summary>
		public event System.Action OnScaleInComplete;
		
		/// <summary>
		/// Event fired when scale out completes
		/// </summary>
		public event System.Action OnScaleOutComplete;

		private Coroutine _scaleCoroutine;

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
			
			Debug.Log("[ScaleFeature] Initialized");
		}

		public override void OnPresenterOpening()
		{
			// Start at minimum scale
			if (_targetTransform != null)
			{
				_targetTransform.localScale = _startScale;
			}
			
			Debug.Log("[ScaleFeature] Starting scale in...");
		}

		public override void OnPresenterOpened()
		{
			// Start scale in coroutine
			if (_scaleCoroutine != null)
			{
				StopCoroutine(_scaleCoroutine);
			}
			_scaleCoroutine = StartCoroutine(ScaleIn());
		}

		public override void OnPresenterClosing()
		{
			// Start scale out coroutine
			if (_scaleCoroutine != null)
			{
				StopCoroutine(_scaleCoroutine);
			}
			_scaleCoroutine = StartCoroutine(ScaleOut());
			
			Debug.Log("[ScaleFeature] Starting scale out...");
		}

		private IEnumerator ScaleIn()
		{
			if (_targetTransform == null) yield break;
			
			float elapsed = 0f;
			Vector3 startScale = _targetTransform.localScale;
			
			while (elapsed < _scaleInDuration)
			{
				elapsed += Time.deltaTime;
				float t = _scaleInCurve.Evaluate(elapsed / _scaleInDuration);
				_targetTransform.localScale = Vector3.LerpUnclamped(startScale, _endScale, t);
				yield return null;
			}
			
			_targetTransform.localScale = _endScale;
			Debug.Log("[ScaleFeature] Scale in complete");
			OnScaleInComplete?.Invoke();
		}

		private IEnumerator ScaleOut()
		{
			if (_targetTransform == null) yield break;
			
			float elapsed = 0f;
			Vector3 startScale = _targetTransform.localScale;
			
			while (elapsed < _scaleOutDuration)
			{
				elapsed += Time.deltaTime;
				float t = _scaleOutCurve.Evaluate(elapsed / _scaleOutDuration);
				_targetTransform.localScale = Vector3.LerpUnclamped(startScale, _startScale, t);
				yield return null;
			}
			
			_targetTransform.localScale = _startScale;
			Debug.Log("[ScaleFeature] Scale out complete");
			OnScaleOutComplete?.Invoke();
		}

		private void OnDisable()
		{
			// Clean up coroutines
			if (_scaleCoroutine != null)
			{
				StopCoroutine(_scaleCoroutine);
				_scaleCoroutine = null;
			}
		}
	}
}

