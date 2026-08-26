namespace GameLovers.UiService
{
	/// <summary>
	/// Identifies the render space occupied by a UI surface.
	/// </summary>
	public enum UiSurfaceSpace
	{
		ScreenOverlay = 0, // Renders directly over camera output.
		ScreenCamera = 1, // Renders through a screen-space camera.
		World = 2 // Renders in world space.
	}
}
