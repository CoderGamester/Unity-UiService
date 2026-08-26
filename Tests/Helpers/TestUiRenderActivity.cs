using UnityEngine;

namespace GameLovers.UiService.Tests
{
	public sealed class TestUiRenderActivity : MonoBehaviour, IUiRenderActivity
	{
		public bool IsRenderActive { get; private set; } = true;
		public int ChangeCount { get; private set; }

		public void SetRenderActive(bool isActive)
		{
			IsRenderActive = isActive;
			ChangeCount++;
		}
	}
}
