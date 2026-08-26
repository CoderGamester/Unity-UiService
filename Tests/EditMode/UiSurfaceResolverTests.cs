using NUnit.Framework;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
	[TestFixture]
	public sealed class UiSurfaceResolverTests
	{
		private GameObject _root;

		[TearDown]
		public void TearDown()
		{
			if (_root != null)
			{
				Object.DestroyImmediate(_root);
			}
		}

		[Test]
		// ADMIT: UiSurfaceResolver must not ignore a Canvas nested below the presenter root.
		// RCR: UiSurfaceResolver.TryResolve — use root-only GetComponent<Canvas>() → RED (child Canvas did not resolve). 2026-08-13
		public void TryResolve_ChildCanvas_AppliesConfiguredOrder()
		{
			_root = new GameObject("Presenter");
			var child = new GameObject("Canvas", typeof(Canvas));
			child.transform.SetParent(_root.transform);
			child.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

			bool resolved = UiSurfaceResolver.TryResolve(_root, out IUiSurface surface);
			Assert.That(resolved, Is.True, "A child Canvas must resolve as the presenter's surface.");
			surface.ApplyOrder(17);

			Assert.That(child.GetComponent<Canvas>().sortingOrder, Is.EqualTo(17));
			Assert.That(surface.SurfaceSpace, Is.EqualTo(UiSurfaceSpace.ScreenOverlay));
		}

		[Test]
		// ADMIT: UiSurfaceResolver must prioritize a custom backend over built-in fallback components.
		// RCR: UiSurfaceResolver.TryResolve — skip IUiSurface candidates → RED (Canvas adapter replaced custom surface). 2026-08-13
		public void TryResolve_CustomSurface_PrioritizesBackendContract()
		{
			_root = new GameObject("Presenter", typeof(Canvas), typeof(TestUiSurface));
			var customSurface = _root.GetComponent<TestUiSurface>();

			bool resolved = UiSurfaceResolver.TryResolve(_root, out IUiSurface surface);
			Assert.That(resolved, Is.True, "A custom surface must resolve from the presenter hierarchy.");
			surface.ApplyOrder(23);

			Assert.That(surface, Is.SameAs(customSurface));
			Assert.That(customSurface.Order, Is.EqualTo(23));
			Assert.That(_root.GetComponent<Canvas>().sortingOrder, Is.Zero);
		}
	}
}
