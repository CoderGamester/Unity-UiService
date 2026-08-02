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
	public class UiCameraStackInsertIndexTests
	{
		[Test]
		// ADMIT: UiCameraStackFeature.InsertIndex must seed its scan at 0, or the very first camera stacked onto
		// an empty URP cameraStack is inserted past the end of the list.
		// RCR: UiCameraStackFeature.cs InsertIndex — replace `var index = 0;` with `var index = 1;` → RED
		// (expected 0, was 1). 2026-08-02
		public void InsertIndex_EmptyList_ReturnsZero()
		{
			Assert.AreEqual(0, UiCameraStackFeature.InsertIndex(new List<int>(), 5));
		}

		[Test]
		// ADMIT: UiCameraStackFeature.InsertIndex breaks on the first entry greater than the new priority; the
		// comparison direction is what puts a low-priority camera underneath the ones already stacked.
		// RCR: UiCameraStackFeature.cs InsertIndex — invert the comparison to
		// `existingPriorities[index] < priority` → RED (expected 0, was 3). 2026-08-02
		public void InsertIndex_LowerPriority_InsertsAtStart()
		{
			var existing = new List<int> { 10, 20, 30 };
			Assert.AreEqual(0, UiCameraStackFeature.InsertIndex(existing, 5));
		}

		[Test]
		// ADMIT: UiCameraStackFeature.InsertIndex's loop must scan the whole list, or the highest-priority camera
		// lands one slot short of the end and renders under the camera it should cover.
		// RCR: UiCameraStackFeature.cs InsertIndex — shorten the loop bound to
		// `index < existingPriorities.Count - 1` → RED (expected 3, was 2). 2026-08-02
		public void InsertIndex_HigherPriority_InsertsAtEnd()
		{
			var existing = new List<int> { 10, 20, 30 };
			Assert.AreEqual(3, UiCameraStackFeature.InsertIndex(existing, 40));
		}

		[Test]
		// ADMIT: UiCameraStackFeature.InsertIndex must stop at the first higher-priority entry; without the break
		// every camera is appended and the stack stops being ordered.
		// RCR: UiCameraStackFeature.cs InsertIndex — replace the `break;` with `continue;` → RED
		// (expected 2, was 3). 2026-08-02
		public void InsertIndex_MiddlePriority_InsertsBetween()
		{
			var existing = new List<int> { 10, 20, 30 };
			Assert.AreEqual(2, UiCameraStackFeature.InsertIndex(existing, 25));
		}

		[Test]
		// ADMIT: UiCameraStackFeature.InsertIndex uses a strict `>` so equal priorities tie-break after existing
		// entries; `>=` would put each new camera underneath its equals, inverting draw order between reopens.
		// RCR: UiCameraStackFeature.cs InsertIndex — relax the comparison to `>=` → RED (expected 3, was 1).
		// The other five ordering tests stay green: none of them supplies a priority equal to an existing one. 2026-08-02
		public void InsertIndex_EqualPriority_InsertsAfterExisting()
		{
			var existing = new List<int> { 10, 20, 20, 30 };
			Assert.AreEqual(3, UiCameraStackFeature.InsertIndex(existing, 20));
		}

		[Test]
		// ADMIT: UiCameraStackFeature.InsertIndex must keep the list sorted across repeated inserts, which is how
		// the real cameraStack stays ordered as presenters open one at a time.
		// RCR: UiCameraStackFeature.cs InsertIndex — invert the comparison to
		// `existingPriorities[index] < priority` → RED (CollectionAssert: expected {5,15,20,30}). 2026-08-02
		public void InsertIndex_SequentialInserts_ProduceSortedOrder()
		{
			var priorities = new List<int>();

			void InsertAndTrack(int priority)
			{
				var index = UiCameraStackFeature.InsertIndex(priorities, priority);
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
