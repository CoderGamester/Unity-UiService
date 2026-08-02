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
		public IEnumerator OnPresenterOpening_MarksBlurOpen()
		{
			ExpectMissingRendererFeatureError();
			_feature.OnPresenterOpening();

			Assert.IsTrue(AnyOpen);
			yield return null;
		}

		[UnityTest]
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
