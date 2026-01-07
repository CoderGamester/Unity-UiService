# Advanced Topics

This document covers advanced features including performance optimization and helper components.

## Table of Contents

- [Helper Views](#helper-views)
- [Performance Optimization](#performance-optimization)
- [Known Limitations](#known-limitations)

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

