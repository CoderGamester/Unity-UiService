# Troubleshooting

Common issues and their solutions.

## Table of Contents

- [Data Not Showing in UiPresenter<TData>](#data-not-showing-in-uipresentertdata)
- [Animations Not Playing](#animations-not-playing)
- [UiConfig Not Added Error](#uiconfig-not-added-error)
- [UI Flickers on Open](#ui-flickers-on-open)
- [Cancellation Not Working](#cancellation-not-working)
- [Getting Help](#getting-help)

---

## Data Not Showing in UiPresenter<TData>

**Symptoms:** UI opens but data fields are empty or show default values.

### Cause 1: Not Using Generic Open Method

```csharp
// ❌ WRONG - Data not passed
await _uiService.OpenUiAsync<PlayerProfile>();

// ✅ CORRECT - Use generic overload with data
var data = new PlayerData { Name = "Hero", Level = 10 };
await _uiService.OpenUiAsync<PlayerProfile, PlayerData>(data);
```

### Cause 2: OnSetData Not Implemented

```csharp
public class PlayerProfile : UiPresenter<PlayerData>
{
    // ❌ MISSING - Must override OnSetData
    
    // ✅ CORRECT
    protected override void OnSetData()
    {
        nameText.text = Data.Name;
        levelText.text = Data.Level.ToString();
    }
}
```

### Cause 3: Wrong Base Class

```csharp
// ❌ WRONG - Missing <T> generic
public class PlayerProfile : UiPresenter

// ✅ CORRECT
public class PlayerProfile : UiPresenter<PlayerData>
```

---

## Animations Not Playing

**Symptoms:** UI with `AnimationDelayFeature` opens/closes instantly without animation.

### Cause 1: Feature Component Not Attached

```csharp
// ❌ MISSING - No feature component
public class AnimatedPopup : UiPresenter
{
}

// ✅ CORRECT - Require and reference the feature
[RequireComponent(typeof(AnimationDelayFeature))]
public class AnimatedPopup : UiPresenter
{
    [SerializeField] private AnimationDelayFeature _animationFeature;
}
```

### Cause 2: Animation Clips Not Assigned

Check in the Inspector:
- `Animation Component` is assigned
- `Intro Animation Clip` is assigned
- `Outro Animation Clip` is assigned

**Alternative:** Use `TimeDelayFeature` for simple delays without animation:

```csharp
[RequireComponent(typeof(TimeDelayFeature))]
public class SimpleDelayedPopup : UiPresenter
{
    [SerializeField] private TimeDelayFeature _delayFeature;
    // Configure delay times in Inspector
}
```

### Cause 3: Animation Component Missing

`AnimationDelayFeature` requires an `Animation` component on the GameObject.

1. Add an `Animation` component to the presenter GameObject
2. Or use `Animator` with `AnimationClip` assets

---

## UiConfig Not Added Error

**Error:** `KeyNotFoundException: The UiConfig of type X was not added to the service`

**Cause:** The UI type is not registered in your `UiConfigs` asset.

### Solution

1. Open your `UiConfigs` ScriptableObject asset
2. Add a new entry for your presenter type:
   - **Type**: Select your presenter class
   - **Addressable Address**: Set the Addressable key (e.g., `UI/MyPresenter`)
   - **Layer**: Set the depth layer number
3. Save the asset

**Verification:**
```csharp
// In UiConfigs inspector:
// Type: MyNewPresenter
// Addressable: Assets/UI/MyNewPresenter.prefab
// Layer: 2
```

---

## UI Flickers on Open

**Symptoms:** UI briefly appears at wrong position or state before proper display.

**Cause:** Prefab's root GameObject is active during initialization.

### Solution

1. Open your UI prefab
2. **Disable** the root GameObject (uncheck the checkbox at top of Inspector)
3. Save the prefab

The UI Service will enable it when ready to display.

**Note:** If creating UI dynamically:
```csharp
var go = Instantiate(prefab);
go.SetActive(false); // Ensure disabled
_uiService.AddUi(go.GetComponent<UiPresenter>(), layer: 3, openAfter: true);
```

---

## Cancellation Not Working

**Symptoms:** `CancellationToken` doesn't stop UI loading.

### Cause 1: Token Not Passed

```csharp
// ❌ WRONG - Token not used
await _uiService.OpenUiAsync<Shop>();

// ✅ CORRECT - Pass the token
var cts = new CancellationTokenSource();
await _uiService.OpenUiAsync<Shop>(cancellationToken: cts.Token);
```

### Cause 2: Cancelling Too Late

Cancellation only works during the async load phase:

```csharp
var cts = new CancellationTokenSource();

// Start loading
var task = _uiService.OpenUiAsync<Shop>(cts.Token);

// Cancel BEFORE await completes
cts.Cancel(); // ✅ Works if still loading

await task; // Already complete
cts.Cancel(); // ❌ Too late - no effect
```

### Cause 3: Not Handling the Exception

```csharp
var cts = new CancellationTokenSource();

try
{
    await _uiService.OpenUiAsync<Shop>(cts.Token);
}
catch (OperationCanceledException)
{
    // ✅ Handle cancellation
    Debug.Log("Loading was cancelled");
}
```

---

## Common Mistakes

### Opening Same UI Multiple Times

```csharp
// ❌ BAD - Can open duplicates
void Update()
{
    if (Input.GetKeyDown(KeyCode.I))
        _uiService.OpenUiAsync<Inventory>().Forget();
}

// ✅ GOOD - Check visibility first
void Update()
{
    if (Input.GetKeyDown(KeyCode.I) && !_uiService.IsVisible<Inventory>())
        _uiService.OpenUiAsync<Inventory>().Forget();
}
```

### Forgetting to Initialize

```csharp
// ❌ WRONG - Service not initialized
private IUiService _uiService = new UiService();

async void Start()
{
    await _uiService.OpenUiAsync<Menu>(); // Throws!
}

// ✅ CORRECT - Call Init first
void Start()
{
    _uiService = new UiService();
    _uiService.Init(_uiConfigs);
}
```

### Not Disposing

```csharp
// ❌ WRONG - Memory leak
void OnDestroy()
{
    // Service still holds references
}

// ✅ CORRECT - Clean up
void OnDestroy()
{
    _uiService?.Dispose();
}
```

---

## Getting Help

### Before Reporting

1. **Check Unity Console** - Look for warnings/errors from UiService
2. **Verify Dependencies** - Ensure UniTask and Addressables are installed correctly
3. **Review CHANGELOG** - Check if recent version introduced breaking changes
4. **Search Issues** - Your issue may already be reported on GitHub

### Reporting a Bug

Create an issue at [GitHub Issues](https://github.com/CoderGamester/com.gamelovers.uiservice/issues) with:

- **Unity version** (e.g., 6000.0.5f1)
- **Package version** (from `package.json`)
- **Code sample** reproducing the issue
- **Error logs** (full stack trace)
- **Steps to reproduce**

### Feature Requests

Use [GitHub Discussions](https://github.com/CoderGamester/com.gamelovers.uiservice/discussions) to:
- Suggest new features
- Ask questions
- Share how you're using the package

---

## Debug Checklist

When something isn't working, check these in order:

- [ ] UI Service is initialized with `Init(uiConfigs)`
- [ ] Presenter type is registered in `UiConfigs`
- [ ] Prefab is marked as Addressable
- [ ] Addressable address matches `UiConfigs` entry
- [ ] Prefab root GameObject is disabled
- [ ] UniTask and Addressables packages are installed
- [ ] No compilation errors in project
- [ ] Feature components (if any) are attached and configured

