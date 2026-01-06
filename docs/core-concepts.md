# Core Concepts

This document covers the fundamental concepts of the UI Service: presenters, layers, sets, features, and configuration.

## Table of Contents

- [Service Interfaces](#service-interfaces)
- [UI Presenter](#ui-presenter)
- [Presenter Features](#presenter-features)
- [UI Layers](#ui-layers)
- [UI Sets](#ui-sets)
- [Multi-Instance Support](#multi-instance-support)
- [UI Configuration](#ui-configuration)
- [Editor Windows](#editor-windows)

---

## Service Interfaces

The UI Service exposes **two interfaces** for different use cases:

| Interface | Purpose | Key Methods |
|-----------|---------|-------------|
| `IUiService` | **Consuming** the service | `OpenUiAsync`, `CloseUi`, `LoadUiAsync`, `UnloadUi`, `IsVisible`, `GetUi` |
| `IUiServiceInit` | **Initializing** the service | Inherits `IUiService` + `Init(UiConfigs)`, `Dispose()` |

### When to Use Each Interface

**Use `IUiServiceInit` when:**
- You create and own the `UiService` instance
- You need to call `Init(UiConfigs)` to initialize
- You need to call `Dispose()` to clean up

**Use `IUiService` when:**
- You receive the service via dependency injection
- You only need to open/close/query UI
- You don't manage the service lifecycle

### Correct Initialization Pattern

```csharp
using UnityEngine;
using GameLovers.UiService;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private UiConfigs _uiConfigs;
    
    // ✅ Use IUiServiceInit - you need Init() and Dispose()
    private IUiServiceInit _uiService;
    
    void Start()
    {
        _uiService = new UiService();
        _uiService.Init(_uiConfigs);  // Available on IUiServiceInit
    }
    
    void OnDestroy()
    {
        _uiService?.Dispose();  // Available on IUiServiceInit
    }
}
```

### Common Mistake

```csharp
// ❌ WRONG - IUiService does NOT have Init()
private IUiService _uiService;

void Start()
{
    _uiService = new UiService();
    _uiService.Init(_uiConfigs);  // CS1061: 'IUiService' does not contain 'Init'
}
```

### Dependency Injection Pattern

```csharp
// Service locator or DI container registers with IUiServiceInit
public class ServiceLocator
{
    private IUiServiceInit _uiService;
    
    public void Initialize(UiConfigs configs)
    {
        _uiService = new UiService();
        _uiService.Init(configs);
    }
    
    // Consumers get IUiService (no access to Init/Dispose)
    public IUiService GetUiService() => _uiService;
    
    public void Shutdown()
    {
        _uiService?.Dispose();
    }
}

// Consumer class only needs IUiService
public class ShopController
{
    private readonly IUiService _uiService;
    
    public ShopController(IUiService uiService)
    {
        _uiService = uiService;
    }
    
    public async void OpenShop()
    {
        await _uiService.OpenUiAsync<ShopPresenter>();
    }
}
```

---

## UI Presenter

The `UiPresenter` is the base class for all UI elements in the system. It provides lifecycle management and service integration.

### Lifecycle Hooks

| Method | When Called | Use For |
|--------|-------------|---------|
| `OnInitialized()` | Once, when first loaded | Setup, event subscriptions |
| `OnOpened()` | Every time UI is shown | Animations, data refresh |
| `OnClosed()` | When UI is hidden | Cleanup, save state |
| `OnOpenTransitionCompleted()` | After all transition features finish opening | Post-transition logic, enable interactions |
| `OnCloseTransitionCompleted()` | After all transition features finish closing | Post-transition cleanup |

> **Note**: `OnOpenTransitionCompleted()` and `OnCloseTransitionCompleted()` are **always called**, even for presenters without transition features. This provides a consistent lifecycle for all presenters.

### Transition Tasks

Presenters expose public `UniTask` properties for external awaiting:

```csharp
// Wait for a presenter to fully open (including transitions)
await presenter.OpenTransitionTask;

// Wait for a presenter to fully close (including transitions)
await presenter.CloseTransitionTask;
```

### Basic Presenter

```csharp
public class BasicPopup : UiPresenter
{
    protected override void OnInitialized()
    {
        // Called once when the presenter is first loaded
        // Set up UI elements, subscribe to events
    }
    
    protected override void OnOpened()
    {
        // Called every time the UI is shown
        // Start animations, refresh data
    }
    
    protected override void OnClosed()
    {
        // Called when the UI is hidden
        // Stop animations, save state
    }
}
```

### Data-Driven Presenter

For UI that needs initialization data, use `UiPresenter<T>`:

```csharp
public struct QuestData
{
    public int QuestId;
    public string Title;
    public string Description;
}

public class QuestPresenter : UiPresenter<QuestData>
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _descriptionText;
    
    protected override void OnSetData()
    {
        // Called automatically whenever Data is assigned
        _titleText.text = Data.Title;
        _descriptionText.text = Data.Description;
    }
}

// Usage - initial data on open
var questData = new QuestData { QuestId = 1, Title = "Dragon Slayer", Description = "..." };
await _uiService.OpenUiAsync<QuestPresenter, QuestData>(questData);
```

### Updating Data Dynamically

The `Data` property on `UiPresenter<T>` has a **public setter** that automatically triggers `OnSetData()` whenever assigned. This enables updating UI data at any time without closing and reopening:

```csharp
// Get the presenter and update its data directly
var questPresenter = _uiService.GetUi<QuestPresenter>();

// This will automatically call OnSetData()
questPresenter.Data = new QuestData 
{ 
    QuestId = 2, 
    Title = "Updated Quest", 
    Description = "New description" 
};
```

> **Note**: Setting `Data` always calls `OnSetData()`, whether called during initial open via `OpenUiAsync` or when updating data afterwards. This provides a consistent lifecycle for data-driven presenters.

### Closing from Within

Presenters can close themselves:

```csharp
public class ConfirmPopup : UiPresenter
{
    public void OnConfirmClicked()
    {
        // Close but keep in memory
        Close(destroy: false);
    }
    
    public void OnCancelClicked()
    {
        // Close and unload from memory
        // Works correctly even for multi-instance presenters
        Close(destroy: true);
    }
}
```

---

## Presenter Features

The UI Service uses a **feature-based composition system** to extend presenter behavior without inheritance complexity.

### Transition Features

Features that implement `ITransitionFeature` provide open/close transition delays. The presenter automatically awaits all transition features before:
- Calling `OnOpenTransitionCompleted()` (after open)
- Hiding the GameObject and calling `OnCloseTransitionCompleted()` (after close)

This ensures visibility is controlled in a single place (`UiPresenter`) and transitions are properly coordinated.

### Built-in Features

#### TimeDelayFeature

Adds time-based delays to UI opening and closing. Implements `ITransitionFeature`:

```csharp
[RequireComponent(typeof(TimeDelayFeature))]
public class DelayedPopup : UiPresenter
{
    [SerializeField] private TimeDelayFeature _delayFeature;
    
    protected override void OnOpened()
    {
        base.OnOpened();
        Debug.Log($"Opening with {_delayFeature.OpenDelayInSeconds}s delay...");
    }
    
    protected override void OnOpenTransitionCompleted()
    {
        Debug.Log("Opening delay completed - UI is ready!");
    }
    
    protected override void OnCloseTransitionCompleted()
    {
        Debug.Log("Closing delay completed!");
    }
}
```

**Inspector Configuration:**
- `Open Delay In Seconds` - Time to wait after opening (default: 0.5s)
- `Close Delay In Seconds` - Time to wait before closing (default: 0.3s)

#### AnimationDelayFeature

Synchronizes UI lifecycle with animation clips. Implements `ITransitionFeature`:

```csharp
[RequireComponent(typeof(AnimationDelayFeature))]
public class AnimatedPopup : UiPresenter
{
    [SerializeField] private AnimationDelayFeature _animationFeature;
    
    protected override void OnOpenTransitionCompleted()
    {
        Debug.Log("Intro animation completed - UI is ready!");
    }
    
    protected override void OnCloseTransitionCompleted()
    {
        Debug.Log("Outro animation completed!");
    }
}
```

**Inspector Configuration:**
- `Animation Component` - Auto-detected or manually assigned
- `Intro Animation Clip` - Plays when opening
- `Outro Animation Clip` - Plays when closing

#### UiToolkitPresenterFeature

Provides UI Toolkit (UI Elements) integration with safe visual tree handling.

> **⚠️ Important:** The `UIDocument.rootVisualElement` may not be ready when `OnInitialized()` is called. Always use `AddVisualTreeAttachedListener` to safely query elements.

**Properties:**
- `Document` - The attached `UIDocument`
- `Root` - The root `VisualElement` (may be null before attachment)
- `IsVisualTreeAttached` - Returns `true` when the visual tree is ready

**Events:**
- `OnVisualTreeAttached` - `UnityEvent<VisualElement>` invoked when the visual tree is ready

**Methods:**
- `AddVisualTreeAttachedListener(callback)` - Subscribes to `OnVisualTreeAttached` and invokes immediately if already attached

```csharp
[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class UIToolkitMenu : UiPresenter
{
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;
    
    private Button _playButton;
    
    protected override void OnInitialized()
    {
        // Use AddVisualTreeAttachedListener for safe element queries
        _toolkitFeature.AddVisualTreeAttachedListener(SetupUI);
    }
    
    private void SetupUI(VisualElement root)
    {
        _playButton = root.Q<Button>("play-button");
        _playButton?.RegisterCallback<ClickEvent>(OnPlayClicked);
    }
    
    private void OnDestroy()
    {
        _playButton?.UnregisterCallback<ClickEvent>(OnPlayClicked);
    }
}
```

### Composing Multiple Features

Features can be combined freely:

```csharp
[RequireComponent(typeof(TimeDelayFeature))]
[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class DelayedUiToolkitPresenter : UiPresenter
{
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        // Disable UI until transition completes
        _toolkitFeature.AddVisualTreeAttachedListener(root => root.SetEnabled(false));
    }
    
    protected override void OnOpenTransitionCompleted()
    {
        // Enable UI after delay completes
        if (_toolkitFeature.IsVisualTreeAttached)
        {
            _toolkitFeature.Root.SetEnabled(true);
        }
    }
}
```

### Creating Custom Features

Extend `PresenterFeatureBase` for basic lifecycle hooks. For transition features, also implement `ITransitionFeature`:

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeFeature : PresenterFeatureBase, ITransitionFeature
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.3f;
    
    private UniTaskCompletionSource _openTransitionCompletion;
    private UniTaskCompletionSource _closeTransitionCompletion;
    
    // ITransitionFeature implementation
    public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;
    public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;
    
    public override void OnPresenterOpening()
    {
        _canvasGroup.alpha = 0f;
    }
    
    public override void OnPresenterOpened()
    {
        FadeInAsync().Forget();
    }
    
    public override void OnPresenterClosing()
    {
        FadeOutAsync().Forget();
    }
    
    private async UniTask FadeInAsync()
    {
        _openTransitionCompletion = new UniTaskCompletionSource();
        
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            _canvasGroup.alpha = elapsed / _fadeDuration;
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        _canvasGroup.alpha = 1f;
        
        _openTransitionCompletion.TrySetResult();
    }
    
    private async UniTask FadeOutAsync()
    {
        _closeTransitionCompletion = new UniTaskCompletionSource();
        
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            _canvasGroup.alpha = 1f - (elapsed / _fadeDuration);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        _canvasGroup.alpha = 0f;
        
        _closeTransitionCompletion.TrySetResult();
    }
}
```

**Available Lifecycle Hooks:**
- `OnPresenterInitialized(UiPresenter presenter)`
- `OnPresenterOpening()`
- `OnPresenterOpened()`
- `OnPresenterClosing()`
- `OnPresenterClosed()`

**Creating Transition Features:**
- Implement `ITransitionFeature` for features that need the presenter to wait
- Expose `OpenTransitionTask` and `CloseTransitionTask` as `UniTask` properties
- Use `UniTaskCompletionSource` to signal when your transition completes
- The presenter will await all `ITransitionFeature` tasks before completing its lifecycle

---

## UI Layers

UI elements are organized into layers, where higher layer numbers appear on top (closer to camera).

### Layer Organization

```csharp
// Recommended layer structure:
// Layer 0: Background UI (skyboxes, parallax)
// Layer 1: Game HUD (health bars, minimap)
// Layer 2: Menus (main menu, settings)
// Layer 3: Popups (confirmations, rewards)
// Layer 4: System messages (errors, loading)
// Layer 5: Debug overlays
```

### Layer Operations

```csharp
// Close all UI in a specific layer
_uiService.CloseAllUi(layer: 2);

// Layers are configured per-presenter in UiConfigs
```

### How Layers Work

- Each presenter is assigned a layer in `UiConfigs`
- `Canvas.sortingOrder` (uGUI) or `UIDocument.sortingOrder` (UI Toolkit) is set automatically
- Higher layers render on top of lower layers

---

## UI Sets

Group related UI elements for batch operations.

### Defining Sets

Sets are defined in your `UiConfigs` asset. Each presenter can optionally belong to a set by its Set ID.

```
Set 0: Core UI (always loaded)
Set 1: Main Menu (logo, menu buttons, background)
Set 2: Gameplay (HUD, minimap, chat)
Set 3: Shop (shop window, inventory, currency)
```

### Set Operations

```csharp
// Load all UI in a set (returns array of tasks)
var loadTasks = _uiService.LoadUiSetAsync(setId: 1);
await UniTask.WhenAll(loadTasks);

// Close all UI in a set
_uiService.CloseAllUiSet(setId: 1);

// Unload set from memory
_uiService.UnloadUiSet(setId: 1);

// Remove set and get removed presenters
var removed = _uiService.RemoveUiSet(setId: 2);
foreach (var presenter in removed)
{
    Destroy(presenter.gameObject);
}
```

### Recommended Set Organization

| Set ID Range | Purpose |
|--------------|---------|
| 0 | Core/Persistent UI (always loaded) |
| 1-10 | Scene-specific UI |
| 11-20 | Feature-specific UI (shop, inventory) |

---

## Multi-Instance Support

By default, each UI presenter type is a singleton. The `UiInstanceId` system enables multiple instances of the same type.

### Use Cases

- Multiple tooltip windows
- Stacked notification popups
- Multiple player info panels (multiplayer)
- Pooled UI elements

### UiInstanceId

```csharp
// Default/singleton instance
var defaultId = UiInstanceId.Default(typeof(TooltipPresenter));

// Named instances
var itemTooltipId = UiInstanceId.Named(typeof(TooltipPresenter), "item");
var skillTooltipId = UiInstanceId.Named(typeof(TooltipPresenter), "skill");

// Check if default
if (instanceId.IsDefault)
{
    Debug.Log("This is the singleton instance");
}
```

### Working with Instances

```csharp
// Get all loaded presenters
List<UiInstance> loaded = _uiService.GetLoadedPresenters();

foreach (var instance in loaded)
{
    Debug.Log($"Type: {instance.Type.Name}");
    Debug.Log($"Address: {instance.Address}"); // Empty for default
    Debug.Log($"Presenter: {instance.Presenter.name}");
}

// Check visible presenters
IReadOnlyList<UiInstanceId> visible = _uiService.VisiblePresenters;
```

### UiInstance vs UiInstanceId

| Struct | Purpose | Contains |
|--------|---------|----------|
| `UiInstanceId` | Identifier for referencing | `PresenterType`, `InstanceAddress` |
| `UiInstance` | Full data about loaded instance | `Type`, `Address`, `Presenter` |

---

## UI Configuration

The `UiConfigs` ScriptableObject stores all UI configuration.

### Creating UiConfigs

1. Right-click in Project View
2. Navigate to `Create` → `ScriptableObjects` → `Configs` → `UiConfigs`

### Configuration Properties

| Property | Description |
|----------|-------------|
| **Type** | The presenter class type |
| **Addressable Address** | Addressable key to the UI prefab |
| **Layer** | Depth layer (higher = closer to camera) |
| **Load Synchronously** | Block main thread during load (use sparingly) |
| **UI Set ID** | Optional grouping for batch operations |

### Runtime Configuration

```csharp
// Add configuration at runtime
var config = new UiConfig(typeof(DynamicPopup), "UI/DynamicPopup", layer: 3);
_uiService.AddUiConfig(config);

// Add UI set at runtime
var setConfig = new UiSetConfig(setId: 5, new[] { typeof(ShopUI), typeof(InventoryUI) });
_uiService.AddUiSet(setConfig);

// Add instantiated UI
var dynamicUi = Instantiate(uiPrefab);
_uiService.AddUi(dynamicUi, layer: 3, openAfter: true);
```

---

## Editor Windows

The package includes three editor windows for development and debugging.

### Analytics Window

**Menu:** `Tools → UI Service → Analytics`

<!-- TODO: Add screenshot of Analytics Window -->
![Analytics Window](docs/analytics-window.png)

Monitor UI performance metrics in real-time during play mode:
- Load, open, close durations
- Open/close counts
- Color-coded performance indicators
- Timeline information

**Performance Thresholds:**
- Load: <0.1s green, <0.5s yellow, ≥0.5s red
- Open/Close: <0.05s green, <0.2s yellow, ≥0.2s red

### Hierarchy Window

**Menu:** `Tools → UI Service → Hierarchy Window`

<!-- TODO: Add screenshot of Hierarchy Window -->
![Hierarchy Window](docs/hierarchy-window.png)

View and control active presenters:
- Live hierarchy grouped by layer
- Open (🟢) / closed (🔴) status
- Quick open/close buttons
- GameObject navigation

### UiConfigs Inspector

![UiConfigs Inspector](uiconfigs-inspector.gif)

Select any `UiConfigs` asset to see the enhanced inspector:
- Visual layer hierarchy
- Color-coded layers
- Drag & drop reordering
- Statistics panel
- UI set management

