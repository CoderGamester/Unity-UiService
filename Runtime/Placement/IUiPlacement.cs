using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>
	/// Resolves the hierarchy parent used to instantiate a presenter.
	/// </summary>
	public interface IUiPlacement
	{
		/// <summary>
		/// Resolves a configured placement to a runtime parent.
		/// </summary>
		/// <param name="placement">The configured placement policy.</param>
		/// <param name="followTarget">The runtime target required by follow-target placement.</param>
		/// <returns>The parent under which the presenter should be instantiated.</returns>
		Transform Resolve(UiPlacementSpace placement, Transform followTarget);
	}
}
