using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public sealed class UiWorldSpaceFeatureTests
	{
		private GameObject _cameraObject;
		private GameObject _presenterObject;
		private GameObject _targetObject;

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_cameraObject);
			Object.DestroyImmediate(_presenterObject);
			Object.DestroyImmediate(_targetObject);
		}

		[UnityTest]
		// ADMIT: UiDistanceVisibilityFeature must suspend renderer and backend work outside its hysteresis band.
		// RCR: UiDistanceVisibilityFeature.EvaluateVisibility — invert SetRenderActive value → RED (expected suspended work). 2026-08-13
		public IEnumerator DistanceVisibility_CrossesBand_UpdatesAllRenderWork()
		{
			_cameraObject = new GameObject("Camera", typeof(Camera));
			_presenterObject = new GameObject(
				"Presenter",
				typeof(MeshRenderer),
				typeof(TestUiPresenter),
				typeof(TestUiRenderActivity),
				typeof(UiDistanceVisibilityFeature));
			var presenter = _presenterObject.GetComponent<TestUiPresenter>();
			var activity = _presenterObject.GetComponent<TestUiRenderActivity>();
			var feature = _presenterObject.GetComponent<UiDistanceVisibilityFeature>();
			feature.Camera = _cameraObject.GetComponent<Camera>();
			feature.MaxDistance = 5f;
			feature.Hysteresis = 1f;
			_presenterObject.transform.position = new Vector3(0f, 0f, 10f);

			feature.OnPresenterInitialized(presenter);

			Assert.That(feature.IsVisible, Is.False);
			Assert.That(activity.IsRenderActive, Is.False);
			Assert.That(_presenterObject.GetComponent<MeshRenderer>().enabled, Is.False);

			_presenterObject.transform.position = new Vector3(0f, 0f, 3f);
			yield return null;

			Assert.That(feature.IsVisible, Is.True);
			Assert.That(activity.IsRenderActive, Is.True);
			Assert.That(_presenterObject.GetComponent<MeshRenderer>().enabled, Is.True);
		}

		[Test]
		// ADMIT: UiFollowTargetFeature must preserve its authored world-space offset after runtime target assignment.
		// RCR: UiFollowTargetFeature.UpdatePosition — omit Offset → RED (expected offset world position). 2026-08-13
		public void FollowTarget_RuntimeTarget_AppliesOffset()
		{
			_presenterObject = new GameObject("Presenter", typeof(TestUiPresenter), typeof(UiFollowTargetFeature));
			_targetObject = new GameObject("Target");
			_targetObject.transform.position = new Vector3(3f, 4f, 5f);
			var feature = _presenterObject.GetComponent<UiFollowTargetFeature>();
			feature.Offset = new Vector3(1f, 2f, 3f);

			feature.Target = _targetObject.transform;

			Assert.That(_presenterObject.transform.position, Is.EqualTo(new Vector3(4f, 6f, 8f)));
		}
	}
}
