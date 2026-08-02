# GameLovers.UiService Tests — AI Agent Guide

This file contains testing conventions for the `com.gamelovers.uiservice` package. It is the source of truth when reading, editing, or creating test files under `Tests/`.

For runtime architecture, gotchas, and package-level context, see the parent [`AGENTS.md`](../AGENTS.md).

§1 and §2 are shared verbatim across every GameLovers package. A change to either must be applied to all six `Tests/AGENTS.md` files in the same working session, one commit per submodule.

## 1. ADMIT — Test Admission Test

A proposed test is admitted only if all five answers are YES. Record the first two
as comments on the test itself.

| | Question |
|---|---|
| **A1 DEFECT** | Can you name the defect in one sentence, referencing a production file and symbol? "It could break" is not a defect. |
| **A2 RED** | Can you name the exact production edit — one line or one branch, identified by `file` + `symbol` — that makes this test fail? If no such single edit exists, the test pins nothing. |
| **A3 PACKAGE** | Does every assertion read a value this package computed? Reject assertions on `new X() != new X()`, `!= null` on a freshly constructed object, default struct/enum values, or anything the C# spec or the Unity engine already guarantees. |
| **A4 CHEAPEST** | Is this the cheapest tier that covers the defect? EditMode beats PlayMode; a `[TestCase]` row on an existing fixture beats a new `[Test]`; a new `[Test]` beats a new fixture. Grep before writing. |
| **A5 UNIQUE** | Does no existing test already fail on the A2 edit? Grep the symbol under test across `Tests/` first. |

**A5-bis — inherited-type coverage.** Before proposing a fixture for a type that
derives from or wraps another tested type, grep `Tests/` for the derived type's
name and for paired `[SetUp]` fields. Base-and-derived pairs are tested jointly in
the base's fixture unless the derived type adds new public surface.

**Two mechanical disqualifiers** — violate one and the test is rejected:

- **D1 — tautology.** If the only assertion is `Assert.DoesNotThrow`,
  `Assert.IsNotNull`, or a disjunction of `Contains(...)` substrings, the test
  fails A2 unless you write down what *would* throw, be null, or not match. A
  substring disjunction that includes a string the input itself embeds is
  unfalsifiable by construction.
- **D2 — name/body contract.** The test name is a claim. If deleting the
  production feature the name mentions leaves the test green, the name is a lie.

**Smoke exemption, by directory.** Fixtures under `Smoke/` are exempt from A1 and
A2 and may assert construction-without-throwing only. Their defect class is "the
assembly no longer loads / bootstrap regressed", which is real and not expressible
otherwise. The exemption is by directory, not by assertion shape — a Unit test
that only asserts `IsNotNull` is still rejected.

## 2. RCR — Revert and Confirm Red

> Every new or strengthened test must be observed failing, once, against a
> one-line production revert, before it is committed.

Line coverage proves a line executed. It does not prove any test would notice if
that line were wrong. RCR is the cheap substitute for mutation testing, and it is
what makes a coverage number trustworthy.

**Procedure** (~90 seconds per test):

1. Write the test. Run it. Green.
2. Apply the A2 edit — invert the comparison, delete the guard clause, return
   early, comment out the one line. **One line only**: a broad deletion proves
   nothing, because it would also "fail" a tautological test via a compile error.
3. Run only that test. It must be **RED**, and the failure message must name the
   thing you broke. A red-by-`NullReferenceException` does not count — that is the
   test crashing, not asserting.
4. `git checkout -- <production file>`. Re-run. Green.
5. Record the mutation in the test's header comment.

**Recording format** — on the test, not in a separate ledger. A ledger rots the
moment a test is renamed; a comment travels with the test, appears in every diff
that touches it, and lets a reviewer re-run the mutation in 30 seconds.

```csharp
[Test]
// ADMIT: <one-sentence defect, naming a production file and symbol>
// RCR:   <file> <symbol> — <the one-line mutation> → RED (<what the failure says>). <YYYY-MM-DD>
public void Method_Condition_ExpectedResult()
```

**Anchor on `file` + `symbol`, never `file:line`.** Line numbers rot on the first
unrelated edit above them — a stale `:474` pointing at a method that moved to `:464`
sends the next reader to the wrong code and quietly destroys the comment's value.

**Budget: four lines is the target, six is the ceiling.** One sentence of ADMIT,
one of RCR, wrapped. This obeys the repo-wide rule in the root `AGENTS.md`
(§ Code comments): *"One sentence usually suffices. Multi-paragraph rationale is a
smell."* Anything past the ceiling belongs in the commit body or `docs/`, not on the
test. Two things in particular must NOT appear here:
- **Change narration.** *"An earlier version of this test was a tautology"* is diff
  context; the root `AGENTS.md` forbids it outright. A comment states the code's
  permanent condition, not its history. Put it in the commit message.
- **Investigation transcript.** The empirical detail that convinced *you* is not
  what the next reader needs. They need the mutation and the expected failure.

The one extension worth its lines is a **negative** result: naming a nearby edit
that looks like a valid mutation but is NOT one (because it is already guarded, or
because it reddens a sibling test instead). That stops the next reader repeating a
dead end, and it cannot be recovered from the code.

Also add one line per new test to the commit body: `RCR: <TestName> ← <file> <symbol> <mutation>`.
That makes `git log --grep=RCR` the audit surface.

**UNFALSIFIABLE — the one honest exemption.** Some correct tests provably have no
one-line mutation. The commonest case is **double-guarded validation**: an
unconfigured object trips two independent guards, so disabling either leaves the
other throwing. Deleting such a test would lose real coverage, so it is exempt —
but only on the same terms as §13, never as a shrug:

```csharp
// RCR: none exists — <input> trips both <guard A> and <guard B>; disabling either
// leaves the other throwing (verified). Double-covered, not single-line falsifiable.
```

The reason must be falsifiable and must record that a mutation was actually tried
and observed green. "Couldn't find one" is not a reason — that is an unfinished RCR,
not an exemption.

**Verdicts for a test that resists mutation.** Work out which of four it is; they
have different answers:

| Finding | Test | Action |
|---|---|---|
| **A3 reject** — no line in `Runtime/` or `Editor/` participates; the assertion is C#- or Unity-guaranteed | pins nothing, ever | **Delete** |
| **A5 duplicate** — the only mutation that reddens it already belongs to a sibling | pins nothing new | **Delete**, naming the surviving sibling in the commit body |
| **D2 overclaim** — the name promises behaviour the body cannot detect | name is a lie | **Strengthen the assertion**, or rename to what it actually checks |
| **UNFALSIFIABLE** — real behaviour, but double-guarded or otherwise unbreakable one line at a time | valid | **Keep**, with the exemption comment above |

**A3 is checked first, and it is the commonest way the exemption gets abused.**
UNFALSIFIABLE is for behaviour this package genuinely owns but cannot be broken one
line at a time. It is *never* for behaviour the package does not own. The tell is in
the reason itself: if you find yourself writing "no line in Runtime/ participates",
"the only edit is a compile error", or "these are C#'s zero-init values", you have
found an A3 reject and the verdict is **delete** — a field-only struct's assignment
and default values are the language's guarantees, not yours. Writing that sentence
under an UNFALSIFIABLE heading launders a test §1 would never have admitted.

Prove the class before acting. An A5 duplicate is confirmed when the sibling's
mutation is observed reddening both; a D2 overclaim is confirmed when the mutation
the name implies leaves the test green; an A3 reject is confirmed when no production
symbol appears anywhere in the causal chain behind the assertion.

**Two consequences, stated so RCR does not become theatre:**

- A test with no `// RCR:` line — and no UNFALSIFIABLE exemption — is not trusted
  coverage. In an audit it is a suspect by default.
- **Benchmarks are included, inverted:** a performance test must be observed
  *changing its number* when the measured operation is removed from the measured
  body. A benchmark whose measured region does not contain the workload is a
  tautology in `Measure` clothing.

## 3. Placement Rules

Three test asmdefs, platform-scoped:

- **`Tests/EditMode/`** — owned by `GameLovers.UiService.Tests.asmdef`, **editor-only** (`includePlatforms: ["Editor"]`). Unit tests for configs, sets, loaders, and core service behavior that do not need a running player. FLAT layout plus `Loaders/` (asset-loader-specific fixtures, including `Loaders/Resources/`) and `Helpers/` (EditMode-only test doubles).
- **`Tests/PlayMode/`** — owned by `GameLovers.UiService.Tests.PlayMode.asmdef`, runtime-compatible (no `includePlatforms` restriction). Subfolders: `Unit/`, `Integration/`, `Performance/`, `Smoke/`, `Helpers/`.
- **`Tests/Helpers/`** — owned by `GameLovers.UiService.Tests.Helpers.asmdef`. This is a **shared, non-test assembly**: it has no NUnit / TestRunner reference, is runtime-compatible, and is gated by `defineConstraints: ["UNITY_INCLUDE_TESTS"]` so it never ships in a player build that doesn't include tests. It exists purely so EditMode and PlayMode can share fixture types without a circular asmdef reference.

**MonoBehaviour-derived test presenters MUST live in `Tests/Helpers/` or `Tests/PlayMode/Helpers/`, never `Tests/EditMode/`.** `GameLovers.UiService.Tests.asmdef` is editor-only; placing a MonoBehaviour there makes Unity reject `AddComponent<T>()` calls (silent `null` return + warning: `Can't add script behaviour '<name>' because it is an editor script`), which causes tests that create prefabs via `TestHelpers.CreateTestPresenterPrefab<T>` to run without ever attaching the presenter component. Examples: `TestUiPresenter`, `TestDataUiPresenter`, `TestUiToolkitPresenter`.

**Decision tree**: if the fixture needs `DontDestroyOnLoad`, a real prefab instantiation, or a running player → **PlayMode**; otherwise → **EditMode**. Fixtures under `PlayMode/Smoke/` get the ADMIT exemption in §1.

## 4. Namespace and Suppression

All EditMode and shared-helper files use `namespace GameLovers.UiService.Tests`; PlayMode files use `namespace GameLovers.UiService.Tests.PlayMode`. No `// ReSharper disable` suppression comment convention is used in this package's tests (unlike `com.gamelovers.services`).

## 5. Naming

- **Test fixture**: `{Subject}Tests` — the suffix is **plural** (`UiConfigTests`, `UiCameraStackFeatureTests`, `InteractableTextViewTests`). This differs from every sibling package, which uses the singular `{Subject}Test`. Do not "fix" this to match siblings — it is this package's universal convention.
- **Test method**: `MethodOrBehavior_Condition_ExpectedResult` (e.g. `UiConfig_Creation_PreservesAllProperties`, `ConfigsSetter_RoundTrip_PreservesLoadSynchronously`).
- **`[SetUp]` method**: named `Setup()` in the large majority of fixtures; two fixtures use `SetUp()`. Either is acceptable — do not rename existing methods just to normalize casing.
- **`[TearDown]` method**: always named `TearDown()`. `[SetUp]`/`[TearDown]` pair 1:1 across every fixture that has one.

## 6. Mock / Helper Types

**Hand-written fakes only — zero NSubstitute.** `NSubstitute.dll` is currently a `precompiledReferences` entry in `GameLovers.UiService.Tests.asmdef` (EditMode), but no test in this package actually calls `Substitute.For<T>()`; a later wave removes the reference. Do not add a new NSubstitute-based mock — follow the existing hand-written-fake pattern instead.

Existing fakes, all hand-written:
- `MockAssetLoader` (EditMode and PlayMode each have their own copy) — implements `IUiAssetLoader` without NSubstitute specifically because the loader manages real Unity object lifecycle (instantiate/destroy), which a dynamic proxy cannot do transparently. This is documented directly in a comment in `MockAssetLoader.cs`.
- `Test*Presenter` family (`TestUiPresenter`, `TestDataUiPresenter`, `TestUiToolkitPresenter`, `TestCameraStackPresenter`, etc.) — concrete `UiPresenter` subclasses used as prefab-attached test doubles. See §3 for placement rules.
- `Tracking*Feature` family (`TrackingFeature`, `TrackingTimeDelayFeature`, `TrackingAnimationDelayFeature`) — `PresenterFeatureBase` subclasses that record lifecycle calls for assertion.
- `Mock*` types (`MockPresenterFeature`, `MockTransitionFeature`) — minimal feature stubs for isolating presenter/feature interaction.

## 7. Black-Box / Reflection Policy

No authorized reflection sites currently exist in this package's tests. `InteractableTextViewTests` explicitly avoids `BindingFlags` reflection on private members in favor of exercising the public API surface (see the comment at `InteractableTextViewTests.cs:36`). Prefer the same: assert through public API and observable state. If a future test genuinely needs private-state assertion because no observable readback path exists, follow the same authorization pattern as `com.gamelovers.services`' `Tests/AGENTS.md` §7 (single setter-storage assertion, documented here with the specific harness blocker named) rather than reflecting ad hoc.

## 8. Fields and Setup

- Fields are prefixed with `_` (e.g. `_mockLoader`, `_service`, `_overlayCamera`, `_canvas`).
- `[SetUp]` constructs fresh fakes/services; `[TearDown]` disposes/destroys them (see §10 for PlayMode-specific cleanup obligations).
- Arrange/Act/Assert comments (`// Arrange`, `// Act`, `// Assert`, or the combined `// Arrange & Act`) are the standard body layout across the majority of fixtures. Keep this shape when adding tests.

## 9. Assertion Style

Both the classic model (`Assert.AreEqual`, `Assert.IsTrue`, `Assert.Throws<T>`, etc.) and the constraint model (`Assert.That(...)`) are used, and **both are acceptable here** — classic dominates numerically, but `Assert.That` is not restricted to a narrow exception list the way it is in `com.gamelovers.services`. Pick whichever reads more clearly for the assertion at hand; do not treat one as the "correct" default for this package.

## 10. PlayMode Test Cleanup

PlayMode fixtures that load presenters through `UiService`, create cameras/canvases for `UiCameraStackFeature`, or instantiate `PanelSettings`/`UIDocument` must tear down what they created:

- Dispose any `UiService` instance created in `[SetUp]` (`_service?.Dispose()`), which unloads presenters and destroys the `"Ui"` root GameObject.
- Unsubscribe any listener registered on `UiService.OnResolutionChanged` / `OnOrientationChanged` — these are static events and are not cleared automatically.
- `MockAssetLoader.Cleanup()` (or equivalent) to release tracked instances.
- Destroy any test-created `Camera`, `Canvas`, or `PanelSettings`/`ThemeStyleSheet` `ScriptableObject` instances.

Do not rely on domain reload or scene teardown between tests — the Unity Test Runner does not guarantee either between tests in a fixture.

## 11. Performance Tests

**`Measure.Method` runs its body `WarmupCount + MeasurementCount` times.** Stateful operations against `UiService` (Load/Unload/Open/Close) MUST use `.SetUp()` and/or `.CleanUp()` to reset per-iteration state — otherwise iterations 2+ hit the cache and log `The Ui <X> was already loaded` / `<X> is already open`, and the benchmark measures a no-op cache hit instead of the real operation.

Correct shapes:
- Measuring **Load**: `body = Load; CleanUp = Unload;`
- Measuring **Unload**: `SetUp = Load; body = Unload;`

See `Tests/PlayMode/Performance/PerformanceTests.cs` (`Perf_LoadUi_SinglePresenter`, `Perf_UnloadUi_SinglePresenter`) for the reference implementation.

Per §2's inverted-benchmark rule: a `[Performance]` test's measured region must actually contain the workload — verify this by removing the operation from the measured body and confirming the reported number changes.

## 12. Test Directory Layout

| Directory | Contents |
|-----------|----------|
| `Tests/EditMode/` (flat) | Config, set, and core-service unit tests (`UiConfigTests`, `UiSetConfigTests`, `UiInstanceIdTests`, `UiServiceCoreTests`) |
| `Tests/EditMode/Loaders/` | Asset-loader unit tests (`PrefabRegistryUiAssetLoaderTests`, `ResourcesUiAssetLoaderTests`, plus `Loaders/Resources/`) |
| `Tests/EditMode/Helpers/` | EditMode-only test doubles (e.g. EditMode's `MockAssetLoader`) |
| `Tests/Helpers/` | Shared, non-test assembly (`GameLovers.UiService.Tests.Helpers.asmdef`) consumed by both EditMode and PlayMode |
| `Tests/PlayMode/Unit/` | Presenter/feature/view unit tests that need a running player (`UiServiceCorePlayModeTests`, `InteractableTextViewTests`, `NonDrawingViewTests`, `MultiInstanceTests`, `UiServiceOpenCloseTests`) |
| `Tests/PlayMode/Integration/` | Cross-component flows (`UiServiceLoadingTests`, `UiCameraStackFeatureTests`, `UiCameraStackFeatureIntegrationTests`, `UiCameraStackInsertIndexTests`, `UiSetManagementTests`, `ConcurrentFeatureTests`) |
| `Tests/PlayMode/Performance/` | `PerformanceTests` (Load/Unload benchmarks) |
| `Tests/PlayMode/Smoke/` | `SmokeTests` — construction-without-throwing only, per §1's Smoke exemption |
| `Tests/PlayMode/Helpers/` | PlayMode-only test doubles and presenters (e.g. `TestUiToolkitPresenter`, `TestMultiFeatureToolkitPresenter`, PlayMode's `MockAssetLoader`) |

## 13. Coverage Register

Every untested symbol worth naming is either ACCEPTED (justified — do not
re-report) or OPEN (a real gap, owed a test). An untested symbol in neither state
is an audit finding.

An ACCEPTED row needs one of exactly three falsifiable reasons:
- **(i) no branching** — zero conditionals, so there is no behaviour to pin.
- **(ii) engine-owned** — the assertion would target Unity/OS behaviour
  (`[DllImport]`, `AndroidJavaObject`, Addressables statics).
- **(iii) harness-impossible** — the state cannot be fabricated in EditMode or
  PlayMode, **with the specific blocker named**.

"Low value", "hard to test", and "covered by manual QA" are NOT valid reasons. If
none of the three applies, the row is OPEN.

ACCEPTED is dated and **expires on edit**: if the symbol's file changes, the
reason is re-checked in that PR. A `(i) no branching` row is void the moment
someone adds an `if`.

OPEN is the only place a deletion may park coverage. A test removed for weakness
either had a stronger sibling (named in the commit body) or leaves an OPEN row.
The count of OPEN rows is the honest coverage-debt number.

| Symbol (file:line) | State | Reason / Owed | Recorded |
|---|---|---|---|
| `AddressablesUiAssetLoader.InstantiatePrefab` happy path (`Runtime/Loaders/AddressablesUiAssetLoader.cs:16-35`) | ACCEPTED | (i)/(ii) — thin wrapper over `Addressables.InstantiateAsync` / `ReleaseInstance` statics; adopts the same exclusion `com.gamelovers.services` states for `AddressablesAssetLoader` in its `Tests/AGENTS.md` §13. The unknown-address throw path (below) is NOT covered by this exclusion and is owed a test. | 2026-07-31 |
| `AddressablesUiAssetLoader.InstantiatePrefab` unknown-address throw path (`Runtime/Loaders/AddressablesUiAssetLoader.cs:29-32`) | OPEN | Owed: no test exercises `operation.Status != AsyncOperationStatus.Succeeded` → `throw operation.OperationException`. Requires a config with an address Addressables cannot resolve. | 2026-07-31 |
| 12 public `Editor/` types, ~2048 LOC (`Editor/**`) | ACCEPTED | (iii) harness-impossible — blocker: no editor test assembly exists in this package, and neither `GameLovers.UiService.Tests.asmdef` nor `GameLovers.UiService.Tests.PlayMode.asmdef` references `GameLovers.UiService.Editor`. | 2026-07-31 |
| Visual/screenshot regression for camera stacking and backdrop blur (`Runtime/Rendering/UiCameraStackFeature.cs`, `Runtime/Rendering/UiBackdropBlurRendererFeature.cs`, `UiBackdropBlurPass.cs`) | ACCEPTED | (iii) harness-impossible — blocker: needs committed reference images per platform × URP version; no such fixture set exists. | 2026-07-31 |
| `InteractableTextView.ResolveEventCamera` non-overlay branches (`Runtime/Views/InteractableTextView.cs:53-66`) | OPEN | Owed: `InteractableTextViewTests` hardcodes `canvas.renderMode = RenderMode.ScreenSpaceOverlay` (`InteractableTextViewTests.cs:26`), so the `ScreenSpaceCamera` (cameraless and camera-bearing) and `WorldSpace` branches of `ResolveEventCamera` are unpinned. The 1.3.0 fix to this method is unverified by any test. | 2026-07-31 |

## 14. Update Policy
Update this file when:
- Test asmdef references, `defineConstraints`, or `includePlatforms` change
- Naming, placement, or namespace conventions change
- New test directories or categories are added
- Mock/helper patterns change (e.g., NSubstitute reference actually removed from the EditMode asmdef, or a new fake type is introduced)
- A Coverage Register row's underlying file changes (re-check ACCEPTED reasons; add/close OPEN rows)
