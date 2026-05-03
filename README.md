# ProTextBlock

`ProTextBlock` is a high-performance Avalonia 12 text rendering library powered by PretextSharp `0.1.0`.

The package provides three controls:

- `ProTextBlock`, a display control that mirrors the text-related `TextBlock` API surface where public Avalonia APIs make that practical.
- `ProTextPresenter`, a reusable TextPresenter-like display component for editable surfaces that need Pretext-powered measurement, selection rectangles, caret bounds, password masking, preedit display, and rich inline presentation.
- `ProTextBox`, a lightweight TextBox-like control with a Fluent TextBox theme copied from Avalonia and a `ProTextPresenter` hosted inside the template.

All three controls use shared rich-inline flattening, Pretext prepared layout, and Skia drawing. Inline runs, trimming, decorations, letter spacing, font-feature-aware cache keys, multilingual text, Skia font fallback, and solid or gradient foreground brushes stay on the Pretext rendering path. The library does not delegate rendering to an internal Avalonia `TextBlock`.

## Projects

- `src/ProTextBlock` - control library.
- `samples/ProTextBlock.Sample` - desktop comparison app for `TextBlock`, `ProTextBlock`, inline content, `ProTextPresenter`, and `ProTextBox`.
- `tests/ProTextBlock.Tests` - unit and Avalonia headless render tests.
- `benchmarks/ProTextBlock.Benchmarks` - BenchmarkDotNet layout and headless render benchmarks.
- `benchmarks/ProTextBlock.InlineBenchmarks` - dedicated inline layout benchmarks.
- `benchmarks/ProTextBlock.PresenterBenchmarks` - dedicated presenter measurement, hit-test, caret, selection, and render benchmarks.
- `benchmarks/ProTextBlock.TextBoxBenchmarks` - TextBox versus ProTextBox measurement and headless render benchmarks.
- `plan` - technical specification and implementation plan.

## Basic Usage

```xml
<pro:ProTextBlock xmlns:pro="clr-namespace:ProTextBlock;assembly=ProTextBlock"
                  Text="High-volume text rendered through PretextSharp"
                  TextWrapping="Wrap"
                  UseGlobalCache="True" />
```

Global caching is enabled by default and can be disabled per control with `UseGlobalCache="False"`.

Rich inline content uses Avalonia document inline types, but layout and rendering remain on the shared Pretext path:

```xml
<pro:ProTextBlock xmlns:pro="clr-namespace:ProTextBlock;assembly=ProTextBlock"
                                    xmlns:docs="clr-namespace:Avalonia.Controls.Documents;assembly=Avalonia.Controls"
                                    TextWrapping="Wrap">
    <docs:Run Text="Inline content: " />
    <docs:Bold>bold</docs:Bold>
    <docs:Run Text=", " />
    <docs:Italic>italic</docs:Italic>
    <docs:Run Text=", " />
    <docs:Underline>underlined</docs:Underline>
</pro:ProTextBlock>
```

`ProTextPresenter` is intended for custom editable controls or text-hosting surfaces. It exposes presenter-style properties and methods such as `CaretIndex`, `SelectionStart`, `SelectionEnd`, `PreeditText`, `PasswordChar`, `ShowCaret()`, `HideCaret()`, `GetCaretBounds(int)`, and `GetCharacterIndex(Point)`.

```xml
<pro:ProTextPresenter xmlns:pro="clr-namespace:ProTextBlock;assembly=ProTextBlock"
                      Text="Editable surface text presented through PretextSharp"
                      TextWrapping="Wrap"
                      CaretIndex="24"
                      SelectionStart="9"
                      SelectionEnd="16" />
```

Avalonia `TextBox` currently expects its template part to be Avalonia's own `TextPresenter` type, so `ProTextPresenter` is a reusable presenter for new/custom controls rather than a drop-in `PART_TextPresenter` replacement for the built-in `TextBox` template.

`ProTextBox` provides a ready-to-use TextBox-like host for `ProTextPresenter`:

```xml
<pro:ProTextBox xmlns:pro="clr-namespace:ProTextBlock;assembly=ProTextBlock"
                Text="Editable text presented through ProTextPresenter"
                TextWrapping="Wrap"
                SelectionStart="9"
                SelectionEnd="22" />
```

Applications that want the Fluent `ProTextBox` theme should merge [Fluent.axaml](src/ProTextBlock/Themes/Fluent.axaml) after adding Avalonia's `FluentTheme`.

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://ProTextBlock/Themes/Fluent.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## Rendering Model

The shared pipeline flattens plain text or supported inlines into immutable `ProTextRichContent`, prepares rich inline paragraphs through PretextSharp, keeps width-local layout snapshots per control, and renders retained custom draw operations through Skia. Render operations store immutable brush and decoration snapshots instead of live Avalonia brush objects.

Supported text inlines are `Run`, `Span`, `Bold`, `Italic`, `Underline`, and `LineBreak`. `InlineUIContainer` is skipped because it is visual content, not text content, and the library does not create an Avalonia fallback visual.

## Performance Summary

The latest corrected TextBox benchmark run verifies that the app applies Avalonia's Fluent TextBox theme and the ProText Fluent theme before measuring. The benchmark fails during setup if Avalonia `TextPresenter` or ProText `ProTextPresenter` is missing from the visual tree, layout measurements alternate width constraints to avoid cached no-op timings, and frame hot paths use one explicit headless render tick plus `GetLastRenderedFrame()` instead of `CaptureRenderedFrame()`'s stabilization loop.

Command used:

```bash
dotnet run -c Release --project benchmarks/ProTextBlock.TextBoxBenchmarks/ProTextBlock.TextBoxBenchmarks.csproj -- --filter "*"
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

Frame decomposition from the same run shows the fixed headless cost separately: `EmptyWindowFrame` is 58.7 us and `ProTextBoxCaptureOnly` is 47.5 us with about 0.52 KB allocated. The invalidated frame benchmark is therefore mostly Avalonia's render tick plus replaying the cached custom Skia draw operation, not text measurement. The render path reuses cached Skia render fonts, records selection and text into a per-layout Skia picture, reuses stable presenter draw operations, and avoids per-frame picture-cache closure allocation. That cuts invalidated-frame allocation from about 13.76 MB to 6.24 KB, while frame time stays close to Avalonia `TextBox` because both controls still pay the retained-renderer and headless frame costs.

## Verification

```bash
dotnet build ProTextBlock.slnx
dotnet test tests/ProTextBlock.Tests/ProTextBlock.Tests.csproj
dotnet run -c Release --project benchmarks/ProTextBlock.Benchmarks/ProTextBlock.Benchmarks.csproj -- --list flat
dotnet run -c Release --project benchmarks/ProTextBlock.InlineBenchmarks/ProTextBlock.InlineBenchmarks.csproj -- --list flat
dotnet run -c Release --project benchmarks/ProTextBlock.PresenterBenchmarks/ProTextBlock.PresenterBenchmarks.csproj -- --list flat
dotnet run -c Release --project benchmarks/ProTextBlock.TextBoxBenchmarks/ProTextBlock.TextBoxBenchmarks.csproj -- --list flat
```

Run the sample app with:

```bash
dotnet run --project samples/ProTextBlock.Sample/ProTextBlock.Sample.csproj
```