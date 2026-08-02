using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Tests for <see cref="AddressablesUiAssetLoader"/>'s failure path when an Addressables key cannot
	/// be resolved.
	/// </summary>
	[TestFixture]
	public class AddressablesUiAssetLoaderTests
	{
		private const string UnknownAddress = "gamelovers_uiservice_tests_definitely_unresolvable_address_42";

		private AddressablesUiAssetLoader _loader;
		private GameObject _parent;

		[SetUp]
		public void Setup()
		{
			_loader = new AddressablesUiAssetLoader();
			_parent = new GameObject("AddressablesUiAssetLoaderTests_Parent");
		}

		[TearDown]
		public void TearDown()
		{
			// Safety net: restore strict log-assertion in case a failed assertion above skipped past the
			// explicit reset line inside the test body.
			LogAssert.ignoreFailingMessages = false;

			if (_parent != null)
			{
				UnityEngine.Object.DestroyImmediate(_parent);
			}
		}

		[UnityTest]
		// ADMIT: AddressablesUiAssetLoader.InstantiatePrefab throws operation.OperationException on failure with
		// no null guard, and no test exercised the unknown-address path (an OPEN Coverage Register row).
		// RCR: AddressablesUiAssetLoader.cs InstantiatePrefab — change `throw operation.OperationException;` to
		// `return null;` → RED (`thrown` stays null). A bare `throw null` is unreachable here: AsyncOperationBase
		// .Complete always substitutes a synthetic OperationException when the given one is null or empty, so this
		// pins the existing safe behaviour rather than a live bug. 2026-08-01
		public IEnumerator InstantiatePrefab_UnknownAddress_ThrowsWithDiagnosableException()
		{
			// Arrange
			var config = new UiConfig { Address = UnknownAddress, UiType = typeof(UiPresenter) };
			Exception thrown = null;

			// Addressables logs its own [Error] the moment the key fails to resolve, independent of
			// whatever the caller does with the thrown exception — and it does so as a variable, versioned
			// number of chained messages (observed: both a raw InvalidKeyException line AND a wrapping
			// "ChainOperation failed because dependent operation failed ..." line for the SAME failure).
			// Pinning the exact count/wording of a third-party package's own internal error-channel logging
			// is fragile and not the point of this test — what this test actually asserts is the exception
			// this package's own InstantiatePrefab throws (below), so the Addressables-internal noise is
			// suppressed for the duration of the Act rather than pinned message-by-message.
			LogAssert.ignoreFailingMessages = true;

			// Act — await the faulted UniTask directly inside a try/catch so the exception is fully
			// observed through the awaiter before it returns. The `task.ToCoroutine(resultHandler,
			// exceptionHandler)` overload does NOT reliably mark a UniTask<T>'s fault as observed: the
			// underlying UniTaskCompletionSource's ExceptionHolder can still be collected and finalized
			// on the GC finalizer thread, whose finalizer calls UniTaskScheduler.PublishUnobservedTaskException
			// -> Debug.LogException from a background thread. Unity's native logging path is not safe to
			// call from there for an Addressables OperationException specifically, and crashes the whole
			// batchmode process (SIGABRT, "BUG_IN_CLIENT_OF_LIBMALLOC_POINTER_BEING_FREED_WAS_NOT_ALLOCATED")
			// instead of failing the test. Directly awaiting consumes the exception synchronously on the
			// main thread, so the finalizer never has anything unobserved left to report.
			yield return UniTask.ToCoroutine(async () =>
			{
				try
				{
					await _loader.InstantiatePrefab(config, _parent.transform);
				}
				catch (Exception ex)
				{
					thrown = ex;
				}
			});

			LogAssert.ignoreFailingMessages = false;

			// Assert - some exception was thrown, and somewhere in its cause chain the message names the
			// unresolved address so the failure is diagnosable rather than a bare, unhelpful crash. Addressables
			// wraps the underlying failure in a ChainOperation "dependent operation failed" OperationException
			// (Library/PackageCache/com.unity.addressables/Runtime/ResourceManager/AsyncOperations/
			// ChainOperation.cs:76,202 — `new OperationException("ChainOperation failed...", x.OperationException)`)
			// whose OWN message does not mention the address; the address lives on the wrapped InnerException.
			// Walking the chain rather than asserting a fixed depth is robust to Addressables adding or removing
			// a wrapping layer in a future version.
			Assert.IsNotNull(thrown, "Instantiating an unresolvable Addressables key should throw.");

			var diagnosable = false;
			for (var ex = thrown; ex != null; ex = ex.InnerException)
			{
				if (ex.Message.Contains(UnknownAddress))
				{
					diagnosable = true;
					break;
				}
			}

			Assert.IsTrue(diagnosable, $"Expected the unresolved address to appear somewhere in the exception " +
				$"cause chain. Top-level message was: \"{thrown.Message}\"");
		}
	}
}
