# GameLovers UI Service

URP-only Unity 6 UI orchestration built around presenter lifecycles, UI sets, composable presenter features, and PrefabRegistry, Resources, or Addressables loading.

[![Unity](https://img.shields.io/badge/Unity-6000.0%20%7C%206000.3%20%7C%206000.5-blue.svg)](https://unity.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
[![Version](https://img.shields.io/github/v/tag/CoderGamester/Unity-UiService?label=version)](CHANGELOG.md)

## When to use it

Use UI Service when presenters need a consistent load, open, close, and unload lifecycle. It
supports uGUI and UI Toolkit surfaces in screen or world space. It is **URP-only** because its
rendering features use URP camera and renderer APIs; do not install it in a BiRP or HDRP project
expecting those assemblies to compile.

## Unity compatibility

| Item | Current policy |
| --- | --- |
| Minimum Unity version | `6000.0` |
| Reference streams | `6000.0.x`, `6000.3.x`, `6000.5.x` |
| Reference editors | `6000.0.81f1`, `6000.3.21f1`, `6000.5.7f1` (primary) |
| Render pipeline | Universal Render Pipeline only |
| Validation status | Compatibility target; fresh clean-host and visible-rendering evidence is required for validation. |

## Install

Install UniTask directly when using Git; Unity resolves Addressables, URP, and the test framework from registry dependencies.

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.10",
    "com.gamelovers.uiservice": "https://github.com/CoderGamester/Unity-UiService.git#1.3.0"
  }
}
```

## First success: PrefabRegistry

Create a `PrefabRegistryUiConfigs` asset from `Create/GameLovers UiService/UiConfigs/PrefabRegistry`, add your presenter prefab, then initialize and dispose one service owner:

```csharp
using GameLovers.UiService;
using UnityEngine;

public sealed class UiBootstrap : MonoBehaviour
{
    [SerializeField] private PrefabRegistryUiConfigs configs;
    private IUiServiceInit service;

    private void Awake()
    {
        service = new UiService();
        service.Init(configs);
    }

    private void OnDestroy() => service?.Dispose();
}
```

Use Addressables or Resources configs only when those systems own your prefab addresses or paths. Keep loader/config pairs matched, and release Addressables resources according to the acquisition operation.

## Core concepts

| Concept | Meaning |
| --- | --- |
| `UiPresenter` / `UiPresenter<T>` | Presenter lifecycle and optional typed data |
| `IUiService` | Load, open, close, unload, and query UI |
| `UiConfigs` | Presenter definitions, loaders, layers, and UI sets |
| UI sets | Batch operations over a configured group of presenters |
| `PresenterFeatureBase` | Composable behavior such as delays and UI Toolkit integration |
| `UiInstanceId` | Multiple instances, where supported by the concrete `UiService` API |

Backdrop blur requires `UiBackdropBlurRendererFeature` on the active URP Renderer asset. A missing renderer feature logs a setup error; it is not a no-op configuration. Screen Space Overlay and Screen Space Camera have different layer-ordering behavior, so test the target render mode.

## Samples

| Sample | Focus |
| --- | --- |
| Basic UI Flow | Presenter lifecycle |
| Data Presenter | `UiPresenter<T>` data updates |
| Delayed Presenter | Time and animation delays |
| UI Toolkit | UI Toolkit presenter feature |
| Delayed UI Toolkit | Combined UI Toolkit and delays |
| UI Sets | Group lifecycle |
| Multi-Instance | Multiple presenter instances |
| Custom Features | Custom presenter feature composition |
| Asset Loading Strategies | PrefabRegistry, Resources, and Addressables |
| URP Rendering | Camera stacking, backdrop blur, and layering hazards |

Every UI Service sample requires URP. Import samples through Package Manager and follow the README beside the selected sample.

## Documentation and support

Read [docs](docs/README.md), [URP rendering guidance](docs/urp-rendering.md),
[troubleshooting](docs/troubleshooting.md), and
[CHANGELOG.md](CHANGELOG.md). Report issues at
[Unity-UiService](https://github.com/CoderGamester/Unity-UiService/issues).
