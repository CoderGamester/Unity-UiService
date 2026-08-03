using System.Collections;
using GameLovers.UiService.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public class NonDrawingViewTests
	{
		// Subclass exposes the protected OnPopulateMesh override via a public passthrough so
		// the test can assert the contract without reflection on private members.
		private class TestableNonDrawingView : NonDrawingView
		{
			public void InvokeOnPopulateMesh(VertexHelper vh)
			{
				OnPopulateMesh(vh);
			}
		}

		private GameObject _canvasGo;
		private GameObject _viewGo;
		private TestableNonDrawingView _view;

		[SetUp]
		public void Setup()
		{
			_canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
			var canvas = _canvasGo.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			_viewGo = new GameObject("View", typeof(RectTransform));
			_viewGo.transform.SetParent(_canvasGo.transform, false);
			_view = _viewGo.AddComponent<TestableNonDrawingView>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_viewGo != null)
			{
				Object.DestroyImmediate(_viewGo);
			}
			if (_canvasGo != null)
			{
				Object.DestroyImmediate(_canvasGo);
			}
		}

		[UnityTest]
		// ADMIT: NonDrawingView.OnPopulateMesh must clear the VertexHelper — emitting zero geometry while remaining a
		// raycast target is the entire purpose of the type.
		// RCR: NonDrawingView.cs OnPopulateMesh — comment out `vh.Clear();` → RED (expected 0 verts, was 3, so the
		// view would draw whatever geometry Graphic handed it). 2026-08-02
		public IEnumerator OnPopulateMesh_AlwaysClearsVertexHelper()
		{
			using var vh = new VertexHelper();
			vh.AddVert(Vector3.zero, Color.white, Vector2.zero);
			vh.AddVert(Vector3.right, Color.white, Vector2.zero);
			vh.AddVert(Vector3.up, Color.white, Vector2.zero);
			vh.AddTriangle(0, 1, 2);

			Assume.That(vh.currentVertCount, Is.EqualTo(3),
				"VertexHelper should accept the 3 seed verts before clearing");

			_view.InvokeOnPopulateMesh(vh);

			Assert.AreEqual(0, vh.currentVertCount,
				"NonDrawingView.OnPopulateMesh must always emit zero verts (its raison d'être)");
			yield return null;
		}

		[UnityTest]
		// ADMIT: NonDrawingView's [RequireComponent(typeof(CanvasRenderer))] is the entire historical 0.9.1 crash
		// fix — without it, AddComponent<NonDrawingView>() on a GameObject with no CanvasRenderer leaves the
		// Graphic base class without the renderer it requires.
		// RCR: NonDrawingView.cs — delete `[RequireComponent(typeof(CanvasRenderer))]` → RED
		// (`GetComponent<CanvasRenderer>() != null` fails). The forced rebuild below is defense-in-depth only:
		// NonDrawingView's SetVerticesDirty/SetMaterialDirty no-ops mean OnPopulateMesh is never re-entered. 2026-08-01
		public IEnumerator AddComponent_OnGameObjectWithoutCanvasRenderer_AutoAddsCanvasRendererAndRebuildsWithoutThrowing()
		{
			// Arrange
			var bareGo = new GameObject("BareView", typeof(RectTransform));
			bareGo.transform.SetParent(_canvasGo.transform, false);

			// Act
			var view = bareGo.AddComponent<NonDrawingView>();

			// Assert
			Assert.IsNotNull(bareGo.GetComponent<CanvasRenderer>(),
				"[RequireComponent(typeof(CanvasRenderer))] on NonDrawingView must make Unity auto-add a CanvasRenderer.");

			view.SetAllDirty();
			Canvas.ForceUpdateCanvases();
			LogAssert.NoUnexpectedReceived();

			Object.DestroyImmediate(bareGo);
			yield return null;
		}
	}
}
