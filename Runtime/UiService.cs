using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <inheritdoc />
	public class UiService : IUiServiceInit
	{
		public static readonly UnityEvent<DeviceOrientation, DeviceOrientation> OnOrientationChanged = new ();
		public static readonly UnityEvent<Vector2, Vector2> OnResolutionChanged = new ();

		/// <summary>
		/// Internal static reference to the current analytics instance for editor tools only.
		/// This is set when a UiService instance is created and cleared when disposed.
		/// Only accessible to editor code within this package.
		/// </summary>
		internal static IUiAnalytics CurrentAnalytics { get; private set; }
		
		private readonly IUiAssetLoader _assetLoader;
		private readonly IUiAnalytics _analytics;
		private readonly IDictionary<Type, UiConfig> _uiConfigs = new Dictionary<Type, UiConfig>();
		private readonly IList<UiInstanceId> _visibleUiList = new List<UiInstanceId>();
		private readonly IDictionary<int, UiSetConfig> _uiSets = new Dictionary<int, UiSetConfig>();
		private readonly IDictionary<Type, IList<UiInstance>> _uiPresenters = new Dictionary<Type, IList<UiInstance>>();

		private readonly IReadOnlyDictionary<int, UiSetConfig> _uiSetsReadOnly;
		private readonly IReadOnlyList<UiInstanceId> _visiblePresentersReadOnly;

		private Transform _uiParent;
		private bool _disposed;

		/// <inheritdoc />
		public IReadOnlyDictionary<int, UiSetConfig> UiSets => _uiSetsReadOnly;

		/// <inheritdoc />
		public IReadOnlyList<UiInstanceId> VisiblePresenters => _visiblePresentersReadOnly;

		/// <summary>
		/// Gets the analytics instance being used by this service
		/// </summary>
		public IUiAnalytics Analytics => _analytics;

		public UiService() : this(new AddressablesUiAssetLoader(), null) { }

		public UiService(IUiAssetLoader assetLoader) : this(assetLoader, null) { }

		public UiService(IUiAssetLoader assetLoader, IUiAnalytics analytics)
		{
			_assetLoader = assetLoader;
			_analytics = analytics ?? new NullAnalytics();
			
			// Set static reference for editor/debugging access
			CurrentAnalytics = _analytics;
			
			// Initialize readonly wrappers to avoid allocations on property access
			_uiSetsReadOnly = new ReadOnlyDictionary<int, UiSetConfig>(_uiSets);
			_visiblePresentersReadOnly = new ReadOnlyCollection<UiInstanceId>(_visibleUiList);
		}

		/// <inheritdoc />
		public void Init(UiConfigs configs)
		{
			if (configs == null)
			{
				throw new ArgumentNullException(nameof(configs), "UiConfigs cannot be null");
			}

			var uiConfigs = configs.Configs;
			var sets = configs.Sets;

			foreach (var uiConfig in uiConfigs)
			{
				if (string.IsNullOrEmpty(uiConfig.Address))
				{
					throw new ArgumentException($"UiConfig for type '{uiConfig.UiType.Name}' has empty address. This UI will fail to load.");
				}
				if (uiConfig.UiType == null)
				{
					throw new ArgumentException($"UiConfig with address '{uiConfig.Address}' has null UiType, skipping");
				}

				if (uiConfig.Layer < 0)
				{
					Debug.LogWarning($"UiConfig for type '{uiConfig.UiType.Name}' has negative layer number ({uiConfig.Layer}). This may cause unexpected behavior.");
				}
				if (uiConfig.Layer > 1000)
				{
					Debug.LogWarning($"UiConfig for type '{uiConfig.UiType.Name}' has very high layer number ({uiConfig.Layer}). Consider using lower values for better organization.");
				}

				AddUiConfig(uiConfig);
			}

			foreach (var set in sets)
			{
				AddUiSet(set);
			}

			_uiParent = new GameObject("Ui").transform;

			_uiParent.gameObject.AddComponent<UiServiceMonoComponent>();
			Object.DontDestroyOnLoad(_uiParent.gameObject);
		}

		/// <inheritdoc />
		public List<UiInstance> GetLoadedPresenters()
		{
			var list = new List<UiInstance>();
			
			foreach (var kvp in _uiPresenters)
			{
				list.AddRange(kvp.Value);
			}
			
			return list;
		}

		/// <inheritdoc />
		public T GetUi<T>() where T : UiPresenter
		{
			return GetUi<T>(ResolveInstanceAddress(typeof(T)));
		}
		
		/// <summary>
		/// Requests the UI of given type <typeparamref name="T"/> with the specified instance address
		/// </summary>
		/// <param name="instanceAddress">Optional instance address. Use null for default/singleton instance.</param>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given type and instance address
		/// </exception>
		public T GetUi<T>(string instanceAddress) where T : UiPresenter
		{
			if (!TryFindPresenter(typeof(T), instanceAddress, out var presenter))
			{
				throw new KeyNotFoundException($"UI presenter of type {typeof(T).Name} with instance '{instanceAddress ?? "default"}' not found.");
			}
			return presenter as T;
		}

		/// <inheritdoc />
		public bool IsVisible<T>() where T : UiPresenter
		{
			return IsVisible<T>(ResolveInstanceAddress(typeof(T)));
		}
		
		/// <summary>
		/// Requests the visible state of the given UI type <typeparamref name="T"/> with the specified instance address
		/// </summary>
		/// <param name="instanceAddress">Optional instance address. Use null for default/singleton instance.</param>
		public bool IsVisible<T>(string instanceAddress) where T : UiPresenter
		{
			var instanceId = new UiInstanceId(typeof(T), instanceAddress);
			return _visibleUiList.Contains(instanceId);
		}

		/// <inheritdoc />
		public void AddUiConfig(UiConfig config)
		{
			if (!_uiConfigs.TryAdd(config.UiType, config))
			{
				Debug.LogWarning($"The UiConfig {config.Address} was already added");
			}
		}

		/// <inheritdoc />
		public void AddUiSet(UiSetConfig uiSet)
		{
			if (!_uiSets.TryAdd(uiSet.SetId, uiSet))
			{
				Debug.LogWarning($"The Ui Configuration with the id {uiSet.SetId.ToString()} was already added");
			}
		}

		/// <inheritdoc />
		public void AddUi<T>(T ui, int layer, bool openAfter = false) where T : UiPresenter
		{
			AddUi(ui, layer, string.Empty, openAfter);
		}
		
		/// <summary>
		/// Adds a UI presenter to the service with an optional instance address
		/// </summary>
		/// <param name="ui">The UI presenter to add</param>
		/// <param name="layer">The layer to include the UI presenter in</param>
		/// <param name="instanceAddress">Optional instance address. Use null for default/singleton instance.</param>
		/// <param name="openAfter">Whether to open the UI presenter after adding it</param>
		public void AddUi<T>(T ui, int layer, string instanceAddress, bool openAfter = false) where T : UiPresenter
		{
			var type = ui.GetType().UnderlyingSystemType;
			var instanceId = new UiInstanceId(type, instanceAddress);

			// Check if already exists
			if (TryFindPresenter(type, instanceAddress, out _))
			{
				Debug.LogWarning($"The Ui {instanceId} was already added");
				return;
			}
			
			// Add to type-indexed collection
			if (!_uiPresenters.TryGetValue(type, out var instanceList))
			{
				instanceList = new List<UiInstance>();
				_uiPresenters[type] = instanceList;
			}
			instanceList.Add(new UiInstance(type, instanceAddress, ui));
			
			// Ensure Canvas sorting order matches layer
			EnsureCanvasSortingOrder(ui.gameObject, layer);

			ui.Init(this);

			if (openAfter)
			{
				OpenUi(instanceId);
			}
		}

		/// <inheritdoc />
		public bool RemoveUi(Type type)
		{
			return RemoveUi(type, ResolveInstanceAddress(type));
		}
		
		/// <summary>
		/// Removes the UI of the specified type and instance address from the service without unloading it
		/// </summary>
		/// <param name="type">The type of UI to remove</param>
		/// <param name="instanceAddress">Optional instance address. Use null for default/singleton instance.</param>
		/// <returns>True if the UI was removed, false otherwise</returns>
		public bool RemoveUi(Type type, string instanceAddress)
		{
			var instanceId = new UiInstanceId(type, instanceAddress);
			_visibleUiList.Remove(instanceId);
			
			if (!_uiPresenters.TryGetValue(type, out var instanceList))
			{
				return false;
			}

			// Find and remove the instance
			for (int i = 0; i < instanceList.Count; i++)
			{
				if (instanceList[i].Address == instanceAddress)
				{
					instanceList.RemoveAt(i);
					
					// Clean up empty type entry
					if (instanceList.Count == 0)
					{
						_uiPresenters.Remove(type);
					}
					
					return true;
				}
			}
			
			return false;
		}

		/// <inheritdoc />
		public List<UiPresenter> RemoveUiSet(int setId)
		{
			if (!_uiSets.TryGetValue(setId, out var set))
			{
				throw new KeyNotFoundException($"UI Set with id {setId} not found.");
			}
			
			var list = new List<UiPresenter>();

			foreach (var instanceId in set.UiInstanceIds)
			{
				if (!TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var ui))
				{
					continue;
				}

				RemoveUi(instanceId.PresenterType, instanceId.InstanceAddress);

				list.Add(ui);
			}

			return list;
		}

		/// <inheritdoc />
		public async UniTask<UiPresenter> LoadUiAsync(Type type, bool openAfter = false, CancellationToken cancellationToken = default)
		{
			return await LoadUiAsync(type, ResolveInstanceAddress(type), openAfter, cancellationToken);
		}
		
		/// <summary>
		/// Loads the UI of the specified type asynchronously with an optional instance address
		/// </summary>
		/// <param name="type">The type of UI to load</param>
		/// <param name="instanceAddress">Optional instance address. Use null for default/singleton instance.</param>
		/// <param name="openAfter">Whether to open the UI after loading</param>
		/// <param name="cancellationToken">Cancellation token to cancel the operation</param>
		/// <returns>A task that completes with the loaded UI</returns>
		public async UniTask<UiPresenter> LoadUiAsync(Type type, string instanceAddress, bool openAfter = false, CancellationToken cancellationToken = default)
		{
			if (!_uiConfigs.TryGetValue(type, out var config))
			{
				throw new KeyNotFoundException($"The UiConfig of type {type} was not added to the service. Call {nameof(AddUiConfig)} first");
			}

			var instanceId = new UiInstanceId(type, instanceAddress);
			
			if (TryFindPresenter(type, instanceAddress, out var existingUi))
			{
				Debug.LogWarning($"The Ui {instanceId} was already loaded");
				existingUi.gameObject.SetActive(openAfter);

				return existingUi;
			}

			_analytics.TrackLoadStart(type);

			// Parent directly to _uiParent - no layer GameObjects needed
			var gameObject = await _assetLoader.InstantiatePrefab(config, _uiParent, cancellationToken);

			// Double check if the same UiPresenter was already loaded. This can happen if the coder spam calls LoadUiAsync
			if (TryFindPresenter(type, instanceAddress, out var uiDouble))
			{
				_assetLoader.UnloadAsset(gameObject);
				uiDouble.gameObject.SetActive(openAfter);

				return uiDouble;
			}

			var uiPresenter = gameObject.GetComponent<UiPresenter>();

			gameObject.SetActive(false);
			AddUi(uiPresenter, config.Layer, instanceAddress, openAfter);
			
			_analytics.TrackLoadComplete(type, config.Layer);

			return uiPresenter;
		}

		/// <inheritdoc />
		public IList<UniTask<UiPresenter>> LoadUiSetAsync(int setId)
		{
			var uiTasks = new List<UniTask<UiPresenter>>();

			if (_uiSets.TryGetValue(setId, out var set))
			{
				foreach (var instanceId in set.UiInstanceIds)
				{
					if (TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out _))
					{
						continue;
					}

					uiTasks.Add(LoadUiAsync(instanceId.PresenterType, instanceId.InstanceAddress));
				}
			}

			return uiTasks;
		}

		/// <inheritdoc />
		public void UnloadUi(Type type)
		{
			UnloadUi(type, ResolveInstanceAddress(type));
		}
		
		/// <summary>
		/// Unloads the UI of the specified type and instance address
		/// </summary>
		/// <param name="type">The type of UI to unload</param>
		/// <param name="instanceAddress">Optional instance address. Use null for default/singleton instance.</param>
		public void UnloadUi(Type type, string instanceAddress)
		{
			var instanceId = new UiInstanceId(type, instanceAddress);
			
			if (!TryFindPresenter(type, instanceAddress, out var ui))
			{
				throw new KeyNotFoundException($"Cannot unload UI {instanceId}. It is not loaded.");
			}
			
			var config = _uiConfigs[type];
			
			RemoveUi(type, instanceAddress);

			_assetLoader.UnloadAsset(ui.gameObject);
			_analytics.TrackUnload(type, config.Layer);
		}

		/// <inheritdoc />
		public void UnloadUiSet(int setId)
		{
			var set = _uiSets[setId];

			foreach (var instanceId in set.UiInstanceIds)
			{
				if (TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out _))
				{
					UnloadUi(instanceId.PresenterType, instanceId.InstanceAddress);
				}
			}
		}

		/// <inheritdoc />
		public async UniTask<UiPresenter> OpenUiAsync(Type type, CancellationToken cancellationToken = default)
		{
			return await OpenUiAsync(type, ResolveInstanceAddress(type), cancellationToken);
		}
		
		/// <summary>
		/// Opens a UI presenter asynchronously with an optional instance address
		/// </summary>
		/// <param name="type">The type of UI presenter to open</param>
		/// <param name="instanceAddress">Optional instance address. Use null for default/singleton instance.</param>
		/// <param name="cancellationToken">Cancellation token to cancel the operation</param>
		public async UniTask<UiPresenter> OpenUiAsync(Type type, string instanceAddress, CancellationToken cancellationToken = default)
		{
			var ui = await GetOrLoadUiAsync(type, instanceAddress, cancellationToken);

			OpenUi(new UiInstanceId(type, instanceAddress));

			return ui;
		}

		/// <inheritdoc />
		public async UniTask<UiPresenter> OpenUiAsync<TData>(Type type, TData initialData, CancellationToken cancellationToken = default) where TData : struct
		{
			return await OpenUiAsync(type, ResolveInstanceAddress(type), initialData, cancellationToken);
		}
		
		/// <summary>
		/// Opens a UI presenter asynchronously with initial data and an optional instance address
		/// </summary>
		/// <param name="type">The type of UI presenter to open</param>
		/// <param name="instanceAddress">Optional instance address. Use null for default/singleton instance.</param>
		/// <param name="initialData">The initial data to set</param>
		/// <param name="cancellationToken">Cancellation token to cancel the operation</param>
		public async UniTask<UiPresenter> OpenUiAsync<TData>(Type type, string instanceAddress, TData initialData, CancellationToken cancellationToken = default) where TData : struct
		{
			var ui = await GetOrLoadUiAsync(type, instanceAddress, cancellationToken);

			if (ui is UiPresenter<TData> uiPresenter)
			{
				uiPresenter.InternalSetData(initialData);
			}
			else
			{
				Debug.LogError($"The UiPresenter {type} is not a {nameof(UiPresenter<TData>)} type. " +
							$"Implement UiPresenter<{typeof(TData).Name}> to allow it to open with initial defined data");
				return ui;
			}
			
			OpenUi(new UiInstanceId(type, instanceAddress));

			return ui;
		}

		/// <inheritdoc />
		public void CloseUi(Type type, bool destroy = false)
		{
			CloseUi(type, ResolveInstanceAddress(type), destroy);
		}
		
		/// <summary>
		/// Closes a UI presenter with an optional instance address and optionally destroys its assets
		/// </summary>
		/// <param name="type">The type of UI presenter to close</param>
		/// <param name="instanceAddress">Optional instance address. Use null for default/singleton instance.</param>
		/// <param name="destroy">Whether to destroy the UI presenter's assets</param>
		public void CloseUi(Type type, string instanceAddress, bool destroy = false)
		{
			var instanceId = new UiInstanceId(type, instanceAddress);
			
			if (!_visibleUiList.Contains(instanceId))
			{
				Debug.LogWarning($"Is trying to close the {instanceId} ui but is not open");
				return;
			}

			_analytics.TrackCloseStart(type);
			
			_visibleUiList.Remove(instanceId);
			
			if (TryFindPresenter(type, instanceAddress, out var presenter))
			{
				presenter.InternalClose(destroy);
				
				var config = _uiConfigs[type];
				_analytics.TrackCloseComplete(type, config.Layer, destroy);
			}
		}

		/// <inheritdoc />
		public void CloseAllUi()
		{
			foreach (var instanceId in _visibleUiList)
			{
				if (TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var presenter))
				{
					presenter.InternalClose(false);
				}
			}

			_visibleUiList.Clear();
		}

		/// <inheritdoc />
		public void CloseAllUi(int layer)
		{
			for (int i = _visibleUiList.Count - 1; i >= 0; i--)
			{
				var instanceId = _visibleUiList[i];

				if (_uiConfigs[instanceId.PresenterType].Layer == layer)
				{
					if (TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var presenter))
					{
						presenter.InternalClose(false);
					}
					_visibleUiList.Remove(instanceId);
				}
			}
		}

		/// <inheritdoc />
		public void CloseAllUiSet(int setId)
		{
			var set = _uiSets[setId];

			foreach (var instanceId in set.UiInstanceIds)
			{
				CloseUi(instanceId.PresenterType, instanceId.InstanceAddress);
			}
		}

		/// <summary>
		/// Disposes of the UI service, cleaning up all resources and unsubscribing from events.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			// Clear static reference
			if (CurrentAnalytics == _analytics)
			{
				CurrentAnalytics = null;
			}

			// Close all visible UI
			CloseAllUi();

			// Unload all UI presenters
			var presenterInstances = new List<UiInstanceId>();
			foreach (var kvp in _uiPresenters)
			{
				var type = kvp.Key;
				foreach (var instance in kvp.Value)
				{
					presenterInstances.Add(new UiInstanceId(type, instance.Address));
				}
			}
			
			foreach (var instanceId in presenterInstances)
			{
				try
				{
					UnloadUi(instanceId.PresenterType, instanceId.InstanceAddress);
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"Failed to unload UI {instanceId} during disposal: {ex.Message}");
				}
			}

			// Clear all collections
			_uiPresenters.Clear();
			_visibleUiList.Clear();
			_uiConfigs.Clear();
			_uiSets.Clear();

			// Clean up static events
			// Note: We don't call RemoveAllListeners on static UnityEvents as it would affect other instances

			// Destroy UI parent GameObject
			if (_uiParent != null)
			{
				Object.Destroy(_uiParent.gameObject);
				_uiParent = null;
			}
		}

		/// <summary>
		/// Attempts to find a presenter by type and address
		/// </summary>
		/// <param name="type">The type of UI presenter to find</param>
		/// <param name="address">The instance address</param>
		/// <param name="presenter">The found presenter, or null if not found</param>
		/// <returns>True if the presenter was found, false otherwise</returns>
		private bool TryFindPresenter(Type type, string address, out UiPresenter presenter)
		{
			if (_uiPresenters.TryGetValue(type, out var instances))
			{
				foreach (var instance in instances)
				{
					if (instance.Address == address)
					{
						presenter = instance.Presenter;
						return true;
					}
				}
			}

			presenter = null;
			return false;
		}

		private void EnsureCanvasSortingOrder(GameObject gameObject, int layer)
		{
			if (gameObject.TryGetComponent<Canvas>(out var canvas))
			{
				canvas.sortingOrder = layer;
			}
			else if (gameObject.TryGetComponent<UnityEngine.UIElements.UIDocument>(out var document))
			{
				document.sortingOrder = layer;
			}
		}

		private void OpenUi(UiInstanceId instanceId)
		{
			if (_visibleUiList.Contains(instanceId))
			{
				Debug.LogWarning($"Is trying to open the {instanceId} ui but is already open");
				return;
			}

			_analytics.TrackOpenStart(instanceId.PresenterType);
			
			if (TryFindPresenter(instanceId.PresenterType, instanceId.InstanceAddress, out var presenter))
			{
				presenter.InternalOpen();
				_visibleUiList.Add(instanceId);
				
				var config = _uiConfigs[instanceId.PresenterType];
				_analytics.TrackOpenComplete(instanceId.PresenterType, config.Layer);
			}
		}

		private async UniTask<UiPresenter> GetOrLoadUiAsync(Type type, string instanceAddress, CancellationToken cancellationToken = default)
		{
			if (!TryFindPresenter(type, instanceAddress, out var ui))
			{
				ui = await LoadUiAsync(type, instanceAddress, false, cancellationToken);
			}

			return ui;
		}

		/// <summary>
		/// Resolves the instance address for a given type when no specific instance is requested.
		/// If only one instance of the type exists, returns that instance's address.
		/// If multiple instances exist, returns the first one found and logs a warning.
		/// If no instances exist, returns string.Empty (default).
		/// </summary>
		/// <param name="type">The type of UI presenter to resolve</param>
		/// <returns>The resolved instance address</returns>
		private string ResolveInstanceAddress(Type type)
		{
			if (!_uiPresenters.TryGetValue(type, out var instances))
			{
				// No instances found, return default (string.Empty)
				return string.Empty;
			}
			
			if (instances.Count == 1)
			{
				return instances[0].Address;
			}
			
			// Multiple instances found - log warning and return the first
			var instanceNames = new List<string>(instances.Count);
			foreach (var instance in instances)
			{
				instanceNames.Add(string.IsNullOrEmpty(instance.Address) ? "default" : instance.Address);
			}
			
			var firstMatch = instances[0];
			var selectedName = string.IsNullOrEmpty(firstMatch.Address) ? "default" : firstMatch.Address;
			Debug.LogWarning($"[UiService] Ambiguous call for {type.Name}: found {instances.Count} instances [{string.Join(", ", instanceNames)}]. " +
							$"Using '{selectedName}'. Specify instance address explicitly to avoid ambiguity.");
			
			return firstMatch.Address;
		}
	}
}