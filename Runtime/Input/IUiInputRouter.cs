namespace GameLovers.UiService
{
	/// <summary>
	/// Establishes input prerequisites for a presenter and its resolved visual surface.
	/// </summary>
	public interface IUiInputRouter
	{
		/// <summary>
		/// Prepares the Unity input path used by a loaded presenter.
		/// </summary>
		/// <param name="presenter">The presenter whose input path is being prepared.</param>
		/// <param name="surface">The presenter's resolved visual surface.</param>
		void Prepare(UiPresenter presenter, IUiSurface surface);
	}
}
