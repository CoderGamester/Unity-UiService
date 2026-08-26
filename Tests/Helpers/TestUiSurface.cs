using UnityEngine;

namespace GameLovers.UiService.Tests
{
	public sealed class TestUiSurface : MonoBehaviour, IUiSurface
	{
		public UiSurfaceSpace SurfaceSpace { get; set; }
		public int Order { get; private set; }

		public void ApplyOrder(int order)
		{
			Order = order;
		}
	}
}
