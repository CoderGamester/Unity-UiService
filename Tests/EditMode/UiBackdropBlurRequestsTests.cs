using GameLovers.UiService.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
	/// <summary>
	/// Pure registry tests -- no URP or rendering types touched, even though the assembly this
	/// type lives in (GameLovers.UiService.Urp) references URP.
	/// </summary>
	[TestFixture]
	public class UiBackdropBlurRequestsTests
	{
		private readonly object _ownerA = new();
		private readonly object _ownerB = new();
		private readonly object _ownerC = new();

		[TearDown]
		public void TearDown()
		{
			UiBackdropBlurRequests.Unregister(_ownerA);
			UiBackdropBlurRequests.Unregister(_ownerB);
			UiBackdropBlurRequests.Unregister(_ownerC);
		}

		[Test]
		public void NoRequests_TryGetActive_ReturnsFalse()
		{
			Assert.AreEqual(0, UiBackdropBlurRequests.ActiveCount);
			Assert.IsFalse(UiBackdropBlurRequests.TryGetActive(out _));
		}

		[Test]
		public void Register_IncrementsActiveCount()
		{
			UiBackdropBlurRequests.Register(_ownerA, UiBackdropBlurSettings.Default, 0);

			Assert.AreEqual(1, UiBackdropBlurRequests.ActiveCount);
		}

		[Test]
		public void Register_SameOwnerTwice_DoesNotDoubleCount()
		{
			UiBackdropBlurRequests.Register(_ownerA, UiBackdropBlurSettings.Default, 0);
			UiBackdropBlurRequests.Register(_ownerA, UiBackdropBlurSettings.Default, 5);

			Assert.AreEqual(1, UiBackdropBlurRequests.ActiveCount);
		}

		[Test]
		public void Unregister_RemovesRequest()
		{
			UiBackdropBlurRequests.Register(_ownerA, UiBackdropBlurSettings.Default, 0);
			UiBackdropBlurRequests.Unregister(_ownerA);

			Assert.AreEqual(0, UiBackdropBlurRequests.ActiveCount);
			Assert.IsFalse(UiBackdropBlurRequests.TryGetActive(out _));
		}

		[Test]
		public void Unregister_UnknownOwner_NoOp()
		{
			Assert.DoesNotThrow(() => UiBackdropBlurRequests.Unregister(_ownerA));
			Assert.AreEqual(0, UiBackdropBlurRequests.ActiveCount);
		}

		[Test]
		public void TryGetActive_SingleRequest_ReturnsItsSettings()
		{
			var settings = new UiBackdropBlurSettings(2, 3, 1.5f, Color.red);
			UiBackdropBlurRequests.Register(_ownerA, settings, 0);

			Assert.IsTrue(UiBackdropBlurRequests.TryGetActive(out var active));
			Assert.AreEqual(2, active.Downsample);
			Assert.AreEqual(3, active.Iterations);
			Assert.AreEqual(1.5f, active.Spread);
			Assert.AreEqual(Color.red, active.Tint);
		}

		[Test]
		public void TryGetActive_MultipleRequests_HighestSortKeyWins()
		{
			UiBackdropBlurRequests.Register(_ownerA, new UiBackdropBlurSettings(0, 1, 1f, Color.red), sortKey: 10);
			UiBackdropBlurRequests.Register(_ownerB, new UiBackdropBlurSettings(1, 2, 2f, Color.green), sortKey: 30);
			UiBackdropBlurRequests.Register(_ownerC, new UiBackdropBlurSettings(2, 3, 3f, Color.blue), sortKey: 20);

			Assert.IsTrue(UiBackdropBlurRequests.TryGetActive(out var active));
			Assert.AreEqual(Color.green, active.Tint, "sortKey 30 (ownerB) should win over 10 and 20.");
		}

		[Test]
		public void TryGetActive_AfterWinnerUnregisters_FallsBackToNextHighest()
		{
			UiBackdropBlurRequests.Register(_ownerA, new UiBackdropBlurSettings(0, 1, 1f, Color.red), sortKey: 10);
			UiBackdropBlurRequests.Register(_ownerB, new UiBackdropBlurSettings(1, 2, 2f, Color.green), sortKey: 30);

			UiBackdropBlurRequests.Unregister(_ownerB);

			Assert.IsTrue(UiBackdropBlurRequests.TryGetActive(out var active));
			Assert.AreEqual(Color.red, active.Tint);
		}

		[Test]
		public void Register_NullOwner_NoOp()
		{
			Assert.DoesNotThrow(() => UiBackdropBlurRequests.Register(null, UiBackdropBlurSettings.Default, 0));
			Assert.AreEqual(0, UiBackdropBlurRequests.ActiveCount);
		}
	}

	[TestFixture]
	public class UiBackdropBlurSettingsTests
	{
		[Test]
		public void Constructor_ClampsDownsampleTo0Through3()
		{
			Assert.AreEqual(0, new UiBackdropBlurSettings(-5, 1, 0f, Color.white).Downsample);
			Assert.AreEqual(3, new UiBackdropBlurSettings(99, 1, 0f, Color.white).Downsample);
		}

		[Test]
		public void Constructor_ClampsIterationsToAtLeast1()
		{
			Assert.AreEqual(1, new UiBackdropBlurSettings(0, 0, 0f, Color.white).Iterations);
			Assert.AreEqual(1, new UiBackdropBlurSettings(0, -3, 0f, Color.white).Iterations);
		}

		[Test]
		public void Constructor_ClampsSpreadToNonNegative()
		{
			Assert.AreEqual(0f, new UiBackdropBlurSettings(0, 1, -2f, Color.white).Spread);
		}
	}

	[TestFixture]
	public class UiBackdropBlurShouldInjectTests
	{
		[Test]
		public void ShouldInject_AllConditionsMet_ReturnsTrue()
		{
			Assert.IsTrue(UiBackdropBlurRequests.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: false, anyActiveRequests: true));
		}

		[Test]
		public void ShouldInject_NotGameCamera_ReturnsFalse()
		{
			Assert.IsFalse(UiBackdropBlurRequests.ShouldInject(
				isGameCamera: false, isOverlayCamera: false, hasTargetTexture: false, anyActiveRequests: true));
		}

		[Test]
		public void ShouldInject_OverlayCamera_ReturnsFalse()
		{
			// Hazard: renderer features run per camera in a stack -- injecting on the UI
			// overlay camera itself would blur the UI, not the world behind it.
			Assert.IsFalse(UiBackdropBlurRequests.ShouldInject(
				isGameCamera: true, isOverlayCamera: true, hasTargetTexture: false, anyActiveRequests: true));
		}

		[Test]
		public void ShouldInject_HasTargetTexture_ReturnsFalse()
		{
			// Hazard: a render-texture-target camera blurring its own output would blur the UI
			// being captured, not the world behind it.
			Assert.IsFalse(UiBackdropBlurRequests.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: true, anyActiveRequests: true));
		}

		[Test]
		public void ShouldInject_NoActiveRequests_ReturnsFalse()
		{
			Assert.IsFalse(UiBackdropBlurRequests.ShouldInject(
				isGameCamera: true, isOverlayCamera: false, hasTargetTexture: false, anyActiveRequests: false));
		}

		[Test]
		public void ShouldInject_BothHazardsAndNoRequests_ReturnsFalse()
		{
			Assert.IsFalse(UiBackdropBlurRequests.ShouldInject(
				isGameCamera: false, isOverlayCamera: true, hasTargetTexture: true, anyActiveRequests: false));
		}
	}
}
