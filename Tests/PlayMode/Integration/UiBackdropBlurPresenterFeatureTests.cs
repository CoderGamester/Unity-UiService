using System.Collections;
using GameLovers.UiService.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Covers the registration lifecycle only -- whether UiBackdropBlurRequests sees a request
	/// appear/disappear at the right moments. Does NOT attempt to force an actual GPU render:
	/// this environment runs Unity in -nographics batchmode (no on-screen surface to render to),
	/// and a RenderTexture-target camera is excluded from injection by design (see
	/// UiBackdropBlurShouldInjectTests), so neither path would exercise or prove anything about the
	/// real shader execution. Whether the blur actually looks right is an explicitly documented
	/// coverage gap -- see docs/urp-rendering.md.
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
		public IEnumerator OnPresenterOpening_RegistersRequest()
		{
			_feature.OnPresenterOpening();

			Assert.AreEqual(1, UiBackdropBlurRequests.ActiveCount);
			Assert.IsTrue(UiBackdropBlurRequests.TryGetActive(out _));
			yield return null;
		}

		[UnityTest]
		public IEnumerator OnPresenterOpening_CalledTwice_DoesNotDoubleRegister()
		{
			_feature.OnPresenterOpening();
			_feature.OnPresenterOpening();

			Assert.AreEqual(1, UiBackdropBlurRequests.ActiveCount);
			yield return null;
		}

		[UnityTest]
		public IEnumerator DisablingPresenter_UnregistersRequest()
		{
			_feature.OnPresenterOpening();
			Assert.AreEqual(1, UiBackdropBlurRequests.ActiveCount);

			_go.SetActive(false); // fires OnDisable

			Assert.AreEqual(0, UiBackdropBlurRequests.ActiveCount);
			yield return null;
		}

		[UnityTest]
		public IEnumerator Reopen_AfterDisable_ReRegisters()
		{
			_feature.OnPresenterOpening();
			_go.SetActive(false);
			_go.SetActive(true);
			_feature.OnPresenterOpening();

			Assert.AreEqual(1, UiBackdropBlurRequests.ActiveCount);
			yield return null;
		}

		[UnityTest]
		public IEnumerator ConfiguredSettings_AreRegisteredCorrectly()
		{
			_feature.ConfigureForTest(downsample: 2, iterations: 3, spread: 1.5f, tint: Color.blue, sortKey: 7);
			_feature.OnPresenterOpening();

			Assert.IsTrue(UiBackdropBlurRequests.TryGetActive(out var settings));
			Assert.AreEqual(2, settings.Downsample);
			Assert.AreEqual(3, settings.Iterations);
			Assert.AreEqual(1.5f, settings.Spread);
			Assert.AreEqual(Color.blue, settings.Tint);
			yield return null;
		}
	}
}
