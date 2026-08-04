using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <summary>
	/// Represents a configuration set of UIs that can be managed together in the <seealso cref="UiService"/>
	/// This can be helpful for a UI combo set that are always visible together (ex: player Hud with currency & settings)
	/// Supports multiple instances of the same UI type via UiInstanceId
	/// </summary>
	public struct UiSetConfig
	{
		public int SetId;
		public UiInstanceId[] UiInstanceIds;
	}

	/// <summary>
	/// Serializable entry for a UI instance in a set.
	/// Stores Type as string and optional instance address.
	/// This is more robust than storing addressable addresses which can change.
	/// </summary>
	[Serializable]
	public struct UiSetEntry
	{
		/// <summary>
		/// The AssemblyQualifiedName of the UI presenter type
		/// </summary>
		public string UiTypeName;
		
		/// <summary>
		/// Optional instance address for multi-instance support. Empty string means default instance.
		/// </summary>
		public string InstanceAddress;
		
		/// <summary>
		/// Resolves this entry to a runtime id. Returns <c>default</c> and logs a warning when
		/// <see cref="UiTypeName"/> no longer resolves, so a renamed presenter degrades instead of throwing.
		/// </summary>
		public UiInstanceId ToUiInstanceId()
		{
			var type = Type.GetType(UiTypeName);
			if (type == null)
			{
				Debug.LogWarning($"Could not find type: {UiTypeName}");
				return default;
			}
			return new UiInstanceId(type, string.IsNullOrEmpty(InstanceAddress) ? null : InstanceAddress);
		}
		
		/// <summary>
		/// Flattens a runtime id for serialization, storing the presenter by assembly-qualified name.
		/// </summary>
		public static UiSetEntry FromUiInstanceId(UiInstanceId instanceId)
		{
			return new UiSetEntry
			{
				UiTypeName = instanceId.PresenterType.AssemblyQualifiedName,
				InstanceAddress = instanceId.InstanceAddress ?? string.Empty
			};
		}
	}

	/// <summary>
	/// Necessary to serialize the data in scriptable object.
	/// Now stores Type names instead of addressable addresses for robustness.
	/// </summary>
	[Serializable]
	public struct UiSetConfigSerializable
	{
		public int SetId;
		public List<UiSetEntry> UiEntries;

		/// <summary>
		/// Rehydrates a set, silently dropping entries whose presenter type no longer resolves.
		/// </summary>
		public static UiSetConfig ToUiSetConfig(UiSetConfigSerializable serializable)
		{
			var instanceIds = new List<UiInstanceId>();
			
			if (serializable.UiEntries != null)
			{
				foreach (var entry in serializable.UiEntries)
				{
					var instanceId = entry.ToUiInstanceId();
					if (instanceId.PresenterType != null)
					{
						instanceIds.Add(instanceId);
					}
				}
			}
			
			return new UiSetConfig 
			{ 
				SetId = serializable.SetId, 
				UiInstanceIds = instanceIds.ToArray()
			};
		}

		/// <summary>
		/// Flattens a set for serialization, skipping ids that carry no presenter type.
		/// </summary>
		public static UiSetConfigSerializable FromUiSetConfig(UiSetConfig config)
		{
			var entries = new List<UiSetEntry>();
			
			if (config.UiInstanceIds != null)
			{
				foreach (var instanceId in config.UiInstanceIds)
				{
					if (instanceId.PresenterType != null)
					{
						entries.Add(UiSetEntry.FromUiInstanceId(instanceId));
					}
				}
			}
			
			return new UiSetConfigSerializable 
			{ 
				SetId = config.SetId, 
				UiEntries = entries 
			};
		}
	}
}