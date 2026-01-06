using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <summary>
	/// Tags the <see cref="UiPresenter"/> as a <see cref="UiPresenterData{T}"/> to allow defining a specific state when
	/// opening the UI via the <see cref="UiService"/>
	/// </summary>
	public interface IUiPresenterData
	{
	}

	/// <summary>
	/// The root base of the UI Presenter of the <seealso cref="IUiService"/>
	/// Implement this abstract class in order to execute the proper UI life cycle
	/// </summary>
	public abstract class UiPresenter : MonoBehaviour
	{
		protected IUiService _uiService;
		private List<PresenterFeatureBase> _features;
		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;

		/// <summary>
		/// The instance address that uniquely identifies this presenter instance.
		/// Empty string for default/singleton instances.
		/// </summary>
		internal string InstanceAddress { get; private set; } = string.Empty;

		/// <summary>
		/// Requests the open status of the <see cref="UiPresenter"/>
		/// </summary>
		public bool IsOpen => gameObject.activeSelf;

		/// <summary>
		/// Task that completes when the open transition finishes.
		/// Useful for external code that needs to await until the UI is fully ready.
		/// </summary>
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <summary>
		/// Task that completes when the close transition finishes.
		/// Useful for external code that needs to await until the UI has fully closed.
		/// </summary>
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <summary>
		/// Allows the ui presenter implementation to directly close the ui presenter without needing to call the service directly
		/// </summary>
		protected void Close(bool destroy)
		{
			_uiService.CloseUi(GetType(), InstanceAddress, destroy);
		}

		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when it is initialized
		/// </summary>
		protected virtual void OnInitialized() {}

		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when it is opened
		/// </summary>
		protected virtual void OnOpened() {}

		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when it is closed
		/// </summary>
		protected virtual void OnClosed() {}

		/// <summary>
		/// Called after all open transitions (animations, delays) have completed.
		/// Override this to react when the UI is fully visible and ready for interaction.
		/// This is always called, even for presenters without transition features.
		/// </summary>
		protected virtual void OnOpenTransitionCompleted() {}

		/// <summary>
		/// Called after all close transitions (animations, delays) have completed.
		/// Override this to react when the UI has finished its closing transition.
		/// This is always called, even for presenters without transition features.
		/// </summary>
		protected virtual void OnCloseTransitionCompleted() {}

		internal void Init(IUiService uiService, string instanceAddress)
		{
			_uiService = uiService;
			InstanceAddress = instanceAddress ?? string.Empty;
			InitializeFeatures();
			OnInitialized();
		}

		internal void InternalOpen()
		{
			InternalOpenProcessAsync().Forget();
		}

		internal void InternalClose(bool destroy)
		{
			InternalCloseProcessAsync(destroy).Forget();
		}

		private async UniTask InternalOpenProcessAsync()
		{
			_openTransitionCompletion = new UniTaskCompletionSource();

			NotifyFeaturesOpening();
			gameObject.SetActive(true);
			OnOpened();
			NotifyFeaturesOpened();

			// Await all feature open transitions
			await WaitForOpenTransitionsAsync();

			// Check if this MonoBehaviour was destroyed during the wait (e.g., during Dispose)
			if (!this)
			{
				_openTransitionCompletion?.TrySetResult();
				return;
			}

			// Always notify - consistent lifecycle for all presenters
			OnOpenTransitionCompleted();

			_openTransitionCompletion.TrySetResult();
		}

		private async UniTask InternalCloseProcessAsync(bool destroy)
		{
			_closeTransitionCompletion = new UniTaskCompletionSource();

			NotifyFeaturesClosing();
			OnClosed();
			NotifyFeaturesClosed();

			// Check if this MonoBehaviour was destroyed (e.g., during Dispose)
			// Using implicit bool conversion which is safe even for destroyed objects
			if (!this)
			{
				_closeTransitionCompletion.TrySetResult();
				return;
			}

			// Await all feature close transitions
			await WaitForCloseTransitionsAsync();

			// Check again after await - object may have been destroyed during the wait
			if (!this)
			{
				_closeTransitionCompletion?.TrySetResult();
				return;
			}

			// Always hide here - single point of responsibility
			gameObject.SetActive(false);

			// Always notify - consistent lifecycle for all presenters
			OnCloseTransitionCompleted();

			_closeTransitionCompletion.TrySetResult();

			if (destroy)
			{
				// UI Service calls the Addressables.UnloadAsset that unloads the asset from the memory and destroys the game object
				_uiService.UnloadUi(GetType(), InstanceAddress);
			}
		}

		private UniTask WaitForOpenTransitionsAsync()
		{
			if (_features == null || _features.Count == 0)
			{
				return UniTask.CompletedTask;
			}

			// Collect all open transition tasks from features that implement ITransitionFeature
			List<UniTask> tasks = null;
			foreach (var feature in _features)
			{
				if (feature is ITransitionFeature transitionFeature)
				{
					var task = transitionFeature.OpenTransitionTask;
					if (task.Status != UniTaskStatus.Succeeded)
					{
						tasks ??= new List<UniTask>();
						tasks.Add(task);
					}
				}
			}

			return tasks != null && tasks.Count > 0 ? UniTask.WhenAll(tasks) : UniTask.CompletedTask;
		}

		private UniTask WaitForCloseTransitionsAsync()
		{
			if (_features == null || _features.Count == 0)
			{
				return UniTask.CompletedTask;
			}

			// Collect all close transition tasks from features that implement ITransitionFeature
			List<UniTask> tasks = null;
			foreach (var feature in _features)
			{
				if (feature is ITransitionFeature transitionFeature)
				{
					var task = transitionFeature.CloseTransitionTask;
					if (task.Status != UniTaskStatus.Succeeded)
					{
						tasks ??= new List<UniTask>();
						tasks.Add(task);
					}
				}

			}

			return tasks != null && tasks.Count > 0 ? UniTask.WhenAll(tasks) : UniTask.CompletedTask;
		}

		private void InitializeFeatures()
		{
			_features = new List<PresenterFeatureBase>();
			GetComponents(_features);

			foreach (var feature in _features)
			{
				feature.OnPresenterInitialized(this);
			}
		}

		private void NotifyFeaturesOpening()
		{
			if (_features == null) return;
			
			foreach (var feature in _features)
			{
				feature.OnPresenterOpening();
			}
		}

		private void NotifyFeaturesOpened()
		{
			if (_features == null) return;
			
			foreach (var feature in _features)
			{
				feature.OnPresenterOpened();
			}
		}

		private void NotifyFeaturesClosing()
		{
			if (_features == null) return;
			
			foreach (var feature in _features)
			{
				feature.OnPresenterClosing();
			}
		}

		private void NotifyFeaturesClosed()
		{
			if (_features == null) return;
			
			foreach (var feature in _features)
			{
				feature.OnPresenterClosed();
			}
		}
	}

	/// <inheritdoc cref="UiPresenter"/>
	/// <remarks>
	/// Extends the <see cref="UiPresenter"/> behaviour to hold data of type <typeparamref name="T"/>
	/// </remarks>
	public abstract class UiPresenter<T> : UiPresenter, IUiPresenterData where T : struct
	{
		private T _data;

		/// <summary>
		/// The Ui data defined when opened via the <see cref="UiService"/>
		/// </summary>
		public T Data
		{
			get => _data;
			set
			{
				_data = value;
				OnSetData();
			}
		}

		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when the data defined for the presenter is set
		/// </summary>
		protected virtual void OnSetData() {}
	}
}

