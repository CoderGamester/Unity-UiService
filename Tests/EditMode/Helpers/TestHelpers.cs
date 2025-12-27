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
				AddressableAddress = address,
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
			
			// We need to use reflection or a helper because UiConfigs doesn't have a direct setter for Sets 
			// that takes UiSetConfig[]. It converts from UiSetConfigSerializable.
			// However, for testing purposes, we can manually initialize the service with these.
			
			return scriptableObject;
		}

		public static GameObject CreateTestPresenterPrefab<T>(string name = null) where T : UiPresenter
		{
			var go = new GameObject(name ?? typeof(T).Name);
			go.AddComponent<T>();
			go.AddComponent<Canvas>(); // Most presenters need a canvas
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
	
	/// <summary>
	/// Simple test presenter for basic tests
	/// </summary>
	public class TestUiPresenter : UiPresenter
	{
		public bool WasInitialized { get; private set; }
		public bool WasOpened { get; private set; }
		public bool WasClosed { get; private set; }
		public int OpenCount { get; private set; }
		public int CloseCount { get; private set; }

		protected override void OnInitialized()
		{
			WasInitialized = true;
		}

		protected override void OnOpened()
		{
			WasOpened = true;
			OpenCount++;
		}

		protected override void OnClosed()
		{
			WasClosed = true;
			CloseCount++;
		}
	}
	
	/// <summary>
	/// Test presenter with data support
	/// </summary>
	public struct TestPresenterData
	{
		public int Id;
		public string Name;
	}

	public class TestDataUiPresenter : UiPresenter<TestPresenterData>
	{
		public bool WasDataSet { get; private set; }
		public TestPresenterData ReceivedData { get; private set; }

		protected override void OnSetData()
		{
			WasDataSet = true;
			ReceivedData = Data;
		}
	}
}

