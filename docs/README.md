# UI Service Documentation

Welcome to the GameLovers UI Service documentation. This guide covers everything you need to know to effectively use the UI Service in your Unity projects.

## Quick Navigation

| Document | Description |
|----------|-------------|
| [Getting Started](getting-started.md) | Installation, setup, and your first UI presenter |
| [Core Concepts](core-concepts.md) | Presenters, layers, sets, features, and configuration |
| [API Reference](api-reference.md) | Complete API documentation with examples |
| [Advanced Topics](advanced.md) | Analytics, performance optimization, helper views |
| [Troubleshooting](troubleshooting.md) | Common issues and solutions |

## Overview

The UI Service provides a centralized system for managing UI in Unity games. Key capabilities include:

- **Lifecycle Management** - Load, open, close, and unload UI presenters
- **Layer Organization** - Depth-sorted UI with configurable layers
- **UI Sets** - Batch operations on grouped UI elements
- **Async Loading** - UniTask-powered asynchronous asset loading via Addressables
- **Feature Composition** - Extend presenter behavior with modular features
- **Analytics** - Optional performance tracking and metrics

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                      Game Code                          │
│                 (GameManager, Systems)                  │
└─────────────────────┬───────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│                    IUiService                           │
│         (Public API for all UI operations)              │
└─────────────────────┬───────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
┌───────────┐  ┌────────────┐  ┌──────────────┐
│ UiConfigs │  │ UiPresenter│  │ UiAssetLoader│
│   (SO)    │  │  (Views)   │  │(Addressables)│
└───────────┘  └─────┬──────┘  └──────────────┘
                     │
                     ▼
              ┌──────────────┐
              │   Features   │
              │ (Composable) │
              └──────────────┘
```

## Package Structure

```
Runtime/
├── IUiService.cs          # Public API interface
├── UiService.cs           # Core implementation
├── UiPresenter.cs         # Base presenter classes
├── UiConfigs.cs           # Configuration ScriptableObject
├── UiAssetLoader.cs       # Addressables integration
├── UiInstanceId.cs        # Multi-instance support
├── UiAnalytics.cs         # Performance tracking
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
├── UiAnalyticsWindow.cs   # Performance monitoring
└── UiServiceHierarchyWindow.cs  # Live debugging
```

## Version History

See [CHANGELOG.md](../CHANGELOG.md) for version history and release notes.

## Contributing

See the main [README.md](../README.md) for contribution guidelines.

