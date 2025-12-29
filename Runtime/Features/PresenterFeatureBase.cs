using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>
	/// Base class for presenter features that can be attached to a <see cref="UiPresenter"/> to extend its functionality.
	/// Inherit from this class to create custom features that hook into the presenter lifecycle.
	/// </summary>
	public abstract class PresenterFeatureBase : MonoBehaviour
	{
		/// <summary>
		/// The presenter that owns this feature
		/// </summary>
		protected UiPresenter Presenter { get; private set; }

		/// <summary>
		/// Called when the presenter is initialized. This is invoked once when the presenter is created.
		/// </summary>
		/// <param name="presenter">The presenter that owns this feature</param>
		public virtual void OnPresenterInitialized(UiPresenter presenter)
		{
			Presenter = presenter;
		}

		/// <summary>
		/// Called when the presenter is being opened. This is invoked before the presenter becomes visible.
		/// </summary>
		public virtual void OnPresenterOpening() { }

		/// <summary>
		/// Called after the presenter has been opened and is now visible.
		/// </summary>
		public virtual void OnPresenterOpened() { }

		/// <summary>
		/// Called when the presenter is being closed. This is invoked before the presenter becomes hidden.
		/// </summary>
		public virtual void OnPresenterClosing() { }

		/// <summary>
		/// Called after the presenter has been closed and is no longer visible.
		/// </summary>
		public virtual void OnPresenterClosed() { }
	}
}

