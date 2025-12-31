using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLovers.UiService.Tests
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
			var scriptableObject = ScriptableObject.CreateInstance<UiConfigs>();
			scriptableObject.Configs = new List<UiConfig>(configs);
			return scriptableObject;
		}
		
		public static UiConfigs CreateTestConfigsWithSets(UiConfig[] configs, UiSetConfig[] sets)
		{
			var scriptableObject = ScriptableObject.CreateInstance<UiConfigs>();
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
	}
}

