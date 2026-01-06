using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Helper methods for creating test objects
	/// </summary>
	public static class TestHelpers
	{
		public static UiConfig CreateTestConfig(Type type, string address, int layer = 0, bool loadSync = false)
		{
			return new UiConfig
			{
				Address = address,
				Layer = layer,
				UiType = type,
				LoadSynchronously = loadSync
			};
		}
		
		public static UiConfigs CreateTestConfigs(params UiConfig[] configs)
		{
			var scriptableObject = ScriptableObject.CreateInstance<PrefabRegistryUiConfigs>();
			scriptableObject.Configs = new List<UiConfig>(configs);
			return scriptableObject;
		}
		
		public static UiConfigs CreateTestConfigsWithSets(UiConfig[] configs, UiSetConfig[] sets)
		{
			var scriptableObject = ScriptableObject.CreateInstance<PrefabRegistryUiConfigs>();
			scriptableObject.Configs = new List<UiConfig>(configs);

			// We use the Serializable versions to set the data
			var serializableSets = new List<UiSetConfigSerializable>();
			foreach (var set in sets)
			{
				serializableSets.Add(UiSetConfigSerializable.FromUiSetConfig(set));
			}

			// We need reflection to set the private field _sets because it's the only way to bypass the read-only property
			var field = typeof(UiConfigs).GetField("_sets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			field.SetValue(scriptableObject, serializableSets);

			return scriptableObject;
		}

		public static GameObject CreateTestPresenterPrefab<T>(string name = null) where T : UiPresenter
		{
			var go = new GameObject(name ?? typeof(T).Name);
			go.AddComponent<Canvas>(); // Most presenters need a canvas
			go.AddComponent<T>(); // Add presenter after canvas so it can find it
			go.SetActive(false);
			return go;
		}
		
		public static UiSetConfig CreateTestUiSet(int setId, params UiInstanceId[] instanceIds)
		{
			return new UiSetConfig
			{
				SetId = setId,
				UiInstanceIds = instanceIds
			};
		}

		public static UiSetEntry CreateSetEntry(Type type, string address = "")
		{
			return new UiSetEntry
			{
				UiTypeName = type.AssemblyQualifiedName,
				InstanceAddress = address
			};
		}

		/// <summary>
		/// Waits until the UIDocument's panel is attached.
		/// UI Toolkit requires a few frames for panel attachment after activation.
		/// </summary>
		/// <param name="document">The UIDocument to wait for.</param>
		/// <param name="maxFrames">Maximum frames to wait before failing.</param>
		/// <returns>Coroutine enumerator.</returns>
		public static IEnumerator WaitForPanelAttachment(UIDocument document, int maxFrames = 10)
		{
			for (int i = 0; i < maxFrames; i++)
			{
				if (document != null && document.rootVisualElement?.panel != null)
				{
					yield break;
				}
				yield return null;
			}

			Assert.Fail($"UIDocument panel not attached after {maxFrames} frames");
		}

		/// <summary>
		/// Waits until the presenter's UIDocument panel is attached.
		/// </summary>
		/// <param name="presenter">The presenter containing a UIDocument.</param>
		/// <param name="maxFrames">Maximum frames to wait before failing.</param>
		/// <returns>Coroutine enumerator.</returns>
		public static IEnumerator WaitForPanelAttachment(UiPresenter presenter, int maxFrames = 10)
		{
			var document = presenter?.GetComponent<UIDocument>();
			if (document == null)
			{
				Assert.Fail("Presenter does not have a UIDocument component");
				yield break;
			}

			yield return WaitForPanelAttachment(document, maxFrames);
		}
	}
}

