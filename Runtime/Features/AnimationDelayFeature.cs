using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>
	/// Feature that adds animation-based delayed opening and closing to a <see cref="UiPresenter"/>.
	/// Plays intro/outro animations and waits for them to complete.
	/// </summary>
	[RequireComponent(typeof(Animation))]
	public class AnimationDelayFeature : PresenterFeatureBase
	{
		[SerializeField] private Animation _animation;
		[SerializeField] private AnimationClip _introAnimationClip;
		[SerializeField] private AnimationClip _outroAnimationClip;

		private UniTaskCompletionSource _currentDelayCompletion;

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

		/// <summary>
		/// Gets the UniTask of the current delay process.
		/// This task can be awaited to wait for the current transition to complete.
		/// </summary>
		public UniTask CurrentDelayTask => _currentDelayCompletion?.Task ?? UniTask.CompletedTask;

		private void OnValidate()
		{
			_animation = _animation ?? GetComponent<Animation>();
		}

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			OpenWithAnimationAsync().Forget();
		}

		/// <inheritdoc />
		public override void OnPresenterClosing()
		{
			if (Presenter && Presenter.gameObject)
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
		protected virtual void OnOpenedCompleted()
		{
			Presenter.NotifyOpenTransitionCompleted();
		}

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
		protected virtual void OnClosedCompleted()
		{
			Presenter.NotifyCloseTransitionCompleted();
		}

		private async UniTask OpenWithAnimationAsync()
		{
			_currentDelayCompletion = new UniTaskCompletionSource();
			
			OnOpenStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(OpenDelayInSeconds));

			if (this && gameObject)
			{
				OnOpenedCompleted();
			}
			
			_currentDelayCompletion?.TrySetResult();
		}

		private async UniTask CloseWithAnimationAsync()
		{
			_currentDelayCompletion = new UniTaskCompletionSource();
			
			OnCloseStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(CloseDelayInSeconds));

			if (this && gameObject)
			{
				gameObject.SetActive(false);
				OnClosedCompleted();
			}
			
			_currentDelayCompletion?.TrySetResult();
		}
	}
}
