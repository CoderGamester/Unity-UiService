using UnityEngine;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Carries the stacking priority of an overlay camera for as long as it stays inserted in a
	/// base camera's <c>UniversalAdditionalCameraData.cameraStack</c>. URP's stack is a plain
	/// <see cref="System.Collections.Generic.List{T}"/> of cameras with no priority of its own,
	/// so ordering a newly-inserted camera against already-stacked ones needs this recorded
	/// somewhere -- attaching it to the camera GameObject keeps that state scoped to the
	/// camera's own lifetime (destroyed with it, no separate registry to leak or clean up).
	/// </summary>
	internal class UiCameraStackPriorityMarker : MonoBehaviour
	{
		public int Priority;
	}
}
