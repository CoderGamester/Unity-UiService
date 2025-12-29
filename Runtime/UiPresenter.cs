using System.Collections.Generic;
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

		/// <summary>
		/// Requests the open status of the <see cref="UiPresenter"/>
		/// </summary>
		public bool IsOpen => gameObject.activeSelf;

		/// <summary>
		/// Allows the ui presenter implementation to directly close the ui presenter without needing to call the service directly
		/// </summary>
		protected void Close(bool destroy)
		{
			_uiService.CloseUi(this, destroy);
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
		/// </summary>
		protected virtual void OnOpenTransitionCompleted() {}

		/// <summary>
		/// Called after all close transitions (animations, delays) have completed.
		/// Override this to react when the UI has finished its closing transition.
		/// </summary>
		protected virtual void OnCloseTransitionCompleted() {}

		internal void Init(IUiService uiService)
		{
			_uiService = uiService;
			InitializeFeatures();
			OnInitialized();
		}

		internal void InternalOpen()
		{
			NotifyFeaturesOpening();
			
			gameObject.SetActive(true);

			OnOpened();
			NotifyFeaturesOpened();
		}

		internal virtual void InternalClose(bool destroy)
		{
			NotifyFeaturesClosing();
			OnClosed();
			NotifyFeaturesClosed();

			if (gameObject == null)
			{
				return;
			}

			if (destroy)
			{
				// UI Service calls the Addressables.UnloadAsset that unloads the asset from the memory and destroys the game object
				_uiService.UnloadUi(GetType());
			}
			else
			{
				gameObject.SetActive(false);
			}
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

		/// <summary>
		/// Called by features to notify that their open transition has completed.
		/// This triggers <see cref="OnOpenTransitionCompleted"/>.
		/// </summary>
		internal void NotifyOpenTransitionCompleted()
		{
			OnOpenTransitionCompleted();
		}

		/// <summary>
		/// Called by features to notify that their close transition has completed.
		/// This triggers <see cref="OnCloseTransitionCompleted"/>.
		/// </summary>
		internal void NotifyCloseTransitionCompleted()
		{
			OnCloseTransitionCompleted();
		}
	}

	/// <inheritdoc cref="UiPresenter"/>
	/// <remarks>
	/// Extends the <see cref="UiPresenter"/> behaviour to hold data of type <typeparamref name="T"/>
	/// </remarks>
	public abstract class UiPresenter<T> : UiPresenter, IUiPresenterData where T : struct
	{
		/// <summary>
		/// The Ui data defined when opened via the <see cref="UiService"/>
		/// </summary>
		public T Data { get; protected set; }

		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when the data defined for the presenter is set
		/// </summary>
		protected virtual void OnSetData() {}

		internal void InternalSetData(T data)
		{
			Data = data;

			OnSetData();
		}
	}
}