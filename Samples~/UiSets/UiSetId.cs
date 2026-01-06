namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Define your UI Set IDs as an enum for type safety.
	/// This enum is used by the UiService to identify UI sets and by the editor to display set names.
	/// </summary>
	public enum UiSetId
	{
		/// <summary>
		/// The main game HUD containing health bar, currency display, etc.
		/// </summary>
		GameHud = 0,

		/// <summary>
		/// The pause menu UI set
		/// </summary>
		PauseMenu = 1,

		/// <summary>
		/// The settings panel UI set
		/// </summary>
		SettingsPanel = 2
	}
}

