using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>
	/// Feature that adds time-based delayed opening and closing to a <see cref="UiPresenter"/>.
	/// Configure delays in seconds directly on this component.
	/// Implements <see cref="ITransitionFeature"/> to allow the presenter to await transitions.
	/// </summary>
	public class TimeDelayFeature : PresenterFeatureBase, ITransitionFeature
	{
		[SerializeField, Range(0f, float.MaxValue)] private float _openDelayInSeconds = 0.5f;
		[SerializeField, Range(0f, float.MaxValue)] private float _closeDelayInSeconds = 0.3f;

		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;

		/// <summary>
		/// Gets the delay in seconds before opening the presenter
		/// </summary>
		public float OpenDelayInSeconds => _openDelayInSeconds;

		/// <summary>
		/// Gets the delay in seconds before closing the presenter
		/// </summary>
		public float CloseDelayInSeconds => _closeDelayInSeconds;

		/// <inheritdoc />
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			if (_openDelayInSeconds > 0)
			{
				OpenWithDelayAsync().Forget();
			}
		}

		/// <inheritdoc />
		public override void OnPresenterClosing()
		{
			if (_closeDelayInSeconds > 0 && Presenter && Presenter.gameObject)
			{
				CloseWithDelayAsync().Forget();
			}
		}

		/// <summary>
		/// Called when the presenter's opening delay starts.
		/// Override this in derived classes to add custom behavior.
		/// </summary>
		protected virtual void OnOpenStarted() { }

		/// <summary>
		/// Called when the presenter's opening delay is completed.
		/// Override this in derived classes to add custom behavior.
		/// </summary>
		protected virtual void OnOpenedCompleted() { }

		/// <summary>
		/// Called when the presenter's closing delay starts.
		/// Override this in derived classes to add custom behavior.
		/// </summary>
		protected virtual void OnCloseStarted() { }

		/// <summary>
		/// Called when the presenter's closing delay is completed.
		/// Override this in derived classes to add custom behavior.
		/// </summary>
		protected virtual void OnClosedCompleted() { }

		private async UniTask OpenWithDelayAsync()
		{
			_openTransitionCompletion = new UniTaskCompletionSource();
			
			OnOpenStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(_openDelayInSeconds));

			if (this && gameObject)
			{
				OnOpenedCompleted();
			}
			
			_openTransitionCompletion?.TrySetResult();
		}

		private async UniTask CloseWithDelayAsync()
		{
			_closeTransitionCompletion = new UniTaskCompletionSource();
			
			OnCloseStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(_closeDelayInSeconds));

			if (this && gameObject)
			{
				// Note: Visibility (SetActive) is now handled by UiPresenter after all transitions complete
				OnClosedCompleted();
			}
			
			_closeTransitionCompletion?.TrySetResult();
		}
	}
}
