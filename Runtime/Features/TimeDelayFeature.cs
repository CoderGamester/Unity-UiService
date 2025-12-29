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

		/// <summary>
		/// Gets the delay in seconds before opening the presenter
		/// </summary>
		public float OpenDelayInSeconds => _openDelayInSeconds;

		/// <summary>
		/// Gets the delay in seconds before closing the presenter
		/// </summary>
		public float CloseDelayInSeconds => _closeDelayInSeconds;

		/// <summary>
		/// Gets the UniTask of the current delay process
		/// </summary>
		public UniTask CurrentDelayTask { get; private set; }

		/// <summary>
		/// Event triggered when the opening delay is completed
		/// </summary>
		internal event Action OnOpenCompletedEvent;

		/// <summary>
		/// Event triggered when the closing delay is completed
		/// </summary>
		internal event Action OnCloseCompletedEvent;

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
			OnOpenCompletedEvent?.Invoke();
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
			OnCloseCompletedEvent?.Invoke();
		}

		private async UniTask OpenWithDelayAsync()
		{
			OnOpenStarted();

			CurrentDelayTask = UniTask.Delay(TimeSpan.FromSeconds(_openDelayInSeconds));
			await CurrentDelayTask;

			if (gameObject != null)
			{
				OnOpenedCompleted();
			}
		}

		private async UniTask CloseWithDelayAsync()
		{
			OnCloseStarted();

			CurrentDelayTask = UniTask.Delay(TimeSpan.FromSeconds(_closeDelayInSeconds));
			await CurrentDelayTask;

			if (gameObject != null)
			{
				gameObject.SetActive(false);
				OnClosedCompleted();
			}
		}
	}
}
