using GameLovers.UiService.Views;
using NUnit.Framework;

namespace GameLovers.UiService.Tests
{
	[TestFixture]
	public sealed class SurfaceOrientationTests
	{
		[Test]
		// ADMIT: SurfaceOrientationResolver.Resolve must return Landscape for an automatic wider-than-tall surface.
		// RCR: SurfaceOrientation.cs Resolve — replace the automatic return with Portrait → RED (WiderThanTall fails: Portrait vs Landscape). 2026-09-11
		public void Resolve_AutomaticWiderThanTall_ReturnsLandscape()
		{
			SurfaceOrientation orientation = SurfaceOrientationResolver.Resolve(SurfaceOrientationMode.Automatic, 1200f, 800f);

			Assert.AreEqual(SurfaceOrientation.Landscape, orientation);
		}

		[Test]
		// ADMIT: SurfaceOrientationResolver.Resolve must return Portrait for an automatic taller-than-wide surface.
		// RCR: SurfaceOrientation.cs Resolve — replace width > height with width != height → RED (TallerThanWide fails: Landscape vs Portrait). 2026-09-11
		public void Resolve_AutomaticTallerThanWide_ReturnsPortrait()
		{
			SurfaceOrientation orientation = SurfaceOrientationResolver.Resolve(SurfaceOrientationMode.Automatic, 800f, 1200f);

			Assert.AreEqual(SurfaceOrientation.Portrait, orientation);
		}

		[Test]
		// ADMIT: SurfaceOrientationResolver.Resolve must return Portrait for an automatic square surface instead of flipping to Landscape.
		// RCR: SurfaceOrientation.cs Resolve — replace width > height with width >= height → RED (Square fails: Landscape vs Portrait). 2026-09-11
		public void Resolve_AutomaticSquare_ReturnsPortrait()
		{
			SurfaceOrientation orientation = SurfaceOrientationResolver.Resolve(SurfaceOrientationMode.Automatic, 800f, 800f);

			Assert.AreEqual(SurfaceOrientation.Portrait, orientation);
		}

		[TestCase(SurfaceOrientationMode.ForcePortrait, 1200f, 800f, SurfaceOrientation.Portrait)]
		[TestCase(SurfaceOrientationMode.ForceLandscape, 800f, 1200f, SurfaceOrientation.Landscape)]
		// ADMIT: SurfaceOrientationResolver.Resolve must honor forced modes regardless of the measured surface shape.
		// RCR: SurfaceOrientation.cs Resolve — flip the ForcePortrait early return to Landscape → RED (ForcedMode ForcePortrait case fails). 2026-09-11
		public void Resolve_ForcedMode_IgnoresSurface(SurfaceOrientationMode mode, float width, float height, SurfaceOrientation expected)
		{
			SurfaceOrientation orientation = SurfaceOrientationResolver.Resolve(mode, width, height);

			Assert.AreEqual(expected, orientation);
		}

		[TestCase(float.NaN, 800f)]
		[TestCase(800f, float.NaN)]
		[TestCase(float.PositiveInfinity, 800f)]
		[TestCase(0f, 800f)]
		[TestCase(-800f, 800f)]
		// ADMIT: SurfaceOrientationResolver.Resolve must return Portrait for an automatic unmeasurable surface instead of following a bogus comparison.
		// RCR: SurfaceOrientation.cs Resolve — remove the IsMeasurable guards → RED (UnmeasurableSurface +Infinity case fails: Landscape vs Portrait). 2026-09-11
		public void Resolve_AutomaticUnmeasurableSurface_ReturnsPortrait(float width, float height)
		{
			SurfaceOrientation orientation = SurfaceOrientationResolver.Resolve(SurfaceOrientationMode.Automatic, width, height);

			Assert.AreEqual(SurfaceOrientation.Portrait, orientation);
		}
	}
}
