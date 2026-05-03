# ProTextBlock Technical Specification

## Scope

`ProTextBlock` is an Avalonia 12.x control package that provides a high-performance text block for text-heavy UI surfaces while preserving `TextBlock` source compatibility wherever Avalonia exposes public APIs.

The implementation targets Avalonia `12.0.2` and PretextSharp `0.1.0`.

## Source Baseline

The compatibility baseline is the local Avalonia checkout at `/Users/wieslawsoltes/GitHub/Avalonia`:

- branch: `master`
- version descriptor: `1.6.1-27572-g1060839683`
- primary source: `src/Avalonia.Controls/TextBlock.cs`
- text document source references: `src/Avalonia.Controls/Documents/*.cs`

Avalonia's in-repo `TextBlock` has access to internal infrastructure such as `IInlineHost`, embedded-control run arrangement, automation peers, and text-line internals. An external package cannot reuse those internal members directly, and `TextBlock.Render` is sealed. To keep the Pretext render path hot and deterministic, `ProTextBlock` derives from `Control`, mirrors the public text-related `TextBlock` property surface, and does not own or delegate to an internal Avalonia `TextBlock` visual.

## Public API

`ProTextBlock` mirrors the public text-related `TextBlock` surface, including:

- `Text`
- `Inlines`
- `Background`
- `Padding`
- `Foreground`
- `FontFamily`
- `FontSize`
- `FontStyle`
- `FontWeight`
- `FontStretch`
- `FontFeatures`
- `TextAlignment`
- `TextWrapping`
- `TextTrimming`
- `TextDecorations`
- `LineHeight`
- `LineSpacing`
- `LetterSpacing`
- `MaxLines`
- attached property helpers equivalent to `TextBlock`

`ProTextBlock` adds:

- `UseGlobalCache`: per-control switch, default `true`; when `false`, the control keeps prepared text/layout data local and bypasses shared cache entries.
- `UsePretextRendering`: per-control switch, default `true`; when `false`, the control suppresses the Pretext text path and does not delegate to Avalonia `TextBlock`.
- `PretextWhiteSpace`: maps to Pretext `WhiteSpaceMode`, default `Normal`.
- `PretextWordBreak`: maps to Pretext `WordBreakMode`, default `Normal`.
- `PretextLineHeightMultiplier`: fallback line-height multiplier used when `LineHeight` is `NaN`, default `1.2`.

`ProTextPresenter` exposes a reusable TextPresenter-like surface for custom editors and text-hosting controls. It uses the same shared Pretext rich-content builder and layout snapshot code as `ProTextBlock`, and includes:

- `Text`
- `Inlines`
- `PreeditText`
- `PreeditTextCursorPosition`
- `Background`
- `Foreground`
- font family/size/style/weight/stretch/features
- text alignment, wrapping, trimming, decorations, line height/spacing, and letter spacing
- `CaretIndex`, `ShowCaret()`, `HideCaret()`, `MoveCaretToTextPosition(int)`, and `MoveCaretToPoint(Point)`
- `SelectionStart`, `SelectionEnd`, `ShowSelectionHighlight`, `SelectionBrush`, and `SelectionForegroundBrush`
- `PasswordChar` and `RevealPassword`
- `GetCaretBounds(int)`, `GetCharacterIndex(Point)`, and `MeasureText(double)`

Avalonia's built-in `TextBox` template part is strongly typed to Avalonia's internal `TextPresenter` control. `ProTextPresenter` is therefore usable by custom controls and future ProText-based editable controls, but it is not a direct drop-in `PART_TextPresenter` replacement for Avalonia `TextBox` without changes in Avalonia itself or a custom text box implementation.

`ProTextBox` is the package's custom TextBox-like host for `ProTextPresenter`. Its Fluent theme is copied from Avalonia's TextBox theme structure and adjusted to target `ProTextBox` and place `ProTextPresenter` at `PART_TextPresenter`. It supports the ProText-backed text display, selection, caret, password reveal, clear-button, placeholder, and basic edit-command surface needed by ProText-backed editable scenarios.

Static cache API:

- `ProTextBlockCache.Clear()` clears the library-level cache and Pretext's internal cache.
- `ProTextBlockCache.GetSnapshot()` returns basic counters for diagnostics and benchmarks.

## Rendering Strategy

The Pretext path is enabled when `UsePretextRendering == true` and the content can be represented as text runs. The rich Pretext path supports:

- plain `Text`
- `Run`, `Span`, `Bold`, `Italic`, `Underline`, and `LineBreak` inline content
- trailing character and word ellipsis trimming
- text decorations rendered by Skia
- letter spacing in measurement and rendering
- font-feature-aware cache keys
- multilingual text through Pretext segmentation and Skia font fallback
- solid, linear-gradient, radial-gradient, and conic-gradient foreground brushes

When enabled:

1. Flatten plain text or inline content into styled `ProTextRichContent` using shared `ProTextInlineBuilder` and `ProTextRichContentBuilder` helpers.
2. Convert Avalonia font properties to an extended Pretext font string that includes ProTextBlock tracking, stretch, and feature markers.
3. Retrieve or prepare `PreparedRichInline` instances through `PretextLayout.PrepareRichInline`.
4. Measure by walking `RichInlineLineRange` data and only materialize fragment strings for retained visible lines.
5. Render with an Avalonia `ICustomDrawOperation` and `ISkiaSharpApiLeaseFeature` when the active renderer is Skia.
6. Draw styled fragments with per-run fonts, Skia font fallback, brushes, letter spacing, and decorations.

`ProTextPresenter` additionally uses text-index metadata on retained layout fragments for caret bounds, hit testing, and selection rectangles. These operations still use the Pretext materialized line data and Skia-backed text measurement helpers; they do not call Avalonia `TextLayout` or `TextPresenter` internals.

Render operations retain value snapshots of foreground brushes, gradient stops, and text decorations instead of live Avalonia objects. This keeps the retained custom drawing data stable while the compositor scrolls or reuses render data.

No Avalonia TextBlock fallback:

- `ProTextBlock` never measures, arranges, or renders through an internal Avalonia `TextBlock`.
- Embedded `InlineUIContainer` content is not rendered by the text control because it is not text content and no fallback visual is created.
- Text containing scripts that require font fallback stays in the Pretext path and is measured/drawn with the ProTextBlock Skia font resolver.
- If the Skia lease is unavailable inside the custom draw operation, the operation skips drawing; the fast path is intended for Avalonia's Skia renderer, which is the default desktop/headless renderer used by the sample, tests, and benchmarks.

## Cache Strategy

Pretext already caches font states and segment measurements internally. `ProTextBlock` adds a global prepared-text/rich-inline cache above it to share prepared text across controls and layout passes.

Cache key fields:

- rich paragraph text
- per-run font descriptor, including letter spacing, stretch, and font-feature fingerprint
- white-space mode and word-break mode for the legacy simple prepared-text cache

Prepared text cache keys intentionally use layout-affecting fingerprints only. Control-local layout snapshots additionally include render fingerprints for foreground brushes and text decorations so visual-only changes repaint correctly without fragmenting the global Pretext preparation cache.

Layout is width-dependent, so prepared text is cached globally while layout results are cached on the control instance for the current width and effective line height. This avoids unbounded global width-key growth and lets repeated measure/render passes stay allocation-light.

Default behavior:

- global cache enabled per control
- cache is thread-safe
- cache entries are bounded by a configurable maximum count
- oldest entries are trimmed when the bound is exceeded

## Measurement Semantics

Pretext measurement returns line count and widths using cached text segment metrics. `ProTextBlock` computes desired size as:

- width: `max measured line width + horizontal padding`
- height: `line count * effective line height + vertical padding`

`TextWrapping.NoWrap` uses `double.PositiveInfinity` for line measurement and reports the natural width.

`MaxLines > 0` clamps measured and rendered line count.

## Compatibility Notes

`ProTextBlock` keeps rich display text on the Pretext path and avoids fragile copies of Avalonia internals. It does not use an Avalonia `TextBlock` fallback; unsupported non-text inline content is skipped rather than delegated.

Known v1 limitations:

- embedded controls inside `InlineUIContainer` are not rendered by `ProTextBlock`
- embedded controls inside `InlineUIContainer` are not rendered by `ProTextPresenter`
- complex-script shaping is limited by the active Skia/Pretext backend; font fallback is handled by the ProTextBlock Skia font resolver
- image, visual, and drawing foreground brushes are not translated to Skia text shaders yet
- OpenType font features are part of the cache/layout identity; final shaping depends on the available Skia backend support
- `ProTextPresenter` selection/caret APIs cover presenter-style display and hit testing, but full text editing command handling and built-in `TextBox` replacement are out of scope for this package phase
- `ProTextBox` is a package-level custom control, not a mutation of Avalonia's built-in `TextBox`; advanced built-in TextBox features should be added deliberately as public API compatibility work

## Projects

- `src/ProTextBlock`: control library
- `samples/ProTextBlock.Sample`: Avalonia desktop sample comparing `TextBlock`, `ProTextBlock`, inline content, and `ProTextPresenter`
- `tests/ProTextBlock.Tests`: xUnit plus Avalonia headless render tests
- `benchmarks/ProTextBlock.Benchmarks`: BenchmarkDotNet layout/render benchmarks
- `benchmarks/ProTextBlock.InlineBenchmarks`: BenchmarkDotNet inline layout benchmarks
- `benchmarks/ProTextBlock.PresenterBenchmarks`: BenchmarkDotNet presenter layout, caret, hit-test, selection, and render benchmarks
- `benchmarks/ProTextBlock.TextBoxBenchmarks`: BenchmarkDotNet Avalonia `TextBox` versus `ProTextBox` measure and headless render benchmarks

## Verification

- `dotnet restore ProTextBlock.slnx`
- `dotnet build ProTextBlock.slnx`
- `dotnet test tests/ProTextBlock.Tests/ProTextBlock.Tests.csproj`
- `dotnet run -c Release --project benchmarks/ProTextBlock.Benchmarks/ProTextBlock.Benchmarks.csproj -- --filter *`
- `dotnet run -c Release --project benchmarks/ProTextBlock.InlineBenchmarks/ProTextBlock.InlineBenchmarks.csproj -- --list flat`
- `dotnet run -c Release --project benchmarks/ProTextBlock.PresenterBenchmarks/ProTextBlock.PresenterBenchmarks.csproj -- --list flat`
- `dotnet run -c Release --project benchmarks/ProTextBlock.TextBoxBenchmarks/ProTextBlock.TextBoxBenchmarks.csproj -- --list flat`
- sample app launch: `dotnet run --project samples/ProTextBlock.Sample/ProTextBlock.Sample.csproj`
