using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <summary>
	/// Represents a configuration of an <seealso cref="UiPresenter"/> with all it's important data
	/// The Id is the int representation of the UI generated by the UiIdsGenerator code generator
	/// </summary>
	[Serializable]
	public struct UiConfig
	{
		public string AddressableAddress;
		public int Layer;
		public Type UiType;
		public bool LoadSynchronously;
	}

	/// <summary>
	/// ScriptableObject tool to import the <seealso cref="UiConfig"/> & <seealso cref="UiSetConfig"/> to be used in the <see cref="IUiService"/>
	/// </summary>
	[CreateAssetMenu(fileName = "UiConfigs", menuName = "ScriptableObjects/Configs/UiConfigs")]
	public class UiConfigs : ScriptableObject//, IConfigsContainer<UiConfig>
	{
		[SerializeField] private string _loadingSpinnerType;
		[SerializeField]
		private List<UiConfigSerializable> _configs = new List<UiConfigSerializable>();
		[SerializeField]
		private List<UiSetConfigSerializable> _sets = new List<UiSetConfigSerializable>();

		/// <summary>
		/// Gets or sets the type of the loading spinner
		/// </summary>
		public Type LoadingSpinnerType
		{
			get => String.IsNullOrEmpty(_loadingSpinnerType) ? null : Type.GetType(_loadingSpinnerType);
			set => _loadingSpinnerType = value?.GetType().AssemblyQualifiedName;
		}

		/// <summary>
		/// Gets or sets the type of the loading spinner as a string
		/// </summary>
		public string LoadingSpinnerTypeString
		{
			get => _loadingSpinnerType;
			set => _loadingSpinnerType = value;
		}

		/// <summary>
		/// Gets or sets the list of UI configurations
		/// </summary>
		public List<UiConfig> Configs
		{
			get { return _configs.ConvertAll(element => (UiConfig)element); }
			set { _configs = value.ConvertAll(element => (UiConfigSerializable)element); }
		}

		/// <summary>
		/// Gets the list of UI set configurations
		/// </summary>
		public List<UiSetConfig> Sets => _sets.ConvertAll(element => (UiSetConfig)element);

		/// <summary>
		/// Sets the new size of this scriptable object <seealso cref="UiSetConfig"/> list.
		/// The UiConfigSets have the same id value that the index in the list
		/// </summary>
		/// <param name="size">The new size of the list</param>
		public void SetSetsSize(int size)
		{
			if (size < _sets.Count)
			{
				_sets.RemoveRange(size, _sets.Count - size);
			}

			for (int i = 0; i < size; i++)
			{
				if (i < _sets.Count)
				{
					var cleanedConfigList = new List<string>(_sets[i].UiConfigsType.Count);

					foreach (var uiConfig in _sets[i].UiConfigsType)
					{
						if (_configs.FindIndex(config => config.UiType == uiConfig) > -1)
						{
							cleanedConfigList.Add(uiConfig);
						}
					}

					var set = _sets[i];
					set.UiConfigsType = cleanedConfigList;
					_sets[i] = set;
					continue;
				}

				_sets.Add(new UiSetConfigSerializable { SetId = i, UiConfigsType = new List<string>() });
			}
		}

		/// <summary>
		/// Necessary to serialize the data in scriptable object
		/// </summary>
		[Serializable]
		public struct UiConfigSerializable
		{
			public string AddressableAddress;
			public int Layer;
			public string UiType;

			public static implicit operator UiConfig(UiConfigSerializable serializable)
			{
				return new UiConfig
				{
					AddressableAddress = serializable.AddressableAddress,
					Layer = serializable.Layer,
					UiType = Type.GetType(serializable.UiType)
				};
			}

			public static implicit operator UiConfigSerializable(UiConfig serializable)
			{
				return new UiConfigSerializable
				{
					AddressableAddress = serializable.AddressableAddress,
					Layer = serializable.Layer,
					UiType = serializable.UiType.AssemblyQualifiedName
				};
			}
		}

		/// <summary>
		/// Necessary to serialize the data in scriptable object
		/// </summary>
		[Serializable]
		public struct UiSetConfigSerializable
		{
			public int SetId;
			public List<string> UiConfigsType;

			public static implicit operator UiSetConfig(UiSetConfigSerializable serializable)
			{
				var configs = new List<Type>();

				foreach (var uiConfig in serializable.UiConfigsType)
				{
					configs.Add(Type.GetType(uiConfig));
				}

				return new UiSetConfig
				{
					SetId = serializable.SetId,
					UiConfigsType = configs.AsReadOnly()
				};
			}
		}
	}
}