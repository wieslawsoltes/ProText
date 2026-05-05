# ProText Implementation Plan

## 1. Repository Setup

- Create a solution with library, sample, test, and benchmark projects.
- Target `net10.0` for the library and developer tooling in this workspace.
- Use central package management for Avalonia, Uno/WinUI, .NET MAUI, PretextSharp, SkiaSharp, xUnit, Avalonia headless testing, Uno-supported UI testing, MAUI-supported UI testing, and BenchmarkDotNet.
- Enable nullable reference types and implicit usings.
- Maintain `src/ProText.Core` as the reusable Pretext/Skia text engine with no Avalonia dependency.
- Maintain `src/ProText.Avalonia` as the Avalonia controls, inline/style/font adapter, theme, and custom draw operation host.
- Add `src/ProText.Uno` as the Uno controls, WinUI/Uno dependency-property adapter, text-element/style/font adapter, theme, and Uno Skia draw operation host.
- Add `src/ProText.MAUI` as the MAUI controls, bindable-property adapter, formatted-text/style/font adapter, theme, and MAUI Skia draw operation host.
- Keep framework adapters isolated from each other: `ProText.Uno` must not reference Avalonia, `ProText.MAUI` must not reference Avalonia or Uno, and `ProText.Avalonia` must not reference Uno or MAUI.

## 2. Control Library

- Add `ProTextBlock`, derived from `Avalonia.Controls.Control` with mirrored text-related `TextBlock` APIs.
- Add styled properties for `UseGlobalCache`, `UsePretextRendering`, `PretextWhiteSpace`, `PretextWordBreak`, and `PretextLineHeightMultiplier`.
- Override `MeasureOverride`, `ArrangeOverride`, and `Render`.
- Use a Pretext rich-inline path for plain text, styled runs, trimming, decorations, letter spacing, font-feature cache identity, and solid or gradient foreground brushes.
- Keep Avalonia inline helpers for `Run`, `Span`, `Bold`, `Italic`, `Underline`, and `LineBreak`, but emit framework-neutral core rich paragraphs, immutable brush snapshots, decoration snapshots, and logical text offsets.
- Add `ProTextPresenter`, derived from `Avalonia.Controls.Control`, with presenter-style text, inline, selection, caret, preedit, password, hit-test, and measurement APIs backed by the shared Pretext rich-content and layout snapshot pipeline.
- Add `ProTextBox`, derived from `Avalonia.Controls.Primitives.TemplatedControl`, with a copied Avalonia Fluent TextBox theme retargeted to host `ProTextPresenter` in the template.
- Keep all text rendering in the Pretext-powered path; do not add or use an internal Avalonia `TextBlock` fallback visual.
- Add `ProTextCache` as the Avalonia facade over `ProText.Core` shared prepared text cache management and diagnostics.
- Keep font descriptor building, layout snapshots, layout/prepared-content cache orchestration, editable text display composition, selection geometry, render font cache, and Skia drawing in `ProText.Core`.

For `ProText.Uno`:

- Add Uno `ProTextBlock`, derived from an appropriate WinUI/Uno control base, with WinUI/Uno dependency properties analogous to the Avalonia `ProTextBlock` API where practical.
- Add Uno-specific properties for `UseGlobalCache`, `UsePretextRendering`, `PretextWhiteSpace`, `PretextWordBreak`, and `PretextLineHeightMultiplier`.
- Implement measure, arrange, and drawing through `ProText.Core` layout snapshots and Uno Skia rendering surfaces.
- Add Uno `ProTextPresenter` with presenter-style text, inline, selection, caret, preedit, password, hit-test, and measurement APIs backed by shared core code.
- Add Uno `ProTextBox` as a lightweight TextBox-like ProTextPresenter-derived host.
- Keep all Uno text rendering in the Pretext-powered path; do not add Avalonia, WinUI, or Uno `TextBlock` fallback visuals.
- Add a Uno `ProTextCache` facade over `ProText.Core` shared prepared text cache management and diagnostics.
- Keep Uno adapters limited to framework type conversion, dependency-property plumbing, resource/theme integration, and renderer bridge code.

For `ProText.MAUI`:

- Add MAUI `ProTextBlock`, derived from an appropriate MAUI control base, with bindable properties analogous to the Avalonia and Uno `ProTextBlock` API where practical.
- Add MAUI-specific properties for `UseGlobalCache`, `UsePretextRendering`, `PretextWhiteSpace`, `PretextWordBreak`, and `PretextLineHeightMultiplier`.
- Implement measure, arrange, and drawing through `ProText.Core` layout snapshots and MAUI Skia rendering surfaces.
- Add MAUI `ProTextPresenter` with presenter-style text, formatted text, selection, caret, preedit, password, hit-test, and measurement APIs backed by shared core code.
- Add MAUI `ProTextBox` as a lightweight Editor-like ProTextPresenter-derived host.
- Keep all MAUI text rendering in the Pretext-powered path; do not add Avalonia, WinUI, Uno, MAUI `Label`, or MAUI `Editor` fallback visuals.
- Add a MAUI `ProTextCache` facade over `ProText.Core` shared prepared text cache management and diagnostics.
- Keep MAUI adapters limited to framework type conversion, bindable-property plumbing, resource/theme integration, and renderer bridge code.

## 3. Rendering

- Use `PretextLayout.PrepareRichInline` for cacheable text preparation.
- Use `PretextLayout.LayoutNextRichInlineLineRange` during measure/layout.
- Materialize only visible rich inline fragments for render.
- Draw through a thin Avalonia `ICustomDrawOperation` using `ISkiaSharpApiLeaseFeature`; delegate per-run `SKFont`, Skia shaders for gradients, manual letter spacing, and Skia decoration strokes to the core Skia renderer.
- Use the direct Pretext draw operation for text content in Skia-backed render contexts, including multilingual text through Pretext measurement and Skia font fallback.
- Draw Uno controls through a thin Uno Skia-backed operation; delegate per-run `SKFont`, Skia shaders for gradients, manual letter spacing, and Skia decoration strokes to the same core Skia renderer.
- Draw MAUI controls through a thin MAUI Skia-backed operation; delegate per-run `SKFont`, Skia shaders for gradients, manual letter spacing, and Skia decoration strokes to the same core Skia renderer.
- If a framework Skia surface is unavailable, skip drawing rather than falling back to framework `TextBlock`, `Label`, or `Editor`.

## 4. Sample App

- Build an Avalonia desktop sample with two synchronized columns: original `TextBlock` and new `ProTextBlock`.
- Include long paragraphs, resize-sensitive wrapping, varied fonts, alignment, padding, rich inline text, `ProTextPresenter`, and a dense repeated-text grid.
- Include controls for global cache, Pretext fast path, wrapping, max lines, font size, and text corpus.
- Use restrained product-tool styling: compact controls, clear comparison surface, and no decorative dashboard-card clutter.
- Add a Uno sample app with equivalent scenarios using Uno `TextBlock`, Uno `ProTextBlock`, Uno `ProTextPresenter`, Uno `TextBox`, and Uno `ProTextBox`.
- Include dense scrolling/sample content and shared-cache diagnostics in the Uno sample.
- Add a MAUI sample app with equivalent scenarios using MAUI `Label`, MAUI formatted text, MAUI `ProTextBlock`, MAUI `ProTextPresenter`, MAUI `Editor`, and MAUI `ProTextBox`.
- Include dense scrolling/sample content and shared-cache diagnostics in the MAUI sample.
- Keep sample comparison content visually fair across framework baseline controls and ProText controls.

## 5. Tests

- Add unit tests for cache reuse, cache bypass, property invalidation, rich-path eligibility, multilingual Pretext rendering, and the no-Avalonia-fallback invariant.
- Add core tests for `ProTextLayoutCache`, render-only style remapping, `ProTextSelectionGeometryCache`, `ProTextEditableText`, and culture-invariant fingerprints.
- Add headless render tests that show `ProTextBlock` and `TextBlock` in the same window and capture a frame.
- Add measurement tests for wrapping, no-wrap, max lines, and padding.
- Add headless and layout tests for `ProTextPresenter` measure, caret bounds, caret movement, hit testing, selection rendering, password masking, preedit text, and inline rendering.
- Add headless template and input tests for `ProTextBox` to ensure the copied Fluent theme creates `ProTextPresenter`, mouse/keyboard selection works, and TextBox-compatible API state/events remain synchronized.
- Add Uno tests for dependency-property defaults, property invalidation, measure/arrange, rich rendering, cache reuse/bypass, multilingual Pretext-path behavior, and the no-framework-TextBlock-fallback invariant.
- Add Uno presenter tests for measure, caret bounds, caret movement, hit testing, selection rendering, password masking, preedit text, and inline rendering using Uno-supported Skia/headless or runtime test lanes.
- Add Uno API and editing-helper tests for `ProTextBox` to ensure editable text display remains on the ProText path.
- Add MAUI tests for bindable-property defaults, property invalidation, measure/arrange, formatted rendering, cache reuse/bypass, multilingual Pretext-path behavior, and the no-framework-Label-or-Editor-fallback invariant.
- Add MAUI presenter tests for measure, caret bounds, caret movement, hit testing, selection rendering, password masking, preedit text, and formatted rendering using MAUI-supported Skia/headless or runtime test lanes.
- Add MAUI API and editing-helper tests for `ProTextBox` to ensure editable text display remains on the ProText path.

## 6. Benchmarks

- Add BenchmarkDotNet benchmarks for:
  - cold Pretext preparation
  - global cache hit preparation
  - wrapping layout across repeated widths
  - Avalonia `TextBlock` measure path
  - `ProTextBlock` measure path
  - rich inline and feature-heavy `TextBlock` versus `ProTextBlock` measure paths
  - headless render frame comparison
- Add core benchmarks for layout cache cold/hit/two-width toggles, editable text composition, selection geometry cache hit/miss, and Skia renderer solid/gradient/selection frames.
- Add dedicated inline benchmark project for `TextBlock`, `ProTextBlock`, and `ProTextPresenter` inline measurement.
- Add dedicated presenter benchmark project for presenter measurement, caret bounds, hit testing, selection, and render capture.
- Add dedicated TextBox benchmark project for Avalonia `TextBox` versus `ProTextBox` measurement and headless render capture, including selected-text state.
- Add a dedicated Uno benchmark project for Uno `TextBlock` versus Uno `ProTextBlock`, Uno `TextBox` versus Uno `ProTextBox`, global/local cache paths, inline measurement, presenter operations, and Uno Skia render capture.
- Add a dedicated MAUI benchmark project for MAUI `Label` versus MAUI `ProTextBlock`, MAUI `Editor` versus MAUI `ProTextBox`, global/local cache paths, formatted text measurement, presenter operations, and MAUI Skia render capture.
- Keep benchmark input corpora realistic: short label, paragraph, and large repeated body text.

## 7. Verification

- Restore packages.
- Build the solution.
- Run tests.
- Run a smoke benchmark or at least verify the benchmark project builds.
- Verify benchmark discovery for the base, inline, presenter, and TextBox benchmark projects sequentially or after a Release prebuild to avoid shared `obj/Release` file locks.
- Run and compare the TextBox benchmark project when changing `ProTextBox` or its theme.
- Start the sample app if the environment can launch an Avalonia desktop process; otherwise report the command.
- Build and test the Uno package when `src/ProText.Uno` or shared core behavior changes.
- Run the Uno sample app on an available Uno target head when the environment supports it; otherwise report the command.
- Verify Uno benchmark discovery for the Uno benchmark project after a Release prebuild.
- Build and test the MAUI package when `src/ProText.MAUI` or shared core behavior changes.
- Run the MAUI sample app on an available MAUI target head when the environment supports it; otherwise report the command.
- Verify MAUI benchmark discovery for the MAUI benchmark project after a Release prebuild.
