using Cysharp.Threading.Tasks;
using UnityEngine;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Custom feature that adds fade in/out effects using CanvasGroup.
	/// Demonstrates a custom transition feature using UniTask.
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
			if (_fadeInDuration > 0)
			{
				FadeInAsync().Forget();
			}
		}

		public override void OnPresenterClosing()
		{
			if (_fadeOutDuration > 0 && Presenter && Presenter.gameObject)
			{
				FadeOutAsync().Forget();
			}
		}

		private async UniTask FadeInAsync()
		{
			if (_canvasGroup == null) return;
			
			_openTransitionCompletion = new UniTaskCompletionSource();
			
			float elapsed = 0f;
			float startAlpha = _canvasGroup.alpha;
			
			while (elapsed < _fadeInDuration)
			{
				if (!this || !gameObject) break;
				
				elapsed += Time.deltaTime;
				_canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / _fadeInDuration);
				await UniTask.Yield();
			}
			
			if (this && gameObject && _canvasGroup != null)
			{
				_canvasGroup.alpha = 1f;
			}
			
			_openTransitionCompletion?.TrySetResult();
		}

		private async UniTask FadeOutAsync()
		{
			if (_canvasGroup == null) return;
			
			_closeTransitionCompletion = new UniTaskCompletionSource();
			
			float elapsed = 0f;
			float startAlpha = _canvasGroup.alpha;
			
			while (elapsed < _fadeOutDuration)
			{
				if (!this || !gameObject) break;
				
				elapsed += Time.deltaTime;
				_canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / _fadeOutDuration);
				await UniTask.Yield();
			}
			
			if (this && gameObject && _canvasGroup != null)
			{
				_canvasGroup.alpha = 0f;
			}
			
			_closeTransitionCompletion?.TrySetResult();
		}
	}
}
