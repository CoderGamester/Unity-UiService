# Advanced Topics

This document covers advanced features including analytics, performance optimization, and helper components.

## Table of Contents

- [UI Analytics](#ui-analytics)
- [Helper Views](#helper-views)
- [Performance Optimization](#performance-optimization)
- [Known Limitations](#known-limitations)

---

## UI Analytics

The UI Service includes an optional analytics system for tracking UI events and performance metrics.

### Enabling Analytics

Analytics is opt-in via dependency injection:

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
        
        // Inject into UI service
        var uiService = new UiService(new AddressablesUiAssetLoader(), analytics);
        uiService.Init(_uiConfigs);
    }
}
```

### Running Without Analytics

By default, there's zero overhead:

```csharp
// No analytics - uses NullAnalytics internally
var uiService = new UiService();
uiService.Init(_uiConfigs);
```

### What Gets Tracked

| Metric | Description |
|--------|-------------|
| Load Duration | Time to load UI from Addressables |
| Open Duration | Time to complete opening (including delays) |
| Close Duration | Time to complete closing |
| Open/Close Counts | How many times each UI was opened/closed |
| Total Lifetime | Cumulative time UI was visible |
| Timestamps | First opened, last closed times |

### Accessing Metrics

```csharp
var analytics = uiService.Analytics;

// Get all metrics
foreach (var kvp in analytics.PerformanceMetrics)
{
    var metrics = kvp.Value;
    Debug.Log($"{metrics.UiName}:");
    Debug.Log($"  Load Time: {metrics.LoadDuration:F3}s");
    Debug.Log($"  Open Time: {metrics.OpenDuration:F3}s");
    Debug.Log($"  Opened {metrics.OpenCount} times");
}

// Get specific UI metrics
var shopMetrics = analytics.GetMetrics(typeof(ShopPresenter));

// Print summary to console
analytics.LogPerformanceSummary();
```

### Subscribing to Events

```csharp
var analytics = uiService.Analytics;

analytics.OnUiOpened.AddListener(data => {
    Debug.Log($"UI Opened: {data.UiName} on layer {data.Layer}");
});

analytics.OnUiClosed.AddListener(data => {
    Debug.Log($"UI Closed: {data.UiName} (destroyed: {data.WasDestroyed})");
});

analytics.OnPerformanceMetricsUpdated.AddListener(metrics => {
    if (metrics.LoadDuration > 1.0f)
    {
        Debug.LogWarning($"{metrics.UiName} is slow to load!");
    }
});
```

### Custom Analytics Backends

Implement `IUiAnalyticsCallback` for integration with external services:

```csharp
public class FirebaseAnalyticsCallback : IUiAnalyticsCallback
{
    public void OnUiLoaded(UiEventData data)
    {
        FirebaseAnalytics.LogEvent("ui_loaded", 
            new Parameter("ui_name", data.UiName));
    }
    
    public void OnUiOpened(UiEventData data)
    {
        FirebaseAnalytics.LogEvent("ui_opened", 
            new Parameter("ui_name", data.UiName),
            new Parameter("layer", data.Layer));
    }
    
    public void OnUiClosed(UiEventData data) { /* ... */ }
    public void OnUiUnloaded(UiEventData data) { /* ... */ }
    public void OnPerformanceMetricsUpdated(UiPerformanceMetrics metrics) { /* ... */ }
}

// Register callback
var analytics = new UiAnalytics();
analytics.SetCallback(new FirebaseAnalyticsCallback());
```

---

## Helper Views

Built-in components for common UI needs.

### SafeAreaHelperView

Automatically adjusts UI for device safe areas (notches, Dynamic Island, rounded corners).

```csharp
// Add to any RectTransform that should respect safe areas
gameObject.AddComponent<SafeAreaHelperView>();
```

**Use for:** Header bars, bottom navigation, fullscreen content that shouldn't be obscured.

### NonDrawingView

An invisible graphic that blocks raycasts without rendering. Reduces draw calls.

```csharp
// Use instead of Image with alpha=0
gameObject.AddComponent<NonDrawingView>();
```

**Use for:** Invisible touch blockers, modal backgrounds that don't need visuals.

### AdjustScreenSizeFitterView

Responsive sizing between min and max constraints.

```csharp
var fitter = gameObject.AddComponent<AdjustScreenSizeFitterView>();
// Configure in Inspector: min/max width and height
```

**Use for:** Panels that should adapt to screen size while staying within bounds.

### InteractableTextView

Makes TextMeshPro text interactive with clickable links.

```csharp
// Add to TextMeshPro component
gameObject.AddComponent<InteractableTextView>();

// In your TMP text, use <link> tags:
// "Visit our <link=https://example.com>website</link>!"
```

**Use for:** Terms of service, clickable URLs, in-game hyperlinks.

---

## Performance Optimization

### Loading Strategies

| Strategy | When to Use | Tradeoff |
|----------|-------------|----------|
| **On-demand** | Rare UIs, large assets | Loads when needed, may hitch |
| **Preload** | Frequent UIs, critical path | Instant display, uses memory |
| **Preload Sets** | Scene-specific UI groups | Batch efficiency, memory overhead |

```csharp
// On-demand (lazy)
await _uiService.OpenUiAsync<SettingsMenu>();

// Preload (eager)
await _uiService.LoadUiAsync<GameHud>();
// Later: instant open
await _uiService.OpenUiAsync<GameHud>();

// Preload sets
var tasks = _uiService.LoadUiSetAsync(setId: 1);
await UniTask.WhenAll(tasks);
```

### Memory Management

```csharp
// Close but keep in memory (fast reopen)
_uiService.CloseUi<Shop>(destroy: false);

// Close and free memory
_uiService.CloseUi<Shop>(destroy: true);

// Or explicitly unload
_uiService.UnloadUi<Shop>();
```

**When to destroy:**
- ✅ Large assets (>5MB)
- ✅ Level-specific UI when changing levels
- ✅ One-time tutorials
- ❌ Frequently reopened UI (Settings, Pause)
- ❌ Small, lightweight presenters

### Parallel Loading

```csharp
// ❌ SLOW - Sequential (3s if each takes 1s)
await _uiService.OpenUiAsync<Hud>();
await _uiService.OpenUiAsync<Minimap>();
await _uiService.OpenUiAsync<Chat>();

// ✅ FAST - Parallel (1s total)
await UniTask.WhenAll(
    _uiService.OpenUiAsync<Hud>(),
    _uiService.OpenUiAsync<Minimap>(),
    _uiService.OpenUiAsync<Chat>()
);
```

### Preload During Loading Screens

```csharp
public async UniTask LoadLevel()
{
    await _uiService.OpenUiAsync<LoadingScreen>();
    
    // Load UI and level in parallel
    var uiTask = UniTask.WhenAll(
        _uiService.LoadUiAsync<GameHud>(),
        _uiService.LoadUiAsync<PauseMenu>()
    );
    var levelTask = SceneManager.LoadSceneAsync("GameLevel").ToUniTask();
    
    await UniTask.WhenAll(uiTask, levelTask);
    
    _uiService.CloseUi<LoadingScreen>();
}
```

### Avoid Repeated Opens

```csharp
// ❌ BAD - Opens every frame if held
void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
        _uiService.OpenUiAsync<PauseMenu>().Forget();
}

// ✅ GOOD - Check first
void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape) && !_uiService.IsVisible<PauseMenu>())
        _uiService.OpenUiAsync<PauseMenu>().Forget();
}
```

### Scene Cleanup

```csharp
async void OnLevelComplete()
{
    // Close gameplay layer
    _uiService.CloseAllUi(layer: 1);
    
    // Unload gameplay UI set
    _uiService.UnloadUiSet(setId: 2);
    
    // Load end-of-level UI
    await _uiService.OpenUiAsync<LevelCompleteScreen>();
}
```

### Feature Performance

**Prefer TimeDelayFeature** for simple delays (lighter):
```csharp
[RequireComponent(typeof(TimeDelayFeature))]
public class SimplePopup : UiPresenter { }
```

**Use AnimationDelayFeature** only for actual animations:
```csharp
[RequireComponent(typeof(AnimationDelayFeature))]
public class ComplexAnimatedPopup : UiPresenter { }
```

**Best practices:**
- Keep delays under 0.5s for responsiveness
- Disable Animator components when UI is closed
- Avoid unnecessary features - each adds overhead

### Monitoring

```csharp
// Check loaded count
var loaded = _uiService.GetLoadedPresenters();
Debug.Log($"Loaded: {loaded.Count}");

// Check visible count
Debug.Log($"Visible: {_uiService.VisiblePresenters.Count}");
```

**Red flags:**
- More than 10 presenters loaded simultaneously
- Loading same UI multiple times per second
- Memory not decreasing after `UnloadUi`

Use Unity Profiler to monitor:
- `Memory.Allocations` - GC spikes during open
- `Memory.Total` - Memory after load/unload
- `Loading.AsyncLoad` - Slow-loading assets

---

## Known Limitations

| Limitation | Description | Workaround |
|------------|-------------|------------|
| **Layer Range** | Layers must be 0-1000 | Use values within this range |
| **UI Toolkit Version** | UI Toolkit features require Unity 6000.0+ | Use uGUI for older versions |
| **WebGL Task.Delay** | Standard `Task.Delay` fails on WebGL | Package uses UniTask automatically |
| **Synchronous Loading** | `LoadSynchronously` blocks main thread | Use sparingly for critical startup UIs |

