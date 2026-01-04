using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Custom feature that adds fade in/out effects using CanvasGroup.
	/// Demonstrates a custom transition feature driven by a coroutine (while still exposing UniTask transition handles).
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
	/// - The presenter waits for transitions via ITransitionFeature before completing lifecycle
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	public class FadeFeature : PresenterFeatureBase, ITransitionFeature
	{
		[Header("Fade Settings")]
		[SerializeField] private float _fadeInDuration = 0.3f;
		[SerializeField] private float _fadeOutDuration = 0.2f;
		[SerializeField] private CanvasGroup _canvasGroup;

		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;
		private Coroutine _fadeCoroutine;

		/// <inheritdoc />
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

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
			StartFade(isOpening: true);
		}

		public override void OnPresenterClosing()
		{
			if (!Presenter || !Presenter.gameObject) return;
			StartFade(isOpening: false);
		}

		private void StartFade(bool isOpening)
		{
			if (_canvasGroup == null) return;

			StopFadeCoroutine();

			float targetAlpha = isOpening ? 1f : 0f;
			float duration = isOpening ? _fadeInDuration : _fadeOutDuration;
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
				_canvasGroup.alpha = targetAlpha;
				completionSource.TrySetResult();
				return;
			}

			float startAlpha = _canvasGroup.alpha;
			_fadeCoroutine = StartCoroutine(FadeRoutine(startAlpha, targetAlpha, duration, completionSource));
		}

		private IEnumerator FadeRoutine(
			float startAlpha,
			float targetAlpha,
			float duration,
			UniTaskCompletionSource completionSource)
		{
			float elapsed = 0f;

			while (elapsed < duration)
			{
				if (!this || !gameObject || _canvasGroup == null) break;

				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				_canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
				yield return null;
			}

			if (this && gameObject && _canvasGroup != null)
			{
				_canvasGroup.alpha = targetAlpha;
			}

			completionSource?.TrySetResult();
			_fadeCoroutine = null;
		}

		private void StopFadeCoroutine()
		{
			if (_fadeCoroutine == null) return;

			StopCoroutine(_fadeCoroutine);
			_fadeCoroutine = null;
		}
	}
}
