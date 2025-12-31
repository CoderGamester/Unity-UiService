# GameLovers UI Service

[![Unity Version](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/version-1.0.0-green.svg)](CHANGELOG.md)

> **Quick Links**: [Installation](#installation) | [Quick Start](#quick-start) | [Documentation](docs/README.md) | [Examples](#examples) | [Troubleshooting](docs/troubleshooting.md)

<!-- TODO: Add a demo GIF or video showing the UiService in action -->
<!-- Recommended content:
     - Opening/closing UI presenters with animations
     - Layer management demonstration
     - Editor windows (Analytics Window, Hierarchy Window)
     - UI Sets batch operations
-->

![UiService Demo](docs/images/demo.gif)

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
- **📊 Analytics & Performance Tracking** - Optional analytics system with dependency injection
- **🛠️ Editor Tools** - Powerful editor windows for debugging and monitoring
- **📱 Responsive Design** - Built-in support for device safe areas (e.g. iPhone dynamic island)

---

## System Requirements

- **[Unity](https://unity.com/download)** (v6.0+) - To run the package
- **[Unity Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest)** (v2.6.0+) - For async asset loading
- **[UniTask](https://github.com/Cysharp/UniTask)** (v2.5.10+) - For efficient async operations

Dependencies are automatically resolved when installing via Unity Package Manager.

### Compatibility Matrix

| Unity Version | Status | Notes |
|---------------|--------|-------|
| 6000.3.x (Unity 6) | ✅ Fully Tested | Primary development target |
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
| [Troubleshooting](docs/troubleshooting.md) | Common issues and solutions |

## Package Structure

```
Runtime/
├── Loaders/
│   ├── IUiAssetLoader.cs          # Asset loading interface
│   ├── AddressablesUiAssetLoader.cs # Addressables implementation
│   ├── PrefabRegistryUiAssetLoader.cs # Direct prefab references
│   └── ResourcesUiAssetLoader.cs # Resources.Load implementation
├── IUiService.cs          # Public API interface
├── UiService.cs           # Core implementation
├── UiPresenter.cs         # Base presenter classes
├── UiConfigs.cs           # Configuration ScriptableObject
├── Features/              # Composable features
│   ├── TimeDelayFeature.cs
│   ├── AnimationDelayFeature.cs
│   └── UiToolkitPresenterFeature.cs
└── Views/                 # Helper components

Editor/
├── UiConfigsEditor.cs     # Enhanced inspector
├── UiAnalyticsWindow.cs   # Performance monitoring
└── UiServiceHierarchyWindow.cs  # Live debugging
```

### Key Files

| Component | Responsibility |
|-----------|----------------|
| **IUiService** | Public API surface for all UI operations |
| **UiService** | Core implementation managing lifecycle, layers, and state |
| **UiPresenter** | Base class for all UI views with lifecycle hooks |
| **UiConfigs** | ScriptableObject storing UI configuration and sets |
| **PrefabRegistryConfig** | Map address keys to UI Prefabs for direct reference |
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
2. Select "UI Service" package
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
| **Analytics** | Performance tracking integration |

---

## Contributing

We welcome contributions! Here's how you can help:

### Reporting Issues

- Use the [GitHub Issues](https://github.com/CoderGamester/com.gamelovers.uiservice/issues) page
- Include Unity version, package version, and reproduction steps
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
- Update CHANGELOG.md for notable changes

---

## Support

- **Issues**: [Report bugs or request features](https://github.com/CoderGamester/com.gamelovers.uiservice/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/CoderGamester/com.gamelovers.uiservice/discussions)
- **Changelog**: See [CHANGELOG.md](CHANGELOG.md) for version history

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

---

**Made with ❤️ for the Unity community**

*If this package helps your project, please consider giving it a ⭐ on GitHub!*
