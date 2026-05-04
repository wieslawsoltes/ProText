# AGENTS.md

## Project Mission

Build `ProText`, a high-performance Avalonia text controls library powered by PretextSharp. `ProTextBlock` should preserve the public text-related `TextBlock` API surface as much as possible while keeping layout, measurement, caching, and rendering on the Pretext-powered path.

Also maintain `ProTextPresenter`, a reusable Pretext-powered presenter for custom editable text surfaces. It shares the same rich inline, layout, cache, and Skia rendering core as `ProTextBlock` and provides presenter-style caret, selection, preedit, password, hit-test, and measurement APIs.

Maintain `ProTextBox` as a lightweight TextBox-like host for `ProTextPresenter`. Its Fluent theme is based on Avalonia's TextBox theme and must keep the visible text presenter on the ProText path.

## Hard Requirements

- Target the current Avalonia package baseline in `Directory.Packages.props`.
- Use PretextSharp for text preparation, layout, rich inline measurement, line materialization, and high-performance text rendering support.
- Do not add or use an internal Avalonia `TextBlock` fallback inside `ProTextBlock`.
- Do not route multilingual text, rich text, unsupported rich cases, or explicit compatibility switches through Avalonia `TextBlock` from inside `ProTextBlock`.
- Keep global prepared-text caching enabled by default with per-control opt-out through `UseGlobalCache`.
- Keep the shared cache bounded and expose diagnostic counters through `ProTextCache.GetSnapshot()`.
- Keep per-control layout snapshots width-local so global cache keys do not grow by viewport width.
- Keep render operations free of live mutable Avalonia brush/decoration objects; snapshot render styles into immutable ProText value data.
- Preserve layout-only and render fingerprints separately: layout fingerprints drive global Pretext prepared-content cache reuse; render fingerprints invalidate control-local snapshots.
- Keep reusable inline and layout code shared between `ProTextBlock` and `ProTextPresenter`; avoid duplicating inline flattening or render-style snapshot logic.

## Text And Rendering Requirements

- Plain text must use Pretext preparation and layout.
- Rich inline text must use Pretext rich inline APIs where representable as text.
- Supported inlines include `Run`, `Span`, `Bold`, `Italic`, `Underline`, and `LineBreak`.
- Embedded `InlineUIContainer` content must not create an Avalonia fallback visual. It may be skipped or treated as unsupported non-text content, but it must not be rendered by an internal `TextBlock`.
- Rich features should remain on the Pretext path: inlines, trimming, text decorations, font features in cache identity, letter spacing, and non-solid foreground brushes.
- Foreground brushes should support solid, linear gradient, radial gradient, and conic gradient where practical.
- Multilingual text must remain on the Pretext path. Use Pretext segmentation and the ProText Skia font resolver for font fallback instead of Avalonia fallback.
- Rendering should use Skia through Avalonia custom drawing with `ISkiaSharpApiLeaseFeature`.
- If the Skia lease is unavailable, the custom draw operation may skip drawing rather than falling back to Avalonia `TextBlock`.
- `ProTextPresenter` must keep selection, caret, preedit, password display, inlines, and hit testing on the Pretext/shared-rendering path. Do not call Avalonia `TextLayout` or use Avalonia `TextPresenter` internals from this package.
- `ProTextBox` must host `ProTextPresenter` in its theme instead of Avalonia `TextPresenter`.

## API Compatibility Goals

- Mirror the public text-related `TextBlock` properties and attached property helpers where practical: `Text`, `Inlines`, `Background`, `Padding`, `Foreground`, font family/size/style/weight/stretch/features, alignment, wrapping, trimming, decorations, line height/spacing, letter spacing, max lines, and baseline offset.
- Additional properties include `UseGlobalCache`, `UsePretextRendering`, `PretextWhiteSpace`, `PretextWordBreak`, and `PretextLineHeightMultiplier`.
- `UsePretextRendering` must not activate an Avalonia `TextBlock` fallback. If disabled, it should not render through Avalonia `TextBlock`.
- `ProTextPresenter` should provide TextPresenter-like public behavior for custom controls, but it is not a direct `PART_TextPresenter` replacement for Avalonia `TextBox` because built-in `TextBox` currently expects Avalonia's own `TextPresenter` type.
- `ProTextBox` may expose a focused TextBox-like API for ProText-backed scenarios; it does not need to clone every built-in TextBox editing feature unless requested.

## Samples, Tests, Benchmarks, Docs

- Maintain a sample app comparing Avalonia `TextBlock` and `ProTextBlock` side by side. The sample may use normal Avalonia `TextBlock` as the baseline comparison outside the `ProTextBlock` control.
- Keep sample comparison content visually fair: when comparing inline behavior, both sides should use equivalent text and styling unless the sample is explicitly demonstrating a ProTextBlock-only feature.
- Include `ProTextPresenter` sample content showing selection/caret behavior and rich inline presentation.
- Include dense scrolling/sample content for artifact checks and cache visibility.
- Maintain headless UI tests for measurement, rich rendering, cache behavior, multilingual Pretext-path behavior, and scroll rendering smoke coverage.
- Maintain headless UI tests for `ProTextPresenter` measurement, caret bounds, hit testing, selection rendering, preedit text, password masking, and inline rendering.
- Maintain headless UI tests for `ProTextPresenter` measurement, caret bounds, hit testing, selection rendering, preedit text, password masking, inline rendering, and `ProTextBox` template presenter wiring.
- Maintain BenchmarkDotNet benchmarks comparing Avalonia `TextBlock`, `ProTextBlock`, rich text, global/local cache paths, Pretext cold prepare, headless render capture, inline-specific paths, `ProTextPresenter` presenter operations, and Avalonia `TextBox` versus `ProTextBox`.
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

## Engineering Guidance

- Prefer small, focused changes that preserve existing project style.
- Do not modify the sibling PretextSharp repository unless the requested feature truly cannot be implemented through existing Pretext APIs and the user explicitly accepts that cross-repo change.
- Keep performance-sensitive paths allocation-conscious, but prefer correctness for glyph/font fallback over drawing missing-glyph boxes.
- Avoid sample-only fixes for control bugs. Fix root behavior in `src/ProText` first, then adjust samples/tests/docs.
- Do not add unrelated refactors while fixing rendering, cache, or API issues.
