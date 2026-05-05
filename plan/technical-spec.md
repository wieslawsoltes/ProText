# ProText Technical Specification

## Scope

ProText is a family of high-performance text controls powered by PretextSharp. The implementation is split into `ProText.Core`, which contains the reusable Pretext/Skia text engine, and framework adapters that expose native control APIs while keeping text preparation, layout, caching, hit testing, selection geometry, and rendering on the ProText path.

`ProText.Avalonia` is the Avalonia adapter. Its display control, `ProTextBlock`, preserves `TextBlock` source compatibility wherever Avalonia exposes public APIs. It adapts `ProText.Core` to Avalonia controls, properties, inlines, brushes, fonts, and custom drawing.

`ProText.Uno` is the Uno adapter. It adapts `ProText.Core` to WinUI/Uno controls, dependency properties, text elements, brushes, fonts, and Uno Skia rendering. It must not reference Avalonia and must not route text through Avalonia, WinUI, or Uno `TextBlock` fallbacks.

`ProText.MAUI` is the .NET MAUI adapter. It adapts `ProText.Core` to MAUI controls, bindable properties, formatted text spans, brushes, fonts, and MAUI Skia rendering. It must not reference Avalonia or Uno and must not route text through MAUI `Label` or `Editor` fallbacks.

## Source Baseline

The compatibility baseline is the local Avalonia checkout at `/Users/wieslawsoltes/GitHub/Avalonia`:

- branch: `master`
- version descriptor: `1.6.1-27572-g1060839683`
- primary source: `src/Avalonia.Controls/TextBlock.cs`
- text document source references: `src/Avalonia.Controls/Documents/*.cs`

Avalonia's in-repo `TextBlock` has access to internal infrastructure such as `IInlineHost`, embedded-control run arrangement, automation peers, and text-line internals. An external package cannot reuse those internal members directly, and `TextBlock.Render` is sealed. To keep the Pretext render path hot and deterministic, `ProTextBlock` derives from `Control`, mirrors the public text-related `TextBlock` property surface, and does not own or delegate to an internal Avalonia `TextBlock` visual.

The Uno adapter baseline is the current Uno/WinUI package baseline selected in repository package management. `ProText.Uno` uses public WinUI/Uno control and dependency-property patterns, keeps renderer-specific code isolated in the Uno adapter, and avoids copying or depending on internal framework text presenters.

The MAUI adapter baseline is the current .NET MAUI package baseline selected in repository package management. `ProText.MAUI` uses public MAUI controls, bindable-property patterns, and handler-friendly renderer code, keeps renderer-specific code isolated in the MAUI adapter, and avoids copying or depending on internal framework label/editor presenters.

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
- `UsePretextRendering`: per-control switch, default `true`; when `false`, the control suppresses the Pretext text path and does not delegate to a framework `TextBlock`.
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

`ProText.Uno` exposes equivalent public cache controls over `ProText.Core.ProTextCoreCache`, including bounded `MaxEntryCount`, `Clear()`, and `GetSnapshot()` diagnostics. The Uno facade does not create a separate cache universe from Avalonia; framework adapters share the same core cache process-wide.

`ProText.MAUI` exposes equivalent public cache controls over `ProText.Core.ProTextCoreCache`, including bounded `MaxEntryCount`, `Clear()`, and `GetSnapshot()` diagnostics. The MAUI facade does not create a separate cache universe from Avalonia or Uno; framework adapters share the same core cache process-wide.

## Uno Public API

The Uno package provides controls analogous to the Avalonia package while using WinUI/Uno types:

- Uno `ProTextBlock` uses WinUI/Uno dependency properties for `Text`, display inlines or representable text elements, background, padding, foreground, font family/size/style/weight/stretch/features where available, alignment, wrapping, trimming, decorations, line height/spacing, letter spacing, max lines, and ProText-specific cache/rendering properties.
- Uno `ProTextPresenter` exposes presenter-style `Text`, display inlines, preedit, caret, selection, password, hit-test, line bounds, and measurement APIs backed by `ProText.Core`.
- Uno `ProTextBox` is a lightweight TextBox-like ProTextPresenter-derived host; it does not attempt to replace an internal framework text presenter in the built-in Uno `TextBox`.
- Uno controls use normal WinUI/Uno dependency-property registration. Uno framework-internal generated dependency-property workflows are not required for this external package.

## MAUI Public API

The MAUI package provides controls analogous to the Avalonia and Uno packages while using MAUI types:

- MAUI `ProTextBlock` uses bindable properties for `Text`, `FormattedText` or representable text spans, background, padding, text color/foreground, font family/size/attributes, alignment, line break mode, decorations, line height, letter spacing, max lines, and ProText-specific cache/rendering properties.
- MAUI `ProTextPresenter` exposes presenter-style `Text`, formatted text, preedit, caret, selection, password, hit-test, line bounds, and measurement APIs backed by `ProText.Core`.
- MAUI `ProTextBox` is a lightweight Editor-like ProTextPresenter-derived host; it does not attempt to replace an internal framework text presenter in the built-in MAUI `Editor`.
- MAUI controls use normal MAUI bindable-property registration. Framework-internal handler or platform text-presenter workflows are not required for this external package.

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

1. Flatten plain text or inline content into styled `ProTextRichContent`. Avalonia inlines and brushes are converted by the Avalonia adapter; Uno text elements and brushes are converted by the Uno adapter; MAUI formatted text spans and brushes are converted by the MAUI adapter. The rich content model itself is framework-neutral.
2. Convert font properties to an extended Pretext font string that includes ProText tracking, stretch, and feature markers.
3. Retrieve or prepare `PreparedRichInline` instances through the bounded `ProText.Core` cache and `PretextLayout.PrepareRichInline`.
4. Measure by walking `RichInlineLineRange` data and only materialize fragment strings for retained visible lines.
5. Render with a thin framework draw operation when the active renderer is Skia. Avalonia uses `ICustomDrawOperation` and `ISkiaSharpApiLeaseFeature`; Uno uses its Skia-backed rendering surface; MAUI uses a MAUI Skia-backed drawing surface.
6. Delegate actual `SKCanvas` text drawing to the framework-neutral `ProTextSkiaRenderer`, including per-run fonts, Skia font fallback, brushes, letter spacing, and decorations.

`ProTextPresenter` additionally uses text-index metadata on retained layout fragments for caret bounds, hit testing, and selection rectangles. These operations are implemented in `ProText.Core` geometry helpers over neutral point/rect types and do not call Avalonia `TextLayout` or `TextPresenter` internals.

Render operations retain value snapshots of foreground brushes, gradient stops, and text decorations instead of live framework objects. This keeps the retained custom drawing data stable while the compositor scrolls or reuses render data.

No framework TextBlock fallback:

- `ProTextBlock` never measures, arranges, or renders through an internal Avalonia `TextBlock`.
- Uno `ProTextBlock` must never measure, arrange, or render through an internal WinUI/Uno `TextBlock`.
- MAUI `ProTextBlock` must never measure, arrange, or render through an internal MAUI `Label` or `Editor`.
- Embedded `InlineUIContainer` content is not rendered by the text control because it is not text content and no fallback visual is created.
- Text containing scripts that require font fallback stays in the Pretext path and is measured/drawn with the ProText Skia font resolver.
- If the Skia lease is unavailable inside the custom draw operation, the operation skips drawing; the fast path is intended for Avalonia's Skia renderer, which is the default desktop/headless renderer used by the sample, tests, and benchmarks.
- If Uno's Skia draw surface is unavailable, the Uno operation may skip drawing rather than falling back to framework `TextBlock`.
- If MAUI's Skia draw surface is unavailable, the MAUI operation may skip drawing rather than falling back to framework `Label` or `Editor`.

## Cache Strategy

Pretext already caches font states and segment measurements internally. `ProText.Core` adds a global prepared-text/rich-inline cache above it to share prepared text across controls, presenters, and non-Avalonia hosts.

Cache key fields:

- rich paragraph text
- per-run font descriptor, including letter spacing, stretch, and font-feature fingerprint
- white-space mode and word-break mode for the legacy simple prepared-text cache

Prepared text cache keys intentionally use layout-affecting fingerprints only. `ProTextLayoutCache` keeps current and previous width-local layout snapshots on the host/control instance and matches those snapshots by layout fingerprint, width, effective line height, max lines, wrapping, and trimming. When only render fingerprints change, the cache remaps retained fragments to the new immutable render styles without re-preparing text or rebuilding line geometry.

Layout is width-dependent, so prepared text is cached globally while layout results are cached on the control instance for the current and previous width/effective line-height request. This avoids unbounded global width-key growth and lets repeated measure/render passes and width toggles stay allocation-light.

`ProTextSelectionGeometryCache` stores selection rectangles by layout snapshot, normalized selection range, bounds width, text alignment, and flow direction. `ProTextEditableText` centralizes presenter display-text composition for password masking, IME preedit insertion, and effective caret index calculation so non-Avalonia adapters can reuse the same editable text behavior.

The public Avalonia `ProTextCache` type is a source-compatible facade over `ProText.Core.ProTextCoreCache`; it also ensures the Avalonia font resolver is installed before controls prepare or measure text. The Uno adapter exposes the same cache facade shape and installs the Uno/Skia font resolver before Uno controls prepare or measure text. The MAUI adapter exposes the same cache facade shape and installs the MAUI/Skia font resolver before MAUI controls prepare or measure text.

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
- Uno starts with the same ProText-path limitations: no framework `TextBlock` fallback, no embedded visual-inline rendering, and TextBox-like features added deliberately through the Uno `ProTextBox` host rather than through built-in `TextBox` internals
- MAUI starts with the same ProText-path limitations: no framework `Label` or `Editor` fallback, no embedded visual-content rendering, and Editor-like features added deliberately through the MAUI `ProTextBox` host rather than through built-in `Editor` internals

## Projects

- `src/ProText.Core`: framework-neutral rich content, layout, cache, font fallback, selection geometry, and Skia rendering engine
- `src/ProText.Avalonia`: Avalonia control library, theme resources, and Avalonia adapters over `ProText.Core`
- `src/ProText.Uno`: Uno control library, WinUI/Uno dependency-property adapters, and Uno Skia adapters over `ProText.Core`
- `src/ProText.MAUI`: MAUI control library, bindable-property adapters, formatted-text adapters, and MAUI Skia adapters over `ProText.Core`
- `samples/ProText.Sample`: Avalonia desktop sample comparing `TextBlock`, `ProTextBlock`, inline content, and `ProTextPresenter`
- `samples/ProText.Uno.Sample`: Uno sample comparing Uno `TextBlock`, Uno `ProTextBlock`, inline content, `ProTextPresenter`, and `ProTextBox`
- `samples/ProText.MAUI.Sample`: MAUI sample comparing MAUI `Label`, MAUI `Editor`, MAUI `ProTextBlock`, formatted content, `ProTextPresenter`, and `ProTextBox`
- `tests/ProText.Tests`: xUnit plus Avalonia headless render tests
- `tests/ProText.Uno.Tests`: Uno adapter, API-surface, cache, and no-fallback boundary tests
- `benchmarks/ProText.Benchmarks`: BenchmarkDotNet layout/render benchmarks
- `benchmarks/ProText.InlineBenchmarks`: BenchmarkDotNet inline layout benchmarks
- `benchmarks/ProText.PresenterBenchmarks`: BenchmarkDotNet presenter layout, caret, hit-test, selection, and render benchmarks
- `benchmarks/ProText.TextBoxBenchmarks`: BenchmarkDotNet Avalonia `TextBox` versus `ProTextBox` measure and headless render benchmarks
- `benchmarks/ProText.Uno.Benchmarks`: BenchmarkDotNet coverage for Uno `TextBlock`, Uno `TextBox`, Uno `ProTextBlock`, Uno `ProTextPresenter`, cache paths, and measurement APIs
- `benchmarks/ProText.MAUI.Benchmarks`: BenchmarkDotNet coverage for MAUI `Label`, MAUI `Editor`, MAUI `ProTextBlock`, MAUI `ProTextPresenter`, cache paths, and measurement APIs

## Verification

- `dotnet restore ProText.slnx`
- `dotnet build ProText.slnx`
- `dotnet test tests/ProText.Tests/ProText.Tests.csproj`
- `dotnet run -c Release --project benchmarks/ProText.Benchmarks/ProText.Benchmarks.csproj -- --list flat`
- `dotnet run -c Release --project benchmarks/ProText.InlineBenchmarks/ProText.InlineBenchmarks.csproj -- --list flat`
- `dotnet run -c Release --project benchmarks/ProText.PresenterBenchmarks/ProText.PresenterBenchmarks.csproj -- --list flat`
- `dotnet run -c Release --project benchmarks/ProText.TextBoxBenchmarks/ProText.TextBoxBenchmarks.csproj -- --list flat`
- sample app launch: `dotnet run --project samples/ProText.Sample/ProText.Sample.csproj`

- `dotnet test tests/ProText.Uno.Tests/ProText.Uno.Tests.csproj`
- `dotnet run --project samples/ProText.Uno.Sample/ProText.Uno.Sample.csproj`
- `dotnet run -c Release --project benchmarks/ProText.Uno.Benchmarks/ProText.Uno.Benchmarks.csproj -- --list flat`
- `dotnet run --project samples/ProText.MAUI.Sample/ProText.MAUI.Sample.csproj`
- `dotnet run -c Release --project benchmarks/ProText.MAUI.Benchmarks/ProText.MAUI.Benchmarks.csproj -- --list flat`
