using UnityEngine;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService.Views
{
	/// <summary>
	/// A concrete subclass of the Unity UI `Graphic` class that just skips drawing.
	/// Useful for providing a raycast target without actually drawing anything.
	/// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
	public class NonDrawingView : Graphic
	{
		/// <inheritdoc />
		public override void SetMaterialDirty() { }

		/// <inheritdoc />
		public override void SetVerticesDirty() { }
		
		/// <summary>
		/// Probably not necessary since the chain of calls
		/// `Rebuild()`->`UpdateGeometry()`->`DoMeshGeneration()`->`OnPopulateMesh()` won't happen.
		/// So here really just as a fail-safe.
		/// </summary>
		protected override void OnPopulateMesh(VertexHelper vh) 
		{
			vh.Clear();
		}
	}
}