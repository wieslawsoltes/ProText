# ProTextBlock

`ProTextBlock` is a high-performance Avalonia 12 text control powered by PretextSharp `0.1.0`.

The control mirrors the text-related `TextBlock` API and uses Pretext prepared layout plus Skia drawing for plain text and rich display text. Inline runs, trimming, decorations, letter spacing, font-feature-aware cache keys, multilingual text, Skia font fallback, and solid or gradient foreground brushes stay on the Pretext rendering path. `ProTextBlock` does not delegate rendering to an internal Avalonia `TextBlock`.

## Projects

- `src/ProTextBlock` - control library.
- `samples/ProTextBlock.Sample` - desktop comparison app for `TextBlock` and `ProTextBlock`.
- `tests/ProTextBlock.Tests` - unit and Avalonia headless render tests.
- `benchmarks/ProTextBlock.Benchmarks` - BenchmarkDotNet layout and headless render benchmarks.
- `plan` - technical specification and implementation plan.

## Basic Usage

```xml
<pro:ProTextBlock xmlns:pro="clr-namespace:ProTextBlock;assembly=ProTextBlock"
                  Text="High-volume text rendered through PretextSharp"
                  TextWrapping="Wrap"
                  UseGlobalCache="True" />
```

Global caching is enabled by default and can be disabled per control with `UseGlobalCache="False"`.

## Verification

```bash
dotnet build ProTextBlock.slnx
dotnet test tests/ProTextBlock.Tests/ProTextBlock.Tests.csproj
dotnet run -c Release --project benchmarks/ProTextBlock.Benchmarks/ProTextBlock.Benchmarks.csproj -- --list flat
```

Run the sample app with:

```bash
dotnet run --project samples/ProTextBlock.Sample/ProTextBlock.Sample.csproj
```