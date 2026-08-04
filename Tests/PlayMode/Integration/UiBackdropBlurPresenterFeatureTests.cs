using System.Collections;
using GameLovers.UiService.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Covers the open/close refcount only. Does NOT force a GPU render: this environment runs Unity in
	/// -nographics batchmode with no surface to render to, and a RenderTexture-target camera is excluded
	/// from injection by design, so neither path would prove anything about the shader. Whether the blur
	/// looks right is a documented coverage gap -- see docs/urp-rendering.md.
	/// </summary>
	[TestFixture]
	public class UiBackdropBlurPresenterFeatureTests
	{
		private GameObject _go;
		private UiBackdropBlurPresenterFeature _feature;

		[SetUp]
		public void Setup()
		{
			_go = new GameObject(nameof(UiBackdropBlurPresenterFeatureTests));
			_feature = _go.AddComponent<UiBackdropBlurPresenterFeature>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_go != null) Object.DestroyImmediate(_go);
		}

		[UnityTest]
		// ADMIT: UiBackdropBlurPresenterFeature.OnPresenterOpening must increment the static _openCount, which is the
		// only thing AnyOpen (and therefore the blur pass) reads.
		// RCR: UiBackdropBlurPresenterFeature.cs OnPresenterOpening — comment out `_openCount++;` → RED
		// (AnyOpen was False after opening). 2026-08-02
		public IEnumerator OnPresenterOpening_MarksBlurOpen()
		{
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();

			Assert.IsTrue(AnyOpen);
			yield return null;
		}

		[UnityTest]
		// ADMIT: UiBackdropBlurPresenterFeature's per-instance _counted guard is what keeps a reopened presenter from
		// double-incrementing the refcount and leaving the blur stuck on after its single close.
		// RCR: UiBackdropBlurPresenterFeature.cs OnPresenterOpening — replace `if (_counted)` with `if (false)` → RED
		// (AnyOpen still True after the close; a second unexpected setup error is also logged). 2026-08-02
		public IEnumerator OnPresenterOpening_CalledTwice_DoesNotDoubleCount()
		{
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();
			_feature.OnPresenterOpening();

			// A double count would survive one close and leave the blur stuck on.
			_go.SetActive(false);

			Assert.IsFalse(AnyOpen);
			yield return null;
		}

		[UnityTest]
		// ADMIT: UiBackdropBlurPresenterFeature.OnDisable must decrement _openCount, or the blur stays on screen for
		// the rest of the session once any presenter has opened.
		// RCR: UiBackdropBlurPresenterFeature.cs OnDisable — comment out `_openCount--;` → RED (AnyOpen was True
		// after the presenter was disabled). 2026-08-02
		public IEnumerator DisablingPresenter_ClearsBlur()
		{
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();
			Assert.IsTrue(AnyOpen);

			_go.SetActive(false); // fires OnDisable

			Assert.IsFalse(AnyOpen);
			yield return null;
		}

		[UnityTest]
		// ADMIT: UiBackdropBlurPresenterFeature.OnDisable must reset _counted, or the reopen path short-circuits on
		// its own stale guard and the blur never comes back.
		// RCR: UiBackdropBlurPresenterFeature.cs OnDisable — comment out `_counted = false;` → RED (AnyOpen was False
		// after reopening). Pins the other half of OnDisable from DisablingPresenter_ClearsBlur. 2026-08-02
		public IEnumerator Reopen_AfterDisable_MarksBlurOpenAgain()
		{
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();
			_go.SetActive(false);
			_go.SetActive(true);
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();

			Assert.IsTrue(AnyOpen);
			yield return null;
		}

		[UnityTest]
		// ADMIT: UiBackdropBlurPresenterFeature.OnDisable must decrement rather than reset _openCount — it is a
		// refcount, and one presenter closing must not drop a blur another presenter still wants.
		// RCR: UiBackdropBlurPresenterFeature.cs OnDisable — replace `_openCount--;` with `_openCount = 0;` → RED
		// (AnyOpen was False after only the first presenter closed). 2026-08-02
		public IEnumerator TwoPresenters_BlurStaysOpenUntilBothClose()
		{
			var secondGo = new GameObject("SecondBlurPresenter");
			var second = secondGo.AddComponent<UiBackdropBlurPresenterFeature>();

			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();
			ExpectMissingRendererFeatureError();
			second.OnPresenterOpening();

			_go.SetActive(false);
			Assert.IsTrue(AnyOpen, "One presenter closing must not clear a blur the other still wants.");

			secondGo.SetActive(false);
			Assert.IsFalse(AnyOpen);

			Object.DestroyImmediate(secondGo);
			yield return null;
		}

		private static bool AnyOpen => UiBackdropBlurPresenterFeature.AnyOpen;

		// No renderer feature is installed in the test environment, so every open logs the setup error.
		/// <summary>
		/// The missing-feature error is logged by <see cref="UiBackdropBlurPresenterFeature.OnPresenterOpening"/>
		/// ONLY when no renderer feature is installed, so the expectation has to follow the environment rather
		/// than assume one. Batchmode never instantiates the URP renderer, so nothing calls
		/// <c>UiBackdropBlurRendererFeature.Create()</c> and the error fires; in the Editor the Game view renders,
		/// the feature registers, and production correctly stays silent. Hard-coding the expectation made these
		/// tests pass only where the feature happens to be absent.
		/// The refcount assertions below are the actual subject and hold either way.
		/// </summary>
		private static void ExpectMissingRendererFeatureError()
		{
			if (UiBackdropBlurRendererFeature.IsInstalled)
			{
				return;
			}

			LogAssert.Expect(LogType.Error,
				new System.Text.RegularExpressions.Regex(".*UiBackdropBlurRendererFeature.*Add Renderer Feature.*"));
		}
	}
}
