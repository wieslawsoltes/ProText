# ProText.Avalonia

High-performance Avalonia 12 text controls powered by PretextSharp.

ProText.Avalonia is a focused text rendering toolkit for Avalonia applications that need fast measurement, predictable caching, rich inline display, editable text presentation, and TextBox-like input surfaces without routing text rendering through Avalonia `TextBlock` fallbacks.

The repository keeps the reusable preparation, layout, cache, font, and Skia rendering implementation in `ProText.Core`. The Avalonia controls package, assembly name, and CLR namespace are `ProText.Avalonia` so the same text engine can be adapted by other UI frameworks without referencing Avalonia.

The controls are mapped into Avalonia's XML namespace through assembly-level `XmlnsDefinition` metadata, so they can be used directly in normal Avalonia XAML once the package or project reference is present.

## Packages

| Package | Version | Downloads | Purpose |
| --- | --- | --- | --- |
| [ProText.Core](https://www.nuget.org/packages/ProText.Core/) | [![NuGet](https://img.shields.io/nuget/v/ProText.Core.svg)](https://www.nuget.org/packages/ProText.Core/) | [![Downloads](https://img.shields.io/nuget/dt/ProText.Core.svg)](https://www.nuget.org/packages/ProText.Core/) | Framework-neutral Pretext rich content, layout snapshots, cache, font fallback, selection geometry, and Skia rendering services. |
| [ProText.Avalonia](https://www.nuget.org/packages/ProText.Avalonia/) | [![NuGet](https://img.shields.io/nuget/v/ProText.Avalonia.svg)](https://www.nuget.org/packages/ProText.Avalonia/) | [![Downloads](https://img.shields.io/nuget/dt/ProText.Avalonia.svg)](https://www.nuget.org/packages/ProText.Avalonia/) | ProText control library with `ProTextBlock`, `ProTextPresenter`, `ProTextBox`, Fluent theme resources, XML documentation, README metadata, and symbol packages. |

Install the Avalonia controls package with:

```bash
dotnet add package ProText.Avalonia
```

Or reference it directly from a project file:

```xml
<PackageReference Include="ProText.Avalonia" Version="0.1.0" />
```

For a non-Avalonia host or framework adapter, reference the core package:

```bash
dotnet add package ProText.Core
```

For local development inside this repository, reference the project directly:

```xml
<ProjectReference Include="../../src/ProText.Avalonia/ProText.Avalonia.csproj" />
```

`ProText.Avalonia` references `ProText.Core`; non-Avalonia hosts can reference [src/ProText.Core/ProText.Core.csproj](src/ProText.Core/ProText.Core.csproj) directly and provide their own font/style/content adapters.

Avalonia package metadata:

- Package id: `ProText.Avalonia`
- Package title: `ProText.Avalonia`
- Description: `High-performance Avalonia 12 text controls powered by ProText.Core and PretextSharp.`
- License: `MIT`
- Repository: `https://github.com/wieslawsoltes/ProText`
- Dependencies: `ProText.Core`, Avalonia, Avalonia Skia rendering support, and Pretext
- Artifacts: `.nupkg`, `.snupkg`, XML documentation, and the root [README.md](README.md)

Core package metadata:

- Package id: `ProText.Core`
- Package title: `ProText.Core`
- Description: `Reusable PretextSharp text layout, cache, font, and Skia rendering core for ProText controls.`
- Dependencies: Pretext, Pretext Skia rendering support, and SkiaSharp
- Artifacts: `.nupkg`, `.snupkg`, XML documentation, and the root [README.md](README.md)

[src/ProText.Core/ProText.Core.csproj](src/ProText.Core/ProText.Core.csproj) contains the reusable engine. [src/ProText.Avalonia/ProText.Avalonia.csproj](src/ProText.Avalonia/ProText.Avalonia.csproj) contains the Avalonia controls and theme resources. Samples, tests, and benchmark projects are explicitly marked as non-packable.

## Controls

### `ProTextBlock`

`ProTextBlock` is a high-performance display control intended for TextBlock-like scenarios: labels, dense rows, document fragments, diagnostics, search results, preview text, telemetry panels, and other text-heavy read-only UI.

It mirrors the public text-related `TextBlock` surface where practical, including:

- `Text` and `Inlines`
- `Background`, `Padding`, and `Foreground`
- `FontFamily`, `FontSize`, `FontStyle`, `FontWeight`, `FontStretch`, and `FontFeatures`
- `TextAlignment`, `TextWrapping`, `TextTrimming`, `TextDecorations`, `LetterSpacing`, `LineHeight`, `LineSpacing`, `MaxLines`, and `BaselineOffset`
- attached property helpers for TextBlock-compatible layout properties
- ProText-specific properties such as `UseGlobalCache`, `UsePretextRendering`, `PretextWhiteSpace`, `PretextWordBreak`, and `PretextLineHeightMultiplier`

Plain text and supported inline content are prepared through PretextSharp, measured through the shared ProText layout layer, and rendered through Skia custom drawing.

### `ProTextPresenter`

`ProTextPresenter` is the reusable presenter layer for custom editable or selectable text controls. It uses the same rich content preparation, layout snapshots, cache identity, font fallback, and Skia renderer as `ProTextBlock`, while exposing presenter-style caret, selection, preedit, password, measurement, and hit-test APIs.

Notable features include:

- `Text` and display-oriented `Inlines`
- `PreeditText` and `PreeditTextCursorPosition` for IME composition display
- `CaretIndex`, `SelectionStart`, `SelectionEnd`, `ShowSelectionHighlight`, `SelectionBrush`, `SelectionForegroundBrush`, `CaretBrush`, and `CaretBlinkInterval`
- `PasswordChar` and `RevealPassword`
- `ShowCaret()`, `HideCaret()`, `MoveCaretToTextPosition(int)`, `MoveCaretToPoint(Point)`, `MoveCaretHorizontal(LogicalDirection)`, and `MoveCaretVertical(LogicalDirection)`
- `GetNextCharacterHit(LogicalDirection)`, `GetCharacterIndex(Point)`, `GetCaretBounds(int)`, `GetLineCount()`, `GetLineBounds(int)`, and `MeasureText(double)`
- `CaretBoundsChanged` event

Use `ProTextPresenter` when building a custom editor, search box, command surface, code-like text host, or any control that needs text presentation primitives but owns its own editing behavior.

### `ProTextBox`

`ProTextBox` is a lightweight TextBox-like control that hosts `ProTextPresenter` in its template. It is designed for ProText-backed editable text scenarios where you want a ready-to-use control rather than building a presenter host yourself.

It provides a focused TextBox-style API:

- two-way `Text`
- `AcceptsReturn`, `AcceptsTab`, and `NewLine`
- `IsReadOnly`, `MaxLength`, `MinLines`, `MaxLines`, `IsUndoEnabled`, `UndoLimit`, `CanUndo`, `CanRedo`, `Undo()`, and `Redo()`
- `CaretIndex`, `SelectionStart`, `SelectionEnd`, `SelectedText`, `SelectAll()`, and `ClearSelection()`
- mouse drag selection, shift-click extension, double-click word selection, triple-click line selection, and keyboard word/line navigation
- clipboard-oriented state such as `CanCut`, `CanCopy`, and `CanPaste`
- text and clipboard routed events such as `TextChanging`, `TextChanged`, `CopyingToClipboard`, `CuttingToClipboard`, and `PastingFromClipboard`
- `PasswordChar`, `RevealPassword`, placeholder/watermark text aliases, floating placeholder/watermark aliases, and placeholder/watermark foreground aliases
- `InnerLeftContent` and `InnerRightContent`
- `TextAlignment`, `TextWrapping`, `TextDecorations`, `LineHeight`, content alignment, selection brushes, caret brush, and caret blink interval
- `GetLineCount()` and `ScrollToLine(int)` backed by `ProTextPresenter` layout data
- `UseGlobalCache`, `UsePretextRendering`, and `PretextLineHeightMultiplier`

Avalonia's built-in `TextBox` expects Avalonia's own `TextPresenter` template part, so `ProTextPresenter` is not a drop-in replacement for the built-in `TextBox` template. `ProTextBox` exists as the ProText-backed editable host.

## Rendering Model

ProText keeps text on the Pretext-powered path from preparation through rendering:

- framework-neutral rich content, layout, cache, selection geometry, font fallback, and Skia drawing live in `ProText.Core`
- Avalonia-specific code adapts `InlineCollection`, brushes, decorations, fonts, flow direction, and `ICustomDrawOperation` into the core value model
- plain text is prepared and segmented through PretextSharp
- rich inline text is flattened into immutable ProText value data, then prepared through Pretext rich inline APIs
- layout snapshots are local to each control and keyed by width
- global prepared-content cache keys exclude viewport width so prepared text can be reused across controls
- layout and render fingerprints are tracked separately; render-only style changes remap retained layout geometry without re-preparing text
- render operations snapshot brushes, decorations, and selection styles into immutable value data
- Skia drawing is performed through Avalonia custom draw operations using `ISkiaSharpApiLeaseFeature`

Supported inline text content includes `Run`, `Span`, `Bold`, `Italic`, `Underline`, and `LineBreak`. `InlineUIContainer` is skipped because it is visual content rather than text content; ProText does not create an internal Avalonia fallback visual for it.

Foreground brushes support solid colors and gradient brushes where practical. Multilingual text remains on the Pretext path and uses the ProText Skia font resolver for font fallback.

## Core Adapter APIs

Non-Avalonia hosts adapt their own text/style objects into `ProText.Core` value data, then reuse the same preparation, layout, geometry, and Skia rendering services:

- `ProTextRichContentBuilder` and `ProTextRichStyle` create immutable plain or rich text content with separate layout and render fingerprints.
- `ProTextLayoutCache` owns per-host current and previous width-local layout snapshots and prepared-content reuse.
- `ProTextLayoutRequest` captures layout width, line height, max lines, wrapping, trimming, and global-cache preference.
- `ProTextSelectionGeometryCache` caches selection rectangles by snapshot, selection range, bounds width, alignment, and flow direction.
- `ProTextEditableText` composes presenter display text for caret, password masking, and IME preedit text without depending on Avalonia.
- `ProTextSkiaRenderer` draws layout snapshots to `SKCanvas` using immutable core render options.

## Cache And Diagnostics

Global prepared-text caching is enabled by default and can be disabled per control with `UseGlobalCache="False"`.

`ProTextCache` exposes process-wide cache controls:

- `MaxEntryCount` bounds the shared prepared-text and rich-inline cache
- `Clear()` clears ProText and PretextSharp internal layout caches
- `GetSnapshot()` returns `Count`, `MaxEntryCount`, `Hits`, and `Misses` for diagnostics, sample UI, and benchmarks

## Quick Start

Reference the package or project, then use the controls from the standard Avalonia XAML namespace.

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="MyApp.MainWindow">
    <ProTextBlock Text="High-volume text rendered through PretextSharp"
                  TextWrapping="Wrap"
                  TextTrimming="CharacterEllipsis"
                  UseGlobalCache="True" />
</Window>
```

### Display Text

```xml
<ProTextBlock Text="High-volume text rendered through PretextSharp"
              TextWrapping="Wrap"
              TextTrimming="CharacterEllipsis"
              UseGlobalCache="True" />
```

### Rich Inline Text

```xml
<ProTextBlock TextWrapping="Wrap"
              xmlns:docs="clr-namespace:Avalonia.Controls.Documents;assembly=Avalonia.Controls">
    <docs:Run Text="Inline content: " />
    <docs:Bold>bold</docs:Bold>
    <docs:Run Text=", " />
    <docs:Italic>italic</docs:Italic>
    <docs:Run Text=", and " />
    <docs:Underline>underlined</docs:Underline>
</ProTextBlock>
```

### Presenter Surface

```xml
<ProTextPresenter Text="Selectable text presented through ProText"
                  TextWrapping="Wrap"
                  CaretIndex="24"
                  SelectionStart="9"
                  SelectionEnd="16"
                  SelectionBrush="#663B82F6"
                  SelectionForegroundBrush="#FFFFFF" />
```

### Editable TextBox-Like Control

```xml
<ProTextBox Text="Editable text presented through ProTextPresenter"
            TextWrapping="Wrap"
            PlaceholderText="Search"
            SelectionStart="9"
            SelectionEnd="22" />
```

To use the Fluent `ProTextBox` theme, merge the ProText theme after Avalonia's Fluent theme:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://ProText.Avalonia/Themes/Fluent.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## Sample App

The sample project demonstrates the current control set and comparison scenarios:

- Avalonia `TextBlock` beside `ProTextBlock`
- equivalent inline text rendered by Avalonia and ProText
- `ProTextPresenter` selection, caret, password, preedit, and rich-inline presentation
- Avalonia `TextBox` beside `ProTextBox`
- dense scrolling text content with cache diagnostics
- zoomable, pannable canvas tabs with 1000 absolute-positioned Avalonia `TextBox` controls and 1000 absolute-positioned `ProTextBox` controls

Run it with:

```bash
dotnet run --project samples/ProText.Sample/ProText.Sample.csproj
```

## Performance Snapshot

Results below were generated on 2026-05-04 with BenchmarkDotNet `0.15.8` on Apple M3 Pro, macOS Tahoe `26.4.1`, .NET SDK `10.0.201`, and .NET runtime `10.0.5`. `ProText.Benchmarks` and `ProText.InlineBenchmarks` use BenchmarkDotNet's default job; `ProText.PresenterBenchmarks` and `ProText.TextBoxBenchmarks` use `ShortRun` as configured in the benchmark projects.

Commands used for the documented run:

```bash
dotnet run -c Release --project benchmarks/ProText.Benchmarks/ProText.Benchmarks.csproj -- --filter "*"
dotnet run -c Release --project benchmarks/ProText.InlineBenchmarks/ProText.InlineBenchmarks.csproj -- --filter "*"
dotnet run -c Release --project benchmarks/ProText.PresenterBenchmarks/ProText.PresenterBenchmarks.csproj -- --filter "*"
dotnet run -c Release --project benchmarks/ProText.TextBoxBenchmarks/ProText.TextBoxBenchmarks.csproj -- --filter "*"
```

### TextBlock Layout And Cache

| Method | Width | Mean | Ratio | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: |
| AvaloniaTextBlockMeasure | 160 | 757,121.0 ns | 1.000 | 229,608 B | 1.000 |
| ProTextBlockGlobalCacheMeasure | 160 | 3,720.3 ns | 0.005 | 49,312 B | 0.215 |
| ProTextBlockLocalCacheMeasure | 160 | 3,723.5 ns | 0.005 | 49,312 B | 0.215 |
| AvaloniaRichTextBlockMeasure | 160 | 51,570.5 ns | 0.068 | 22,328 B | 0.097 |
| ProTextBlockRichMeasure | 160 | 4,796.9 ns | 0.006 | 32,327 B | 0.141 |
| PretextColdPrepare | 160 | 480,168.2 ns | 0.634 | 1,295,812 B | 5.644 |
| PretextMeasureLineStats | 160 | 950.9 ns | 0.001 | 88 B | 0.000 |
| AvaloniaTextBlockMeasure | 320 | 676,603.0 ns | 1.000 | 157,456 B | 1.000 |
| ProTextBlockGlobalCacheMeasure | 320 | 3,782.3 ns | 0.006 | 49,312 B | 0.313 |
| ProTextBlockLocalCacheMeasure | 320 | 3,791.4 ns | 0.006 | 49,312 B | 0.313 |
| AvaloniaRichTextBlockMeasure | 320 | 45,619.8 ns | 0.067 | 15,840 B | 0.101 |
| ProTextBlockRichMeasure | 320 | 4,775.3 ns | 0.007 | 32,327 B | 0.205 |
| PretextColdPrepare | 320 | 478,321.7 ns | 0.707 | 1,295,812 B | 8.230 |
| PretextMeasureLineStats | 320 | 851.7 ns | 0.001 | 88 B | 0.001 |
| AvaloniaTextBlockMeasure | 640 | 637,930.0 ns | 1.000 | 115,864 B | 1.000 |
| ProTextBlockGlobalCacheMeasure | 640 | 3,782.7 ns | 0.006 | 49,312 B | 0.426 |
| ProTextBlockLocalCacheMeasure | 640 | 3,801.7 ns | 0.006 | 49,312 B | 0.426 |
| AvaloniaRichTextBlockMeasure | 640 | 44,579.6 ns | 0.070 | 14,496 B | 0.125 |
| ProTextBlockRichMeasure | 640 | 4,734.0 ns | 0.007 | 32,327 B | 0.279 |
| PretextColdPrepare | 640 | 473,190.0 ns | 0.742 | 1,295,812 B | 11.184 |
| PretextMeasureLineStats | 640 | 1,024.3 ns | 0.002 | 88 B | 0.001 |

### TextBlock Headless Render

| Method | Mean | Ratio | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: |
| AvaloniaTextBlockFrame | 62.52 us | 1.00 | 655 B | 1.00 |
| ProTextBlockFrame | 62.77 us | 1.00 | 656 B | 1.00 |

### Inline Layout

| Method | Width | Mean | Ratio | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: |
| AvaloniaTextBlockInlineMeasure | 180 | 466,180.91 ns | 1.000 | 177,808 B | 1.00 |
| ProTextBlockInlineMeasure | 180 | 57,538.85 ns | 0.123 | 295,863 B | 1.66 |
| ProTextPresenterInlineMeasure | 180 | 79.03 ns | 0.000 | - | 0.00 |
| AvaloniaTextBlockInlineMeasure | 360 | 438,062.62 ns | 1.000 | 148,632 B | 1.00 |
| ProTextBlockInlineMeasure | 360 | 58,334.96 ns | 0.133 | 295,863 B | 1.99 |
| ProTextPresenterInlineMeasure | 360 | 78.16 ns | 0.000 | - | 0.00 |
| AvaloniaTextBlockInlineMeasure | 720 | 426,446.30 ns | 1.000 | 143,296 B | 1.00 |
| ProTextBlockInlineMeasure | 720 | 57,865.73 ns | 0.136 | 295,863 B | 2.06 |
| ProTextPresenterInlineMeasure | 720 | 80.31 ns | 0.000 | - | 0.00 |

### Presenter Operations

| Method | Width | Mean | Ratio | Allocated |
| --- | ---: | ---: | ---: | ---: |
| PresenterMeasure | 240 | 81.80 ns | 1.00 | - |
| PresenterCaretBounds | 240 | 314.18 ns | 3.84 | 128 B |
| PresenterHitTest | 240 | 4,723.75 ns | 57.75 | 1,912 B |
| EmptyWindowFrame | 240 | 50,813.18 ns | 621.17 | 606 B |
| PresenterPlainFrame | 240 | 340,712.50 ns | 4,165.10 | 7,012 B |
| PresenterSelectedFrame | 240 | 343,965.24 ns | 4,204.86 | 7,026 B |
| PresenterMeasure | 480 | 80.45 ns | 1.00 | - |
| PresenterCaretBounds | 480 | 320.81 ns | 3.99 | 128 B |
| PresenterHitTest | 480 | 8,654.58 ns | 107.58 | 3,456 B |
| EmptyWindowFrame | 480 | 50,954.18 ns | 633.37 | 606 B |
| PresenterPlainFrame | 480 | 339,838.00 ns | 4,224.26 | 7,010 B |
| PresenterSelectedFrame | 480 | 341,366.86 ns | 4,243.27 | 7,010 B |
| PresenterMeasure | 960 | 80.63 ns | 1.00 | - |
| PresenterCaretBounds | 960 | 4,709.12 ns | 58.40 | 1,128 B |
| PresenterHitTest | 960 | 17,238.17 ns | 213.78 | 6,800 B |
| EmptyWindowFrame | 960 | 50,832.60 ns | 630.42 | 606 B |
| PresenterPlainFrame | 960 | 350,925.74 ns | 4,352.12 | 7,000 B |
| PresenterSelectedFrame | 960 | 340,326.70 ns | 4,220.67 | 7,012 B |

### TextBox Layout

The TextBox benchmark suite applies Avalonia's Fluent TextBox theme and the ProText Fluent theme before measuring. Setup validates that Avalonia `TextPresenter` and ProText `ProTextPresenter` are present in the visual tree. Layout benchmarks alternate width constraints to avoid cached no-op timings.

| Method | Width | Mean | Ratio | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: |
| AvaloniaTextBoxMeasure | 220 | 1,291.90 us | 1.00 | 285.70 KB | 1.00 |
| ProTextBoxMeasure | 220 | 46.83 us | 0.04 | 174.84 KB | 0.61 |
| AvaloniaTextBoxSelectedMeasure | 220 | 1,316.04 us | 1.02 | 285.70 KB | 1.00 |
| ProTextBoxSelectedMeasure | 220 | 46.56 us | 0.04 | 174.84 KB | 0.61 |
| AvaloniaTextBoxMeasure | 440 | 1,238.14 us | 1.00 | 215.05 KB | 1.00 |
| ProTextBoxMeasure | 440 | 37.33 us | 0.03 | 121.86 KB | 0.57 |
| AvaloniaTextBoxSelectedMeasure | 440 | 1,218.57 us | 0.98 | 215.05 KB | 1.00 |
| ProTextBoxSelectedMeasure | 440 | 37.46 us | 0.03 | 121.86 KB | 0.57 |
| AvaloniaTextBoxMeasure | 880 | 1,169.68 us | 1.00 | 185.08 KB | 1.00 |
| ProTextBoxMeasure | 880 | 32.94 us | 0.03 | 99.11 KB | 0.54 |
| AvaloniaTextBoxSelectedMeasure | 880 | 1,171.24 us | 1.00 | 185.08 KB | 1.00 |
| ProTextBoxSelectedMeasure | 880 | 32.52 us | 0.03 | 99.11 KB | 0.54 |

### TextBox Headless Render

Frame benchmarks use one explicit headless render tick plus `GetLastRenderedFrame()` rather than `CaptureRenderedFrame()`'s stabilization loop.

| Method | Mean | Ratio | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: |
| EmptyWindowFrame | 44.59 us | 0.10 | 533 B | 0.05 |
| AvaloniaTextBoxCaptureOnly | 161.11 us | 0.35 | 1,068 B | 0.11 |
| ProTextBoxCaptureOnly | 128.78 us | 0.28 | 908 B | 0.09 |
| AvaloniaTextBoxFrame | 460.62 us | 1.00 | 9,993 B | 1.00 |
| ProTextBoxFrame | 488.39 us | 1.06 | 7,123 B | 0.71 |
| DirectProTextPresenterFrame | 460.73 us | 1.00 | 7,392 B | 0.74 |

## Project Layout

- `src/ProText.Core` - framework-neutral rich content, layout, cache, font, selection geometry, and Skia renderer
- `src/ProText.Avalonia` - Avalonia control library, themes, inline/style/font adapters, and custom draw operation host
- `samples/ProText.Sample` - desktop comparison and stress sample app
- `tests/ProText.Tests` - unit tests and Avalonia headless rendering tests
- `benchmarks/ProText.Benchmarks` - TextBlock, layout, cache, and render benchmarks
- `benchmarks/ProText.InlineBenchmarks` - rich-inline layout benchmarks
- `benchmarks/ProText.PresenterBenchmarks` - presenter measurement, hit-test, caret, selection, and render benchmarks
- `benchmarks/ProText.TextBoxBenchmarks` - Avalonia TextBox versus ProTextBox benchmarks
- `plan` - technical specification and implementation plan

## Verification

```bash
dotnet build ProText.slnx
dotnet test tests/ProText.Tests/ProText.Tests.csproj
dotnet run -c Release --project benchmarks/ProText.Benchmarks/ProText.Benchmarks.csproj -- --list flat
dotnet run -c Release --project benchmarks/ProText.InlineBenchmarks/ProText.InlineBenchmarks.csproj -- --list flat
dotnet run -c Release --project benchmarks/ProText.PresenterBenchmarks/ProText.PresenterBenchmarks.csproj -- --list flat
dotnet run -c Release --project benchmarks/ProText.TextBoxBenchmarks/ProText.TextBoxBenchmarks.csproj -- --list flat
```

## Design Principles

- Keep text measurement, layout, and rendering on the Pretext path.
- Keep prepared-text caching global by default, bounded, and diagnosable.
- Keep viewport-width-dependent layout snapshots local to each control.
- Snapshot mutable Avalonia brushes and text decorations before rendering.
- Share inline flattening, layout, selection, and render-style code between display and editable controls.
- Avoid internal Avalonia `TextBlock` fallbacks inside ProText controls.
