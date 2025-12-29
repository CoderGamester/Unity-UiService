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
		public float OpenDelayInSeconds => _introAnimationClip?.length ?? 0f;

		/// <summary>
		/// Gets the delay in seconds for closing (based on outro animation length)
		/// </summary>
		public float CloseDelayInSeconds => _outroAnimationClip?.length ?? 0f;

		/// <summary>
		/// Gets the UniTask of the current delay process
		/// </summary>
		public UniTask CurrentDelayTask { get; private set; }

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
			if (!_introAnimationClip)
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
			if (!_outroAnimationClip)
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
			OnOpenStarted();

			CurrentDelayTask = UniTask.Delay(TimeSpan.FromSeconds(OpenDelayInSeconds));
			await CurrentDelayTask;

			if (gameObject != null)
			{
				OnOpenedCompleted();
			}
		}

		private async UniTask CloseWithAnimationAsync()
		{
			OnCloseStarted();

			CurrentDelayTask = UniTask.Delay(TimeSpan.FromSeconds(CloseDelayInSeconds));
			await CurrentDelayTask;

			if (gameObject != null)
			{
				gameObject.SetActive(false);
				OnClosedCompleted();
			}
		}
	}
}
