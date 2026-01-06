# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html)

## [1.1.0] - 2026-01-06

**New**:
- Added `OpenUiSetAsync(int setId, CancellationToken)` method to `IUiService` for opening all UI presenters in a set with proper address handling, ensuring compatibility with `CloseAllUiSet` and `UnloadUiSet`
- Added `OnOpenTransitionCompleted()` and `OnCloseTransitionCompleted()` lifecycle hooks to `UiPresenter` for reacting after all transition animations/delays complete
- Added comprehensive test suite:
  - Unit tests for `UiAnalytics`, `UiConfig`, `UiInstanceId`, `UiServiceCore`, `UiSetConfig`
  - PlayMode integration tests for multi-instance, loading, open/close, and UI set management
  - Performance and smoke tests
  - Feature-specific tests for `AnimationDelayFeature`, `TimeDelayFeature`, and `PresenterFeatureBase`
- Added `ITransitionFeature` interface for features that provide open/close transition delays
- Added `OpenTransitionTask` and `CloseTransitionTask` public properties on `UiPresenter` for awaiting transition completion externally
- Added `AGENTS.md` documentation for AI coding agents
- Added structured documentation under `docs/` folder with separate pages for getting started, core concepts, API reference, advanced topics, and troubleshooting
- Added multiple new samples to the package library
- Added multiple `IUiAssetLoader` implementations to support different asset loading scenarios:
  - `AddressablesUiAssetLoader` (default): Integration with Unity Addressables.
  - `PrefabRegistryUiAssetLoader`: Simple loader for direct prefab references (useful for testing and samples).
  - `ResourcesUiAssetLoader`: Support for loading from Unity's `Resources` folder.
- Added `AddressablesUiConfigs`, `ResourcesUiConfigs`, `PrefabRegistryUiConfigs`, `ResourcesUiConfigsEditor`, `AddressablesUiConfigsEditor` and `PrefabRegistryUiConfigsEditor` for managing UI configurations.
- Added `AddVisualTreeAttachedListener(UnityAction<VisualElement>)` convenience method to `UiToolkitPresenterFeature` that subscribes to `OnVisualTreeAttached` and invokes the callback immediately if already attached

**Changed**:
- **BREAKING**: Made `UiConfigs` class `abstract` to enforce usage of specialized subclasses (`AddressablesUiConfigs`, `ResourcesUiConfigs`, `PrefabRegistryUiConfigs`) and prevent runtime errors from misconfiguration
- **BREAKING**: Removed `IPresenterFeature` interface; features now extend `PresenterFeatureBase` directly
- **BREAKING**: Renamed `UiAssetLoader` to `AddressablesUiAssetLoader` to reflect its specific loading mechanism.
- **BREAKING**: Renamed `UiConfig.AddressableAddress` to `UiConfig.Address` for loader-agnosticism
- Changed `UiPresenter<T>.Data` property to have a public setter that automatically triggers `OnSetData()` when assigned
- Refactored `TimeDelayFeature` and `AnimationDelayFeature` to no longer call `gameObject.SetActive(false)` directly; visibility is now controlled solely by `UiPresenter`
- Refactored `UiPresenter.InternalOpen()` and `InternalClose()` to use internal async processes that await `ITransitionFeature` tasks
- Refactored `AnimationDelayFeature` and `TimeDelayFeature` to use `Presenter.NotifyOpenTransitionCompleted()` and `Presenter.NotifyCloseTransitionCompleted()` instead of internal events
- Removed `OnOpenCompletedEvent` and `OnCloseCompletedEvent` internal events from delay features
- Updated all samples to use UI buttons instead of input system dependencies for better project compatibility

**Fixed**:
- Fixed `AnimationDelayFeature` animation playback logic - was incorrectly checking `!_introAnimationClip` instead of `_introAnimationClip != null`
- Fixed `UiPresenterEditor` play-mode buttons to properly call `InternalOpen()` and `InternalClose()` instead of just toggling `gameObject.SetActive()`
- Fixed delay features to work correctly when tests run together (UniTaskCompletionSource lifecycle)
- Fixed null checks in delay features using explicit null comparisons instead of null-conditional operators for Unity object compatibility
- Fixed inconsistent lifecycle where `OnOpenTransitionCompleted`/`OnCloseTransitionCompleted` were only called when features existed
- Fixed split responsibility for visibility control where both `UiPresenter` and features could call `SetActive(false)`, allowing now to properly close the presenters in all scenarios
- Fixed `LoadUiAsync` visibility state inconsistency where calling it on an already-visible presenter with `openAfter=false` would disable the GameObject but not update `VisiblePresenters`, causing subsequent `OpenUiAsync` calls to fail silently
- Fixed multi-instance ambiguity when calling `Close(destroy: true)` from within a presenter and now correctly unloads the specific instance instead of potentially unloading the wrong one
- Fixed UI Toolkit timing issue where element queries in `OnInitialized()` would fail because the visual tree was not yet attached to a panel

## [1.0.0] - 2025-11-04

**New**:
- Added `IUiAnalytics` interface and `UiAnalytics` implementation for performance tracking
- Added three editor windows: `UiAnalyticsWindow`, `UiServiceHierarchyWindow`
- Added new "UI Layer Hierarchy Visualizer" section to the `UiConfigsEditor` inspector
- Added `UiPresenterSceneGizmos` for visual debugging in Scene view
- Added `UiPresenterEditor` custom inspector with quick open/close buttons
- Added multi-instance support for UI presenters via `UiInstanceId` struct and instance addresses
- Added `UiInstance` struct to encapsulate presenter metadata (type, address, presenter reference)
- Added feature-based presenter composition architecture with `IPresenterFeature` interface
- Added `PresenterFeatureBase` base class for composable presenter features
- Added `AnimationDelayFeature` and `TimeDelayFeature` components for delayed UI operations
- Added `UiToolkitPresenterFeature` for UI Toolkit integration
- Added `DefaultUiConfigsEditor` for out-of-the-box UI configuration (no custom implementation required)

**Changed**:
- Replaced `Task.Delay` with `UniTask.Delay` throughout for better performance and WebGL compatibility
- Updated `CloseAllUi` to avoid modifying collection during iteration
- Enhanced `UiService.Dispose()` with proper cleanup of all presenters, layers, and asset loader
- `LoadUiAsync`, `OpenUiAsync` methods now accept optional `CancellationToken` parameter
- Updated the README with a complete information of the project
- Replaced `LoadedPresenters` property with `GetLoadedPresenters()` method for better encapsulation
- Migrated all delay functionality from `PresenterDelayerBase` to feature-based system (`AnimationDelayFeature`, `TimeDelayFeature`)
- Converted all editor scripts to use UI Toolkit for better performance and modern UI
- Refactored `UiConfigsEditor` to use UI Toolkit with improved visuals and drag-and-drop support
- Optimized collection types (`Dictionary`, `List`) for better performance in `UiService`
- Removed loading spinner from `UiService` (simplified initialization)

**Fixed**:
- **CRITICAL**: Fixed `GetOrLoadUiAsync` returning null when loading new UI (now properly assigns return value)
- Fixed exception handling in `UnloadUi` with proper `TryGetValue` checks
- Fixed exception handling in `RemoveUiSet` with proper `TryGetValue` checks
- Fixed redundant operations in `CloseAllUi` logic
- Fixed initial value handling for UI sets in editor
- Fixed serialization updates before property binding in editor
- Fixed script indentation issues in delay presenter implementations

## [0.13.1] - 2025-09-28

**New**:
- Added *UiToolkitPresenter<Data>* script to allow UI Toolkit based UIs to work similar to *UiPresenter<Data>*

**Changed**:
- Refactored *UiToolkitPresenter* to also pass the root visual element to it's implemented class and properly assign the element OnValidate

## [0.13.0] - 2025-09-25

**New**:
- Added *UiToolkitPresenter* script to allow UI Toolkit based UIs to work with the library

**Changed**:
- Updated *README* to reflect the project structure
- Adjusted the Editor tools and *UiService* to process UI Toolkit based views

## [0.12.0] - 2025-01-08

**New**:
- Added *InteractableTextView* script to allow linking text code execution, e.g open URLs in the browser

**Changed**:
- Renamed *AdjustScreenSizeFitter* to *AdjustScreenSizeFitterView* to mark it as a View in the architecture conventions
- Moved *AdjustScreenSizeFitterView* and *NonDrawingView*, *SafeAreaHelperView* to the Views folder and namespace to organize the codebase accordingly 

## [0.11.0] - 2025-01-05

**New**:
- Added two new static event calls to UiService that are triggered when the app changes resolution and another trigger when changes screen orientation
- The new *UiServiceMonoComponent* was added to the project for internal purposes, in order to support Unity's loop or generic *GameObjects* dependency code (e.g. the screen resolution trigger change)
- Added new *AdjustScreenSizeFitter* to extend the ui behaviour of Unity's *ContentSizeFitter* in order to allow the *LayoutElement* to fit between the *LayoutElement.minSize* & *LayoutElement.flexibleSize*, values defined in the Unity's inspector

**Changed**:
- Renamed *UiPresenterData<T>* to *UiPresenter<T>* to make it less verbose

## [0.10.0] - 2024-11-13

**New**:
- Added *UniTask* dependency to enable WebGL platform support

**Changed**:
- Updated *IUiService* async methods to use *UniTask* instead of *Task* for better performance and WebGL compatibility

## [0.9.1] - 2024-11-04

**Fixed**:
- Fixed the issue that would crash *NonDrawingView* if the *GameObject* would be missing a *CanvasRenderer*

## [0.9.0] - 2024-11-01

***New**:
- Added *GetUi<T>* method to the *IUiService*. It requests the *UiPresenter* by directly using generic T
- Added *IsVisible<T>* method to the *IUiService*. It requests the visibility state of *UiPresenter*
- Added IReadOnlyList property *VisiblePresenters* to the *IUiService* to allow external entities to access the list of visible *UiPresenter*

***Changed**:
- Removed *GetAllVisibleUi()* method. Use *IsVisible<T>* method instead

## [0.8.0] - 2024-10-29

***New**:
- Added new *PresenterDelayerBase*, *AnimationDelayer* and *TimeDelayer* to support presenters that open/close with a delay
- Added new *DelayUiPresenter* to interact with *PresenterDelayerBase* implementations and allow presenters to open/close with a delay
- Improved performance of *UiService*

***Changed**:
- Removed *AnimatedUiPresenter*. Use the new *DelayUiPresenter* with one of the *PresenterDelayerBase* implementations
- Removed *UiCloseActivePresenter*  and *UiCloseActivePresenterData*. Use the new *DelayUiPresenter* with one of the *PresenterDelayerBase* implementations
- Removed the dependency of *UiPresenter* from Canvas. Allowing different structures of UI Unity project hierarchy to work with the *UiService*
- Removed all Get and Has methods from *IUiService*. Replaced with IReadOnlyDictionaries for all the collections being requested from the service
- Changed all OpenUi methods to be async. This guarantees the expected behaviour that will always load the Ui first before opening
- Changed all CloseUi methods to be synchronous. Closing an Ui will now always be atomic. To get the close delay, you can request directly from the *DelayUiPresenter*
- Changed *IUiAssetLoader* to unify the prefab instantiation into a single call. This simplefies the method so the caller doesn't have to worry about synchronous or async behaviour
- Changed the *UiConfig* to know contain the information of the *UiPresenter* if is loaded sync or async

## [0.7.2] - 2021-05-09

**Fixed**:
- Fixed the issue where closing *UiPresenter* would be called after the game object is disabled

## [0.7.1] - 2021-05-03

***New**:
- Added the possibility for *SafeAreaHelpersView* to maintain the View in the same position if not placed outside of the safe area

**Fixed**:
- Fixed the duplicated memory issue when loading the same *UiPresenter* multiple times at the same time before when of them is finished

## [0.7.0] - 2021-03-12

***New**:
- Added *NonDrawingView* to have an Image without a renderer to not add additional draw calls.
- Added *SafeAreaHelperView* to add the possibility for the *RectTransform* to adjust himself to the screen notches
- Added *AnimatedUiPresenter* to play animation on enter or closing
- Added the possibility to add *Layers* externally into the *UiService*
  
**Changed**:
- Now *Canvas* are single *GameObjects* that can be controlled outside of the *UiService*

**Fixed**:
- Fixed the issue when setting data on *UiPresenterData* not being invoked

## [0.6.1] - 2020-09-24

**Fixed**:
- Updated dependency packages

## [0.6.0] - 2020-09-24

***New**:
- Added the possibility for the *IUiService* to allow to open/close already opened/closed *UiPresenters*, and throw an exception if not. 
- Added the visible property to UiPresenter of its current visual status Added *IUiServiceInit* to give a new contract interface for the *UiService" initialisation

**Fixed**:
- Fixed issue that was not unloading the *UiPresenter* correctly with the asset bundles
- Fixed issue when *UiPresenter* might not have a canvas attached
- Fixed crash when trying to open a *UiPresenter* after loading it

## [0.5.0] - 2020-07-13

***New**:
- Added *UiAssetLoader* to load Ui assets to memory

**Changed**:
- Removed the *UiService* dependency from the *com.gamelovers.assetLoader*

## [0.4.0] - 2020-07-13

**Changed**:
- Removed the *UiService* dependency from the *com.unity.addressables*
- Modified the *UiService* to be testable and injectable into other systems

## [0.3.2] - 2020-04-18

**Changed**:
- Moved interface *IUiService* to a separate file to improve the code readability

## [0.3.1] - 2020-02-15

**Changed**:
- Updated dependency packages

## [0.3.0] - 2020-02-11

***New**:
- Added new *UiPresenterData* class for the case where the *UiPresenter* needs to be initialized with a default data value
- Added new *OnInitialize* method that is invoked after the *UiPresenter* is initialized

## [0.2.1] - 2020-02-09

***New**:
- Added the possibility to open the ui after adding or loading it to the *UiService*
- Added the possibility to get the canvas reference object based on the given layer
- Added the possibility to remove and unload the *UiPresenter* by only passing it's reference

**Fixed**:
- Fixed bug that prevented the *UiService* to properly unload an *UiPresenter*

## [0.2.0] - 2020-01-19

***New**:
- Added easy selection of the *UiConfigs.asset* file. Just go to *Tools > Select UiConfigs.asset*. If the *UiConfigs.asset* does not exist, it will create a new one in the Assets folder
- Added the protected *Close()* method to directly allow to close the *UiPresenter* from the *UiPresenter* object file without needing to call the *UiService*. Also now is possible to close an Ui in the service by referencing the object directly without needing to reference the object type by calling *CloseUi<T>(T presenter)*
- Now the *UnloadUi* & *UnloadUiSet* properly unloads the ui from memory and removes it from the service
- Added *RemoveUi* & *RemoveUiPresentersFromSet* to allow the ui to be removed from the service without being unloaded from memory
- Improved documentation

**Changed**
- Now the Refresh method on the *UiPresenter* is public and can be called from any object that has asset to it. The *UiService* will not call this method anymore if trying to open the same *UiPresenter* twice without closing
- *UiService.IsUiLoaded* changed to *HasUiPresenter*
- *UiService.IsAllUiLoadedInSet* changed to *HasAllUiPresentersInSet*
- Unified *AddUi* methods

**Fixed**:
- Fixed bug that sometimes don't save the *UiConfigs* state correctly

## [0.1.3] - 2020-01-09

**Fixed**:
- Bug that sometimes was not save+ing the *UiConfigs* state correctly

## [0.1.2] - 2020-01-09

**Fixed**:
- Compiler Errors

## [0.1.1] - 2020-01-09

**Fixed**:
- The state of a the *UiPresenter* when loaded. Now the *UiPresenters* are always disabled when loaded

## [0.1.0] - 2020-01-05

- Initial submission for package distribution
