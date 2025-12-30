using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Test presenter for layer testing (on a different layer)
	/// </summary>
	public class TestLayerUiPresenter : UiPresenter
	{
		public int AssignedLayer { get; private set; }

		protected override void OnInitialized()
		{
			// Canvas sorting order reflects the layer
			var canvas = GetComponent<Canvas>();
			if (canvas != null)
			{
				AssignedLayer = canvas.sortingOrder;
			}
		}
	}
}

