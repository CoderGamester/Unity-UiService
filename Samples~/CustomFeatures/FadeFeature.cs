using System.Collections;
using UnityEngine;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Custom feature that adds fade in/out effects using CanvasGroup.
	/// Demonstrates a basic custom feature with coroutine-based animation.
	/// 
	/// Usage:
	/// 1. Add this component to your presenter prefab
	/// 2. Ensure the prefab has a CanvasGroup component
	/// 3. Configure fade durations in the inspector
	/// 
	/// The feature will automatically:
	/// - Start with alpha 0 when opening
	/// - Fade in over the configured duration
	/// - Fade out when closing
	/// - Notify the presenter when transitions complete via OnOpenTransitionCompleted/OnCloseTransitionCompleted
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	public class FadeFeature : PresenterFeatureBase
	{
		[Header("Fade Settings")]
		[SerializeField] private float _fadeInDuration = 0.3f;
		[SerializeField] private float _fadeOutDuration = 0.2f;
		[SerializeField] private CanvasGroup _canvasGroup;

		private Coroutine _fadeCoroutine;

		private void OnValidate()
		{
			// Auto-assign CanvasGroup in editor
			_canvasGroup = _canvasGroup ?? GetComponent<CanvasGroup>();
		}

		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			
			// Ensure we have a CanvasGroup
			if (_canvasGroup == null)
			{
				_canvasGroup = GetComponent<CanvasGroup>();
			}
		}

		public override void OnPresenterOpening()
		{
			// Start invisible
			_canvasGroup.alpha = 0f;
		}

		public override void OnPresenterOpened()
		{
			// Start fade in coroutine
			if (_fadeCoroutine != null)
			{
				StopCoroutine(_fadeCoroutine);
			}
			_fadeCoroutine = StartCoroutine(FadeIn());
		}

		public override void OnPresenterClosing()
		{
			// Start fade out coroutine
			if (_fadeCoroutine != null)
			{
				StopCoroutine(_fadeCoroutine);
			}
			_fadeCoroutine = StartCoroutine(FadeOut());
		}

		private IEnumerator FadeIn()
		{
			if (_canvasGroup == null) yield break;
			
			float elapsed = 0f;
			float startAlpha = _canvasGroup.alpha;
			
			while (elapsed < _fadeInDuration)
			{
				elapsed += Time.deltaTime;
				_canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / _fadeInDuration);
				yield return null;
			}
			
			_canvasGroup.alpha = 1f;
			
			// Notify presenter that open transition is complete
			Presenter.NotifyOpenTransitionCompleted();
		}

		private IEnumerator FadeOut()
		{
			if (_canvasGroup == null) yield break;
			
			float elapsed = 0f;
			float startAlpha = _canvasGroup.alpha;
			
			while (elapsed < _fadeOutDuration)
			{
				elapsed += Time.deltaTime;
				_canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / _fadeOutDuration);
				yield return null;
			}
			
			_canvasGroup.alpha = 0f;
			
			// Notify presenter that close transition is complete
			Presenter.NotifyCloseTransitionCompleted();
		}

		private void OnDisable()
		{
			// Clean up coroutines
			if (_fadeCoroutine != null)
			{
				StopCoroutine(_fadeCoroutine);
				_fadeCoroutine = null;
			}
		}
	}
}

