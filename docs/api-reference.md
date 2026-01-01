# API Reference

Complete API documentation for the UI Service.

## Table of Contents

- [IUiService Interface](#iuiservice-interface)
- [Loading and Unloading](#loading-and-unloading)
- [Opening and Closing](#opening-and-closing)
- [UI Sets Operations](#ui-sets-operations)
- [Query Methods](#query-methods)
- [Async Operations](#async-operations)
- [Runtime Configuration](#runtime-configuration)

---

## IUiService Interface

The main interface for all UI operations.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `VisiblePresenters` | `IReadOnlyList<UiInstanceId>` | All currently visible presenter instances |
| `UiSets` | `IReadOnlyDictionary<int, UiSetConfig>` | All registered UI sets |

### Methods Overview

| Method | Returns | Description |
|--------|---------|-------------|
| `GetLoadedPresenters()` | `List<UiInstance>` | All presenters currently in memory |
| `GetUi<T>()` | `T` | Get loaded presenter by type |
| `IsVisible<T>()` | `bool` | Check if presenter is visible |
| `LoadUiAsync<T>()` | `UniTask<T>` | Load presenter into memory |
| `OpenUiAsync<T>()` | `UniTask<T>` | Open presenter (loads if needed) |
| `CloseUi<T>()` | `void` | Close presenter |
| `UnloadUi<T>()` | `void` | Unload presenter from memory |

---

## Loading and Unloading

### LoadUiAsync

Load a UI presenter into memory without opening it.

```csharp
// Load into memory (stays hidden)
var inventory = await _uiService.LoadUiAsync<InventoryPresenter>();

// Load and immediately open
var inventory = await _uiService.LoadUiAsync<InventoryPresenter>(openAfter: true);

// With cancellation
var cts = new CancellationTokenSource();
var inventory = await _uiService.LoadUiAsync<InventoryPresenter>(
    openAfter: false, 
    cancellationToken: cts.Token
);
```

**Signatures:**
```csharp
UniTask<T> LoadUiAsync<T>(bool openAfter = false, CancellationToken cancellationToken = default) 
    where T : UiPresenter;

UniTask<UiPresenter> LoadUiAsync(Type type, bool openAfter = false, CancellationToken cancellationToken = default);
```

### UnloadUi

Unload a presenter from memory (releases Addressables reference).

```csharp
// Unload by type
_uiService.UnloadUi<InventoryPresenter>();

// Unload by instance
_uiService.UnloadUi(inventoryPresenter);

// Unload by Type object
_uiService.UnloadUi(typeof(InventoryPresenter));
```

**Note:** Unloading a visible presenter will close it first.

---

## Opening and Closing

### OpenUiAsync

Open a presenter, loading it first if necessary.

```csharp
// Basic open
var shop = await _uiService.OpenUiAsync<ShopPresenter>();

// Open with data
var questData = new QuestData { QuestId = 101, Title = "Dragon Slayer" };
var quest = await _uiService.OpenUiAsync<QuestPresenter, QuestData>(questData);

// With cancellation
var cts = new CancellationTokenSource();
try
{
    var ui = await _uiService.OpenUiAsync<LoadingPresenter>(cts.Token);
}
catch (OperationCanceledException)
{
    Debug.Log("UI loading was cancelled");
}
```

**Signatures:**
```csharp
UniTask<T> OpenUiAsync<T>(CancellationToken cancellationToken = default) 
    where T : UiPresenter;

UniTask<T> OpenUiAsync<T, TData>(TData initialData, CancellationToken cancellationToken = default) 
    where T : class, IUiPresenterData 
    where TData : struct;
```

### CloseUi

Close a visible presenter.

```csharp
// Close but keep in memory (fast to reopen)
_uiService.CloseUi<ShopPresenter>();
_uiService.CloseUi<ShopPresenter>(destroy: false);

// Close and unload from memory
_uiService.CloseUi<ShopPresenter>(destroy: true);

// Close by instance
_uiService.CloseUi(shopPresenter);
_uiService.CloseUi(shopPresenter, destroy: true);
```

**Transition Behavior:**
- If the presenter has `ITransitionFeature` components (e.g., `TimeDelayFeature`, `AnimationDelayFeature`), the close will wait for all transitions to complete before hiding the GameObject.
- Use `presenter.CloseTransitionTask` to await the full close process including transitions.

```csharp
// Wait for close transition to complete
_uiService.CloseUi<AnimatedPopup>();
await presenter.CloseTransitionTask;
Debug.Log("Popup fully closed with animation");
```

### CloseAllUi

Close multiple presenters at once.

```csharp
// Close all visible UI
_uiService.CloseAllUi();

// Close all UI in a specific layer
_uiService.CloseAllUi(layer: 2);
```

---

## UI Sets Operations

### LoadUiSetAsync

Load all presenters in a set.

```csharp
// Returns array of tasks - load in parallel
IList<UniTask<UiPresenter>> loadTasks = _uiService.LoadUiSetAsync(setId: 1);

// Wait for all to complete
var presenters = await UniTask.WhenAll(loadTasks);

// Or process as they complete
foreach (var task in loadTasks)
{
    var presenter = await task;
    Debug.Log($"Loaded: {presenter.name}");
}
```

### CloseAllUiSet

Close all presenters in a set.

```csharp
_uiService.CloseAllUiSet(setId: 1);
```

### UnloadUiSet

Unload all presenters in a set from memory.

```csharp
_uiService.UnloadUiSet(setId: 1);
```

### RemoveUiSet

Remove and return all presenters from a set.

```csharp
List<UiPresenter> removed = _uiService.RemoveUiSet(setId: 2);

// Clean up manually if needed
foreach (var presenter in removed)
{
    Destroy(presenter.gameObject);
}
```

---

## Query Methods

### GetUi

Get a loaded presenter by type.

```csharp
var hud = _uiService.GetUi<GameHudPresenter>();

if (hud != null)
{
    hud.UpdateScore(newScore);
}
```

**Throws:** `KeyNotFoundException` if not loaded.

### IsVisible

Check if a presenter is currently visible.

```csharp
if (_uiService.IsVisible<MainMenuPresenter>())
{
    Debug.Log("Main menu is showing");
}

// Use before opening to avoid duplicates
if (!_uiService.IsVisible<PauseMenu>())
{
    await _uiService.OpenUiAsync<PauseMenu>();
}
```

### GetLoadedPresenters

Get all presenters currently loaded in memory.

```csharp
List<UiInstance> loaded = _uiService.GetLoadedPresenters();

foreach (var instance in loaded)
{
    Debug.Log($"Loaded: {instance.Type.Name} [{instance.Address}]");
}

// Check if specific type is loaded
bool isLoaded = loaded.Any(p => p.Type == typeof(InventoryPresenter));
```

### VisiblePresenters

Get all currently visible presenters.

```csharp
IReadOnlyList<UiInstanceId> visible = _uiService.VisiblePresenters;

Debug.Log($"Visible count: {visible.Count}");

foreach (var id in visible)
{
    Debug.Log($"Visible: {id}");
}
```

---

## Async Operations

All async operations use UniTask for better performance and WebGL compatibility.

### Sequential Loading

```csharp
// Load one after another
var menu = await _uiService.OpenUiAsync<MainMenuPresenter>();
var settings = await _uiService.OpenUiAsync<SettingsPresenter>();
```

### Parallel Loading

```csharp
// Load multiple UI simultaneously (faster)
var menuTask = _uiService.OpenUiAsync<MainMenuPresenter>();
var hudTask = _uiService.OpenUiAsync<GameHudPresenter>();
var chatTask = _uiService.OpenUiAsync<ChatPresenter>();

await UniTask.WhenAll(menuTask, hudTask, chatTask);

// Access results
var menu = menuTask.GetAwaiter().GetResult();
```

### Cancellation

```csharp
var cts = new CancellationTokenSource();

// Start loading
var loadTask = _uiService.OpenUiAsync<HeavyPresenter>(cts.Token);

// Cancel after timeout
cts.CancelAfter(TimeSpan.FromSeconds(5));

try
{
    var presenter = await loadTask;
}
catch (OperationCanceledException)
{
    Debug.Log("Loading was cancelled");
}

// Or cancel manually
cts.Cancel();
```

### Fire-and-Forget

```csharp
// When you don't need to await
_uiService.OpenUiAsync<NotificationPresenter>().Forget();
```

---

## Runtime Configuration

### AddUiConfig

Add a UI configuration at runtime.

```csharp
// Note: Actual UiConfig constructor may vary
var config = new UiConfig
{
    PresenterType = typeof(DynamicPopup),
    AddressableAddress = "UI/DynamicPopup",
    Layer = 3,
    LoadSynchronously = false
};

_uiService.AddUiConfig(config);
```

### AddUiSet

Add a UI set configuration at runtime.

```csharp
var setConfig = new UiSetConfig(setId: 10);
_uiService.AddUiSet(setConfig);
```

### AddUi

Add an already-instantiated presenter to the service.

```csharp
// Instantiate manually
var dynamicUi = Instantiate(uiPrefab).GetComponent<UiPresenter>();

// Add to service
_uiService.AddUi(dynamicUi, layer: 3, openAfter: true);
```

### RemoveUi

Remove a presenter from the service without unloading.

```csharp
// Remove by type
bool removed = _uiService.RemoveUi<DynamicPopup>();

// Remove by instance
bool removed = _uiService.RemoveUi(dynamicPresenter);

// Remove by Type object
bool removed = _uiService.RemoveUi(typeof(DynamicPopup));
```

---

## IUiServiceInit Interface

Extended interface for initialization and disposal.

### Init

Initialize the service with configuration.

```csharp
IUiServiceInit uiService = new UiService();
uiService.Init(uiConfigs);

// Or with custom loader and analytics
var loader = new AddressablesUiAssetLoader();
var analytics = new UiAnalytics();
IUiServiceInit uiService = new UiService(loader, analytics);
uiService.Init(uiConfigs);

// Using other built-in loaders
var prefabLoader = new PrefabRegistryUiAssetLoader();
var resourcesLoader = new ResourcesUiAssetLoader();
```

### IUiAssetLoader Interface

Custom asset loading strategies can be implemented using this interface.

```csharp
public interface IUiAssetLoader
{
    UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken ct = default);
    void UnloadAsset(GameObject asset);
}
```

### Dispose

Clean up all resources.

```csharp
// Closes all UI, unloads assets, destroys root GameObject
uiService.Dispose();
```

**Note:** Always call `Dispose()` when done with the service (e.g., in `OnDestroy`).

