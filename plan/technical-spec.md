# ProText.Avalonia Technical Specification

## Scope

ProText.Avalonia is a package of high-performance Avalonia text controls powered by PretextSharp. Its display control, `ProTextBlock`, preserves `TextBlock` source compatibility wherever Avalonia exposes public APIs. The implementation is split into `ProText.Core`, which contains the reusable Pretext/Skia text engine, and `ProText.Avalonia`, which adapts that engine to Avalonia controls, properties, inlines, brushes, fonts, and custom drawing.

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
- `CaretIndex`, `ShowCaret()`, `HideCaret()`, `MoveCaretToTextPosition(int)`, `MoveCaretToPoint(Point)`, `MoveCaretHorizontal(LogicalDirection)`, and `MoveCaretVertical(LogicalDirection)`
- `SelectionStart`, `SelectionEnd`, `ShowSelectionHighlight`, `SelectionBrush`, and `SelectionForegroundBrush`
- `PasswordChar` and `RevealPassword`
- `GetNextCharacterHit(LogicalDirection)`, `GetCaretBounds(int)`, `GetCharacterIndex(Point)`, `GetLineCount()`, `GetLineBounds(int)`, and `MeasureText(double)`

Avalonia's built-in `TextBox` template part is strongly typed to Avalonia's internal `TextPresenter` control. `ProTextPresenter` is therefore usable by custom controls and future ProText-based editable controls, but it is not a direct drop-in `PART_TextPresenter` replacement for Avalonia `TextBox` without changes in Avalonia itself or a custom text box implementation.

`ProTextBox` is the package's custom TextBox-like host for `ProTextPresenter`. Its Fluent theme is copied from Avalonia's TextBox theme structure and adjusted to target `ProTextBox` and place `ProTextPresenter` at `PART_TextPresenter`. It supports the ProText-backed text display, mouse drag and keyboard selection, caret, password reveal, clear-button, placeholder/watermark aliases, undo/redo state, clipboard state/events, line count/scroll APIs, and TextBox-compatible edit-command surface needed by ProText-backed editable scenarios.

Static cache API:

- `ProTextCache.Clear()` clears the library-level cache and Pretext's internal cache.
- `ProTextCache.GetSnapshot()` returns basic counters for diagnostics and benchmarks.

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

1. Flatten plain text or inline content into styled `ProTextRichContent`. Avalonia inlines and brushes are converted by the Avalonia adapter; the rich content model itself is framework-neutral.
2. Convert font properties to an extended Pretext font string that includes ProText tracking, stretch, and feature markers.
3. Retrieve or prepare `PreparedRichInline` instances through the bounded `ProText.Core` cache and `PretextLayout.PrepareRichInline`.
4. Measure by walking `RichInlineLineRange` data and only materialize fragment strings for retained visible lines.
5. Render with an Avalonia `ICustomDrawOperation` and `ISkiaSharpApiLeaseFeature` when the active renderer is Skia.
6. Delegate actual `SKCanvas` text drawing to the framework-neutral `ProTextSkiaRenderer`, including per-run fonts, Skia font fallback, brushes, letter spacing, and decorations.

`ProTextPresenter` additionally uses text-index metadata on retained layout fragments for caret bounds, hit testing, and selection rectangles. These operations are implemented in `ProText.Core` geometry helpers over neutral point/rect types and do not call Avalonia `TextLayout` or `TextPresenter` internals.

Render operations retain value snapshots of foreground brushes, gradient stops, and text decorations instead of live Avalonia objects. This keeps the retained custom drawing data stable while the compositor scrolls or reuses render data.

No Avalonia TextBlock fallback:

- `ProTextBlock` never measures, arranges, or renders through an internal Avalonia `TextBlock`.
- Embedded `InlineUIContainer` content is not rendered by the text control because it is not text content and no fallback visual is created.
- Text containing scripts that require font fallback stays in the Pretext path and is measured/drawn with the ProText Skia font resolver.
- If the Skia lease is unavailable inside the custom draw operation, the operation skips drawing; the fast path is intended for Avalonia's Skia renderer, which is the default desktop/headless renderer used by the sample, tests, and benchmarks.

## Cache Strategy

Pretext already caches font states and segment measurements internally. `ProText.Core` adds a global prepared-text/rich-inline cache above it to share prepared text across controls, presenters, and non-Avalonia hosts.

Cache key fields:

- rich paragraph text
- per-run font descriptor, including letter spacing, stretch, and font-feature fingerprint
- white-space mode and word-break mode for the legacy simple prepared-text cache

Prepared text cache keys intentionally use layout-affecting fingerprints only. `ProTextLayoutCache` keeps current and previous width-local layout snapshots on the host/control instance and matches those snapshots by layout fingerprint, width, effective line height, max lines, wrapping, and trimming. When only render fingerprints change, the cache remaps retained fragments to the new immutable render styles without re-preparing text or rebuilding line geometry.

Layout is width-dependent, so prepared text is cached globally while layout results are cached on the control instance for the current and previous width/effective line-height request. This avoids unbounded global width-key growth and lets repeated measure/render passes and width toggles stay allocation-light.

`ProTextSelectionGeometryCache` stores selection rectangles by layout snapshot, normalized selection range, bounds width, text alignment, and flow direction. `ProTextEditableText` centralizes presenter display-text composition for password masking, IME preedit insertion, and effective caret index calculation so non-Avalonia adapters can reuse the same editable text behavior.

The public Avalonia `ProTextCache` type is a source-compatible facade over `ProText.Core.ProTextCoreCache`; it also ensures the Avalonia font resolver is installed before controls prepare or measure text.

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
- complex-script shaping is limited by the active Skia/Pretext backend; font fallback is handled by the ProText Skia font resolver
- image, visual, and drawing foreground brushes are not translated to Skia text shaders yet
- OpenType font features are part of the cache/layout identity; final shaping depends on the available Skia backend support
- `ProTextPresenter` exposes presenter-style movement, hit testing, caret bounds, line count, and selection APIs backed by retained ProText layout snapshots rather than Avalonia `TextLayout`
- `ProTextBox` is a package-level custom control, not a mutation of Avalonia's built-in `TextBox`; future built-in TextBox feature gaps should be added deliberately as public API compatibility work on the ProText path

## Projects

- `src/ProText.Core`: framework-neutral rich content, layout, cache, font fallback, selection geometry, and Skia rendering engine
- `src/ProText.Avalonia`: Avalonia control library, theme resources, and Avalonia adapters over `ProText.Core`
- `samples/ProText.Sample`: Avalonia desktop sample comparing `TextBlock`, `ProTextBlock`, inline content, and `ProTextPresenter`
- `tests/ProText.Tests`: xUnit plus Avalonia headless render tests
- `benchmarks/ProText.Benchmarks`: BenchmarkDotNet layout/render benchmarks
- `benchmarks/ProText.InlineBenchmarks`: BenchmarkDotNet inline layout benchmarks
- `benchmarks/ProText.PresenterBenchmarks`: BenchmarkDotNet presenter layout, caret, hit-test, selection, and render benchmarks
- `benchmarks/ProText.TextBoxBenchmarks`: BenchmarkDotNet Avalonia `TextBox` versus `ProTextBox` measure and headless render benchmarks

## Verification

- `dotnet restore ProText.slnx`
- `dotnet build ProText.slnx`
- `dotnet test tests/ProText.Tests/ProText.Tests.csproj`
- `dotnet run -c Release --project benchmarks/ProText.Benchmarks/ProText.Benchmarks.csproj -- --list flat`
- `dotnet run -c Release --project benchmarks/ProText.InlineBenchmarks/ProText.InlineBenchmarks.csproj -- --list flat`
- `dotnet run -c Release --project benchmarks/ProText.PresenterBenchmarks/ProText.PresenterBenchmarks.csproj -- --list flat`
- `dotnet run -c Release --project benchmarks/ProText.TextBoxBenchmarks/ProText.TextBoxBenchmarks.csproj -- --list flat`
- sample app launch: `dotnet run --project samples/ProText.Sample/ProText.Sample.csproj`
