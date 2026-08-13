# GameLovers UiService Tests — Agent Guide

This guide adds test-only rules to the package and host guides.

## Shared test rules

<!-- BEGIN SHARED TEST RULES -->
### Admission

A new test is admitted only when all six answers are yes:

| Check | Requirement |
|---|---|
| **A1 Defect** | Name the production file/symbol and the incorrect behavior in one sentence. “It could break” is not a defect. |
| **A2 Red** | Name a plausible production edit that should make the assertion fail. Prefer one line/branch; shared-path integration mutations are allowed when isolation is dishonest. |
| **A3 Package-owned** | Every assertion must read behavior this package computes, not C# defaults, fresh-object non-nullness, or Unity guarantees. |
| **A4 Cheapest** | Use the cheapest honest tier: a test case before a new test, EditMode before PlayMode, and an existing fixture before a new fixture. |
| **A5 Unique** | Grep the symbol, derived/wrapper types, and paired setup fields. Do not add a test already reddened by the same narrow defect. |
| **A6 Environment** | Control ambient renderer, Addressables, sample, static, and project state, or branch the expectation on the state actually observed. |

Two additional rejects apply:

- **D1 Tautology:** a lone `DoesNotThrow`, freshly-created non-null assertion, language default, or input-derived substring match pins no package behavior unless used by a named harness sentinel.
- **D2 Name/body mismatch:** deleting or bypassing the behavior promised by the test name must not leave the test green. Strengthen the assertion or rename the test to its actual claim.

Fixtures under `Smoke/` are exempt from A1/A2 and may assert construction/bootstrap viability. The exemption is directory-scoped, not permission to use smoke assertions in Unit or Integration fixtures.

### Revert and Confirm Red (RCR)

Every new or strengthened behavioral test must be observed failing once against a plausible production mutation before commit:

1. Run the new test against normal production code and observe GREEN.
2. Preserve the exact working patch or use an isolated worktree; then apply the A2 mutation. Never restore a dirty file from `HEAD`.
3. Run the smallest attributable filter. RED must come from the intended assertion with a diagnostic failure, not a compile error or unrelated `NullReferenceException`.
4. Restore the saved production state, confirm the mutation is gone without losing other edits, and observe GREEN again.
5. Record the observation on the test using `file + symbol`, never a line number.

Use this compact form, targeting four lines and never exceeding six:

```csharp
[Test]
// ADMIT: <owned defect naming production file and symbol>
// RCR: <file> <symbol> — <mutation> → RED (<assertion failure>). <YYYY-MM-DD>
public void Method_Condition_ExpectedResult()
```

Do not narrate investigation history in the test. A nearby mutation that looked valid but stayed green may be recorded when that negative result prevents repeated work.

When a test resists an isolated mutation, classify it before acting:

| Verdict | Meaning | Action |
|---|---|---|
| **A3 reject** | No package production behavior participates. | Delete the test. |
| **A5 duplicate** | The same narrow mutation already belongs to a sibling. | Delete it and name the surviving sibling in review/commit context. |
| **D2 overclaim** | The mutation implied by the name leaves the body green. | Strengthen or rename. |
| **UNFALSIFIABLE** | Real package behavior is double-guarded or cannot be broken by a safe isolated edit. | Keep only after attempted mutations are recorded with the specific reason. |
| **SHARED-PATH** | A broader mutation reddens this legitimate integration path together with siblings. | Keep, recording the observed mutation and blast radius. |

Unannotated tests have three possible histories: observed RED with lost write-back, collateral RED under another test's mutation, or never probed. Check `.test-all/rcr/` before probing and never write prepared annotation text without matching observed evidence. Mutation records stay under `.test-all/rcr/`, not `/tmp`.

Benchmarks use the inverted check: removing the workload from the measured body must materially change the result. Run the actual test assembly; a plain Unity open does not compile assemblies constrained by `UNITY_INCLUDE_TESTS`.
<!-- END SHARED TEST RULES -->

## Assembly placement

- `Tests/EditMode/`: Editor-only `GameLovers.UiService.Tests` for configs, loaders, sets, and core logic not requiring a running player.
- `Tests/PlayMode/`: runtime-compatible `GameLovers.UiService.Tests.PlayMode` for presenter lifecycle, Unity objects, rendering, integration, performance, and smoke.
- `Tests/Helpers/`: runtime-compatible non-test helper assembly shared by both suites and gated by `UNITY_INCLUDE_TESTS`.
- MonoBehaviour presenters used by `AddComponent` or prefab tests belong in `Tests/Helpers/` or `PlayMode/Helpers/`, never the Editor-only EditMode assembly.

## Package conventions

- Fixtures use plural `{Subject}Tests`. EditMode/shared helpers use `GameLovers.UiService.Tests`; PlayMode uses its `.PlayMode` child namespace.
- Use hand-written fakes. The current test asmdefs do not reference NSubstitute.
- Assert through public/internal observable behavior; no private-reflection sites are authorized.
- Both NUnit classic and constraint assertions are accepted when they express intent clearly.
- Rendering expectations branch on observed renderer-feature/project state or establish and restore the required state explicitly.

## Cleanup and performance

- Dispose `UiService`, clean tracked loader instances, unsubscribe static resolution/orientation listeners, and destroy created cameras, canvases, presenters, `PanelSettings`, and `ThemeStyleSheet` objects.
- Restore camera tags/enabled state and any renderer/static overrides exactly to their pre-test values.
- `Measure.Method` repeats its body. Load benchmarks clean up with unload; unload benchmarks set up with load. Reset all state per iteration so later iterations do not measure cached no-ops.

## Verification

- Core data/config behavior may run in EditMode. Presenter lifecycle, UI Toolkit attachment, camera stacking, backdrop blur, and resource ownership run in PlayMode.
- Rendering and renderer-feature work must pass both batchmode and Editor PlayMode and include inspected visual evidence when pixels are the claimed result.
- Update this guide only when a stable asmdef, placement, cleanup, fake, or performance convention changes.
