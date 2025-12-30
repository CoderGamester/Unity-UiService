# UI Service Samples

This folder contains example implementations demonstrating the **feature composition** pattern of the UI Service.

---

## Quick Start

1. Open **Window → Package Manager**
2. Select **UiService** from the list
3. Expand the **Samples** section
4. Click **Import** next to the sample you want

Samples are imported to `Assets/Samples/UiService/{version}/{SampleName}/`.

---

## Architecture Overview

All samples use the self-contained feature pattern:

- **Base:** `UiPresenter` or `UiPresenter<T>`
- **Features:** Self-contained components (`TimeDelayFeature`, `AnimationDelayFeature`, `UiToolkitPresenterFeature`)
- **Composition:** Mix features as needed using `[RequireComponent]`

### Service Interfaces (Important for AI Assistants)

The UI Service uses **two interfaces** with different purposes:

| Interface | Purpose | Has `Init()` | Has `Dispose()` |
|-----------|---------|--------------|-----------------|
| `IUiService` | **Consuming** - Use when you only need to open/close/query UI | ❌ No | ❌ No |
| `IUiServiceInit` | **Initializing** - Use when you create and initialize the service | ✅ Yes | ✅ Yes |

**⚠️ Critical:** The `Init(UiConfigs)` method is **only available on `IUiServiceInit`**, not on `IUiService`.

**Correct initialization pattern:**
```csharp
// ✅ CORRECT - Use IUiServiceInit when you need to call Init()
private IUiServiceInit _uiService;

void Start()
{
    _uiService = new UiService();
    _uiService.Init(_uiConfigs);  // Works!
}

void OnDestroy()
{
    _uiService?.Dispose();  // Also available on IUiServiceInit
}
```

**Common mistake (will cause CS1061 error):**
```csharp
// ❌ WRONG - IUiService does NOT have Init()
private IUiService _uiService;

void Start()
{
    _uiService = new UiService();
    _uiService.Init(_uiConfigs);  // ERROR: CS1061
}
```

---

## Samples Included

| # | Sample | Focus |
|---|--------|-------|
| 1 | [BasicUiFlow](#1-basicuiflow) | Core lifecycle |
| 2 | [DataPresenter](#2-datapresenter) | Data-driven UI |
| 3 | [DelayedPresenter](#3-delayedpresenter) | Time & animation delays |
| 4 | [UiToolkit](#4-uitoolkit) | UI Toolkit integration |
| 5 | [DelayedUiToolkit](#5-delayeduitoolkit) | Multi-feature composition |
| 6 | [Analytics](#6-analytics) | Performance tracking |
| 7 | [UiSets](#7-uisets) | HUD management |
| 8 | [MultiInstance](#8-multiinstance) | Popup stacking |
| 9 | [CustomFeatures](#9-customfeatures) | Create your own features |

---

### 1. BasicUiFlow

**Files:**
- `BasicUiExamplePresenter.cs` - Simple presenter without features
- `BasicUiFlowExample.cs` - Scene setup and UI service usage

**Demonstrates:**
- Basic UI presenter lifecycle
- Loading, opening, closing, and unloading UI
- Simple button interactions

**Pattern:**
```csharp
public class BasicUiExamplePresenter : UiPresenter
{
    protected override void OnInitialized() { }
    protected override void OnOpened() { }
    protected override void OnClosed() { }
}
```

---

### 2. DataPresenter

**Files:**
- `DataUiExamplePresenter.cs` - Presenter with typed data
- `DataPresenterExample.cs` - Data-driven UI example

**Demonstrates:**
- Using `UiPresenter<T>` for data-driven UI
- Setting and accessing presenter data
- `OnSetData()` lifecycle hook

**Pattern:**
```csharp
public struct PlayerData
{
    public string PlayerName;
    public int Level;
}

public class DataUiExamplePresenter : UiPresenter<PlayerData>
{
    protected override void OnSetData()
    {
        // Access data via Data.PlayerName, Data.Level
    }
}

// Opening with data
_uiService.OpenUiAsync<DataUiExamplePresenter, PlayerData>(playerData);
```

---

### 3. DelayedPresenter

**Files:**
- `DelayedUiExamplePresenter.cs` - Time-based delays
- `AnimatedUiExamplePresenter.cs` - Animation-based delays
- `DelayedPresenterExample.cs` - Scene setup

**Demonstrates:**
- Using `TimeDelayFeature` for time-based delays
- Using `AnimationDelayFeature` for animation-synchronized delays
- Reacting to delay completion via presenter lifecycle hooks

**Pattern (Time Delay):**
```csharp
[RequireComponent(typeof(TimeDelayFeature))]
public class DelayedUiExamplePresenter : UiPresenter
{
    [SerializeField] private TimeDelayFeature _delayFeature;
    
    protected override void OnOpened()
    {
        base.OnOpened();
        // UI is visible, delay is starting...
    }
    
    protected override void OnOpenTransitionCompleted()
    {
        // Called after delay finishes - UI is fully ready for interaction
        Debug.Log($"Opened after {_delayFeature.OpenDelayInSeconds}s delay!");
    }
    
    protected override void OnCloseTransitionCompleted()
    {
        // Called after close delay finishes
        Debug.Log("Closing transition completed!");
    }
}
```

**Pattern (Animation Delay):**
```csharp
[RequireComponent(typeof(AnimationDelayFeature))]
public class AnimatedUiExamplePresenter : UiPresenter
{
    [SerializeField] private AnimationDelayFeature _animationFeature;
    
    protected override void OnOpenTransitionCompleted()
    {
        // Called after intro animation finishes
        Debug.Log("Intro animation completed!");
    }
}
```

**Feature Configuration:**

| Feature | Inspector Settings |
|---------|-------------------|
| `TimeDelayFeature` | Open/Close delay in seconds |
| `AnimationDelayFeature` | Animation component, Intro/Outro clips |

---

### 4. UiToolkit

**Files:**
- `UiToolkitExamplePresenter.cs` - UI Toolkit presenter
- `UiToolkitExample.cs` - Scene setup
- `UiToolkitExample.uxml` - UI Toolkit layout

**Demonstrates:**
- Using `UiToolkitPresenterFeature` for UI Toolkit integration
- Querying VisualElements from the root
- Binding UI Toolkit events

**Pattern:**
```csharp
[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class UiToolkitExamplePresenter : UiPresenter
{
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        var root = _toolkitFeature.Root;
        var button = root.Q<Button>("MyButton");
        button.clicked += OnButtonClicked;
    }
}
```

---

### 5. DelayedUiToolkit

**Files:**
- `TimeDelayedUiToolkitPresenter.cs` - Time delay + UI Toolkit
- `AnimationDelayedUiToolkitPresenter.cs` - Animation delay + UI Toolkit + Data
- `DelayedUiToolkitExample.cs` - Scene setup
- `DelayedUiToolkitExample.uxml` - UI Toolkit layout

**Demonstrates:**
- Composing multiple features together
- Combining delay features with UI Toolkit
- Using data with multiple features

**Pattern:**
```csharp
[RequireComponent(typeof(TimeDelayFeature))]
[RequireComponent(typeof(UiToolkitPresenterFeature))]
public class TimeDelayedUiToolkitPresenter : UiPresenter
{
    [SerializeField] private UiToolkitPresenterFeature _toolkitFeature;
    
    protected override void OnOpened()
    {
        base.OnOpened();
        _toolkitFeature.Root.SetEnabled(false);
    }
    
    protected override void OnOpenTransitionCompleted()
    {
        // Enable UI after delay completes
        _toolkitFeature.Root.SetEnabled(true);
    }
}
```

---

### 6. Analytics

**Files:**
- `AnalyticsCallbackExample.cs` - Analytics integration example

**Demonstrates:**
- Creating a `UiAnalytics` instance
- Setting custom analytics callbacks
- Subscribing to UI lifecycle events
- Viewing performance metrics

**Pattern:**
```csharp
// Create analytics instance
var analytics = new UiAnalytics();

// Set custom callback
analytics.SetCallback(new CustomAnalyticsCallback());

// Pass to UiService constructor
IUiServiceInit uiService = new UiService(new UiAssetLoader(), analytics);
uiService.Init(_uiConfigs);

// Subscribe to UnityEvents
analytics.OnUiOpened.AddListener(data => Debug.Log($"Opened: {data.UiName}"));

// Access metrics
var metrics = analytics.GetMetrics(typeof(MyPresenter));
Debug.Log($"Opens: {metrics.OpenCount}, Load time: {metrics.LoadDuration}s");

// Log summary
analytics.LogPerformanceSummary();
```

**Custom Callback:**
```csharp
public class CustomAnalyticsCallback : IUiAnalyticsCallback
{
    public void OnUiLoaded(UiEventData data) { }
    public void OnUiOpened(UiEventData data) { }
    public void OnUiClosed(UiEventData data) { }
    public void OnUiUnloaded(UiEventData data) { }
    public void OnPerformanceMetricsUpdated(UiPerformanceMetrics metrics) { }
}
```

---

### 7. UiSets

**Files:**
- `UiSetsExample.cs` - Scene setup and UI set management
- `HudHealthBarPresenter.cs` - Example HUD element (health bar)
- `HudCurrencyPresenter.cs` - Example HUD element (currency display)

**Demonstrates:**
- Grouping multiple UIs for simultaneous management
- Common HUD pattern (health, currency, minimap, etc.)
- Preloading UI sets for smooth transitions

**Pattern:**
```csharp
// Define set IDs as enum for type safety
public enum UiSetId { GameHud = 0, PauseMenu = 1 }

// Load all UIs in a set (preload without showing)
var loadTasks = _uiService.LoadUiSetAsync((int)UiSetId.GameHud);
await UniTask.WhenAll(loadTasks);

// Close all UIs in a set (hide but keep in memory)
_uiService.CloseAllUiSet((int)UiSetId.GameHud);

// Unload all UIs in a set (destroy)
_uiService.UnloadUiSet((int)UiSetId.GameHud);

// List configured sets
foreach (var kvp in _uiService.UiSets)
{
    Debug.Log($"Set {kvp.Key}: {kvp.Value.UiInstanceIds.Length} UIs");
}
```

**Setup:**
1. Create UI presenters for your HUD elements
2. Configure them in `UiConfigs` with appropriate layers
3. Create a UI Set in `UiConfigs` containing all HUD presenters
4. Use `LoadUiSetAsync` to preload all at once

---

### 8. MultiInstance

**Files:**
- `MultiInstanceExample.cs` - Scene setup and instance management
- `NotificationPopupPresenter.cs` - Popup that supports multiple instances

**Demonstrates:**
- Creating multiple instances of the same UI type
- Using instance addresses for unique identification
- Managing popup stacking and notifications

**Pattern:**
```csharp
// Create unique instance address
var instanceAddress = $"popup_{_counter}";

// Load with instance address
await _uiService.LoadUiAsync(typeof(MyPopup), instanceAddress, openAfter: false);

// Open specific instance
await _uiService.OpenUiAsync(typeof(MyPopup), instanceAddress);

// Check visibility of specific instance
bool visible = _uiService.IsVisible<MyPopup>(instanceAddress);

// Close specific instance
_uiService.CloseUi(typeof(MyPopup), instanceAddress, destroy: true);

// Unload specific instance
_uiService.UnloadUi(typeof(MyPopup), instanceAddress);
```

**Key Concepts:**

| Concept | Description |
|---------|-------------|
| `UiInstanceId` | Combines Type + InstanceAddress for unique identification |
| Instance Address | A string that distinguishes instances (e.g., `"popup_1"`, `"popup_2"`) |
| Default Instance | When instanceAddress is `null` or empty (singleton behavior) |

---

### 9. CustomFeatures

**Files:**
- `CustomFeaturesExample.cs` - Scene setup demonstrating custom features
- `FadeFeature.cs` - Custom feature for fade in/out effects
- `ScaleFeature.cs` - Custom feature for scale in/out with curves
- `SoundFeature.cs` - Custom feature for open/close sounds
- `FadingPresenter.cs` - Presenter using FadeFeature
- `ScalingPresenter.cs` - Presenter using ScaleFeature
- `FullFeaturedPresenter.cs` - Presenter combining all three features

**Demonstrates:**
- Creating custom presenter features
- Extending `PresenterFeatureBase`
- Implementing lifecycle hooks
- Feature composition (multiple features on one presenter)

**Pattern (Custom Feature):**
```csharp
using UnityEngine;
using System.Collections;
using GameLovers.UiService;

[RequireComponent(typeof(CanvasGroup))]
public class FadeFeature : PresenterFeatureBase
{
    [SerializeField] private float _fadeInDuration = 0.3f;
    [SerializeField] private CanvasGroup _canvasGroup;

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
        while (elapsed < _fadeInDuration)
        {
            _canvasGroup.alpha = elapsed / _fadeInDuration;
            elapsed += Time.deltaTime;
            yield return null;
        }
        _canvasGroup.alpha = 1f;
        
        // Notify presenter that transition is complete
        Presenter.NotifyOpenTransitionCompleted();
    }
}
```

**Pattern (Using Custom Features):**
```csharp
[RequireComponent(typeof(FadeFeature))]
[RequireComponent(typeof(ScaleFeature))]
[RequireComponent(typeof(SoundFeature))]
public class FullFeaturedPresenter : UiPresenter
{
    protected override void OnOpenTransitionCompleted()
    {
        // Called when feature notifies transition completion
        Debug.Log("All animations complete - UI is ready!");
    }
}
```

**Lifecycle Hooks:**

| Hook | When Called |
|------|-------------|
| `OnPresenterInitialized(presenter)` | Once when presenter is created |
| `OnPresenterOpening()` | Before presenter becomes visible |
| `OnPresenterOpened()` | After presenter is visible |
| `OnPresenterClosing()` | Before presenter is hidden |
| `OnPresenterClosed()` | After presenter is hidden |

---

## Best Practices

### 1. Use Presenter Lifecycle Hooks for Transitions

Override `OnOpenTransitionCompleted()` and `OnCloseTransitionCompleted()` to react to feature transitions:

```csharp
protected override void OnOpenTransitionCompleted()
{
    // Called when delay/animation features complete their open transition
    Debug.Log("UI is fully ready for interaction!");
}

protected override void OnCloseTransitionCompleted()
{
    // Called when delay/animation features complete their close transition
    Debug.Log("UI closing transition finished!");
}
```

### 2. Use OnValidate for Auto-Assignment

Let Unity auto-assign component references in the editor:

```csharp
private void OnValidate()
{
    _canvasGroup = _canvasGroup ?? GetComponent<CanvasGroup>();
}
```

### 3. Configure in Inspector

Use `[SerializeField]` to expose settings to designers:

```csharp
[SerializeField] private float _fadeDuration = 0.3f;
[SerializeField] private AnimationCurve _easeCurve;
```

### 4. Compose Freely

Mix and match features without worrying about inheritance conflicts:

```csharp
[RequireComponent(typeof(TimeDelayFeature))]
[RequireComponent(typeof(UiToolkitPresenterFeature))]
[RequireComponent(typeof(SoundFeature))]
public class MyPresenter : UiPresenter { }
```

---

## Architecture Benefits

| Benefit | Description |
|---------|-------------|
| ✅ Self-Contained | Each feature owns its complete logic |
| ✅ Composable | Mix any features freely |
| ✅ Configurable | All settings in Unity inspector |
| ✅ Clear | One component = one capability |
| ✅ Scalable | Add new features without modifying existing code |

---

## Sample Scene Setup

All samples use **UI buttons** for input to avoid dependency on any specific input system (legacy vs new). This ensures samples work regardless of your project's input configuration.

### Prefab Structure

When setting up a sample scene, create a UI Canvas with control buttons. Example structure:

```
Scene
├── EventSystem (with StandaloneInputModule or InputSystemUIInputModule)
├── SampleControlsCanvas (Screen Space - Overlay)
│   └── VerticalLayoutGroup
│       ├── HeaderText ("Sample Name")
│       ├── Button_Action1 ("Load UI")
│       ├── Button_Action2 ("Open UI")
│       ├── Button_Action3 ("Close UI")
│       └── ... more buttons as needed
├── SampleExample (MonoBehaviour)
│   ├── UiConfigs reference
│   └── Button references (serialized fields)
└── (UI presenter prefabs are instantiated at runtime by UiService)
```

### Button Wiring

Each sample MonoBehaviour exposes button fields that should be wired in the inspector:

```csharp
[Header("UI Buttons")]
[SerializeField] private Button _loadButton;
[SerializeField] private Button _openButton;
[SerializeField] private Button _closeButton;
```

The sample will:
1. Subscribe to button click events in `Start()`
2. Unsubscribe in `OnDestroy()` to prevent memory leaks
3. Expose public methods that can also be called from code

### Input System Compatibility

| Project Setting | What Works |
|-----------------|------------|
| Legacy Input Manager | ✅ Buttons work via `StandaloneInputModule` |
| New Input System | ✅ Buttons work via `InputSystemUIInputModule` |
| Both | ✅ Either module works |

---

## Getting Started

1. **Choose a sample** that matches your use case
2. **Import it** via Package Manager
3. **Create a scene** with a control Canvas and buttons (see structure above)
4. **Wire button references** in the sample MonoBehaviour inspector
5. **Copy the pattern** to your own presenter
6. **Add required features** via `[RequireComponent]`
7. **Configure in inspector** - delays, animations, etc.
8. **Override transition hooks** (`OnOpenTransitionCompleted`, `OnCloseTransitionCompleted`) to react to feature completions

---

## Documentation

For complete documentation, see:

- **[docs/README.md](../docs/README.md)** - Documentation index
- **[docs/getting-started.md](../docs/getting-started.md)** - Quick start guide
- **[docs/core-concepts.md](../docs/core-concepts.md)** - Core concepts
- **[docs/api-reference.md](../docs/api-reference.md)** - API reference
- **[docs/advanced.md](../docs/advanced.md)** - Advanced topics
- **[docs/troubleshooting.md](../docs/troubleshooting.md)** - Troubleshooting

---

**All samples use the feature composition pattern for maximum flexibility and reusability.**
