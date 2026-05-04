# ProText

High-performance Avalonia 12 text controls powered by PretextSharp.

ProText is a focused text rendering toolkit for Avalonia applications that need fast measurement, predictable caching, rich inline display, editable text presentation, and TextBox-like input surfaces without routing text rendering through Avalonia `TextBlock` fallbacks.

The repository, library project, package id, assembly name, and CLR namespace are now `ProText` because the scope covers display, presenter, and editable text controls.

The controls are mapped into Avalonia's XML namespace through assembly-level `XmlnsDefinition` metadata, so they can be used directly in normal Avalonia XAML once the package or project reference is present.

## Packages

| Package | Version | Downloads | Purpose |
| --- | --- | --- | --- |
| [ProText](https://www.nuget.org/packages/ProText/) | [![NuGet](https://img.shields.io/nuget/v/ProText.svg)](https://www.nuget.org/packages/ProText/) | [![Downloads](https://img.shields.io/nuget/dt/ProText.svg)](https://www.nuget.org/packages/ProText/) | ProText control library with `ProTextBlock`, `ProTextPresenter`, `ProTextBox`, Fluent theme resources, XML documentation, README metadata, and symbol packages. |

Install the package with:

```bash
dotnet add package ProText
```

Or reference it directly from a project file:

```xml
<PackageReference Include="ProText" Version="0.1.0" />
```

For local development inside this repository, reference the project directly:

```xml
<ProjectReference Include="../../src/ProText/ProText.csproj" />
```

Package metadata:

- Package id: `ProText`
- Package title: `ProText`
- Description: `High-performance Avalonia 12 text controls powered by PretextSharp.`
- License: `MIT`
- Repository: `https://github.com/wieslawsoltes/ProText`
- Dependencies: Avalonia, Avalonia Skia rendering support, Pretext, and Pretext Skia rendering support
- Artifacts: `.nupkg`, `.snupkg`, XML documentation, and the root [README.md](README.md)

Only [src/ProText/ProText.csproj](src/ProText/ProText.csproj) is packable. Samples, tests, and benchmark projects are explicitly marked as non-packable.

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
- `ShowCaret()`, `HideCaret()`, `MoveCaretToTextPosition(int)`, and `MoveCaretToPoint(Point)`
- `GetCharacterIndex(Point)`, `GetCaretBounds(int)`, and `MeasureText(double)`
- `CaretBoundsChanged` event

Use `ProTextPresenter` when building a custom editor, search box, command surface, code-like text host, or any control that needs text presentation primitives but owns its own editing behavior.

### `ProTextBox`

`ProTextBox` is a lightweight TextBox-like control that hosts `ProTextPresenter` in its template. It is designed for ProText-backed editable text scenarios where you want a ready-to-use control rather than building a presenter host yourself.

It provides a focused TextBox-style API:

- two-way `Text`
- `AcceptsReturn`, `AcceptsTab`, and `NewLine`
- `IsReadOnly`, `MaxLength`, `UndoLimit`, `CanUndo`, `CanRedo`, `Undo()`, and `Redo()`
- `CaretIndex`, `SelectionStart`, `SelectionEnd`, `SelectedText`, and `SelectAll()`
- clipboard-oriented state such as `CanCut`, `CanCopy`, and `CanPaste`
- `PasswordChar`, `RevealPassword`, placeholder text, floating placeholders, and placeholder foreground
- `InnerLeftContent` and `InnerRightContent`
- `TextAlignment`, `TextWrapping`, `TextDecorations`, `LineHeight`, content alignment, selection brushes, caret brush, and caret blink interval
- `UseGlobalCache`, `UsePretextRendering`, and `PretextLineHeightMultiplier`

Avalonia's built-in `TextBox` expects Avalonia's own `TextPresenter` template part, so `ProTextPresenter` is not a drop-in replacement for the built-in `TextBox` template. `ProTextBox` exists as the ProText-backed editable host.

## Rendering Model

ProText keeps text on the Pretext-powered path from preparation through rendering:

- plain text is prepared and segmented through PretextSharp
- rich inline text is flattened into immutable ProText value data, then prepared through Pretext rich inline APIs
- layout snapshots are local to each control and keyed by width
- global prepared-content cache keys exclude viewport width so prepared text can be reused across controls
- layout and render fingerprints are tracked separately
- render operations snapshot brushes, decorations, and selection styles into immutable value data
- Skia drawing is performed through Avalonia custom draw operations using `ISkiaSharpApiLeaseFeature`

Supported inline text content includes `Run`, `Span`, `Bold`, `Italic`, `Underline`, and `LineBreak`. `InlineUIContainer` is skipped because it is visual content rather than text content; ProText does not create an internal Avalonia fallback visual for it.

Foreground brushes support solid colors and gradient brushes where practical. Multilingual text remains on the Pretext path and uses the ProText Skia font resolver for font fallback.

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
            <ResourceInclude Source="avares://ProText/Themes/Fluent.axaml" />
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

The TextBox benchmark suite applies Avalonia's Fluent TextBox theme and the ProText Fluent theme before measuring. Setup validates that Avalonia `TextPresenter` and ProText `ProTextPresenter` are present in the visual tree. Layout benchmarks alternate width constraints to avoid cached no-op timings, and frame benchmarks use one explicit headless render tick plus `GetLastRenderedFrame()` rather than `CaptureRenderedFrame()`'s stabilization loop.

Command used for the latest documented TextBox run:

```bash
dotnet run -c Release --project benchmarks/ProText.TextBoxBenchmarks/ProText.TextBoxBenchmarks.csproj -- --filter "*"
```

Environment: Apple M3 Pro, .NET `10.0.5`, BenchmarkDotNet `0.15.8`, ShortRun job.

| Scenario | Avalonia TextBox | ProTextBox | Result |
| --- | ---: | ---: | ---: |
| Measure, width 220 | 1.778 ms, 285.7 KB | 51.43 us, 174.84 KB | 34.6x faster, 39% less memory |
| Measure, width 440 | 1.322 ms, 215.05 KB | 42.78 us, 121.86 KB | 30.9x faster, 43% less memory |
| Measure, width 880 | 1.307 ms, 185.08 KB | 37.12 us, 99.11 KB | 35.2x faster, 46% less memory |
| Selected measure, width 220 | 2.237 ms, 285.7 KB | 58.54 us, 174.84 KB | 38.2x faster, 39% less memory |
| Selected measure, width 440 | 1.607 ms, 215.05 KB | 40.34 us, 121.86 KB | 39.8x faster, 43% less memory |
| Selected measure, width 880 | 1.446 ms, 185.08 KB | 35.49 us, 99.11 KB | 40.7x faster, 46% less memory |
| Invalidated frame, single render tick | 306.3 us, 9.09 KB | 318.5 us, 6.24 KB | 4% slower, 31% less memory |

Frame decomposition from the same run shows fixed headless rendering cost separately: `EmptyWindowFrame` is 58.7 us and `ProTextBoxCaptureOnly` is 47.5 us with about 0.52 KB allocated. The invalidated frame result is therefore dominated by Avalonia's retained renderer and headless frame capture cost, while ProTextBox measurement remains substantially faster and lower allocation.

## Project Layout

- `src/ProText` - ProText control library, themes, shared layout, cache, and Skia renderer
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