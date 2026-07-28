using UnityEngine;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Resolves the "base" URP camera that an overlay UI camera should stack onto.
	/// Swappable because <see cref="Camera.main"/> (tag-based lookup) is the wrong answer in
	/// plenty of games -- multiple cameras, no "MainCamera" tag, split-screen, etc.
	/// </summary>
	public interface IUiBaseCameraProvider
	{
		/// <summary>Returns the current base camera, or null if none is available right now.</summary>
		Camera GetBaseCamera();
	}
}
