namespace GameLovers.UiService
{
	/// <summary>
	/// Identifies the hierarchy location used to instantiate a presenter.
	/// </summary>
	public enum UiPlacementSpace
	{
		ScreenRoot = 0, // Uses the service-owned screen hierarchy.
		WorldRoot = 1, // Uses the service-owned world hierarchy.
		FollowTarget = 2 // Uses a caller-supplied runtime target.
	}
}
