using System.Collections.Generic;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Pure ordering logic for stacking N overlay cameras by priority. Deliberately has zero
	/// engine/URP references so its insert-position math stays testable in isolation -- the
	/// URP-typed caller (<see cref="UiCameraStackFeature"/>) reduces to one Insert call.
	/// </summary>
	public static class UiCameraStackRegistry
	{
		/// <summary>
		/// Returns the index at which an entry with <paramref name="priority"/> should be inserted
		/// into <paramref name="existingPriorities"/> (ascending order; ties insert after existing
		/// entries with the same priority, i.e. first-registered-wins for equal priorities).
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
