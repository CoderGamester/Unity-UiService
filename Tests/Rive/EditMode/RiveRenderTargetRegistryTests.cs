using NUnit.Framework;
using UnityEngine;

namespace GameLovers.UiService.Rive.Tests
{
	[TestFixture]
	public sealed class RiveRenderTargetRegistryTests
	{
		[Test]
		// ADMIT: RiveRenderTargetRegistry must share one atlas strategy and keep it alive until its final consumer releases.
		// RCR: RiveRenderTargetRegistry.Acquire — make every key unique → RED (strategies were not shared). 2026-08-13
		public void Acquire_SharedAtlas_ReferenceCountsOneStrategy()
		{
			RiveRenderTargetLease first = RiveRenderTargetRegistry.Acquire(
				UiRenderTargetPolicy.SharedAtlas,
				"world",
				new Vector2Int(512, 512),
				1,
				2);
			RiveRenderTargetLease second = RiveRenderTargetRegistry.Acquire(
				UiRenderTargetPolicy.SharedAtlas,
				"world",
				new Vector2Int(512, 512),
				1,
				2);
			var strategyObject = (MonoBehaviour)first.Strategy;

			Assert.That(second.Strategy, Is.SameAs(first.Strategy));
			first.Dispose();
			Assert.That(strategyObject, Is.Not.Null, "The strategy must survive while a second lease is active.");
			second.Dispose();
			Assert.That(strategyObject == null, Is.True, "The final release must destroy the shared strategy.");
		}

		[Test]
		// ADMIT: RiveRenderTargetRegistry must not retain strategy GameObjects across repeated presenter lifetimes.
		// RCR: RiveRenderTargetRegistry.Release — skip final-owner destruction → RED (cycle 0 retained strategy). 2026-08-13
		public void AcquireRelease_OneHundredCycles_LeavesNoStrategyOwners()
		{
			for (int i = 0; i < 100; i++)
			{
				RiveRenderTargetLease lease = RiveRenderTargetRegistry.Acquire(
					UiRenderTargetPolicy.Pooled,
					"cycle",
					new Vector2Int(256, 256),
					1,
					2);
				var strategyObject = (MonoBehaviour)lease.Strategy;

				lease.Dispose();

				Assert.That(strategyObject == null, Is.True, $"Cycle {i} retained its pooled strategy.");
			}
		}
	}
}
