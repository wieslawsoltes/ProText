# ProText Implementation Plan

## 1. Repository Setup

- Create a solution with library, sample, test, and benchmark projects.
- Target `net10.0` for the library and developer tooling in this workspace.
- Use central package management for Avalonia, PretextSharp, SkiaSharp, xUnit, Avalonia headless testing, and BenchmarkDotNet.
- Enable nullable reference types and implicit usings.
- Maintain `src/ProText.Core` as the reusable Pretext/Skia text engine with no Avalonia dependency.
- Maintain `src/ProText` as the Avalonia controls, inline/style/font adapter, theme, and custom draw operation host.

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
- Keep font descriptor building, layout snapshots, selection geometry, render font cache, and Skia drawing in `ProText.Core`.

## 3. Rendering

- Use `PretextLayout.PrepareRichInline` for cacheable text preparation.
- Use `PretextLayout.LayoutNextRichInlineLineRange` during measure/layout.
- Materialize only visible rich inline fragments for render.
- Draw through a thin Avalonia `ICustomDrawOperation` using `ISkiaSharpApiLeaseFeature`; delegate per-run `SKFont`, Skia shaders for gradients, manual letter spacing, and Skia decoration strokes to the core Skia renderer.
- Use the direct Pretext draw operation for text content in Skia-backed render contexts, including multilingual text through Pretext measurement and Skia font fallback.

## 4. Sample App

- Build an Avalonia desktop sample with two synchronized columns: original `TextBlock` and new `ProTextBlock`.
- Include long paragraphs, resize-sensitive wrapping, varied fonts, alignment, padding, rich inline text, `ProTextPresenter`, and a dense repeated-text grid.
- Include controls for global cache, Pretext fast path, wrapping, max lines, font size, and text corpus.
- Use restrained product-tool styling: compact controls, clear comparison surface, and no decorative dashboard-card clutter.

## 5. Tests

- Add unit tests for cache reuse, cache bypass, property invalidation, rich-path eligibility, multilingual Pretext rendering, and the no-Avalonia-fallback invariant.
- Add headless render tests that show `ProTextBlock` and `TextBlock` in the same window and capture a frame.
- Add measurement tests for wrapping, no-wrap, max lines, and padding.
- Add headless and layout tests for `ProTextPresenter` measure, caret bounds, caret movement, hit testing, selection rendering, password masking, preedit text, and inline rendering.
- Add headless template and input tests for `ProTextBox` to ensure the copied Fluent theme creates `ProTextPresenter`, mouse/keyboard selection works, and TextBox-compatible API state/events remain synchronized.

## 6. Benchmarks

- Add BenchmarkDotNet benchmarks for:
  - cold Pretext preparation
  - global cache hit preparation
  - wrapping layout across repeated widths
  - Avalonia `TextBlock` measure path
  - `ProTextBlock` measure path
  - rich inline and feature-heavy `TextBlock` versus `ProTextBlock` measure paths
  - headless render frame comparison
- Add dedicated inline benchmark project for `TextBlock`, `ProTextBlock`, and `ProTextPresenter` inline measurement.
- Add dedicated presenter benchmark project for presenter measurement, caret bounds, hit testing, selection, and render capture.
- Add dedicated TextBox benchmark project for Avalonia `TextBox` versus `ProTextBox` measurement and headless render capture, including selected-text state.
- Keep benchmark input corpora realistic: short label, paragraph, and large repeated body text.

## 7. Verification

- Restore packages.
- Build the solution.
- Run tests.
- Run a smoke benchmark or at least verify the benchmark project builds.
- Verify benchmark discovery for the base, inline, and presenter benchmark projects.
- Run and compare the TextBox benchmark project when changing `ProTextBox` or its theme.
- Start the sample app if the environment can launch an Avalonia desktop process; otherwise report the command.
