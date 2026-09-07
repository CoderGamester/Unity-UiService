using System.Collections;
using GameLovers.UiService.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public sealed class SafeAreaViewTests
	{
		private GameObject _canvasGo;
		private GameObject _panelGo;
		private RectTransform _panelRect;
		private SafeAreaPanelView _panel;
		private GameObject _documentGo;
		private PanelSettings _panelSettings;
		private ThemeStyleSheet _themeStyleSheet;

		[SetUp]
		public void Setup()
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

			_documentGo = new GameObject("SafeAreaDocument", typeof(UIDocument));
			_panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
			_panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
			_panelSettings.scale = 1f;
			_themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
			_panelSettings.themeStyleSheet = _themeStyleSheet;
			_documentGo.GetComponent<UIDocument>().panelSettings = _panelSettings;
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_documentGo);
			Object.DestroyImmediate(_panelSettings);
			Object.DestroyImmediate(_themeStyleSheet);
			Object.DestroyImmediate(_panelGo);
			Object.DestroyImmediate(_canvasGo);
		}

		[UnityTest]
		// ADMIT: SafeAreaPanelView's enable lifecycle must apply device anchors, clear offsets, stay inert while disabled, and repair the rect after re-enable.
		// A2: SafeAreaPanelView.cs OnEnable — remove the `Apply()` call and the final offset assertion fails immediately after re-enable.
		public IEnumerator PanelEnableLifecycle_AppliesPausesAndRepairsCurrentSafeArea()
		{
			Rect safeArea = UnityEngine.Device.Screen.safeArea;
			var expectedMin = new Vector2(safeArea.xMin / UnityEngine.Device.Screen.width, safeArea.yMin / UnityEngine.Device.Screen.height);
			var expectedMax = new Vector2(safeArea.xMax / UnityEngine.Device.Screen.width, safeArea.yMax / UnityEngine.Device.Screen.height);

			Assert.AreEqual(expectedMin, _panelRect.anchorMin);
			Assert.AreEqual(expectedMax, _panelRect.anchorMax);
			Assert.AreEqual(Vector2.zero, _panelRect.offsetMin);
			Assert.AreEqual(Vector2.zero, _panelRect.offsetMax);

			_panelGo.SetActive(false);
			_panelRect.offsetMin = new Vector2(17f, 19f);
			_panel.Apply();
			yield return null;
			Assert.AreEqual(new Vector2(17f, 19f), _panelRect.offsetMin);

			_panelGo.SetActive(true);
			Assert.AreEqual(Vector2.zero, _panelRect.offsetMin);
			Assert.AreEqual(Vector2.zero, _panelRect.offsetMax);
		}

		[UnityTest]
		// ADMIT: SafeAreaContainer must apply current padding on panel attachment and reattachment and scale pixel insets into panel points.
		// A2: SafeAreaContainer.cs ApplySafeArea — replace the attached panel scale with 1f and the scaled-padding assertions fail.
		public IEnumerator ContainerPanelLifecycle_AttachesReattachesAndAppliesScale()
		{
			var document = _documentGo.GetComponent<UIDocument>();
			yield return TestHelpers.WaitForPanelAttachment(document);

			var container = new SafeAreaContainer();
			container.style.paddingLeft = 31f;
			document.rootVisualElement.Add(container);
			Rect safeArea = UnityEngine.Device.Screen.safeArea;
			float scale = container.panel.scaledPixelsPerPoint;
			float expectedLeft = Mathf.Max(0f, safeArea.xMin) / scale;
			float expectedTop = Mathf.Max(0f, UnityEngine.Device.Screen.height - safeArea.yMax) / scale;
			float expectedRight = Mathf.Max(0f, UnityEngine.Device.Screen.width - safeArea.xMax) / scale;
			float expectedBottom = Mathf.Max(0f, safeArea.yMin) / scale;
			Assert.AreEqual(expectedLeft, container.style.paddingLeft.value.value);
			Assert.AreEqual(expectedTop, container.style.paddingTop.value.value);
			Assert.AreEqual(expectedRight, container.style.paddingRight.value.value);
			Assert.AreEqual(expectedBottom, container.style.paddingBottom.value.value);

			_panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
			_panelSettings.scale = 2f;
			yield return null;
			float scaledPixelsPerPoint = container.panel.scaledPixelsPerPoint;
			Assert.AreEqual(scale * 2f, scaledPixelsPerPoint, 0.001f);
			container.ApplySafeArea(new Rect(20f, 10f, 160f, 80f), 200, 100);
			Assert.AreEqual(20f / scaledPixelsPerPoint, container.style.paddingLeft.value.value);
			Assert.AreEqual(10f / scaledPixelsPerPoint, container.style.paddingTop.value.value);
			Assert.AreEqual(20f / scaledPixelsPerPoint, container.style.paddingRight.value.value);
			Assert.AreEqual(10f / scaledPixelsPerPoint, container.style.paddingBottom.value.value);

			safeArea = UnityEngine.Device.Screen.safeArea;

			container.RemoveFromHierarchy();
			container.style.paddingLeft = 23f;
			document.rootVisualElement.Add(container);
			expectedLeft = Mathf.Max(0f, safeArea.xMin) / scaledPixelsPerPoint;
			expectedTop = Mathf.Max(0f, UnityEngine.Device.Screen.height - safeArea.yMax) / scaledPixelsPerPoint;
			expectedRight = Mathf.Max(0f, UnityEngine.Device.Screen.width - safeArea.xMax) / scaledPixelsPerPoint;
			expectedBottom = Mathf.Max(0f, safeArea.yMin) / scaledPixelsPerPoint;
			Assert.AreEqual(expectedLeft, container.style.paddingLeft.value.value);
			Assert.AreEqual(expectedTop, container.style.paddingTop.value.value);
			Assert.AreEqual(expectedRight, container.style.paddingRight.value.value);
			Assert.AreEqual(expectedBottom, container.style.paddingBottom.value.value);
		}
	}
}
