# GameLovers UI Service

[![Unity Version](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/version-1.2.0-green.svg)](CHANGELOG.md)

> **Quick Links**: [Installation](#installation) | [Quick Start](#quick-start) | [Documentation](docs/README.md) | [Examples](#examples) | [Troubleshooting](docs/troubleshooting.md)

![UiService Demo](docs/demo.gif)

## Why Use This Package?

Managing UI in Unity games often becomes a tangled mess of direct references, scattered open/close logic, and manual lifecycle management. This **UI Service** solves these pain points:

| Problem | Solution |
|---------|----------|
| **Scattered UI logic** | Centralized service manages all UI lifecycle (load → open → close → unload) |
| **Memory management headaches** | Addressables integration with automatic asset loading/unloading |
| **Rigid UI hierarchies** | Layer-based organization with flexible depth sorting |
| **Duplicated boilerplate** | Feature composition system extends behavior without inheritance complexity |
| **Async loading complexity** | UniTask-powered async operations with cancellation support |
| **No visibility into UI state** | Editor windows for real-time analytics, hierarchy debugging, and configuration |
| **Difficult testing** | Injectable interfaces (`IUiService`, `IUiAssetLoader`) and built-in loaders enable easy mocking |

**Built for production:** Used in real games with WebGL, mobile, and desktop support. Zero per-frame allocations in hot paths.

### Key Features

- **🎭 UI Model-View-Presenter Pattern** - Clean separation of UI logic with lifecycle management
- **🎨 UI Toolkit Support** - Compatible with both uGUI and UI Toolkit
- **🧩 Feature Composition** - Modular feature system for extending presenter behavior
- **🔄 Async Loading** - Load UI assets asynchronously with UniTask support
- **📦 UI Group Organization** - Organize UI elements by depth layers and in groups for batch operations
- **💾 Memory Management** - Efficient loading/unloading of UI assets with Unity's Addressables system
- **🛠️ Editor Tools** - Presenter Manager window for real-time debugging and monitoring
- **📱 Responsive Design** - Built-in support for device safe areas (e.g. iPhone dynamic island)

---

## System Requirements

- **[Unity](https://unity.com/download)** (v6.0+) - To run the package
- **[Unity Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest)** (v2.6.0+) - For async asset loading
- **[UniTask](https://github.com/Cysharp/UniTask)** (v2.5.10+) - For efficient async operations
- **[Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)** (v17.0.1+) - Required by the camera-stacking and backdrop-blur presenter features (`Runtime/Rendering/`)

Dependencies are automatically resolved when installing via Unity Package Manager.

> **Using the backdrop blur needs one manual step**: add `UiBackdropBlurFeature` to your Universal **Renderer** asset (**Add Renderer Feature**), not just the presenter component. Without it the blur silently does nothing. See [URP Rendering Features](docs/urp-rendering.md).

### Compatibility Matrix

| Unity Version | Status | Notes |
|---------------|--------|-------|
| 6000.5.x (Unity 6) | ✅ Fully Tested | Primary development target |
| 6000.3.x (Unity 6) | ✅ Fully Tested | |
| 6000.0.x (Unity 6) | ✅ Fully Tested | Fully supported |
| 2022.3 LTS | ⚠️ Untested | May require minor adaptations |

| Platform | Status | Notes |
|----------|--------|-------|
| Standalone (Windows/Mac/Linux) | ✅ Supported | Full feature support |
| WebGL | ✅ Supported | Requires UniTask (no Task.Delay) |
| Mobile (iOS/Android) | ✅ Supported | Full feature support |
| Console | ⚠️ Untested | Should work with Addressables setup |

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

## Documentation

| Document | Description |
|----------|-------------|
| [Getting Started](docs/getting-started.md) | Installation, setup, and first presenter |
| [Core Concepts](docs/core-concepts.md) | Presenters, layers, sets, features |
| [API Reference](docs/api-reference.md) | Complete API documentation |
| [Advanced Topics](docs/advanced.md) | Analytics, performance, helper views |
| [URP Rendering Features](docs/urp-rendering.md) | Camera stacking, backdrop blur, render-texture authoring, layering hazards |
| [Troubleshooting](docs/troubleshooting.md) | Common issues and solutions |

## Key Components

| Component | Responsibility |
|-----------|----------------|
| **IUiService** | Public API surface for all UI operations |
| **UiService** | Core implementation managing lifecycle, layers, and state |
| **UiPresenter** | Base class for all UI views with lifecycle hooks |
| **UiConfigs** | ScriptableObject storing UI configuration and sets |
| **IUiAssetLoader** | Interface for custom asset loading strategies |
| **AddressablesUiAssetLoader** | Handles Addressables integration for async loading |
| **PrefabRegistryUiAssetLoader** | Simple loader for direct prefab references |
| **ResourcesUiAssetLoader** | Loads UI from Unity's Resources folder |
| **PresenterFeatureBase** | Base class for composable presenter behaviors |
| **UiInstanceId** | Enables multiple instances of the same presenter type |

---

## Quick Start

### 1. Create UI Configuration

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
        _uiService = new UiService();
        _uiService.Init(_uiConfigs);
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
    
    protected override void OnInitialized()
    {
        _playButton.onClick.AddListener(OnPlayClicked);
    }
    
    protected override void OnOpened()
    {
        Debug.Log("Main menu opened!");
    }
    
    protected override void OnClosed()
    {
        Debug.Log("Main menu closed!");
    }
    
    private void OnPlayClicked()
    {
        Close(destroy: false);
    }
}
```

### 4. Open and Manage UI

```csharp
// Open UI
var mainMenu = await _uiService.OpenUiAsync<MainMenuPresenter>();

// Check visibility
if (_uiService.IsVisible<MainMenuPresenter>())
{
    Debug.Log("Main menu is visible");
}

// Close UI
_uiService.CloseUi<MainMenuPresenter>();
```

📖 **For complete setup guide, see [Getting Started](docs/getting-started.md)**

---

## Examples

The package includes sample implementations in the `Samples~` folder.

### Importing Samples

1. Open Unity Package Manager (`Window` → `Package Manager`)
2. Select "GameLovers UiService" package
3. Navigate to the "Samples" tab
4. Click "Import" next to the sample you want

### Available Samples

| Sample | Description |
|--------|-------------|
| **BasicUiFlow** | Basic presenter lifecycle and button interactions |
| **DataPresenter** | Data-driven UI with `UiPresenter<T>` |
| **DelayedPresenter** | Time and animation delay features |
| **UiToolkit** | UI Toolkit (UI Elements) integration |
| **DelayedUiToolkit** | Multiple features combined |
| **UiSets** | Group multiple UIs for batch operations (e.g., game HUD) |
| **MultiInstance** | Create multiple instances of the same presenter type |
| **CustomFeatures** | Create custom presenter features (fade, scale, sound) |
| **AssetLoadingStrategies** | Compare PrefabRegistry, Addressables, and Resources loading |

---

## Contributing

Contributions are welcome! Report bugs or request features via [GitHub Issues](https://github.com/CoderGamester/com.gamelovers.uiservice/issues). For development setup, architecture, assembly conventions, and coding standards, see [AGENTS.md](AGENTS.md).

---

## Related docs

| Document | Purpose |
|---|---|
| [docs/README.md](docs/README.md) | Full documentation (getting started, concepts, API, advanced) |
| [AGENTS.md](AGENTS.md) | Contributor/agent guide (architecture, gotchas, workflows) |
| [CHANGELOG.md](CHANGELOG.md) | Version history |

## Support

- **Issues**: [Report bugs or request features](https://github.com/CoderGamester/com.gamelovers.uiservice/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/CoderGamester/com.gamelovers.uiservice/discussions)
- **Changelog**: See [CHANGELOG.md](CHANGELOG.md) for version history

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

---

**Made with ❤️ for the Unity community**

*If this package helps your project, please consider giving it a ⭐ on GitHub!*
