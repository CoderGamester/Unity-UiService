using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLovers.UiService.Tests
{
	[TestFixture]
	public sealed class UiPlacementTests
	{
		private GameObject _screenRoot;
		private GameObject _followTarget;

		[SetUp]
		public void SetUp()
		{
			_screenRoot = new GameObject("Ui");
			_followTarget = new GameObject("Target");
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_screenRoot);
			Object.DestroyImmediate(_followTarget);
		}

		[Test]
		// ADMIT: DefaultUiPlacement must retain one stable world root under the service-owned hierarchy.
		// RCR: DefaultUiPlacement.Resolve — route every policy as ScreenRoot → RED (world result was screen root). 2026-08-13
		public void Resolve_WorldRoot_ReturnsCachedChild()
		{
			var placement = new DefaultUiPlacement(_screenRoot.transform);

			Transform first = placement.Resolve(UiPlacementSpace.WorldRoot, null);
			Transform second = placement.Resolve(UiPlacementSpace.WorldRoot, null);

			Assert.That(second, Is.SameAs(first));
			Assert.That(first.parent, Is.SameAs(_screenRoot.transform));
		}

		[Test]
		// ADMIT: FollowTarget placement must reject a missing runtime target rather than silently using the screen root.
		// RCR: DefaultUiPlacement.Resolve — route every policy as ScreenRoot → RED (no ArgumentNullException). 2026-08-13
		public void Resolve_FollowTargetWithoutTarget_ThrowsNamedException()
		{
			var placement = new DefaultUiPlacement(_screenRoot.transform);

			var exception = Assert.Throws<ArgumentNullException>(
				() => placement.Resolve(UiPlacementSpace.FollowTarget, null));

			Assert.That(exception.ParamName, Is.EqualTo("followTarget"));
		}
	}
}
