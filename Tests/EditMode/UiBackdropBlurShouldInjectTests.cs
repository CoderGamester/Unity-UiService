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
		public void GameCamera_NotOverlay_NoTargetTexture_PresenterOpen_Injects()
		{
			Assert.IsTrue(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: false, anyPresenterOpen: true));
		}

		[Test]
		public void NoPresenterOpen_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: false, anyPresenterOpen: false));
		}

		[Test]
		public void SceneOrPreviewCamera_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: false, isOverlayCamera: false, hasTargetTexture: false, anyPresenterOpen: true));
		}

		[Test]
		public void StackedOverlayCamera_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: true, hasTargetTexture: false, anyPresenterOpen: true));
		}

		[Test]
		public void RenderTextureTargetCamera_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: true, anyPresenterOpen: true));
		}

		[Test]
		public void EveryRejectionCombined_DoesNotInject()
		{
			Assert.IsFalse(UiBackdropBlurPass.ShouldInject(
				isGameCamera: false, isOverlayCamera: true, hasTargetTexture: true, anyPresenterOpen: false));
		}
	}
}
