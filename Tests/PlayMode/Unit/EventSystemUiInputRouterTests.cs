using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public sealed class EventSystemUiInputRouterTests
	{
		private GameObject _presenterObject;
		private GameObject _eventSystemObject;
		private GameObject _cameraObject;

		[TearDown]
		public void TearDown()
		{
			if (_presenterObject != null)
			{
				Object.DestroyImmediate(_presenterObject);
			}

			if (_eventSystemObject != null)
			{
				Object.DestroyImmediate(_eventSystemObject);
			}

			if (_cameraObject != null)
			{
				Object.DestroyImmediate(_cameraObject);
			}
		}

		[Test]
		// ADMIT: EventSystemUiInputRouter must make a screen Canvas raycastable and install the consumer-selected input module.
		// RCR: EventSystemUiInputRouter.Prepare — skip GraphicRaycaster installation → RED (expected raycaster). 2026-08-13
		public void Prepare_ScreenCanvas_EstablishesPointerPrerequisites()
		{
			_presenterObject = new GameObject("Presenter", typeof(Canvas), typeof(TestUiPresenter));
			var presenter = _presenterObject.GetComponent<TestUiPresenter>();
			_presenterObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
			UiSurfaceResolver.TryResolve(_presenterObject, out IUiSurface surface);
			var router = new EventSystemUiInputRouter(
				eventSystemObject => eventSystemObject.AddComponent<TestInputModule>());

			router.Prepare(presenter, surface);
			EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
			_eventSystemObject = eventSystem.gameObject;

			Assert.That(_presenterObject.GetComponent<GraphicRaycaster>(), Is.Not.Null);
			Assert.That(eventSystem.GetComponent<TestInputModule>(), Is.Not.Null);
		}

		[Test]
		// ADMIT: EventSystemUiInputRouter must add a PhysicsRaycaster to the camera used by an interactive world surface.
		// RCR: EventSystemUiInputRouter.Prepare — skip PhysicsRaycaster installation → RED (expected raycaster). 2026-08-13
		public void Prepare_WorldSurface_AddsPhysicsRaycaster()
		{
			_cameraObject = new GameObject("Camera", typeof(Camera));
			_presenterObject = new GameObject(
				"World Presenter",
				typeof(MeshFilter),
				typeof(MeshRenderer),
				typeof(MeshCollider),
				typeof(TestUiPresenter),
				typeof(TestUiSurface));
			var surface = _presenterObject.GetComponent<TestUiSurface>();
			surface.SurfaceSpace = UiSurfaceSpace.World;
			var router = new EventSystemUiInputRouter(
				eventSystemObject => eventSystemObject.AddComponent<TestInputModule>(),
				() => _cameraObject.GetComponent<Camera>());

			router.Prepare(_presenterObject.GetComponent<TestUiPresenter>(), surface);
			EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
			_eventSystemObject = eventSystem.gameObject;

			Assert.That(_cameraObject.GetComponent<PhysicsRaycaster>(), Is.Not.Null);
		}
	}
}
