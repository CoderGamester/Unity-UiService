using NUnit.Framework;
using Rive.Components;
using UnityEngine;

namespace GameLovers.UiService.Rive.Tests.PlayMode
{
	[TestFixture]
	public sealed class RiveUiSurfaceTests
	{
		private GameObject _surfaceObject;

		[TearDown]
		public void TearDown()
		{
			if (_surfaceObject != null)
			{
				Object.DestroyImmediate(_surfaceObject);
			}
		}

		[Test]
		// ADMIT: RiveUiSurface must translate UiService order into the Canvas that displays a Rive panel.
		// RCR: RiveUiSurface.ApplyOrder — force Canvas order to zero → RED (expected 42). 2026-08-13
		public void ApplyOrder_CanvasRenderer_UpdatesCanvas()
		{
			_surfaceObject = new GameObject(
				"Rive Surface",
				typeof(RectTransform),
				typeof(Canvas),
				typeof(RivePanel),
				typeof(RiveCanvasRenderer),
				typeof(RiveUiSurface));
			var surface = _surfaceObject.GetComponent<RiveUiSurface>();
			surface.PanelRenderer = _surfaceObject.GetComponent<RiveCanvasRenderer>();
			surface.SurfaceSpace = UiSurfaceSpace.ScreenOverlay;

			surface.ApplyOrder(42);

			Assert.That(_surfaceObject.GetComponent<Canvas>().sortingOrder, Is.EqualTo(42));
			Assert.That(surface.Order, Is.EqualTo(42));
		}
	}
}
