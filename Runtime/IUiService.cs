using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <summary>
	/// Represents a UI presenter instance with its type, address and presenter reference
	/// </summary>
	public readonly struct UiInstance
	{
		/// <summary>
		/// The type of the UI presenter
		/// </summary>
		public readonly Type Type;
		
		/// <summary>
		/// The instance address (empty string for default/singleton instances)
		/// </summary>
		public readonly string Address;
		
		/// <summary>
		/// The UI presenter reference
		/// </summary>
		public readonly UiPresenter Presenter;

		public UiInstance(Type type, string address, UiPresenter presenter)
		{
			Type = type;
			Address = address;
			Presenter = presenter;
		}
	}

	/// <summary>
	/// This service provides an abstraction layer to interact with the game's UI <seealso cref="UiPresenter"/>
	/// The Ui Service is organized by layers. The higher the layer the more close is to the camera viewport.
	/// Supports multiple instances of the same UI type through the UiInstanceId system.
	/// </summary>
	public interface IUiService
	{
		
		/// <summary>
		/// Gets a read-only list of all Presenter instances currently visible.
		/// Each entry is a UiInstanceId containing the Type and instance name.
		/// </summary>
		IReadOnlyList<UiInstanceId> VisiblePresenters { get; }

		/// <summary>
		/// Gets a read-only dictionary of the containers of UI, called 'Ui Set' maintained by the UI service.
		/// </summary>
		IReadOnlyDictionary<int, UiSetConfig> UiSets { get; }

		/// <summary>
		/// Gets all UI presenters currently loaded in memory by the UI service.
		/// </summary>
		/// <returns>A list of all loaded UI instances</returns>
		List<UiInstance> GetLoadedPresenters();

		/// <summary>
		/// Requests the UI of given type <typeparamref name="T"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given <typeparamref name="T"/>
		/// </exception>
		/// <typeparam name="T">The type of UI presenter requested.</typeparam>
		/// <returns>The UI of type <typeparamref name="T"/> requested</returns>
		T GetUi<T>() where T : UiPresenter;

		/// <summary>
		/// Requests the visible state of the given UI type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of UI presenter to check if is visible or not.</typeparam>
		/// <returns>True if the UI is visble, false otherwise</returns>
		bool IsVisible<T>() where T : UiPresenter;

		/// <summary>
		/// Adds a UI configuration to the service.
		/// </summary>
		/// <param name="config">The UI configuration to add.</param>
		void AddUiConfig(UiConfig config);

		/// <summary>
		/// Adds a UI set configuration to the service.
		/// </summary>
		/// <param name="uiSet">The UI set configuration to add.</param>
		void AddUiSet(UiSetConfig uiSet);

		/// <summary>
		/// Adds a UI presenter to the service and includes it in the specified layer.
		/// If <paramref name="openAfter"/> is true, the UI presenter will be opened after being added to the service.
		/// </summary>
		/// <typeparam name="T">The type of UI presenter to add.</typeparam>
		/// <param name="ui">The UI presenter to add.</param>
		/// <param name="layer">The layer to include the UI presenter in.</param>
		/// <param name="openAfter">Whether to open the UI presenter after adding it to the service.</param>
		void AddUi<T>(T ui, int layer, bool openAfter = false) where T : UiPresenter;

		/// <summary>
		/// Removes the UI of the specified type from the service without unloading it.
		/// </summary>
		/// <typeparam name="T">The type of UI to remove.</typeparam>
		/// <returns>True if the UI was removed, false otherwise.</returns>
		bool RemoveUi<T>() where T : UiPresenter => RemoveUi(typeof(T));

		/// <summary>
		/// Removes the specified UI presenter from the service without unloading it.
		/// </summary>
		/// <typeparam name="T">The type of UI presenter to remove.</typeparam>
		/// <param name="uiPresenter">The UI presenter to remove.</param>
		/// <returns>True if the UI presenter was removed, false otherwise.</returns>
		bool RemoveUi<T>(T uiPresenter) where T : UiPresenter => RemoveUi(uiPresenter.GetType().UnderlyingSystemType);

		/// <summary>
		/// Removes the UI of the specified type from the service without unloading it.
		/// </summary>
		/// <param name="type">The type of UI to remove.</param>
		/// <returns>True if the UI was removed, false otherwise.</returns>
		bool RemoveUi(Type type);

		/// <summary>
		/// Removes and returns all UI presenters from the specified UI set that are still present in the service.
		/// </summary>
		/// <param name="setId">The ID of the UI set to remove from.</param>
		/// <returns>A list of removed UI presenters.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the service does not contain a UI set with the specified ID.</exception>
		List<UiPresenter> RemoveUiSet(int setId);

		/// <summary>
		/// Loads the UI of the specified type asynchronously.
		/// This method can be controlled in an async method and returns the loaded UI.
		/// If <paramref name="openAfter"/> is true, the UI will be opened after loading.
		/// </summary>
		/// <typeparam name="T">The type of UI to load.</typeparam>
		/// <param name="openAfter">Whether to open the UI after loading.</param>
		/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
		/// <returns>A task that completes with the loaded UI.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the service does not contain a UI configuration for the specified type.</exception>
		async UniTask<T> LoadUiAsync<T>(bool openAfter = false, CancellationToken cancellationToken = default) where T : UiPresenter => (await LoadUiAsync(typeof(T), openAfter, cancellationToken)) as T;

		/// <summary>
		/// Loads the UI of the specified type asynchronously.
		/// This method can be controlled in an async method and returns the loaded UI.
		/// If <paramref name="openAfter"/> is true, the UI will be opened after loading.
		/// </summary>
		/// <param name="type">The type of UI to load.</param>
		/// <param name="openAfter">Whether to open the UI after loading.</param>
		/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
		/// <returns>A task that completes with the loaded UI.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the service does not contain a UI configuration for the specified type.</exception>
		UniTask<UiPresenter> LoadUiAsync(Type type, bool openAfter = false, CancellationToken cancellationToken = default);

		/// <summary>
		/// Loads all UI presenters from the specified UI set asynchronously.
		/// This method can be controlled in an async method and returns each UI when it is loaded.
		/// The UIs are returned in a first-load-first-return scheme.
		/// </summary>
		/// <param name="setId">The ID of the UI set to load from.</param>
		/// <returns>An array of tasks that complete with each loaded UI.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the service does not contain a UI set with the specified ID.</exception>
		IList<UniTask<UiPresenter>> LoadUiSetAsync(int setId);

		/// <summary>
		/// Unloads the UI of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of UI to unload.</typeparam>
		/// <exception cref="KeyNotFoundException">Thrown if the service does not contain a UI of the specified type.</exception>
		void UnloadUi<T>() where T : UiPresenter => UnloadUi(typeof(T));

		/// <summary>
		/// Unloads the specified UI presenter.
		/// </summary>
		/// <typeparam name="T">The type of UI presenter to unload.</typeparam>
		/// <param name="uiPresenter">The UI presenter to unload.</param>
		/// <exception cref="KeyNotFoundException">Thrown if the service does not contain the specified UI presenter.</exception>
		void UnloadUi<T>(T uiPresenter) where T : UiPresenter => UnloadUi(uiPresenter.GetType().UnderlyingSystemType);

		/// <summary>
		/// Unloads the UI of the specified type.
		/// </summary>
		/// <param name="type">The type of UI to unload.</param>
		/// <exception cref="KeyNotFoundException">Thrown if the service does not contain a UI of the specified type.</exception>
		void UnloadUi(Type type);

		/// <summary>
		/// Unloads all UI presenters from the specified UI set.
		/// </summary>
		/// <param name="setId">The ID of the UI set to unload from.</param>
		/// <exception cref="KeyNotFoundException">Thrown if the service does not contain a UI set with the specified ID.</exception>
		void UnloadUiSet(int setId);

		/// <summary>
		/// Opens a UI presenter asynchronously, loading its assets if necessary.
		/// </summary>
		/// <typeparam name="T">The type of UI presenter to open.</typeparam>
		/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
		/// <returns>A task that completes when the UI presenter is opened.</returns>
		async UniTask<T> OpenUiAsync<T>(CancellationToken cancellationToken = default) where T : UiPresenter => (await OpenUiAsync(typeof(T), cancellationToken)) as T;

		/// <summary>
		/// Opens a UI presenter asynchronously, loading its assets if necessary.
		/// </summary>
		/// <param name="type">The type of UI presenter to open.</param>
		/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
		/// <returns>A task that completes when the UI presenter is opened.</returns>
		UniTask<UiPresenter> OpenUiAsync(Type type, CancellationToken cancellationToken = default);

		/// <summary>
		/// Opens a UI presenter asynchronously, loading its assets if necessary, and sets its initial data.
		/// </summary>
		/// <typeparam name="T">The type of UI presenter to open.</typeparam>
		/// <typeparam name="TData">The type of initial data to set.</typeparam>
		/// <param name="initialData">The initial data to set.</param>
		/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
		/// <returns>A task that completes when the UI presenter is opened.</returns>
		async UniTask<T> OpenUiAsync<T, TData>(TData initialData, CancellationToken cancellationToken = default) 
			where T : class, IUiPresenterData 
			where TData : struct => await OpenUiAsync(typeof(T), initialData, cancellationToken) as T;

		/// <summary>
		/// Opens a UI presenter asynchronously, loading its assets if necessary, and sets its initial data.
		/// </summary>
		/// <param name="type">The type of UI presenter to open.</param>
		/// <param name="initialData">The initial data to set.</param>
		/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
		/// <returns>A task that completes when the UI presenter is opened.</returns>
		UniTask<UiPresenter> OpenUiAsync<TData>(Type type, TData initialData, CancellationToken cancellationToken = default) where TData : struct;

		/// <summary>
		/// Opens all UI presenters in the specified UI set, loading them if necessary.
		/// This method ensures proper address handling for UI sets, making it safe to use
		/// in combination with <see cref="CloseAllUiSet"/> and <see cref="UnloadUiSet"/>.
		/// All UIs in the set are opened in parallel using UniTask.WhenAll.
		/// </summary>
		/// <param name="setId">The ID of the UI set to open.</param>
		/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
		/// <returns>A task that completes with an array of all opened UI presenters when all UIs in the set are opened.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the service does not contain a UI set with the specified ID.</exception>
		UniTask<UiPresenter[]> OpenUiSetAsync(int setId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Closes a UI presenter and optionally destroys its assets.
		/// </summary>
		/// <typeparam name="T">The type of UI presenter to close.</typeparam>
		/// <param name="destroy">Whether to destroy the UI presenter's assets.</param>
		void CloseUi<T>(bool destroy = false) where T : UiPresenter => CloseUi(typeof(T), destroy);

		/// <summary>
		/// Closes a UI presenter and optionally destroys its assets.
		/// </summary>
		/// <param name="uiPresenter">The UI presenter to close.</param>
		/// <param name="destroy">Whether to destroy the UI presenter's assets.</param>
		/// <returns>A task that completes when the UI presenter is closed.</returns>
		void CloseUi<T>(T uiPresenter, bool destroy = false) where T : UiPresenter => CloseUi(uiPresenter.GetType().UnderlyingSystemType, destroy);

		/// <summary>
		/// Closes a UI presenter and optionally destroys its assets.
		/// </summary>
		/// <param name="type">The type of UI presenter to close.</param>
		/// <param name="destroy">Whether to destroy the UI presenter's assets.</param>
		/// <returns>A task that completes when the UI presenter is closed.</returns>
		void CloseUi(Type type, bool destroy = false);

		/// <summary>
		/// Closes all visible UI presenters.
		/// </summary>
		void CloseAllUi();

		/// <summary>
		/// Closes all visible UI presenters in the given layer.
		/// </summary>
		/// <param name="layer">The layer to close UI presenters in.</param>
		void CloseAllUi(int layer);

		/// <summary>
		/// Closes all UI presenters that are part of the given UI set configuration.
		/// </summary>
		/// <param name="setId">The ID of the UI set configuration to close.</param>
		void CloseAllUiSet(int setId);
	}

	/// <inheritdoc cref="IUiService" />
	/// <remarks>
	/// This interface provides a way to initialize the UI service with the game's UI configurations.
	/// </remarks>
	public interface IUiServiceInit : IUiService, IDisposable
	{
		/// <summary>
		/// Initializes the UI service with the given UI configurations.
		/// </summary>
		/// <param name="configs">The UI configurations to initialize the service with.</param>
		/// <remarks>
		/// To help configure the game's UI, you need to create a UiConfigs Scriptable object by:
		/// - Right Click on the Project View > Create > ScriptableObjects > Configs > UiConfigs
		/// - Duplicate UI configs or UI sets will log warnings but will not throw exceptions
		/// - Layer numbers below 0 or above 1000 will log warnings
		/// - Empty addressable addresses or null UI types will throw ArgumentException
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="configs"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if any UI config has an empty addressable address or null UI type.
		/// </exception>
		void Init(UiConfigs configs);
	}
}