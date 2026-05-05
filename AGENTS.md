# AGENTS.md

## Project Mission

Build `ProText.Core` plus framework adapters for high-performance text controls powered by PretextSharp. `ProText.Core` owns reusable preparation, layout, bounded shared caching, font fallback, selection geometry, editable text helpers, and Skia rendering.

Maintain `ProText.Avalonia`, a high-performance Avalonia text controls library powered by `ProText.Core`. Avalonia `ProTextBlock` should preserve the public text-related `TextBlock` API surface as much as possible while keeping layout, measurement, caching, and rendering on the Pretext-powered path.

Add and maintain `ProText.Uno`, a first Uno adapter analogous to `ProText.Avalonia`. Uno controls should use `ProText.Core`, WinUI/Uno dependency properties, Uno-compatible themes, and Uno Skia rendering while avoiding Avalonia dependencies and framework `TextBlock` fallbacks.

Add and maintain `ProText.MAUI`, a .NET MAUI adapter analogous to `ProText.Avalonia` and `ProText.Uno`. MAUI controls should use `ProText.Core`, MAUI bindable properties, MAUI-compatible themes, formatted text adapters, and MAUI Skia rendering while avoiding Avalonia, Uno, and framework `Label` or `Editor` fallbacks.

Also maintain `ProTextPresenter`, a reusable Pretext-powered presenter for custom editable text surfaces. It shares the same rich inline, layout, cache, and Skia rendering core as `ProTextBlock` and provides presenter-style caret, selection, preedit, password, hit-test, and measurement APIs.

Maintain `ProTextBox` as a lightweight TextBox-like host for `ProTextPresenter`. Avalonia, Uno, and MAUI themes may follow their framework TextBox/Editor styling conventions, but must keep the visible text presenter on the ProText path.

## Hard Requirements

- Target the current Avalonia package baseline in `Directory.Packages.props`; when Uno or MAUI packages are added, target the current Uno or MAUI package baseline there as well.
- Use PretextSharp for text preparation, layout, rich inline measurement, line materialization, and high-performance text rendering support.
- Do not add or use an internal Avalonia `TextBlock` fallback inside `ProTextBlock`.
- Do not add or use an internal WinUI/Uno `TextBlock` fallback inside Uno `ProTextBlock`.
- Do not add or use an internal MAUI `Label` or `Editor` fallback inside MAUI `ProTextBlock`, `ProTextPresenter`, or `ProTextBox`.
- Do not route multilingual text, rich text, unsupported rich cases, or explicit compatibility switches through Avalonia `TextBlock` from inside `ProTextBlock`.
- Do not route multilingual text, rich text, unsupported rich cases, or explicit compatibility switches through WinUI/Uno `TextBlock` from inside Uno `ProTextBlock`.
- Do not route multilingual text, rich text, unsupported rich cases, or explicit compatibility switches through MAUI `Label` or `Editor` from inside MAUI ProText controls.
- Keep global prepared-text caching enabled by default with per-control opt-out through `UseGlobalCache`.
- Keep the shared cache bounded and expose diagnostic counters through `ProTextCache.GetSnapshot()`.
- Keep per-control layout snapshots width-local so global cache keys do not grow by viewport width.
- Keep render operations free of live mutable framework brush/decoration objects; snapshot render styles into immutable ProText value data.
- Preserve layout-only and render fingerprints separately: layout fingerprints drive global Pretext prepared-content cache reuse; render fingerprints invalidate control-local snapshots.
- Keep reusable inline and layout code shared between `ProTextBlock` and `ProTextPresenter`, and between framework adapters through `ProText.Core`; avoid duplicating inline flattening or render-style snapshot logic.

## Text And Rendering Requirements

- Plain text must use Pretext preparation and layout.
- Rich inline text must use Pretext rich inline APIs where representable as text.
- Supported inlines include `Run`, `Span`, `Bold`, `Italic`, `Underline`, and `LineBreak`.
- Embedded visual inline content such as Avalonia `InlineUIContainer` must not create a framework fallback visual. It may be skipped or treated as unsupported non-text content, but it must not be rendered by an internal `TextBlock`.
- Rich features should remain on the Pretext path: inlines, trimming, text decorations, font features in cache identity, letter spacing, and non-solid foreground brushes.
- Foreground brushes should support solid, linear gradient, radial gradient, and conic gradient where practical.
- Multilingual text must remain on the Pretext path. Use Pretext segmentation and the ProText Skia font resolver for font fallback instead of framework fallback.
- Rendering should use Skia through Avalonia custom drawing with `ISkiaSharpApiLeaseFeature`.
- Uno rendering should use Uno Skia rendering surfaces and delegate drawing to the shared ProText Skia renderer.
- MAUI rendering should use MAUI Skia rendering surfaces and delegate drawing to the shared ProText Skia renderer.
- If the Skia lease is unavailable, the custom draw operation may skip drawing rather than falling back to Avalonia `TextBlock`.
- If the Uno Skia surface is unavailable, the Uno draw operation may skip drawing rather than falling back to WinUI/Uno `TextBlock`.
- If the MAUI Skia surface is unavailable, the MAUI draw operation may skip drawing rather than falling back to MAUI `Label` or `Editor`.
- `ProTextPresenter` must keep selection, caret, preedit, password display, inlines, and hit testing on the Pretext/shared-rendering path. Do not call Avalonia `TextLayout` or use Avalonia `TextPresenter` internals from this package.
- Uno `ProTextPresenter` must keep selection, caret, preedit, password display, inlines, and hit testing on the Pretext/shared-rendering path. Do not call framework text layout APIs or use built-in text presenter internals from this package.
- MAUI `ProTextPresenter` must keep selection, caret, preedit, password display, formatted text, and hit testing on the Pretext/shared-rendering path. Do not call framework text layout APIs or use built-in `Label` or `Editor` internals from this package.
- `ProTextBox` must host `ProTextPresenter` in its theme instead of Avalonia `TextPresenter`.
- Uno `ProTextBox` must keep visible text on the Uno `ProTextPresenter` path instead of a built-in WinUI/Uno text presenter.
- MAUI `ProTextBox` must keep visible text on the MAUI `ProTextPresenter` path instead of a built-in MAUI `Editor` text presenter.

## API Compatibility Goals

- Mirror the public text-related `TextBlock` properties and attached property helpers where practical: `Text`, `Inlines`, `Background`, `Padding`, `Foreground`, font family/size/style/weight/stretch/features, alignment, wrapping, trimming, decorations, line height/spacing, letter spacing, max lines, and baseline offset.
- Additional properties include `UseGlobalCache`, `UsePretextRendering`, `PretextWhiteSpace`, `PretextWordBreak`, and `PretextLineHeightMultiplier`.
- `UsePretextRendering` must not activate an Avalonia `TextBlock` fallback. If disabled, it should not render through Avalonia `TextBlock`.
- Uno controls should expose analogous APIs through WinUI/Uno dependency properties where practical.
- MAUI controls should expose analogous APIs through MAUI bindable properties where practical.
- `UsePretextRendering` must not activate a WinUI/Uno `TextBlock` fallback in Uno controls. If disabled, it should not render through framework `TextBlock`.
- `UsePretextRendering` must not activate a MAUI `Label` or `Editor` fallback in MAUI controls. If disabled, it should not render through framework `Label` or `Editor`.
- `ProTextPresenter` should provide TextPresenter-like public behavior for custom controls, but it is not a direct `PART_TextPresenter` replacement for Avalonia `TextBox` because built-in `TextBox` currently expects Avalonia's own `TextPresenter` type.
- Uno `ProTextPresenter` should provide TextPresenter-like public behavior for custom Uno controls, but it should not rely on built-in Uno `TextBox` internals.
- MAUI `ProTextPresenter` should provide presenter-like public behavior for custom MAUI controls, but it should not rely on built-in MAUI `Editor` internals.
- `ProTextBox` may expose a focused TextBox-like API for ProText-backed scenarios; framework adapters do not need to clone every built-in TextBox editing feature unless requested.

## Samples, Tests, Benchmarks, Docs

- Maintain a sample app comparing Avalonia `TextBlock` and `ProTextBlock` side by side. The sample may use normal Avalonia `TextBlock` as the baseline comparison outside the `ProTextBlock` control.
- Maintain a Uno sample app comparing Uno `TextBlock` and Uno `ProTextBlock` side by side. The sample may use normal Uno `TextBlock` as the baseline comparison outside the `ProTextBlock` control.
- Maintain a MAUI sample app comparing MAUI `Label` and `Editor` baselines with MAUI `ProTextBlock` and `ProTextBox` side by side. The sample may use normal MAUI `Label` and `Editor` as baseline comparisons outside ProText controls.
- Keep sample comparison content visually fair: when comparing inline behavior, both sides should use equivalent text and styling unless the sample is explicitly demonstrating a ProTextBlock-only feature.
- Include `ProTextPresenter` sample content showing selection/caret behavior and rich inline presentation.
- Include dense scrolling/sample content for artifact checks and cache visibility.
- Maintain headless UI tests for measurement, rich rendering, cache behavior, multilingual Pretext-path behavior, and scroll rendering smoke coverage.
- Maintain headless UI tests for `ProTextPresenter` measurement, caret bounds, hit testing, selection rendering, preedit text, password masking, and inline rendering.
- Maintain headless UI tests for `ProTextPresenter` measurement, caret bounds, hit testing, selection rendering, preedit text, password masking, inline rendering, and `ProTextBox` template presenter wiring.
- Maintain Uno UI/runtime tests for measurement, rich rendering, cache behavior, multilingual Pretext-path behavior, presenter behavior, and `ProTextBox` template presenter wiring.
- Maintain MAUI UI/runtime tests for measurement, formatted rendering, cache behavior, multilingual Pretext-path behavior, presenter behavior, and `ProTextBox` presenter wiring.
- Maintain BenchmarkDotNet benchmarks comparing Avalonia `TextBlock`, `ProTextBlock`, rich text, global/local cache paths, Pretext cold prepare, headless render capture, inline-specific paths, `ProTextPresenter` presenter operations, and Avalonia `TextBox` versus `ProTextBox`.
- Maintain BenchmarkDotNet benchmarks comparing Uno `TextBlock`, Uno `ProTextBlock`, rich text, global/local cache paths, inline-specific paths, Uno `ProTextPresenter` presenter operations, and Uno `TextBox` versus Uno `ProTextBox`.
- Maintain BenchmarkDotNet benchmarks comparing MAUI `Label`, MAUI `ProTextBlock`, formatted text, global/local cache paths, formatted-text paths, MAUI `ProTextPresenter` presenter operations, and MAUI `Editor` versus MAUI `ProTextBox`.
- Keep `README.md`, `plan/technical-spec.md`, and `plan/implementation-plan.md` aligned with actual behavior.

## Verification Commands

Run these after implementation changes:

```bash
dotnet build ProText.slnx
dotnet test tests/ProText.Tests/ProText.Tests.csproj
dotnet run --project samples/ProText.Sample/ProText.Sample.csproj
```

For benchmark discovery or execution:

```bash
dotnet run -c Release --project benchmarks/ProText.Benchmarks/ProText.Benchmarks.csproj -- --list flat
dotnet run -c Release --project benchmarks/ProText.InlineBenchmarks/ProText.InlineBenchmarks.csproj -- --list flat
dotnet run -c Release --project benchmarks/ProText.PresenterBenchmarks/ProText.PresenterBenchmarks.csproj -- --list flat
dotnet run -c Release --project benchmarks/ProText.TextBoxBenchmarks/ProText.TextBoxBenchmarks.csproj -- --list flat
```

When Uno projects exist, add and run the equivalent Uno build, test, sample, and benchmark-discovery commands for `ProText.Uno`.

When MAUI projects exist, add and run the equivalent MAUI build, sample, and benchmark-discovery commands for `ProText.MAUI`.

## Engineering Guidance

- Prefer small, focused changes that preserve existing project style.
- Do not modify the sibling PretextSharp repository unless the requested feature truly cannot be implemented through existing Pretext APIs and the user explicitly accepts that cross-repo change.
- Keep performance-sensitive paths allocation-conscious, but prefer correctness for glyph/font fallback over drawing missing-glyph boxes.
- Avoid sample-only fixes for control bugs. Fix root behavior in `src/ProText.Avalonia` first, then adjust samples/tests/docs.
- Avoid sample-only fixes for Uno control bugs. Fix root behavior in `src/ProText.Uno` or shared `src/ProText.Core` first, then adjust samples/tests/docs.
- Avoid sample-only fixes for MAUI control bugs. Fix root behavior in `src/ProText.MAUI` or shared `src/ProText.Core` first, then adjust samples/tests/docs.
- Do not add unrelated refactors while fixing rendering, cache, or API issues.
