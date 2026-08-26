namespace GameLovers.UiService.Rive
{
	/// <summary>
	/// Selects how a Rive panel obtains its render texture.
	/// </summary>
	public enum UiRenderTargetPolicy
	{
		Dedicated = 0, // Uses one render texture for this panel.
		SharedAtlas = 1, // Packs panels in the same group into one texture.
		Pooled = 2 // Reuses fixed-size textures within the same group.
	}
}
