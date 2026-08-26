namespace GameLovers.UiService
{
	/// <summary>
	/// Describes the render space and ordering behavior of a UI presentation backend.
	/// </summary>
	public interface IUiSurface
	{
		/// <summary>Gets the render space occupied by this surface.</summary>
		UiSurfaceSpace SurfaceSpace { get; }

		/// <summary>Gets the current backend order.</summary>
		int Order { get; }

		/// <summary>
		/// Applies the service order to this surface.
		/// </summary>
		/// <param name="order">The order configured for the presenter.</param>
		void ApplyOrder(int order);
	}
}
