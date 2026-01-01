using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>
	/// Feature that adds animation-based delayed opening and closing to a <see cref="UiPresenter"/>.
	/// Plays intro/outro animations and waits for them to complete.
	/// Implements <see cref="ITransitionFeature"/> to allow the presenter to await transitions.
	/// </summary>
	[RequireComponent(typeof(Animation))]
	public class AnimationDelayFeature : PresenterFeatureBase, ITransitionFeature
	{
		[SerializeField] private Animation _animation;
		[SerializeField] private AnimationClip _introAnimationClip;
		[SerializeField] private AnimationClip _outroAnimationClip;

		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;

		/// <summary>
		/// Gets the Animation component
		/// </summary>
		public Animation AnimationComponent => _animation;

		/// <summary>
		/// Gets the intro animation clip
		/// </summary>
		public AnimationClip IntroAnimationClip => _introAnimationClip;

		/// <summary>
		/// Gets the outro animation clip
		/// </summary>
		public AnimationClip OutroAnimationClip => _outroAnimationClip;

		/// <summary>
		/// Gets the delay in seconds for opening (based on intro animation length)
		/// </summary>
		public float OpenDelayInSeconds => _introAnimationClip == null ? 0f : _introAnimationClip.length;

		/// <summary>
		/// Gets the delay in seconds for closing (based on outro animation length)
		/// </summary>
		public float CloseDelayInSeconds => _outroAnimationClip == null ? 0f : _outroAnimationClip.length;

		/// <inheritdoc />
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		private void OnValidate()
		{
			_animation = _animation ?? GetComponent<Animation>();
		}

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			if (_introAnimationClip != null)
			{
				OpenWithAnimationAsync().Forget();
			}
		}

		/// <inheritdoc />
		public override void OnPresenterClosing()
		{
			if (_outroAnimationClip != null && Presenter && Presenter.gameObject)
			{
				CloseWithAnimationAsync().Forget();
			}
		}

		/// <summary>
		/// Called when the presenter's opening animation starts.
		/// Override this in derived classes to add custom behavior.
		/// </summary>
		protected virtual void OnOpenStarted()
		{
			if (_introAnimationClip != null)
			{
				_animation.clip = _introAnimationClip;
				_animation.Play();
			}
		}

		/// <summary>
		/// Called when the presenter's opening animation is completed.
		/// Override this in derived classes to add custom behavior.
		/// </summary>
		protected virtual void OnOpenedCompleted() { }

		/// <summary>
		/// Called when the presenter's closing animation starts.
		/// Override this in derived classes to add custom behavior.
		/// </summary>
		protected virtual void OnCloseStarted()
		{
			if (_outroAnimationClip != null)
			{
				_animation.clip = _outroAnimationClip;
				_animation.Play();
			}
		}

		/// <summary>
		/// Called when the presenter's closing animation is completed.
		/// Override this in derived classes to add custom behavior.
		/// </summary>
		protected virtual void OnClosedCompleted() { }

		private async UniTask OpenWithAnimationAsync()
		{
			_openTransitionCompletion = new UniTaskCompletionSource();
			
			OnOpenStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(OpenDelayInSeconds));

			if (this && gameObject)
			{
				OnOpenedCompleted();
			}
			
			_openTransitionCompletion?.TrySetResult();
		}

		private async UniTask CloseWithAnimationAsync()
		{
			_closeTransitionCompletion = new UniTaskCompletionSource();
			
			OnCloseStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(CloseDelayInSeconds));

			if (this && gameObject)
			{
				// Note: Visibility (SetActive) is now handled by UiPresenter after all transitions complete
				OnClosedCompleted();
			}
			
			_closeTransitionCompletion?.TrySetResult();
		}
	}
}
