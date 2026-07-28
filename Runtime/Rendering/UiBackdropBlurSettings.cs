using UnityEngine;

namespace GameLovers.UiService.Rendering
{
	/// <summary>Tunable parameters for one <see cref="UiBackdropBlurPresenterFeature"/> request.</summary>
	public readonly struct UiBackdropBlurSettings
	{
		/// <summary>Downsample power-of-two step (0 = full res, 1 = half res, 2 = quarter res, ...). Clamped to [0, 3].</summary>
		public readonly int Downsample;

		/// <summary>Number of blur passes applied at the downsampled resolution. At least 1.</summary>
		public readonly int Iterations;

		/// <summary>Sample offset spread in texels, per iteration. At least 0.</summary>
		public readonly float Spread;

		/// <summary>Multiplied against the blurred result -- e.g. a translucent dark tint behind a modal.</summary>
		public readonly Color Tint;

		public UiBackdropBlurSettings(int downsample, int iterations, float spread, Color tint)
		{
			Downsample = Mathf.Clamp(downsample, 0, 3);
			Iterations = Mathf.Max(1, iterations);
			Spread = Mathf.Max(0f, spread);
			Tint = tint;
		}

		public static UiBackdropBlurSettings Default => new(1, 2, 1f, Color.white);
	}
}
