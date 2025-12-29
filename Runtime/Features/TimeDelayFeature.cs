using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>
	/// Feature that adds time-based delayed opening and closing to a <see cref="UiPresenter"/>.
	/// Configure delays in seconds directly on this component.
	/// </summary>
	public class TimeDelayFeature : PresenterFeatureBase
	{
		[SerializeField, Range(0f, float.MaxValue)] private float _openDelayInSeconds = 0.5f;
		[SerializeField, Range(0f, float.MaxValue)] private float _closeDelayInSeconds = 0.3f;

		private UniTaskCompletionSource _currentDelayCompletion;

		/// <summary>
		/// Gets the delay in seconds before opening the presenter
		/// </summary>
		public float OpenDelayInSeconds => _openDelayInSeconds;

		/// <summary>
		/// Gets the delay in seconds before closing the presenter
		/// </summary>
		public float CloseDelayInSeconds => _closeDelayInSeconds;

		/// <summary>
		/// Gets the UniTask of the current delay process.
		/// This task can be awaited to wait for the current transition to complete.
		/// </summary>
		public UniTask CurrentDelayTask => _currentDelayCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			OpenWithDelayAsync().Forget();
		}

		/// <inheritdoc />
		public override void OnPresenterClosing()
		{
			if (Presenter && Presenter.gameObject)
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
		protected virtual void OnOpenedCompleted()
		{
			Presenter.NotifyOpenTransitionCompleted();
		}

		/// <summary>
		/// Called when the presenter's closing delay starts.
		/// Override this in derived classes to add custom behavior.
		/// </summary>
		protected virtual void OnCloseStarted() { }

		/// <summary>
		/// Called when the presenter's closing delay is completed.
		/// Override this in derived classes to add custom behavior.
		/// </summary>
		protected virtual void OnClosedCompleted()
		{
			Presenter.NotifyCloseTransitionCompleted();
		}

		private async UniTask OpenWithDelayAsync()
		{
			_currentDelayCompletion = new UniTaskCompletionSource();
			
			OnOpenStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(_openDelayInSeconds));

			if (this && gameObject)
			{
				OnOpenedCompleted();
			}
			
			_currentDelayCompletion?.TrySetResult();
		}

		private async UniTask CloseWithDelayAsync()
		{
			_currentDelayCompletion = new UniTaskCompletionSource();
			
			OnCloseStarted();

			await UniTask.Delay(TimeSpan.FromSeconds(_closeDelayInSeconds));

			if (this && gameObject)
			{
				gameObject.SetActive(false);
				OnClosedCompleted();
			}
			
			_currentDelayCompletion?.TrySetResult();
		}
	}
}
