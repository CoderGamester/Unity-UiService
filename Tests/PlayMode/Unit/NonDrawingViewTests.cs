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
	}
}
