namespace GameLovers.UiService
{
	/// <summary>
	/// Controls backend-specific render work without coupling presenter features to a rendering implementation.
	/// </summary>
	public interface IUiRenderActivity
	{
		/// <summary>
		/// Starts or stops render work for the associated UI.
		/// </summary>
		/// <param name="isActive">Whether render work should be active.</param>
		void SetRenderActive(bool isActive);
	}
}
