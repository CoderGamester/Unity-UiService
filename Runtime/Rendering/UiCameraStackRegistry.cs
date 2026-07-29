using System.Collections.Generic;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Ordering maths for stacking overlay cameras by priority.
	/// </summary>
	/// <remarks>
	/// Kept separate from <see cref="UiCameraStackFeature"/> and free of URP types so it stays testable
	/// in EditMode, where no URP reference is available.
	/// </remarks>
	public static class UiCameraStackRegistry
	{
		/// <summary>
		/// Returns the ascending insert position for <paramref name="priority"/>, placing ties after
		/// existing entries so the first registered stays lower in the stack.
		/// </summary>
		public static int InsertIndex(IReadOnlyList<int> existingPriorities, int priority)
		{
			var index = 0;

			for (; index < existingPriorities.Count; index++)
			{
				if (existingPriorities[index] > priority)
				{
					break;
				}
			}

			return index;
		}
	}
}
