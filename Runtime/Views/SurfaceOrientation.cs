// ReSharper disable CheckNamespace

namespace GameLovers.UiService.Views
{
	/// <summary>
	/// Selects which arrangement a safe-area screen presents.
	/// </summary>
	public enum SurfaceOrientationMode
	{
		Automatic, // follow the measured surface
		ForcePortrait, // always stack, regardless of the surface
		ForceLandscape // always use side-by-side columns, regardless of the surface
	}

	/// <summary>
	/// Identifies the arrangement a safe-area screen presents for the surface it occupies.
	/// </summary>
	public enum SurfaceOrientation
	{
		Portrait, // stack content in one column
		Landscape // arrange content in side-by-side columns
	}

	/// <summary>
	/// Resolves the safe-area arrangement from a configured mode and the measured surface.
	/// </summary>
	public static class SurfaceOrientationResolver
	{
		/// <summary>
		/// Resolves the arrangement for a surface of the supplied size, treating an unmeasurable surface as portrait.
		/// </summary>
		public static SurfaceOrientation Resolve(SurfaceOrientationMode mode, float width, float height)
		{
			if (mode == SurfaceOrientationMode.ForcePortrait)
			{
				return SurfaceOrientation.Portrait;
			}

			if (mode == SurfaceOrientationMode.ForceLandscape)
			{
				return SurfaceOrientation.Landscape;
			}

			// Arrangement follows surface shape, not Screen.orientation, so tablets, foldables, and split-view resolve correctly.
			bool isWiderThanTall = SafeAreaMath.IsMeasurable(width) && SafeAreaMath.IsMeasurable(height) && width > height;
			return isWiderThanTall ? SurfaceOrientation.Landscape : SurfaceOrientation.Portrait;
		}
	}
}
