# Getting Started

This guide walks you through setting up the UI Service in your Unity project and creating your first UI presenter.

## Prerequisites

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

### Via OpenUPM

```bash
openupm add com.gamelovers.uiservice
```

---

## Step 1: Create UI Configuration

The UI Service requires a configuration asset to know about your UI presenters.

1. Right-click in Project View
2. Navigate to `Create` → `ScriptableObjects` → `Configs` → `UiConfigs`
3. Name it (e.g., `GameUiConfigs`)

This ScriptableObject will store:
- UI presenter types and their addressable addresses
- Layer assignments for depth sorting
- UI set groupings for batch operations

---

## Step 2: Initialize the UI Service

Create a game initializer script to set up the UI Service:

```csharp
using UnityEngine;
using GameLovers.UiService;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private UiConfigs _uiConfigs;
    private IUiServiceInit _uiService;
    
    void Start()
    {
        // Create and initialize the UI service
        _uiService = new UiService();
        _uiService.Init(_uiConfigs);
        
        // The service is now ready to use
    }
    
    void OnDestroy()
    {
        // Clean up when done
        _uiService?.Dispose();
    }
}
```

### With Analytics (Optional)

```csharp
void Start()
{
    // Create analytics instance for performance tracking
    var analytics = new UiAnalytics();
    
    // Inject into UI service
    _uiService = new UiService(new AddressablesUiAssetLoader(), analytics);
    _uiService.Init(_uiConfigs);
}
```

### Choosing an Asset Loader

The UI Service supports multiple asset loading strategies out of the box:

| Loader | Scenario |
|--------|----------|
| `AddressablesUiAssetLoader` | **Recommended** - Uses Unity's Addressables system for async loading. |
| `PrefabRegistryUiAssetLoader` | Best for samples or when prefabs are directly referenced in game code. |
| `ResourcesUiAssetLoader` | Loads assets from the `Resources` folder (traditional Unity workflow). |

Example using the `ResourcesUiAssetLoader`:

```csharp
void Start()
{
    // Initialize using Resources loader
    _uiService = new UiService(new ResourcesUiAssetLoader());
    _uiService.Init(_uiConfigs);
}
```

---

## Step 3: Create Your First UI Presenter

### 3.1 Create the Presenter Script

```csharp
using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

public class MainMenuPresenter : UiPresenter
{
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _settingsButton;
    
    protected override void OnInitialized()
    {
        // Called once when the presenter is first loaded
        _playButton.onClick.AddListener(OnPlayClicked);
        _settingsButton.onClick.AddListener(OnSettingsClicked);
    }
    
    protected override void OnOpened()
    {
        // Called every time the UI is shown
        Debug.Log("Main menu opened!");
    }
    
    protected override void OnClosed()
    {
        // Called when the UI is hidden
        Debug.Log("Main menu closed!");
    }
    
    private void OnPlayClicked()
    {
        Close(destroy: false);
        // Open gameplay UI...
    }
    
    private void OnSettingsClicked()
    {
        // Open settings...
    }
}
```

### 3.2 Create the UI Prefab

1. Create a new Canvas in your scene
2. Add your UI elements (buttons, text, images)
3. Add the `MainMenuPresenter` script to the Canvas
4. **Important:** Set the prefab's root GameObject to **disabled** (the service will enable it when opened)
5. Save as a prefab

### 3.3 Make It Addressable

1. Select your prefab in the Project view
2. In the Inspector, check "Addressable"
3. Set the address (e.g., `UI/MainMenu`)

### 3.4 Register in UiConfigs

1. Open your `UiConfigs` asset
2. Add a new entry:
   - **Type**: `MainMenuPresenter`
   - **Address**: `UI/MainMenu`
   - **Layer**: `1` (or your preferred layer)

---

## Step 4: Open and Manage UI

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
        
        // Close specific UI (keeps in memory for fast reopen)
        _uiService.CloseUi<MainMenuPresenter>();
        
        // Close and destroy (frees memory)
        _uiService.CloseUi<MainMenuPresenter>(destroy: true);
        
        // Close all visible UI
        _uiService.CloseAllUi();
    }
}
```

---

## Step 5: Create a Data-Driven Presenter

For UI that needs initialization data:

```csharp
// Define your data structure
public struct PlayerProfileData
{
    public string PlayerName;
    public int Level;
    public Sprite Avatar;
}

// Create a presenter that uses the data
public class PlayerProfilePresenter : UiPresenter<PlayerProfileData>
{
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _levelText;
    [SerializeField] private Image _avatarImage;
    
    protected override void OnSetData()
    {
        // Called when data is set - use Data property
        _nameText.text = Data.PlayerName;
        _levelText.text = $"Level {Data.Level}";
        _avatarImage.sprite = Data.Avatar;
    }
}

// Open with data
var profileData = new PlayerProfileData 
{ 
    PlayerName = "Hero", 
    Level = 42,
    Avatar = avatarSprite
};

await _uiService.OpenUiAsync<PlayerProfilePresenter, PlayerProfileData>(profileData);
```

---

## Next Steps

- [Core Concepts](core-concepts.md) - Learn about layers, sets, and features
- [API Reference](api-reference.md) - Complete API documentation
- [Advanced Topics](advanced.md) - Analytics, performance optimization

---

## Sample Projects

The package includes sample implementations in the `Samples~` folder:

1. Open Unity Package Manager (`Window` → `Package Manager`)
2. Select "UI Service" package
3. Navigate to the "Samples" tab
4. Click "Import" next to the sample you want

Available samples:
- **BasicUiFlow** - Basic presenter lifecycle
- **DataPresenter** - Data-driven UI
- **DelayedPresenter** - Time and animation delays
- **UiToolkit** - UI Toolkit integration
- **Analytics** - Performance tracking

