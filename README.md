# GameLovers UI Service

[![Unity Version](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/version-1.0.0-green.svg)](CHANGELOG.md)

A powerful and flexible UI management system for Unity that provides a robust abstraction layer for handling game UI with support for layers, async loading, and UI sets. This service streamlines UI development by managing the complete lifecycle of UI presenters, from loading and initialization to display and cleanup.

## Table of Contents

- [Key Features](#key-features)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Package Structure](#package-structure)
- [Dependencies](#dependencies)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
  - [Editor Windows](#editor-windows)
  - [UI Presenter](#ui-presenter)
  - [Presenter Features](#presenter-features)
  - [UI Layers](#ui-layers)
  - [UI Sets](#ui-sets)
  - [Multi-Instance Support](#multi-instance-support)
  - [UI Configuration](#ui-configuration)
- [API Documentation](#api-documentation)
  - [Managing UI Lifecycle](#managing-ui-lifecycle)
  - [Working with UI Sets](#working-with-ui-sets)
  - [Async Operations](#async-operations)
- [Advanced Features](#advanced-features)
  - [UI Analytics and Performance Tracking](#ui-analytics-and-performance-tracking)
  - [Helper Views](#helper-views)
- [Performance Optimization](#performance-optimization)
- [Troubleshooting](#troubleshooting)
- [Examples](#examples)
- [Contributing](#contributing)
- [Support](#support)
- [License](#license)

## Key Features

- **🎭 UI Presenter Pattern** - Clean separation of UI logic with lifecycle management
- **🧩 Feature Composition** - Modular feature system for extending presenter behavior
- **📚 Layer-based Organization** - Organize UI elements by depth layers
- **🔄 Async Loading** - Load UI assets asynchronously with UniTask support
- **📦 UI Sets** - Group related UI elements for batch operations
- **🔀 Multi-Instance Support** - Multiple instances of the same UI type with unique addresses
- **💾 Memory Management** - Efficient loading/unloading of UI assets
- **🎯 Type-safe API** - Generic methods for compile-time safety
- **📊 Analytics & Performance Tracking** - Optional analytics system with dependency injection
- **🛠️ Editor Tools** - Three powerful editor windows for debugging and monitoring
- **📱 Responsive Design** - Built-in support for safe areas and screen size adjustments
- **🔧 Addressables Integration** - Seamless integration with Unity's Addressables system
- **🎨 UI Toolkit Support** - Compatible with both uGUI and UI Toolkit

## System Requirements

- **Unity** 6000.0 or higher
- **Addressables** 2.6.0 or higher
- **UniTask** 2.5.10 or higher

## Installation

### Via Unity Package Manager (Recommended)

1. Open Unity Package Manager (`Window` → `Package Manager`)
2. Click the `+` button and select `Add package from git URL`
3. Enter the following URL:
   ```
   https://github.com/CoderGamester/com.gamelovers.uiservice.git
   ```

### Via manifest.json

Add the following line to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gamelovers.uiservice": "https://github.com/CoderGamester/com.gamelovers.uiservice.git"
  }
}
```

## Package Structure

```
<root>
  ├── package.json          # Package manifest with dependencies and metadata
  ├── README.md             # This documentation file
  ├── CHANGELOG.md          # Version history and release notes
  ├── LICENSE.md            # MIT license terms
  ├── Runtime/              # Core runtime scripts for the UI service
  │   ├── GameLovers.UiService.asmdef  # Assembly definition for runtime code
  │   ├── IUiService.cs                # Main service interfaces
  │   ├── UiService.cs                 # Core service implementation
  │   ├── UiPresenter.cs               # Base presenter classes
  │   ├── UiConfigs.cs                 # Configuration ScriptableObjects
  │   ├── UiInstanceId.cs              # Multi-instance support structure
  │   ├── UiSetConfig.cs               # UI set configuration
  │   ├── UiAnalytics.cs               # Analytics and performance tracking
  │   ├── UiAssetLoader.cs             # Addressables integration
  │   ├── Features/                    # Composable presenter features
  │   │   ├── IPresenterFeature.cs     # Feature interface
  │   │   ├── PresenterFeatureBase.cs  # Base feature implementation
  │   │   ├── AnimationDelayFeature.cs # Animation-based delays
  │   │   ├── TimeDelayFeature.cs      # Time-based delays
  │   │   └── UiToolkitPresenterFeature.cs # UI Toolkit integration
  │   └── Views/                       # Helper view components
  │       ├── SafeAreaHelperView.cs
  │       ├── NonDrawingView.cs
  │       ├── InteractableTextView.cs
  │       └── AdjustScreenSizeFitterView.cs
  ├── Editor/               # Unity Editor extensions and tools
  │   ├── GameLovers.UiService.Editor.asmdef # Assembly definition for editor code
  │   ├── UiConfigsEditor.cs           # Custom inspector for UiConfigs with visual hierarchy
  │   ├── UiPresenterEditor.cs         # Custom inspector for presenters with quick controls
  │   ├── UiAnalyticsWindow.cs         # Real-time analytics monitoring window
  │   ├── UiServiceHierarchyWindow.cs  # Live UI hierarchy debugging window
  │   ├── DefaultUiConfigsEditor.cs    # Default configuration setup
  │   └── NonDrawingViewEditor.cs      # Custom inspector for NonDrawingView
  └── Samples~/             # Example implementations
      ├── README.md                    # Samples documentation
      ├── BasicUiFlow/                 # Basic presenter usage
      ├── DataPresenter/               # Data-driven UI examples
      ├── DelayedPresenter/            # Time and animation delay examples
      ├── UiToolkit/                   # UI Toolkit integration
      ├── DelayedUiToolkit/            # Combined features examples
      └── Analytics/                   # Analytics integration example
```

## Dependencies

This package requires:

- **[Unity Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest)** (v2.6.0+) - For async asset loading
- **[UniTask](https://github.com/Cysharp/UniTask)** (v2.5.10+) - For efficient async operations

Dependencies are automatically resolved when installing via Unity Package Manager.

## Quick Start

### 1. Create UI Configuration

First, create a UI configuration asset:

1. Right-click in Project View
2. Navigate to `Create` → `ScriptableObjects` → `Configs` → `UiConfigs`
3. Configure your UI presenters in the created asset

### 2. Initialize the UI Service

```csharp
using UnityEngine;
using GameLovers.UiService;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private UiConfigs _uiConfigs;
    private IUiServiceInit _uiService;
    
    void Start()
    {
        // Create and initialize the UI service (without analytics)
        _uiService = new UiService();
        _uiService.Init(_uiConfigs);
        
        // Or with analytics (opt-in)
        // var analytics = new UiAnalytics();
        // _uiService = new UiService(new UiAssetLoader(), analytics);
        // _uiService.Init(_uiConfigs);
    }
}
```

### 3. Create Your First UI Presenter

```csharp
using UnityEngine;
using GameLovers.UiService;

public class MainMenuPresenter : UiPresenter
{
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _settingsButton;
    
    protected override void OnInitialized()
    {
        _playButton.onClick.AddListener(OnPlayClicked);
        _settingsButton.onClick.AddListener(OnSettingsClicked);
    }
    
    protected override void OnOpened()
    {
        Debug.Log("Main menu opened!");
        // Perform opening animations or setup
    }
    
    protected override void OnClosed()
    {
        Debug.Log("Main menu closed!");
        // Cleanup or save state
    }
    
    private void OnPlayClicked()
    {
        Close(destroy: false);
        _uiService.OpenUiAsync<GameplayHudPresenter>();
    }
    
    private async void OnSettingsClicked()
    {
        var settings = await _uiService.OpenUiAsync<SettingsPresenter>();
        // Settings is now open and ready
    }
}
```

### 4. Open and Manage UI

```csharp
public class GameManager : MonoBehaviour
{
    private IUiService _uiService;
    
    async void Start()
    {
        // Open main menu
        var mainMenu = await _uiService.OpenUiAsync<MainMenuPresenter>();
        
        // Check if a UI is visible
        if (_uiService.IsVisible<MainMenuPresenter>())
        {
            Debug.Log("Main menu is currently visible");
        }
        
        // Close specific UI
        _uiService.CloseUi<MainMenuPresenter>();
        
        // Close all UI
        _uiService.CloseAllUi();
    }
}
```

## Core Concepts

### Editor Windows

The package includes three powerful editor windows for managing, monitoring, and debugging your UI system:

#### 1. Analytics Window

Monitor UI performance metrics and events in real-time during play mode.

**Opening the Window:**
- Navigate to **Tools → UI Service → Analytics**

**Features:**
- **Real-time Performance Metrics** - View load, open, and close durations for each UI
- **Usage Statistics** - Track open/close counts and total lifetime for each presenter
- **Color-coded Performance** - Visual indicators for slow operations (green/yellow/red)
- **Timeline Information** - See when UIs were first opened and last closed
- **Auto-refresh** - Automatically updates metrics while playing
- **Clear Data** - Reset all collected analytics
- **Log Summary** - Export performance summary to console

**Usage:**
1. Open **Tools → UI Service → Analytics**
2. Enter Play Mode
3. Use your UI system normally
4. View metrics updating in real-time

**Performance Thresholds:**
- **Load Time**: <0.1s (green), <0.5s (yellow), ≥0.5s (red)
- **Open/Close Time**: <0.05s (green), <0.2s (yellow), ≥0.2s (red)

**Note:** Analytics must be enabled in your UiService initialization to see data:
```csharp
var analytics = new UiAnalytics();
var uiService = new UiService(new UiAssetLoader(), analytics);
```

#### 2. Hierarchy Window

View and control all active UI presenters in the scene during play mode.

**Opening the Window:**
- Navigate to **Tools → UI Service → Hierarchy Window**

**Features:**
- **Live UI Hierarchy** - See all loaded presenters grouped by layer
- **Status Indicators** - Visual open (🟢) / closed (🔴) status for each UI
- **Quick Controls** - Open/close any UI with one click
- **Detailed Inspector** - Expand presenters to see full details
- **GameObject Navigation** - Select and ping presenters in the hierarchy
- **Instance Information** - View instance addresses for multi-instance UIs
- **Batch Operations** - Close all UIs at once
- **Auto-refresh** - Updates every 0.5 seconds automatically

**Usage:**
1. Open **Tools → UI Service → Hierarchy Window**
2. Enter Play Mode
3. See all active presenters organized by layer
4. Click on any presenter to expand details
5. Use quick controls to open/close/destroy UIs

**Keyboard Shortcuts:**
- Click presenter name to select in hierarchy
- "Open" button to open closed UIs
- "Close" button to close open UIs

#### 3. UiConfigs Inspector (Layer Visualizer)

Built-in custom inspector for visualizing your UI configuration and layer organization.

**Features:**
- **Visual Layer Hierarchy** - See all UIs grouped by layer number in the inspector
- **Color-coded Layers** - Each layer has a unique color for easy identification
- **Configuration Overview** - View UiConfigs structure directly in the inspector
- **Drag & Drop Support** - Reorder UI configs and sets with drag-and-drop
- **Collapsible Sections** - Expand/collapse layers and sets for better organization
- **Search & Filter** - Find specific UIs quickly within the inspector
- **Statistics Panel** - Total UIs, layer distribution, sync vs async loading stats
- **Synchronous Loading Indicators** - Clearly marks UIs that load synchronously
- **UI Set Management** - Create, edit, and organize UI sets visually

**Usage:**
1. Select your `UiConfigs` ScriptableObject in the Project window
2. View the enhanced inspector with visual layer hierarchy
3. Browse layer organization and configuration
4. Use drag-and-drop to reorder items
5. Review statistics for optimization insights

**Inspector Sections:**
- **Configuration** - Main UI configuration list with layer visualization
- **UI Sets** - Manage groups of related UIs
- **Statistics** - Overview of your UI configuration
- **Layer Hierarchy** - Visual tree view of all layers and their UIs

### UI Presenter

The `UiPresenter` is the base class for all UI elements in the system. It provides:

- **Lifecycle callbacks** - `OnInitialized()`, `OnOpened()`, `OnClosed()`
- **State management** - Track open/closed state
- **Service integration** - Direct access to UI service

#### Basic UI Presenter

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

#### UI Presenter with Data

For UI that needs initialization data:

```csharp
public struct PlayerProfileData
{
    public string PlayerName;
    public int Level;
    public Sprite Avatar;
}

public class PlayerProfilePresenter : UiPresenter<PlayerProfileData>
{
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _levelText;
    [SerializeField] private Image _avatarImage;
    
    protected override void OnSetData()
    {
        // Called when data is set
        _nameText.text = Data.PlayerName;
        _levelText.text = $"Level {Data.Level}";
        _avatarImage.sprite = Data.Avatar;
    }
}

// Usage
var profileData = new PlayerProfileData 
{ 
    PlayerName = "Hero", 
    Level = 42,
    Avatar = avatarSprite
};

await _uiService.OpenUiAsync<PlayerProfilePresenter, PlayerProfileData>(profileData);
```

---

### Presenter Features

The UI Service uses a **feature-based composition system** that allows you to extend presenter behavior without inheritance complexity. Features are self-contained components that add specific capabilities to your presenters.

#### Built-in Features

##### 1. TimeDelayFeature

Adds time-based delays to UI opening and closing:

```csharp
[RequireComponent(typeof(TimeDelayFeature))]
public class DelayedPopup : UiPresenter
{
    [SerializeField] private TimeDelayFeature _delayFeature;
    
    protected override void OnInitialized()
    {
        // Subscribe to delay completion events
        _delayFeature.OnOpenCompletedEvent += OnDelayComplete;
    }
    
    private void OnDelayComplete()
    {
        Debug.Log("Opening delay completed!");
    }
    
    private void OnDestroy()
    {
        if (_delayFeature != null)
            _delayFeature.OnOpenCompletedEvent -= OnDelayComplete;
    }
}
```

**Configuration:**
- `Open Delay In Seconds` - Time to wait after opening (default: 0.5s)
- `Close Delay In Seconds` - Time to wait before closing (default: 0.3s)

##### 2. AnimationDelayFeature

Synchronizes UI lifecycle with animation clips:

```csharp
[RequireComponent(typeof(AnimationDelayFeature))]
public class AnimatedPopup : UiPresenter
{
    [SerializeField] private AnimationDelayFeature _animationFeature;
    
    protected override void OnInitialized()
    {
        _animationFeature.OnOpenCompletedEvent += OnAnimationComplete;
    }
    
    private void OnAnimationComplete()
    {
        Debug.Log("Animation completed!");
    }
}
```

**Configuration:**
- `Animation Component` - Auto-detected or manually assigned
- `Intro Animation Clip` - Plays when opening
- `Outro Animation Clip` - Plays when closing

**Note:** Delays automatically match animation clip lengths!

##### 3. UiToolkitPresenterFeature

Provides UI Toolkit (UI Elements) integration:

```csharp
[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class UIToolkitMenu : UiPresenter
{
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;
    
    private Button _playButton;
    private Label _titleLabel;
    
    protected override void OnInitialized()
    {
        var root = _toolkitFeature.Root;
        
        _playButton = root.Q<Button>("play-button");
        _titleLabel = root.Q<Label>("title-label");
        
        _playButton.clicked += OnPlayClicked;
    }
    
    private void OnPlayClicked()
    {
        Close(destroy: false);
    }
}
```

**Configuration:**
- `Document` - Auto-detects `UIDocument` component
- Access via `_toolkitFeature.Document` or `_toolkitFeature.Root`

#### Composing Multiple Features

Features can be freely combined:

```csharp
[RequireComponent(typeof(TimeDelayFeature))]
[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class DelayedUiToolkitPresenter : UiPresenter
{
    [SerializeField] private TimeDelayFeature _delayFeature;
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;
    
    protected override void OnInitialized()
    {
        // Disable UI until delay completes
        _toolkitFeature.Root.SetEnabled(false);
        _delayFeature.OnOpenCompletedEvent += EnableUI;
    }
    
    private void EnableUI()
    {
        _toolkitFeature.Root.SetEnabled(true);
    }
}
```

#### Creating Custom Features

Extend `PresenterFeatureBase` to create your own features:

```csharp
using UnityEngine;
using GameLovers.UiService;

/// <summary>
/// Custom feature that adds fade effect
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class FadeFeature : PresenterFeatureBase
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.3f;
    
    private void OnValidate()
    {
        _canvasGroup = _canvasGroup ?? GetComponent<CanvasGroup>();
    }
    
    public override void OnPresenterOpening()
    {
        _canvasGroup.alpha = 0f;
    }
    
    public override void OnPresenterOpened()
    {
        StartCoroutine(FadeIn());
    }
    
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            _canvasGroup.alpha = elapsed / _fadeDuration;
            elapsed += Time.deltaTime;
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }
}
```

**Available Lifecycle Hooks:**
- `OnPresenterInitialized(UiPresenter presenter)` - Called once when presenter is initialized
- `OnPresenterOpening()` - Called before UI is shown
- `OnPresenterOpened()` - Called after UI is shown
- `OnPresenterClosing()` - Called before UI is hidden
- `OnPresenterClosed()` - Called after UI is hidden

**Benefits of Feature Composition:**
- ✅ **No Inheritance Conflicts** - Mix any features freely
- ✅ **Self-Contained Logic** - Each feature owns its complete behavior
- ✅ **Inspector Configuration** - All settings visible and editable
- ✅ **Reusable** - Use the same feature across multiple presenters
- ✅ **Scalable** - Add new features without modifying existing code

---

### UI Layers

UI elements are organized into layers, where higher layer numbers appear on top:

```csharp
// Configure layers in your UiConfigs asset
// Layer 0: Background UI
// Layer 1: Game HUD
// Layer 2: Menus
// Layer 3: Popups
// Layer 4: System messages

// Close all UI in a specific layer
_uiService.CloseAllUi(layer: 2);
```

### UI Sets

Group related UI elements for batch operations:

```csharp
// Define UI sets in your UiConfigs
// Set 1: Main Menu Set (logo, menu, background)
// Set 2: Gameplay Set (HUD, minimap, controls)
// Set 3: Shop Set (shop window, inventory, currency display)

// Load entire UI set
var loadTasks = _uiService.LoadUiSetAsync(setId: 2);
await UniTask.WhenAll(loadTasks);

// Close entire UI set
_uiService.CloseAllUiSet(setId: 2);

// Unload UI set from memory
_uiService.UnloadUiSet(setId: 2);
```

### Multi-Instance Support

The UI Service supports multiple instances of the same UI type using the `UiInstanceId` system. This allows you to have multiple instances of the same presenter active simultaneously, each with a unique instance address.

#### Creating Multi-Instance UIs

```csharp
// Load multiple instances of the same UI type
var chest1 = await _uiService.LoadUiAsync<ChestRewardPresenter>(
    instanceAddress: "chest_1");
var chest2 = await _uiService.LoadUiAsync<ChestRewardPresenter>(
    instanceAddress: "chest_2");

// Open with data
await _uiService.OpenUiAsync<ChestRewardPresenter, ChestData>(
    chestData1, instanceAddress: "chest_1");
await _uiService.OpenUiAsync<ChestRewardPresenter, ChestData>(
    chestData2, instanceAddress: "chest_2");
```

#### Working with Instances

```csharp
// Get specific instance
var chest1 = _uiService.GetUi<ChestRewardPresenter>(instanceAddress: "chest_1");

// Check if specific instance is visible
bool isVisible = _uiService.IsVisible<ChestRewardPresenter>(
    instanceAddress: "chest_1");

// Close specific instance
_uiService.CloseUi<ChestRewardPresenter>(
    instanceAddress: "chest_1", destroy: false);

// Unload specific instance
_uiService.UnloadUi<ChestRewardPresenter>(instanceAddress: "chest_1");
```

#### Getting All Instances

```csharp
// Get all loaded presenters
var allInstances = _uiService.GetLoadedPresenters();

foreach (var instance in allInstances)
{
    Debug.Log($"Type: {instance.Type.Name}, Address: {instance.Address}");
}

// Filter by type
var chestInstances = allInstances
    .Where(i => i.Type == typeof(ChestRewardPresenter))
    .ToList();
```

#### Default vs Named Instances

```csharp
// Default instance (singleton behavior)
await _uiService.OpenUiAsync<MainMenuPresenter>();

// Named instances (multi-instance)
await _uiService.OpenUiAsync<DialogPresenter>(
    instanceAddress: "confirm_purchase");
await _uiService.OpenUiAsync<DialogPresenter>(
    instanceAddress: "confirm_exit");
```

**Best Practices:**
- Use default instances (no address) for singleton UIs (HUD, main menu, etc.)
- Use named instances for UIs that can appear multiple times (dialogs, notifications, rewards)
- Always specify instance address when working with multi-instance UIs to avoid ambiguity

**Note:** If you call `GetUi<T>()` without specifying an instance address when multiple instances exist, a warning will be logged and the first found instance will be returned.

---

### UI Configuration

Configure your UI in the `UiConfigs` ScriptableObject:

1. **Type** - The presenter class type
2. **Addressable Address** - Addressable reference key to the UI prefab
3. **Layer** - Which layer the UI belongs to (higher = closer to camera)
4. **Load Synchronously** - Whether to load synchronously (use sparingly for critical UIs)
5. **UI Set ID** - Optional grouping ID for batch operations

**Creating a UiConfigs Asset:**
1. Right-click in Project View
2. Navigate to `Create` → `ScriptableObjects` → `Configs` → `UiConfigs`
3. Configure your UI presenters in the created asset
4. Assign the asset to your UI service initialization

## API Documentation

### Managing UI Lifecycle

#### Loading and Unloading

```csharp
// Load UI into memory without opening
var ui = await _uiService.LoadUiAsync<InventoryPresenter>();

// Load and immediately open
var ui = await _uiService.LoadUiAsync<InventoryPresenter>(openAfter: true);

// Load with specific instance address (multi-instance)
var ui = await _uiService.LoadUiAsync<NotificationPresenter>(
    instanceAddress: "reward_notification", 
    openAfter: false);

// Check if loaded
var loadedPresenters = _uiService.GetLoadedPresenters();
bool isLoaded = loadedPresenters.Any(p => p.Type == typeof(InventoryPresenter));

// Unload from memory
_uiService.UnloadUi<InventoryPresenter>();

// Unload specific instance
_uiService.UnloadUi<NotificationPresenter>(instanceAddress: "reward_notification");
```

#### Opening and Closing

```csharp
// Open UI (loads if necessary)
var shop = await _uiService.OpenUiAsync<ShopPresenter>();

// Open with data
var questData = new QuestData { QuestId = 101, Title = "Dragon Slayer" };
await _uiService.OpenUiAsync<QuestPresenter, QuestData>(questData);

// Close UI (keeps in memory)
_uiService.CloseUi<ShopPresenter>();

// Close and destroy
_uiService.CloseUi<ShopPresenter>(destroy: true);

// Get UI if loaded
var hud = _uiService.GetUi<GameHudPresenter>();
```

### Working with UI Sets

```csharp
// Load all UI in a set
var loadTasks = _uiService.LoadUiSetAsync(setId: 1);
var presenters = await UniTask.WhenAll(loadTasks);

// Add runtime UI to service
var dynamicUi = Instantiate(uiPrefab);
_uiService.AddUi(dynamicUi, layer: 3, openAfter: true);

// Remove UI set and get removed presenters
var removedPresenters = _uiService.RemoveUiSet(setId: 2);
foreach (var presenter in removedPresenters)
{
    Destroy(presenter.gameObject);
}
```

### Async Operations

All async operations use UniTask for better performance and WebGL support:

```csharp
// Sequential loading
var menu = await _uiService.OpenUiAsync<MainMenuPresenter>();
var settings = await _uiService.OpenUiAsync<SettingsPresenter>();

// Parallel loading
var menuTask = _uiService.OpenUiAsync<MainMenuPresenter>();
var hudTask = _uiService.OpenUiAsync<GameHudPresenter>();
await UniTask.WhenAll(menuTask, hudTask);

// With cancellation
var cts = new CancellationTokenSource();
try
{
    await _uiService.OpenUiAsync<LoadingPresenter>()
        .AttachExternalCancellation(cts.Token);
}
catch (OperationCanceledException)
{
    Debug.Log("UI loading was cancelled");
}
```

## Advanced Features

### UI Analytics and Performance Tracking

The UI Service includes an optional analytics system for tracking UI events and performance metrics. Analytics is **opt-in** via dependency injection.

#### Enabling Analytics

```csharp
using GameLovers.UiService;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private UiConfigs _uiConfigs;
    
    void Start()
    {
        // Create analytics instance
        var analytics = new UiAnalytics();
        
        // Inject analytics into UI service
        var uiService = new UiService(new UiAssetLoader(), analytics);
        uiService.Init(_uiConfigs);
        
        // Analytics is now tracking all UI events
    }
}
```

#### Running Without Analytics

By default, the UI Service runs without analytics overhead:

```csharp
// No analytics - uses NullAnalytics internally (zero overhead)
var uiService = new UiService();
uiService.Init(_uiConfigs);

// Or explicitly without analytics
var uiService = new UiService(new UiAssetLoader(), null);
```

#### What Gets Tracked

When analytics is enabled, the system automatically tracks:

- **UI Loading** - Time to load UI from Addressables
- **UI Opening** - Time to complete opening animations
- **UI Closing** - Time to complete closing animations
- **UI Lifecycle** - Total lifetime from first open to final close
- **Open/Close Counts** - How many times each UI was opened/closed
- **Timestamps** - When UI was first opened and last closed

#### Accessing Performance Metrics

```csharp
public class PerformanceMonitor : MonoBehaviour
{
    private IUiService _uiService;
    
    void LogPerformance()
    {
        var analytics = _uiService.Analytics;
        
        // Get all metrics
        foreach (var kvp in analytics.PerformanceMetrics)
        {
            var metrics = kvp.Value;
            Debug.Log($"{metrics.UiName}:");
            Debug.Log($"  Load Time: {metrics.LoadDuration:F3}s");
            Debug.Log($"  Open Time: {metrics.OpenDuration:F3}s");
            Debug.Log($"  Close Time: {metrics.CloseDuration:F3}s");
            Debug.Log($"  Opened {metrics.OpenCount} times");
            Debug.Log($"  Total Lifetime: {metrics.TotalLifetime:F1}s");
        }
        
        // Get specific UI metrics
        var shopMetrics = analytics.GetMetrics(typeof(ShopPresenter));
        if (shopMetrics.OpenCount > 0)
        {
            Debug.Log($"Shop was opened {shopMetrics.OpenCount} times");
        }
        
        // Print summary to console
        analytics.LogPerformanceSummary();
    }
}
```

#### Subscribing to Analytics Events

```csharp
public class AnalyticsListener : MonoBehaviour
{
    private IUiService _uiService;
    
    void Start()
    {
        var analytics = _uiService.Analytics;
        
        // Subscribe to Unity Events
        analytics.OnUiOpened.AddListener(OnUiOpened);
        analytics.OnUiClosed.AddListener(OnUiClosed);
        analytics.OnPerformanceMetricsUpdated.AddListener(OnMetricsUpdated);
    }
    
    private void OnUiOpened(UiEventData data)
    {
        Debug.Log($"UI Opened: {data.UiName} on layer {data.Layer}");
        // Send to your analytics backend
        SendToBackend("ui_opened", data.UiName);
    }
    
    private void OnUiClosed(UiEventData data)
    {
        Debug.Log($"UI Closed: {data.UiName} (destroyed: {data.WasDestroyed})");
    }
    
    private void OnMetricsUpdated(UiPerformanceMetrics metrics)
    {
        // Track performance issues
        if (metrics.LoadDuration > 1.0f)
        {
            Debug.LogWarning($"{metrics.UiName} took {metrics.LoadDuration:F2}s to load!");
        }
    }
}
```

#### Custom Analytics Integration

Implement `IUiAnalyticsCallback` for custom analytics backends:

```csharp
public class CustomAnalytics : IUiAnalyticsCallback
{
    public void OnUiLoaded(UiEventData data)
    {
        // Send to Firebase, Unity Analytics, or custom backend
        FirebaseAnalytics.LogEvent("ui_loaded", new Parameter("ui_name", data.UiName));
    }
    
    public void OnUiOpened(UiEventData data)
    {
        MyAnalyticsService.Track("ui_opened", new { 
            name = data.UiName, 
            layer = data.Layer,
            timestamp = data.Timestamp 
        });
    }
    
    public void OnUiClosed(UiEventData data)
    {
        MyAnalyticsService.Track("ui_closed", new { 
            name = data.UiName,
            destroyed = data.WasDestroyed
        });
    }
    
    public void OnUiUnloaded(UiEventData data)
    {
        // Track memory management
    }
    
    public void OnPerformanceMetricsUpdated(UiPerformanceMetrics metrics)
    {
        // Send performance data
        if (metrics.LoadDuration > 0.5f)
        {
            MyAnalyticsService.TrackPerformance("slow_ui_load", metrics.UiName, metrics.LoadDuration);
        }
    }
}

// Register callback
void Start()
{
    var analytics = new UiAnalytics();
    analytics.SetCallback(new CustomAnalytics());
    
    var uiService = new UiService(new UiAssetLoader(), analytics);
    uiService.Init(_uiConfigs);
}
```

#### Creating Custom Analytics Implementations

You can create custom analytics implementations for special use cases:

```csharp
public class FileLoggingAnalytics : IUiAnalytics
{
    private readonly UiAnalytics _baseAnalytics = new UiAnalytics();
    private readonly StreamWriter _logFile;
    
    public FileLoggingAnalytics(string logPath)
    {
        _logFile = new StreamWriter(logPath, append: true);
    }
    
    public void TrackOpenComplete(Type uiType, int layer)
    {
        // Log to file
        _logFile.WriteLine($"{DateTime.Now:HH:mm:ss} - Opened: {uiType.Name}");
        _logFile.Flush();
        
        // Also track normally
        _baseAnalytics.TrackOpenComplete(uiType, layer);
    }
    
    // Implement other IUiAnalytics methods...
    // Delegate to _baseAnalytics for standard tracking
}

// Usage
var analytics = new FileLoggingAnalytics("ui_events.log");
var uiService = new UiService(new UiAssetLoader(), analytics);
```

### Helper Views

The package includes several helper components:

#### Safe Area Helper

Automatically adjusts UI for device safe areas (notches, rounded corners):

```csharp
// Add SafeAreaHelperView component to UI panels that should respect safe areas
[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    void Awake()
    {
        gameObject.AddComponent<SafeAreaHelperView>();
    }
}
```

#### Non-Drawing View

Optimize UI performance by making non-visible graphics non-rendering:

```csharp
// Add to invisible UI elements that need to block raycasts
gameObject.AddComponent<NonDrawingView>();
```

#### Screen Size Fitter

Responsive UI that adjusts to screen size:

```csharp
// Add AdjustScreenSizeFitterView for responsive layouts
var fitter = gameObject.AddComponent<AdjustScreenSizeFitterView>();
// Configure min/max sizes in the inspector
```

#### Interactable Text View

Make text elements interactive with clickable links:

```csharp
// Add InteractableTextView to TextMeshPro components
var interactableText = gameObject.AddComponent<InteractableTextView>();
// Supports <link> tags in TextMeshPro for clickable URLs
```

## Performance Optimization

### Memory Management Best Practices

#### When to Load vs. Preload UI

**Load on Demand** (Lazy Loading):
```csharp
// Best for: Infrequently used UI, large asset sizes
await _uiService.OpenUiAsync<SettingsMenu>();
// Loads and opens in one call - UI loads when needed
```

**Preload** (Eager Loading):
```csharp
// Best for: Frequently used UI, critical path, avoid loading hitches
await _uiService.LoadUiAsync<GameHud>();
// Loads into memory but keeps hidden - ready to open instantly
```

**When to Use Each Strategy**:

| Strategy | Use For | Pros | Cons |
|----------|---------|------|------|
| **On Demand** | Settings, shops, rare popups | Lower initial memory | Loading hitch when opened |
| **Preload** | HUD, frequent dialogs | Instant display | Higher memory usage |
| **Preload Sets** | Level-specific UI groups | Batch loading efficiency | Memory overhead |

#### Unloading UI from Memory

```csharp
// Close but keep in memory (fast to reopen)
_uiService.CloseUi<Shop>(destroy: false);

// Close and unload from memory (free resources)
_uiService.CloseUi<Shop>(destroy: true);
// Or explicitly:
_uiService.UnloadUi<Shop>();
```

**When to Destroy**:
- ✅ Large UI assets (>5MB)
- ✅ Level-specific UI when changing levels
- ✅ One-time tutorial/onboarding screens
- ❌ Frequently reopened UI (Settings, Pause Menu)
- ❌ Small, lightweight presenters

#### UI Sets for Efficient Batch Management

```csharp
// Define in UiConfigs: SetId 1 = Main Menu Set
// Contains: MainMenu, Background, Logo, Buttons

// Load entire menu system at once
var tasks = _uiService.LoadUiSetAsync(setId: 1);
await UniTask.WhenAll(tasks);

// When done with menu (e.g., entering gameplay):
_uiService.CloseAllUiSet(setId: 1);
_uiService.UnloadUiSet(setId: 1); // Free all menu memory
```

**Recommended Set Organization**:
- Set 0: Core/Persistent UI (always loaded)
- Set 1-10: Scene-specific UI (load per scene)
- Set 11-20: Feature-specific UI (shop, inventory, etc.)

---

### Performance Monitoring

#### Detecting Memory Issues

```csharp
// Check what's currently loaded
Debug.Log($"Loaded presenters: {_uiService.LoadedPresenters.Count}");
foreach (var kvp in _uiService.LoadedPresenters)
{
    Debug.Log($"  - {kvp.Key.Name}");
}

// Monitor visible UI
Debug.Log($"Visible presenters: {_uiService.VisiblePresenters.Count}");
```

#### Profiling UI Operations

Use Unity Profiler to monitor:
1. **Memory.Allocations** - Watch for GC spikes during `OpenUiAsync`
2. **Memory.Total** - Track memory after `LoadUiAsync` / `UnloadUi`
3. **Loading.AsyncLoad** - Identify slow-loading UI assets

**Red Flags**:
- More than 10 presenters loaded simultaneously
- Loading same UI multiple times per second
- Memory not decreasing after `UnloadUi`

---

### Optimization Tips

#### 1. Avoid Opening Same UI Repeatedly

```csharp
// ❌ BAD - Opens new instance every frame
void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
        _uiService.OpenUiAsync<PauseMenu>().Forget();
}

// ✅ GOOD - Check if already visible
void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape) && !_uiService.IsVisible<PauseMenu>())
        _uiService.OpenUiAsync<PauseMenu>().Forget();
}
```

#### 2. Use Parallel Loading for Multiple UI

```csharp
// ❌ SLOW - Sequential loading (3 seconds total if each takes 1s)
await _uiService.OpenUiAsync<Hud>();
await _uiService.OpenUiAsync<Minimap>();
await _uiService.OpenUiAsync<Chat>();

// ✅ FAST - Parallel loading (1 second total)
await UniTask.WhenAll(
    _uiService.OpenUiAsync<Hud>(),
    _uiService.OpenUiAsync<Minimap>(),
    _uiService.OpenUiAsync<Chat>()
);
```

#### 3. Preload Critical UI During Loading Screen

```csharp
public async UniTask LoadLevel()
{
    // Show loading screen
    await _uiService.OpenUiAsync<LoadingScreen>();
    
    // Preload level UI in parallel with level load
    var uiTask = UniTask.WhenAll(
        _uiService.LoadUiAsync<GameHud>(),
        _uiService.LoadUiAsync<PauseMenu>(),
        _uiService.LoadUiAsync<GameOverScreen>()
    );
    var levelTask = SceneManager.LoadSceneAsync("GameLevel").ToUniTask();
    
    await UniTask.WhenAll(uiTask, levelTask);
    
    // Hide loading screen
    _uiService.CloseUi<LoadingScreen>();
}
```

#### 4. Clean Up When Switching Scenes

```csharp
async void OnLevelComplete()
{
    // Close all gameplay UI
    _uiService.CloseAllUi(layer: 1); // Gameplay layer
    
    // Unload gameplay UI set
    _uiService.UnloadUiSet(setId: 2);
    
    // Load main menu
    await _uiService.OpenUiAsync<LevelCompleteScreen>();
}
```

#### 5. Optimize Feature Usage

**Animation Performance**:
```csharp
// ✅ GOOD - Use TimeDelayFeature for simple fades (lighter)
[RequireComponent(typeof(TimeDelayFeature))]
public class SimpleFadePopup : UiPresenter
{
    [SerializeField] private TimeDelayFeature _delayFeature;
    // Configure 0.3s delays in inspector
}

// ⚠️ HEAVIER - AnimationDelayFeature for complex animations
[RequireComponent(typeof(AnimationDelayFeature))]
public class ComplexAnimatedPopup : UiPresenter
{
    [SerializeField] private AnimationDelayFeature _animationFeature;
    // Only use if you need complex animation sequences
}
```

**Feature Best Practices**:
- Keep open/close delays under 0.5 seconds for better user experience
- Use `TimeDelayFeature` for simple timed delays (more performant)
- Use `AnimationDelayFeature` only when synchronized with actual animations
- Disable `Animation`/`Animator` components when UI is closed
- Avoid adding unnecessary features - each feature adds minimal overhead but it adds up

## Troubleshooting

### Common Issues and Solutions

#### Issue: Data Not Showing in UiPresenter<TData>

**Symptoms**: UI opens but data fields are empty/default

**Possible Causes & Solutions**:

1. **Not Using Generic Open Method**
   ```csharp
   // ❌ WRONG - Data not passed
   await _uiService.OpenUiAsync<PlayerProfile>();
   
   // ✅ CORRECT - Use generic overload
   var data = new PlayerData { Name = "Hero", Level = 10 };
   await _uiService.OpenUiAsync<PlayerProfile, PlayerData>(data);
   ```

2. **OnSetData Not Implemented**
   ```csharp
   public class PlayerProfile : UiPresenter<PlayerData>
   {
       // ❌ MISSING - Override OnSetData
       
       // ✅ CORRECT
       protected override void OnSetData()
       {
           nameText.text = Data.Name;
           levelText.text = Data.Level.ToString();
       }
   }
   ```

3. **Wrong Base Class**
   ```csharp
   // ❌ WRONG - Missing <T> generic
   public class PlayerProfile : UiPresenter
   
   // ✅ CORRECT
   public class PlayerProfile : UiPresenter<PlayerData>
   ```

---

#### Issue: Animations Not Playing

**Symptoms**: UI with `AnimationDelayFeature` opens/closes instantly without animation

**Possible Causes & Solutions**:

1. **Feature Not Added**
   ```csharp
   // ❌ MISSING - Feature component not attached
   public class AnimatedPopup : UiPresenter
   {
       // Missing [RequireComponent(typeof(AnimationDelayFeature))]
   }
   
   // ✅ CORRECT
   [RequireComponent(typeof(AnimationDelayFeature))]
   public class AnimatedPopup : UiPresenter
   {
       [SerializeField] private AnimationDelayFeature _animationFeature;
   }
   ```

2. **Animation Clips Not Assigned**
   ```csharp
   // Check in Inspector:
   // - Animation Component is assigned
   // - Intro Animation Clip is assigned
   // - Outro Animation Clip is assigned
   
   // Or use TimeDelayFeature instead:
   [RequireComponent(typeof(TimeDelayFeature))]
   public class SimpleDelayedPopup : UiPresenter
   {
       [SerializeField] private TimeDelayFeature _delayFeature;
       // Configure delays in inspector
   }
   ```

3. **Animation Component Missing**
   ```csharp
   // AnimationDelayFeature requires an Animation component
   // Add Animation component to the GameObject
   // Or use Animator with AnimationClips
   ```

---

#### Issue: "UiConfig was not added to the service" Error

**Error**: `KeyNotFoundException: The UiConfig of type X was not added to the service`

**Cause**: UI type not registered in `UiConfigs` asset

**Solution**:
1. Open your `UiConfigs` ScriptableObject asset
2. Add a new entry for your presenter type
3. Set the addressable reference to your UI prefab
4. Set the layer number
5. Save the asset

```csharp
// In UiConfigs inspector:
// Type: MyNewPresenter
// Addressable: Assets/UI/MyNewPresenter.prefab
// Layer: 2
```

---

#### Issue: UI Flickers or Appears Briefly Before Opening

**Cause**: UI GameObject is active during initialization

**Solution**: UiService automatically handles this, but if creating UI manually:
```csharp
// Ensure prefab's root GameObject is DISABLED in the prefab
// The service will enable it when ready
```

---

#### Issue: Cancellation Not Working

**Symptoms**: `CancellationToken` doesn't stop UI loading

**Possible Causes**:

1. **Token Not Passed Through**
   ```csharp
   // ❌ WRONG - Token not used
   await _uiService.OpenUiAsync<Shop>();
   
   // ✅ CORRECT - Pass token
   var cts = new CancellationTokenSource();
   await _uiService.OpenUiAsync<Shop>(cancellationToken: cts.Token);
   ```

2. **Cancelling After Load Complete**
   ```csharp
   // Can only cancel during async load, not after
   var task = _uiService.OpenUiAsync<Shop>(ct);
   await task; // Already complete
   cts.Cancel(); // Too late - no effect
   ```

### Still Having Issues?

1. **Check Unity Console** - Look for warnings/errors from UiService
2. **Verify Dependencies** - Ensure UniTask and Addressables are installed
3. **Review CHANGELOG** - Check if recent version introduced breaking changes
4. **Search GitHub Issues** - Your issue may already be reported
5. **Create New Issue** - [Report bug](https://github.com/CoderGamester/com.gamelovers.uiservice/issues) with:
   - Unity version
   - Package version
   - Code sample
   - Error logs
   - Steps to reproduce

---

## Examples

The package includes comprehensive examples demonstrating all features and patterns. Examples are located in the `Samples~` folder.

### Importing Samples

1. Open Unity Package Manager (`Window` → `Package Manager`)
2. Select "UI Service" package
3. Navigate to the "Samples" tab
4. Click "Import" next to the sample you want

### Available Samples

#### 1. **BasicUiFlow**
Demonstrates basic presenter lifecycle and simple button interactions.

**What you'll learn:**
- Creating basic UI presenters
- Opening and closing UI
- Handling button clicks
- Lifecycle methods (`OnInitialized`, `OnOpened`, `OnClosed`)

#### 2. **DataPresenter**
Shows how to create data-driven UI with `UiPresenter<T>`.

**What you'll learn:**
- Using `UiPresenter<TData>` generic base class
- Setting presenter data with `OnSetData()`
- Passing data when opening UI
- Data-driven UI updates

#### 3. **DelayedPresenter**
Examples of time-based and animation-based delays.

**What you'll learn:**
- Using `TimeDelayFeature` for timed delays
- Using `AnimationDelayFeature` for animation synchronization
- Subscribing to delay completion events
- Configuring delays in the Inspector

#### 4. **UiToolkit**
UI Toolkit (UI Elements) integration example.

**What you'll learn:**
- Using `UiToolkitPresenterFeature`
- Querying VisualElements
- Binding UI Toolkit events
- Working with UXML layouts

#### 5. **DelayedUiToolkit**
Advanced example combining multiple features.

**What you'll learn:**
- Composing multiple features together
- Combining delays with UI Toolkit
- Coordinating feature interactions
- Using data with multiple features

#### 6. **Analytics**
Analytics integration and custom callbacks.

**What you'll learn:**
- Enabling analytics tracking
- Implementing `IUiAnalyticsCallback`
- Tracking UI events
- Integrating with custom analytics backends

### Sample Documentation

Each sample folder includes:
- **Source code** with detailed comments
- **Example scenes** ready to run
- **Prefabs** demonstrating proper setup
- **UXML files** for UI Toolkit examples

For detailed sample documentation, see `Samples~/README.md` after importing.

### Running Samples

1. Import the desired sample
2. Open the example scene from `Samples/UiService/<SampleName>/`
3. Enter Play Mode
4. Interact with the UI to see features in action

**Note:** All samples use the pure feature composition pattern introduced in v1.0.0.

---

## Contributing

We welcome contributions from the community! Here's how you can help:

### Reporting Issues

- Use the [GitHub Issues](https://github.com/CoderGamester/com.gamelovers.uiservice/issues) page
- Include your Unity version, package version, and reproduction steps
- Attach relevant code samples, error logs, or screenshots

### Development Setup

1. Fork the repository on GitHub
2. Clone your fork: `git clone https://github.com/yourusername/com.gamelovers.uiservice.git`
3. Create a feature branch: `git checkout -b feature/amazing-feature`
4. Make your changes with tests
5. Commit: `git commit -m 'Add amazing feature'`
6. Push: `git push origin feature/amazing-feature`
7. Create a Pull Request

### Code Guidelines

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Add XML documentation to all public APIs
- Include unit tests for new features
- Maintain backward compatibility when possible
- Update CHANGELOG.md for notable changes

### Pull Request Process

1. Ensure all tests pass
2. Update documentation if needed
3. Add changelog entry if applicable
4. Request review from maintainers

## Support

### Documentation

- **API Reference**: See inline XML documentation in code
- **Examples**: Check sample implementations in this README
- **Changelog**: See [CHANGELOG.md](CHANGELOG.md) for version history

### Getting Help

- **Issues**: [Report bugs or request features](https://github.com/CoderGamester/com.gamelovers.uiservice/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/CoderGamester/com.gamelovers.uiservice/discussions)

### Community

- Follow [@CoderGamester](https://github.com/CoderGamester) for updates
- Star the repository if you find it useful
- Share your projects using this package

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

---

**Made with ❤️ for the Unity community**

*If this package helps your project, please consider giving it a ⭐ on GitHub!*
