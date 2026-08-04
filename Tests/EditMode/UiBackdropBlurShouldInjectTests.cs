using GameLovers.UiService.Rendering;
using NUnit.Framework;

namespace GameLovers.UiService.Tests
{
	/// <summary>
	/// The injection matrix takes plain bools on purpose, so every cross-feature rejection is covered
	/// here without this assembly needing a URP reference.
	/// </summary>
	[TestFixture]
	public class UiBackdropBlurShouldInjectTests
	{
		[Test]
		// ADMIT: UiBackdropBlurPass.ShouldInject must return true for the one accepting combination; a stray
		// rejection there disables the blur entirely on the normal game camera.
		// RCR: UiBackdropBlurPass.cs ShouldInject — replace the conjunction with `return false;` → RED (expected
		// True, was False). The five rejection siblings all assert False and stay green. 2026-08-02
		public void GameCamera_NotOverlay_NoTargetTexture_PresenterOpen_Injects()
		{
			Assert.IsTrue(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: false, anyPresenterOpen: true));
		}

		[Test]
		// ADMIT: UiBackdropBlurPass.ShouldInject's `anyPresenterOpen` conjunct is what keeps the blur pass out of
		// every frame when no presenter is holding a backdrop open.
		// RCR: UiBackdropBlurPass.cs ShouldInject — drop `&& anyPresenterOpen` → RED (expected False, was True).
		// Each sibling rejection trips a different conjunct, so all of them stay green. 2026-08-02
		public void NoPresenterOpen_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: false, anyPresenterOpen: false));
		}

		[Test]
		// ADMIT: UiBackdropBlurPass.ShouldInject's `isGameCamera` conjunct keeps the blur out of Scene view and
		// material-preview cameras, which have no UI to sit behind.
		// RCR: UiBackdropBlurPass.cs ShouldInject — drop `isGameCamera &&` → RED (expected False, was True).
		// Siblings each trip a different conjunct and stay green. 2026-08-02
		public void SceneOrPreviewCamera_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: false, isOverlayCamera: false, hasTargetTexture: false, anyPresenterOpen: true));
		}

		[Test]
		// ADMIT: UiBackdropBlurPass.ShouldInject's `!isOverlayCamera` conjunct stops the pass running once per
		// stacked overlay camera, which would blur the already-blurred base result N times.
		// RCR: UiBackdropBlurPass.cs ShouldInject — drop `!isOverlayCamera &&` → RED (expected False, was True).
		// Siblings each trip a different conjunct and stay green. 2026-08-02
		public void StackedOverlayCamera_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: true, hasTargetTexture: false, anyPresenterOpen: true));
		}

		[Test]
		// ADMIT: UiBackdropBlurPass.ShouldInject's `!hasTargetTexture` conjunct excludes cameras rendering into
		// their own RenderTexture, whose target the pass cannot sample as a backdrop.
		// RCR: UiBackdropBlurPass.cs ShouldInject — drop `!hasTargetTexture &&` → RED (expected False, was True).
		// Siblings each trip a different conjunct and stay green. 2026-08-02
		public void RenderTextureTargetCamera_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: true, anyPresenterOpen: true));
		}

		[Test]
		// ADMIT: UiBackdropBlurPass.ShouldInject must still reject when every rejection reason applies at once.
		// RCR: none exists — this input trips all four conjuncts, so deleting any one leaves the other three
		// returning false (verified against each of the four single-conjunct deletions the siblings above use).
		// Quadruple-covered, not single-line falsifiable. 2026-08-02
		public void EveryRejectionCombined_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: false, isOverlayCamera: true, hasTargetTexture: true, anyPresenterOpen: false));
		}
	}
}
