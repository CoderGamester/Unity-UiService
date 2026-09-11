using GameLovers.UiService.Views;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
	[TestFixture]
	public sealed class SafeAreaPanelViewTests
	{
		[Test]
		// ADMIT: SafeAreaMath.ComputeNormalizedAnchors must map all four safe-area edges, not a stretched-path xMin/yMax pair.
		// RCR: SafeAreaMath.cs ComputeNormalizedAnchors — use xMax for anchorMin.x → RED (Expected: 0.0507559404f But was: 0.949244082f). 2026-09-01
		public void ComputeNormalizedAnchors_NotchedLandscape_MapsAllFourEdges()
		{
			var safeArea = new Rect(141f, 63f, 2496f, 1221f);
			const int width = 2778;
			const int height = 1284;

			SafeAreaMath.ComputeNormalizedAnchors(safeArea, width, height, out Vector2 anchorMin, out Vector2 anchorMax);

			Assert.AreEqual(141f / width, anchorMin.x);
			Assert.AreEqual(63f / height, anchorMin.y);
			Assert.AreEqual(2637f / width, anchorMax.x);
			Assert.AreEqual(1f, anchorMax.y);
		}
	}
}
