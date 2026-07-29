namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Pure decision logic for whether <see cref="UiBackdropBlurPass"/> should run against a given
	/// camera. Extracted to plain bools specifically so the injection matrix -- including every
	/// cross-feature hazard it guards against -- is testable without a URP reference; the
	/// URP-typed caller in <see cref="UiBackdropBlurPass.RecordRenderGraph"/> reduces to one call.
	/// </summary>
	public static class UiBackdropBlurInjection
	{
		/// <param name="isGameCamera">False for Scene view / Preview cameras -- blurring those would
		/// blur the Editor's own Scene view and material preview thumbnails.</param>
		/// <param name="isOverlayCamera">True for a URP camera-stack Overlay camera (see
		/// <see cref="UiCameraStackFeature"/>) -- post-processing-style passes run per camera in a
		/// stack, so injecting here would blur the UI overlay itself, not the world behind it.</param>
		/// <param name="hasTargetTexture">True if the camera renders into a RenderTexture -- a UI camera
		/// authored to capture its Canvas into a texture, for example. Blurring that blurs the UI being
		/// captured, not the world behind it, and costs a full blur chain per frame for no visual benefit.</param>
		/// <param name="anyActiveRequests">True if at least one <see cref="UiBackdropBlurPresenterFeature"/> is currently requesting a blur.</param>
		public static bool ShouldInject(bool isGameCamera, bool isOverlayCamera, bool hasTargetTexture, bool anyActiveRequests)
		{
			return isGameCamera && !isOverlayCamera && !hasTargetTexture && anyActiveRequests;
		}
	}
}
