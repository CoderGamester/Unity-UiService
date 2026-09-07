using GameLovers.UiService.Views;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
	[TestFixture]
	public sealed class SafeAreaViewTests
	{
		[Test]
		// ADMIT: SafeAreaContainer.ComputeInsets must flip the bottom-origin safe area into top-origin UI Toolkit padding and divide every edge by the panel scale.
		// A2: SafeAreaContainer.cs ComputeInsets — remove the scaledPixelsPerPoint division from `top` and this test fails.
		public void ComputeInsets_NotchedLandscape_MapsAllEdgesAtPanelScale()
		{
			var safeArea = new Rect(120f, 40f, 1680f, 1000f);

			SafeAreaContainer.ComputeInsets(safeArea, 1920, 1080, 2f, out float left, out float top, out float right, out float bottom);

			Assert.AreEqual(60f, left);
			Assert.AreEqual(20f, top);
			Assert.AreEqual(60f, right);
			Assert.AreEqual(20f, bottom);
		}

		[Test]
		// ADMIT: SafeAreaContainer.Refresh must clear stale inline padding outside Play Mode so UI Builder and Edit Mode previews remain uninset.
		// A2: SafeAreaContainer.cs Refresh — delete `ApplyInsets(0f, 0f, 0f, 0f)` from the preview branch and this test leaves the authored padding in place.
		public void Refresh_EditMode_ClearsPreviewInsets()
		{
			var container = new SafeAreaContainer();
			container.style.paddingLeft = 31f;
			container.style.paddingTop = 29f;

			container.Refresh();

			Assert.AreEqual(0f, container.style.paddingLeft.value.value);
			Assert.AreEqual(0f, container.style.paddingTop.value.value);
		}

		[Test]
		// ADMIT: SafeAreaContainer.ApplySafeArea must skip identical computed insets so polling does not repeatedly overwrite inline style values.
		// A2: SafeAreaContainer.cs ApplyInsets — delete the cached-insets early return and this test resets paddingLeft from 9 to 20.
		public void ApplySafeArea_UnchangedInsets_DoesNotRewriteStyles()
		{
			var container = new SafeAreaContainer();
			var safeArea = new Rect(20f, 10f, 160f, 80f);
			container.ApplySafeArea(safeArea, 200, 100);
			container.style.paddingLeft = 9f;

			container.ApplySafeArea(safeArea, 200, 100);

			Assert.AreEqual(9f, container.style.paddingLeft.value.value);
		}

		[Test]
		// ADMIT: SafeAreaPanelView.ComputeNormalizedAnchors must map all four safe-area edges into full-screen normalized anchors.
		// A2: SafeAreaPanelView.cs ComputeNormalizedAnchors — use safeArea.xMax for anchorMin.x and this test fails.
		public void ComputeNormalizedAnchors_NotchedLandscape_MapsAllFourEdges()
		{
			var safeArea = new Rect(141f, 63f, 2496f, 1221f);

			SafeAreaPanelView.ComputeNormalizedAnchors(safeArea, 2778, 1284, out Vector2 anchorMin, out Vector2 anchorMax);

			Assert.AreEqual(141f / 2778f, anchorMin.x);
			Assert.AreEqual(63f / 1284f, anchorMin.y);
			Assert.AreEqual(2637f / 2778f, anchorMax.x);
			Assert.AreEqual(1f, anchorMax.y);
		}

		[Test]
		// ADMIT: SafeAreaPanelView.HasScreenChanged must notice a safe-area change when resolution is unchanged, or runtime polling misses simulator posture changes.
		// A2: SafeAreaPanelView.cs HasScreenChanged — remove `safeArea != appliedSafeArea` and this test returns false.
		public void HasScreenChanged_SameResolutionDifferentSafeArea_ReturnsTrue()
		{
			var previous = new Rect(0f, 0f, 1920f, 1080f);
			var current = new Rect(80f, 0f, 1760f, 1080f);

			bool changed = SafeAreaPanelView.HasScreenChanged(current, 1920, 1080, previous, 1920, 1080);

			Assert.IsTrue(changed);
		}
	}
}
