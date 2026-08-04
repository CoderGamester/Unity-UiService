using System.Collections;
using System.Collections.Generic;
using GameLovers.UiService.Views;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public class InteractableTextViewTests
	{
		private GameObject _canvasGo;
		private GameObject _textGo;
		private TextMeshProUGUI _tmp;
		private InteractableTextView _view;
		private int _onLinkedInfoClickedCount;
		private TMP_LinkInfo _lastLinkInfo;
		private readonly List<GameObject> _extraGameObjects = new List<GameObject>();

		/// <summary>
		/// Builds a standalone Canvas GameObject configured for the given render mode.
		/// Does not parent anything under it — callers own that wiring (see <see cref="Setup"/>).
		/// </summary>
		private GameObject BuildCanvas(RenderMode mode, Camera worldCamera = null)
		{
			var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
			var canvas = canvasGo.GetComponent<Canvas>();

			canvas.renderMode = mode;
			canvas.worldCamera = worldCamera;

			return canvasGo;
		}

		[SetUp]
		public void Setup()
		{
			_canvasGo = BuildCanvas(RenderMode.ScreenSpaceOverlay);

			_textGo = new GameObject("Text", typeof(RectTransform));
			_textGo.transform.SetParent(_canvasGo.transform, false);

			_tmp = _textGo.AddComponent<TextMeshProUGUI>();
			_view = _textGo.AddComponent<InteractableTextView>();

			// InteractableTextView wires its `_text` field via OnValidate (an editor-only callback).
			// At runtime the field starts null; SendMessage triggers the same wiring path safely
			// without resorting to BindingFlags reflection on a private member.
			_view.SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);

			_onLinkedInfoClickedCount = 0;
			_lastLinkInfo = default;
			_view.OnLinkedInfoClicked = new UnityEngine.Events.UnityEvent<TMP_LinkInfo>();
			_view.OnLinkedInfoClicked.AddListener(info =>
			{
				_onLinkedInfoClickedCount++;
				_lastLinkInfo = info;
			});
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var go in _extraGameObjects)
			{
				if (go != null)
				{
					Object.DestroyImmediate(go);
				}
			}
			_extraGameObjects.Clear();

			if (_textGo != null)
			{
				Object.DestroyImmediate(_textGo);
			}
			if (_canvasGo != null)
			{
				Object.DestroyImmediate(_canvasGo);
			}
		}

		[UnityTest]
		// ADMIT: InteractableTextView.OnPointerClick's `linkedText > -1` hit test must admit index 0 — TMP returns -1
		// for a miss, so an off-by-one drops every click on a text's first link.
		// RCR: InteractableTextView.cs OnPointerClick — replace `if (linkedText > -1)` with `if (linkedText > 0)` →
		// RED (expected 1 OnLinkedInfoClicked invocation, was 0). The two render-mode siblings redden too. 2026-08-02
		public IEnumerator OnPointerClick_OnLinkedTextHit_FiresOnLinkedInfoClickedWithLinkInfo()
		{
			_tmp.text = "click <link=\"my_link_id\">here</link> now";
			_tmp.ForceMeshUpdate();
			yield return null;

			Assume.That(_tmp.textInfo.linkInfo.Length, Is.GreaterThan(0),
				"TMP failed to register link info — likely missing default font asset in test runtime");
			Assume.That(_tmp.textInfo.linkInfo[0].GetLinkID(), Is.EqualTo("my_link_id"));

			var rectTransform = _textGo.GetComponent<RectTransform>();
			var firstLinkChar = _tmp.textInfo.linkInfo[0].linkTextfirstCharacterIndex;
			var charInfo = _tmp.textInfo.characterInfo[firstLinkChar];
			var localCenter = (charInfo.bottomLeft + charInfo.topRight) * 0.5f;
			var worldPos = rectTransform.TransformPoint(localCenter);
			var screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);

			var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

			_view.OnPointerClick(eventData);

			Assert.AreEqual(1, _onLinkedInfoClickedCount);
			Assert.AreEqual("my_link_id", _lastLinkInfo.GetLinkID());
		}

		[UnityTest]
		// ADMIT: InteractableTextView.OnPointerClick must not raise OnLinkedInfoClicked when TMP reports
		// no hit (-1); firing on a miss indexes linkInfo out of range.
		// RCR: InteractableTextView.cs OnPointerClick - `if (linkedText > -1)` -> `if (linkedText > -2)`
		// -> RED. The hit sibling's `> 0` mutation leaves this test green, so the two pin opposite polarities.
		public IEnumerator OnPointerClick_NoLinkInText_DoesNotFireOnLinkedInfoClicked()
		{
			_tmp.text = "no links here at all";
			_tmp.ForceMeshUpdate();
			yield return null;

			var screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
			var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

			_view.OnPointerClick(eventData);

			Assert.AreEqual(0, _onLinkedInfoClickedCount);
		}

		[UnityTest]
		// ADMIT: InteractableTextView.ResolveEventCamera returns canvas.worldCamera for a ScreenSpaceCamera canvas
		// with an assigned camera; both pre-existing tests hardcode ScreenSpaceOverlay, leaving this branch unpinned.
		// RCR: InteractableTextView.cs ResolveEventCamera — replace the return with `return null;` → RED (expected
		// 1 link hit, got 0: the click point was projected through the real camera and no longer resolves). The two
		// pre-existing tests stay green — they exercise only the isOverlay path, already null. 2026-08-01
		public IEnumerator OnPointerClick_ScreenSpaceCameraCanvas_UsesWorldCameraForLinkHitTest()
		{
			// Arrange
			var canvas = _canvasGo.GetComponent<Canvas>();
			var cameraGo = new GameObject("EventCamera", typeof(Camera));
			_extraGameObjects.Add(cameraGo);
			var camera = cameraGo.GetComponent<Camera>();

			// Offset both the camera's transform and its pixelRect from Unity's implicit
			// full-screen-at-origin defaults, so the real camera's projection is genuinely
			// different from the screen-centered mapping the null-camera path assumes.
			camera.transform.position = new Vector3(2f, 1f, -8f);
			camera.orthographic = false;
			camera.fieldOfView = 45f;
			camera.pixelRect = new Rect(50f, 50f, Screen.width - 100f, Screen.height - 100f);

			canvas.renderMode = RenderMode.ScreenSpaceCamera;
			canvas.worldCamera = camera;
			canvas.planeDistance = 10f;

			_tmp.text = "click <link=\"my_link_id\">here</link> now";
			_tmp.ForceMeshUpdate();

			// A Screen Space - Camera canvas is repositioned/rescaled by Unity every frame to
			// fit the assigned camera's viewport at planeDistance; give that at least one frame
			// to settle before reading the text's resulting world position.
			yield return null;
			yield return null;

			Assume.That(_tmp.textInfo.linkInfo.Length, Is.GreaterThan(0),
				"TMP failed to register link info — likely missing default font asset in test runtime");
			Assume.That(_tmp.textInfo.linkInfo[0].GetLinkID(), Is.EqualTo("my_link_id"));

			var rectTransform = _textGo.GetComponent<RectTransform>();
			var firstLinkChar = _tmp.textInfo.linkInfo[0].linkTextfirstCharacterIndex;
			var charInfo = _tmp.textInfo.characterInfo[firstLinkChar];
			var localCenter = (charInfo.bottomLeft + charInfo.topRight) * 0.5f;
			var worldPos = rectTransform.TransformPoint(localCenter);

			// This screen point is only meaningful when projected back through the SAME real
			// camera: TMP recovers the click's world point via `camera.ScreenPointToRay(...)`
			// intersected with the text's plane. Resolved via a null camera instead, TMP falls
			// back to the Overlay convention (screen point treated as already centered on the
			// canvas origin), which does not describe this canvas at all — Unity has moved it
			// out into real 3D space to match the camera's viewport — so the link would be missed.
			var screenPos = RectTransformUtility.WorldToScreenPoint(camera, worldPos);
			var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

			// Act
			_view.OnPointerClick(eventData);

			// Assert
			Assert.AreEqual(1, _onLinkedInfoClickedCount);
			Assert.AreEqual("my_link_id", _lastLinkInfo.GetLinkID());
		}

		[UnityTest]
		// ADMIT: InteractableTextView.ResolveEventCamera returns canvas.worldCamera unconditionally for
		// RenderMode.WorldSpace — isCameraless is computed only for ScreenSpaceCamera. WorldSpace has no
		// engine-managed screen-space fallback, so returning null there resolves every link click against the
		// wrong world point; both pre-existing tests hardcode ScreenSpaceOverlay.
		// RCR: InteractableTextView.cs ResolveEventCamera — replace the return with `return null;` → RED
		// (expected 1 link hit, got 0). 2026-08-01
		public IEnumerator OnPointerClick_WorldSpaceCanvas_UsesWorldCameraForLinkHitTest()
		{
			// Arrange
			var canvas = _canvasGo.GetComponent<Canvas>();
			var cameraGo = new GameObject("EventCamera", typeof(Camera));
			_extraGameObjects.Add(cameraGo);
			var camera = cameraGo.GetComponent<Camera>();

			camera.transform.position = Vector3.zero;
			camera.transform.rotation = Quaternion.identity;
			camera.orthographic = false;
			camera.fieldOfView = 60f;

			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = camera;

			// Unlike ScreenSpaceCamera, Unity never auto-positions a WorldSpace canvas — its
			// transform is exactly what we set here, far from the screen-centered origin the
			// null-camera convention assumes.
			_canvasGo.transform.position = new Vector3(15f, 8f, 40f);
			_canvasGo.transform.rotation = Quaternion.Euler(10f, -20f, 0f);

			_tmp.text = "click <link=\"my_link_id\">here</link> now";
			_tmp.ForceMeshUpdate();
			yield return null;

			Assume.That(_tmp.textInfo.linkInfo.Length, Is.GreaterThan(0),
				"TMP failed to register link info — likely missing default font asset in test runtime");
			Assume.That(_tmp.textInfo.linkInfo[0].GetLinkID(), Is.EqualTo("my_link_id"));

			var rectTransform = _textGo.GetComponent<RectTransform>();
			var firstLinkChar = _tmp.textInfo.linkInfo[0].linkTextfirstCharacterIndex;
			var charInfo = _tmp.textInfo.characterInfo[firstLinkChar];
			var localCenter = (charInfo.bottomLeft + charInfo.topRight) * 0.5f;
			var worldPos = rectTransform.TransformPoint(localCenter);

			// As with the ScreenSpaceCamera case, this screen point round-trips correctly only
			// through the same real camera. For WorldSpace there is no screen-space fallback to
			// even approximate a match — resolving via null is unambiguously wrong here.
			var screenPos = RectTransformUtility.WorldToScreenPoint(camera, worldPos);
			var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

			// Act
			_view.OnPointerClick(eventData);

			// Assert
			Assert.AreEqual(1, _onLinkedInfoClickedCount);
			Assert.AreEqual("my_link_id", _lastLinkInfo.GetLinkID());
		}

		[UnityTest]
		// ADMIT: InteractableTextView.ResolveEventCamera guards a missing Canvas ancestor with an early
		// `if (canvas == null) return null;` before touching canvas.renderMode — a real configuration (a prefab
		// authored without a Canvas, or a text object detached mid-reparent).
		// RCR: InteractableTextView.cs ResolveEventCamera — delete the `if (canvas == null)` guard → RED
		// (NullReferenceException on `canvas.renderMode`). 2026-08-01
		public IEnumerator OnPointerClick_TextWithNoParentCanvas_DoesNotThrow()
		{
			// Arrange
			_textGo.transform.SetParent(null, false);

			_tmp.text = "click <link=\"my_link_id\">here</link> now";
			_tmp.ForceMeshUpdate();
			yield return null;

			var screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
			var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

			// Act & Assert
			Assert.DoesNotThrow(() => _view.OnPointerClick(eventData));
		}
	}
}
