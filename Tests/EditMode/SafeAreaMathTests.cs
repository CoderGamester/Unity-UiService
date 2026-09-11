using GameLovers.UiService.Views;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
	[TestFixture]
	public sealed class SafeAreaMathTests
	{
		[TestCase(0, 1080)]
		[TestCase(1920, 0)]
		[TestCase(-1920, 1080)]
		[TestCase(1920, -1080)]
		// ADMIT: SafeAreaMath.ComputeNormalizedAnchors must return identity anchors for degenerate extents instead of dividing by zero or a negative span.
		// RCR: SafeAreaMath.cs ComputeNormalizedAnchors — remove the IsMeasurable extent guard → RED (4 cases fail: Infinity/negative anchors vs identity). 2026-09-11
		public void ComputeNormalizedAnchors_DegenerateExtents_ReturnsIdentityAnchors(int width, int height)
		{
			var safeArea = new Rect(120f, 40f, 1680f, 1000f);

			SafeAreaMath.ComputeNormalizedAnchors(safeArea, width, height, out Vector2 anchorMin, out Vector2 anchorMax);

			Assert.AreEqual(Vector2.zero, anchorMin);
			Assert.AreEqual(Vector2.one, anchorMax);
		}

		[TestCase(float.NaN, 0f, 1920f, 1080f)]
		[TestCase(0f, float.PositiveInfinity, 1920f, 1080f)]
		[TestCase(0f, 0f, float.NaN, 1080f)]
		[TestCase(0f, 0f, 1920f, float.NegativeInfinity)]
		// ADMIT: SafeAreaMath.ComputeNormalizedAnchors must return identity anchors for a non-finite safe area instead of propagating NaN into the rect.
		// RCR: SafeAreaMath.cs ComputeNormalizedAnchors — remove the IsFinite safe-area guard → RED (4 cases fail: NaN/Infinity anchors vs identity). 2026-09-11
		public void ComputeNormalizedAnchors_NonFiniteSafeArea_ReturnsIdentityAnchors(float x, float y, float rectWidth, float rectHeight)
		{
			var safeArea = new Rect(x, y, rectWidth, rectHeight);

			SafeAreaMath.ComputeNormalizedAnchors(safeArea, 1920, 1080, out Vector2 anchorMin, out Vector2 anchorMax);

			Assert.AreEqual(Vector2.zero, anchorMin);
			Assert.AreEqual(Vector2.one, anchorMax);
		}

		[TestCase(0, 100)]
		[TestCase(200, 0)]
		[TestCase(-200, 100)]
		[TestCase(200, -100)]
		// ADMIT: SafeAreaMath.ComputeInsets must return zero insets for degenerate extents instead of measuring against a zero or negative screen.
		// RCR: SafeAreaMath.cs ComputeInsets — remove the IsMeasurable extent guard → RED (4 cases fail: 20px insets vs zero). 2026-09-11
		public void ComputeInsets_DegenerateExtents_ReturnsZeroInsets(int width, int height)
		{
			var safeArea = new Rect(20f, 10f, 160f, 80f);

			SafeAreaMath.ComputeInsets(safeArea, width, height, 1f, out float left, out float top, out float right, out float bottom);

			Assert.AreEqual(0f, left);
			Assert.AreEqual(0f, top);
			Assert.AreEqual(0f, right);
			Assert.AreEqual(0f, bottom);
		}

		[TestCase(0f)]
		[TestCase(-2f)]
		[TestCase(float.NaN)]
		[TestCase(float.PositiveInfinity)]
		// ADMIT: SafeAreaMath.ComputeInsets must return zero insets for an unmeasurable panel scale instead of dividing by zero or propagating NaN.
		// RCR: SafeAreaMath.cs ComputeInsets — remove the IsMeasurable scale guard → RED (3 of 4 cases fail; +Inf scale still yields zero). 2026-09-11
		public void ComputeInsets_UnmeasurableScale_ReturnsZeroInsets(float scaledPixelsPerPoint)
		{
			var safeArea = new Rect(20f, 10f, 160f, 80f);

			SafeAreaMath.ComputeInsets(safeArea, 200, 100, scaledPixelsPerPoint, out float left, out float top, out float right, out float bottom);

			Assert.AreEqual(0f, left);
			Assert.AreEqual(0f, top);
			Assert.AreEqual(0f, right);
			Assert.AreEqual(0f, bottom);
		}

		[TestCase(float.NaN, 10f, 160f, 80f)]
		[TestCase(20f, float.PositiveInfinity, 160f, 80f)]
		[TestCase(20f, 10f, float.NaN, 80f)]
		[TestCase(20f, 10f, 160f, float.NegativeInfinity)]
		// ADMIT: SafeAreaMath.ComputeInsets must return zero insets for a non-finite safe area instead of propagating NaN into the panel style.
		// RCR: SafeAreaMath.cs ComputeInsets — remove the IsFinite safe-area guard → RED (4 cases fail: NaN/20px insets vs zero). 2026-09-11
		public void ComputeInsets_NonFiniteSafeArea_ReturnsZeroInsets(float x, float y, float rectWidth, float rectHeight)
		{
			var safeArea = new Rect(x, y, rectWidth, rectHeight);

			SafeAreaMath.ComputeInsets(safeArea, 200, 100, 1f, out float left, out float top, out float right, out float bottom);

			Assert.AreEqual(0f, left);
			Assert.AreEqual(0f, top);
			Assert.AreEqual(0f, right);
			Assert.AreEqual(0f, bottom);
		}

		[Test]
		// ADMIT: SafeAreaMath.ComputeInsets must clamp overscanned edges to zero instead of leaking negative padding into the panel style.
		// RCR: SafeAreaMath.cs ComputeInsets — remove the Mathf.Max clamp on left → RED (OverscannedSafeArea fails: -20px vs zero). 2026-09-11
		public void ComputeInsets_OverscannedSafeArea_ClampsNegativeEdgesToZero()
		{
			var safeArea = new Rect(-20f, -10f, 240f, 120f);

			SafeAreaMath.ComputeInsets(safeArea, 200, 100, 1f, out float left, out float top, out float right, out float bottom);

			Assert.AreEqual(0f, left);
			Assert.AreEqual(0f, top);
			Assert.AreEqual(0f, right);
			Assert.AreEqual(0f, bottom);
		}

		[Test]
		// ADMIT: SafeAreaMath.HasScreenChanged must report no change for identical inputs so polling does not rewrite anchors every frame.
		// RCR: SafeAreaMath.cs HasScreenChanged — replace the comparison with true → RED (IdenticalInputs fails: True vs False). 2026-09-11
		public void HasScreenChanged_IdenticalInputs_ReturnsFalse()
		{
			var safeArea = new Rect(80f, 0f, 1760f, 1080f);

			bool changed = SafeAreaMath.HasScreenChanged(safeArea, 1920, 1080, safeArea, 1920, 1080);

			Assert.IsFalse(changed);
		}
	}
}
