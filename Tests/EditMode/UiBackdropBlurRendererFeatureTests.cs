using GameLovers.UiService.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
	/// <summary>
	/// Tests for <see cref="UiBackdropBlurRendererFeature"/>'s runtime iteration override and material
	/// lifecycle. <see cref="UiBackdropBlurRendererFeature.IterationsOverride"/> is a `static` property with
	/// no reference back to any specific asset instance (see its remarks) — this fixture treats that as a
	/// load-bearing design constraint, not an implementation detail.
	/// </summary>
	[TestFixture]
	public class UiBackdropBlurRendererFeatureTests
	{
		private UiBackdropBlurRendererFeature _feature;

		[SetUp]
		public void Setup()
		{
			_feature = ScriptableObject.CreateInstance<UiBackdropBlurRendererFeature>();
		}

		[TearDown]
		public void TearDown()
		{
			// IterationsOverride is a static field shared by every instance and every test in the process;
			// reset it so this fixture cannot leak state into other suites.
			UiBackdropBlurRendererFeature.IterationsOverride = 0;

			if (_feature != null)
			{
				Object.DestroyImmediate(_feature);
			}
		}

		[Test]
		// ADMIT: UiBackdropBlurRendererFeature.IterationsOverride must not write the serialized `_iterations`
		// field — this feature is a ScriptableObject asset, so a runtime write persists to disk in the Editor and
		// silently changes the authored blur look project-wide.
		// RCR: none reachable — the property is static and the instance `[SerializeField] _iterations` is not in
		// scope inside its setter, so no one-line edit can write through to serialized state. That unreachability
		// IS the guarantee; reddening this test requires structurally reintroducing an instance reference. 2026-08-01
		public void IterationsOverride_WhenSet_DoesNotDirtyTheSerializedAsset()
		{
			// Arrange
			var jsonBefore = EditorJsonUtility.ToJson(_feature);

			// Act
			UiBackdropBlurRendererFeature.IterationsOverride = 5;
			var jsonAfter = EditorJsonUtility.ToJson(_feature);

			// Assert - the serialized asset (including its `_iterations` field) is byte-identical
			Assert.AreEqual(jsonBefore, jsonAfter);
		}

		[Test]
		// ADMIT: UiBackdropBlurRendererFeature.IterationsOverride clamps to [MinIterations, MaxIterations];
		// without it an out-of-range override feeds straight into the blur pass's loop count.
		// RCR: UiBackdropBlurRendererFeature.cs IterationsOverride setter — replace
		// `Mathf.Clamp(value, MinIterations, MaxIterations)` with `value` → RED (expected 8, was 20). 2026-08-01
		public void IterationsOverride_AboveMaxIterations_ClampsToMax()
		{
			// Act
			UiBackdropBlurRendererFeature.IterationsOverride = UiBackdropBlurRendererFeature.MaxIterations + 12;

			// Assert
			Assert.AreEqual(UiBackdropBlurRendererFeature.MaxIterations, UiBackdropBlurRendererFeature.IterationsOverride);

			// Act 2 / Assert 2 - same line's `value <= 0 ? 0` branch: zero-or-below clears the override entirely,
			// covered here as a second assertion rather than a second test since both paths are the one clamp line.
			UiBackdropBlurRendererFeature.IterationsOverride = 0;
			Assert.AreEqual(0, UiBackdropBlurRendererFeature.IterationsOverride);
		}

		[Test]
		// ADMIT: UiBackdropBlurRendererFeature.Dispose must call CoreUtils.Destroy(_blurMaterial) before nulling
		// the field, or the material Create() allocated leaks across play sessions when Domain Reload is disabled.
		// RCR: UiBackdropBlurRendererFeature.cs Dispose — comment out `CoreUtils.Destroy(_blurMaterial);` → RED
		// (the captured reference is still alive; `_blurMaterial = null;` alone cannot fake-null it). 2026-08-01
		public void Dispose_AfterCreate_DestroysBlurMaterial()
		{
			// Arrange
			_feature.Create();
			var materialBeforeDispose = _feature.BlurMaterialInternal;
			Assert.IsNotNull(materialBeforeDispose, "Create() should have produced a blur material to dispose.");

			// Act
			_feature.Dispose();

			// Assert - captured BEFORE Dispose so this exercises Unity's fake-null via CoreUtils.Destroy,
			// not merely the field being reassigned to literal null afterwards.
			Assert.IsTrue(materialBeforeDispose == null);
			Assert.IsNull(_feature.BlurMaterialInternal);
		}
	}
}
