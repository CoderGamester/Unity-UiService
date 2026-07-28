using System.Collections.Generic;
using GameLovers.UiService.Rendering;
using NUnit.Framework;

namespace GameLovers.UiService.Tests
{
	/// <summary>
	/// Pure ordering-logic tests -- no URP or engine types touched, even though the assembly
	/// this type lives in (GameLovers.UiService.Urp) references URP.
	/// </summary>
	[TestFixture]
	public class UiCameraStackRegistryTests
	{
		[Test]
		public void InsertIndex_EmptyList_ReturnsZero()
		{
			Assert.AreEqual(0, UiCameraStackRegistry.InsertIndex(new List<int>(), 5));
		}

		[Test]
		public void InsertIndex_LowerPriority_InsertsAtStart()
		{
			var existing = new List<int> { 10, 20, 30 };
			Assert.AreEqual(0, UiCameraStackRegistry.InsertIndex(existing, 5));
		}

		[Test]
		public void InsertIndex_HigherPriority_InsertsAtEnd()
		{
			var existing = new List<int> { 10, 20, 30 };
			Assert.AreEqual(3, UiCameraStackRegistry.InsertIndex(existing, 40));
		}

		[Test]
		public void InsertIndex_MiddlePriority_InsertsBetween()
		{
			var existing = new List<int> { 10, 20, 30 };
			Assert.AreEqual(2, UiCameraStackRegistry.InsertIndex(existing, 25));
		}

		[Test]
		public void InsertIndex_EqualPriority_InsertsAfterExisting()
		{
			var existing = new List<int> { 10, 20, 20, 30 };
			Assert.AreEqual(3, UiCameraStackRegistry.InsertIndex(existing, 20));
		}

		[Test]
		public void InsertIndex_SequentialInserts_ProduceSortedOrder()
		{
			var priorities = new List<int>();

			void InsertAndTrack(int priority)
			{
				var index = UiCameraStackRegistry.InsertIndex(priorities, priority);
				priorities.Insert(index, priority);
			}

			InsertAndTrack(20);
			InsertAndTrack(5);
			InsertAndTrack(30);
			InsertAndTrack(15);

			CollectionAssert.AreEqual(new[] { 5, 15, 20, 30 }, priorities);
		}
	}
}
