using System;

namespace GameLovers.UiService.Rive
{
	/// <summary>
	/// Identifies the data type expected at a Rive view-model path.
	/// </summary>
	public enum RivePropertyType
	{
		Number = 0, // A floating-point value.
		Boolean = 1, // A true-or-false value.
		String = 2, // A text value.
		Color = 3, // A color value.
		Trigger = 4 // A trigger action.
	}

	/// <summary>
	/// Declares a Rive property path for edit-time validation.
	/// </summary>
	[Serializable]
	public struct RivePropertyReference
	{
		/// <summary>Gets the path authored in the Rive view model.</summary>
		public string Path;

		/// <summary>Gets the expected property type.</summary>
		public RivePropertyType Type;
	}
}
