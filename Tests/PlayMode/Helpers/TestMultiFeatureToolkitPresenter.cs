using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Tests.PlayMode
{
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	[RequireComponent(typeof(TimeDelayFeature))]
	[RequireComponent(typeof(UIDocument))]
	public class TestMultiFeatureToolkitPresenter : UiPresenter
	{
		public UiToolkitPresenterFeature ToolkitFeature { get; private set; }
		public TimeDelayFeature DelayFeature { get; private set; }

		private void Awake()
		{
			// TestHelpers.CreateTestPresenterPrefab<T>() unconditionally adds a Canvas ("most presenters
			// need a canvas") before adding the presenter component. This presenter is UIDocument-only and
			// never wants one: UiService.EnsureCanvasSortingOrder checks Canvas before UIDocument (by
			// design — see the package root AGENTS.md §4, "the Canvas/UIDocument pair it already handles
			// is complete"), so a stray factory-added Canvas silently steals the sorting-order write that
			// should land on this presenter's UIDocument instead. DestroyImmediate (not Destroy) because
			// EnsureCanvasSortingOrder's TryGetComponent<Canvas> check runs later in the same synchronous
			// call chain as this Awake() (no frame boundary in between for a zero-simulated-delay load), so
			// a deferred Destroy would not have taken effect yet.
			var strayCanvas = GetComponent<Canvas>();
			if (strayCanvas != null)
			{
				DestroyImmediate(strayCanvas);
			}

			var document = GetComponent<UIDocument>();
			if (document == null)
			{
				document = gameObject.AddComponent<UIDocument>();
			}

			// Empty ThemeStyleSheet must be assigned BEFORE panelSettings is set on the document,
			// otherwise Unity logs the "No Theme Style Sheet set to PanelSettings" warning twice
			// (once on assignment and again on the first SetActive(true)).
			if (document.panelSettings == null)
			{
				var panel = ScriptableObject.CreateInstance<PanelSettings>();
				panel.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
				document.panelSettings = panel;
			}

			ToolkitFeature = GetComponent<UiToolkitPresenterFeature>();
			if (ToolkitFeature == null)
			{
				ToolkitFeature = gameObject.AddComponent<UiToolkitPresenterFeature>();
			}

			DelayFeature = GetComponent<TimeDelayFeature>();
			if (DelayFeature == null)
			{
				DelayFeature = gameObject.AddComponent<TimeDelayFeature>();
			}

			ToolkitFeature.SetDocument(document);
			DelayFeature.SetDelays(0.01f, DelayFeature.CloseDelayInSeconds);
		}
	}
}
