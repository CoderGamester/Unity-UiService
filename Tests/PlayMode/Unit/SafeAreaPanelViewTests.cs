using System.Collections;
using GameLovers.UiService.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public sealed class SafeAreaPanelViewTests
	{
		private GameObject _canvasGo;
		private GameObject _panelGo;
		private RectTransform _panelRect;
		private SafeAreaPanelView _panel;

		[SetUp]
		public void SetUp()
		{
			_canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
			_canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

			_panelGo = new GameObject("SafeArea", typeof(RectTransform));
			_panelRect = (RectTransform)_panelGo.transform;
			_panelRect.SetParent(_canvasGo.transform, false);
			_panelRect.anchorMin = Vector2.zero;
			_panelRect.anchorMax = Vector2.one;
			_panelRect.offsetMin = new Vector2(10f, 10f);
			_panelRect.offsetMax = new Vector2(-10f, -10f);
			_panel = _panelGo.AddComponent<SafeAreaPanelView>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_panelGo != null)
			{
				Object.DestroyImmediate(_panelGo);
			}

			if (_canvasGo != null)
			{
				Object.DestroyImmediate(_canvasGo);
			}
		}

		[UnityTest]
		// ADMIT: SafeAreaPanelView.Apply must write Device.Screen anchors and clear leftover offsets on a stretched rect.
		// RCR: SafeAreaPanelView.cs Apply — drop `_rect.offsetMin = Vector2.zero` → RED (Expected: (0.00, 0.00) But was: (10.00, 10.00)). 2026-09-01
		public IEnumerator Apply_Playing_WritesDeviceScreenAnchorsAndClearsOffsets()
		{
			Assert.IsNotNull(_panel);

			SafeAreaPanelView.ComputeNormalizedAnchors(
				UnityEngine.Device.Screen.safeArea,
				UnityEngine.Device.Screen.width,
				UnityEngine.Device.Screen.height,
				out Vector2 expectedMin,
				out Vector2 expectedMax);

			Assert.AreEqual(expectedMin, _panelRect.anchorMin);
			Assert.AreEqual(expectedMax, _panelRect.anchorMax);
			Assert.AreEqual(Vector2.zero, _panelRect.offsetMin);
			Assert.AreEqual(Vector2.zero, _panelRect.offsetMax);
			yield return null;
		}
	}
}
