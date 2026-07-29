using UnityEngine;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Tunable parameters for one <see cref="UiBackdropBlurPresenterFeature"/> request.
	/// </summary>
	public readonly struct UiBackdropBlurSettings
	{
		public readonly int Downsample; // Power-of-two step: 0 = full res, 1 = half, 2 = quarter. Clamped to [0, 3]
		public readonly int Iterations; // Blur passes at the downsampled resolution. At least 1
		public readonly float Spread; // Sample offset in texels, per iteration. At least 0
		public readonly Color Tint; // Multiplied against the blurred result

		public UiBackdropBlurSettings(int downsample, int iterations, float spread, Color tint)
		{
			Downsample = Mathf.Clamp(downsample, 0, 3);
			Iterations = Mathf.Max(1, iterations);
			Spread = Mathf.Max(0f, spread);
			Tint = tint;
		}

		/// <summary>Half resolution, two iterations, untinted.</summary>
		public static UiBackdropBlurSettings Default => new(1, 2, 1f, Color.white);
	}
}
