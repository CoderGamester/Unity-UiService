# UI Service Documentation

Welcome to the GameLovers UI Service documentation. This guide covers everything you need to know to effectively use the UI Service in your Unity projects.

## Quick Navigation

| Document | Description |
|----------|-------------|
| [Getting Started](getting-started.md) | Installation, setup, and your first UI presenter |
| [Core Concepts](core-concepts.md) | Presenters, layers, sets, features, and configuration |
| [API Reference](api-reference.md) | Complete API documentation with examples |
| [Advanced Topics](advanced.md) | Performance optimization, helper views |
| [Troubleshooting](troubleshooting.md) | Common issues and solutions |

## Overview

The UI Service provides a centralized system for managing UI in Unity games. Key capabilities include:

- **Lifecycle Management** - Load, open, close, and unload UI presenters
- **Layer Organization** - Depth-sorted UI with configurable layers
- **UI Sets** - Batch operations on grouped UI elements
- **Async Loading** - Customizable asset loading strategies (Addressables, Resources, Prefab Registry)
- **Feature Composition** - Extend presenter behavior with modular features

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                      Game Code                          │
│                 (GameManager, Systems)                  │
└─────────────────────┬───────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│              IUiServiceInit : IUiService                │
│    ┌────────────────────────────────────────────────┐   │
│    │ IUiService (consume: open/close/load/unload)   │   │
│    └────────────────────────────────────────────────┘   │
│    + Init(UiConfigs) + Dispose()                        │
└─────────────────────┬───────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
┌───────────┐  ┌────────────┐  ┌─────────────────┐
│ UiConfigs │  │ UiPresenter│  │ IUiAssetLoader  │
│   (SO)    │  │  (Views)   │  │ (Abstraction)   │
└───────────┘  └─────┬──────┘  └─────────────────┘
                     │
                     ▼
              ┌──────────────┐
              │   Features   │
              │ (Composable) │
              └──────────────┘
```

### Service Interfaces

The UI Service exposes **two interfaces**:

| Interface | Purpose | Key Capability |
|-----------|---------|----------------|
| `IUiService` | **Consuming** - open/close/query UI | All UI lifecycle operations |
| `IUiServiceInit` | **Initializing** - extends `IUiService` | `Init(UiConfigs)` + `Dispose()` |

> **⚠️ Important:** Use `IUiServiceInit` when you need to call `Init()`. The `Init()` method is **not** available on `IUiService`. See [Core Concepts - Service Interfaces](core-concepts.md#service-interfaces) for details.

## Package Structure

```
Runtime/
├── IUiService.cs          # Public API interface
├── UiService.cs           # Core implementation
├── UiPresenter.cs         # Base presenter classes
├── UiConfigs.cs           # Configuration ScriptableObject
├── Loaders/
│   ├── IUiAssetLoader.cs      # Asset loading interface
│   ├── AddressablesUiAssetLoader.cs # Addressables implementation
│   ├── PrefabRegistryUiAssetLoader.cs # Direct prefab references
│   └── ResourcesUiAssetLoader.cs # Resources.Load implementation
├── UiInstanceId.cs        # Multi-instance support
├── Features/              # Composable features
│   ├── TimeDelayFeature.cs
│   ├── AnimationDelayFeature.cs
│   └── UiToolkitPresenterFeature.cs
└── Views/                 # Helper components
    ├── SafeAreaHelperView.cs
    ├── NonDrawingView.cs
    └── AdjustScreenSizeFitterView.cs

Editor/
├── UiConfigsEditor.cs     # Enhanced inspector
└── UiPresenterManagerWindow.cs  # Live debugging and management
```

## Version History

See [CHANGELOG.md](../CHANGELOG.md) for version history and release notes.

## Contributing

See the main [README.md](../README.md) for contribution guidelines.

