using Pretext;
using ProText.Core;
using SkiaSharp;
using System.Globalization;

namespace ProText.Tests;

public sealed class ProTextCoreTests
{
    [Fact]
    public void Core_assembly_does_not_reference_avalonia()
    {
        var references = typeof(ProTextLayoutSnapshot)
            .Assembly
            .GetReferencedAssemblies()
            .Select(static assembly => assembly.Name)
            .ToArray();

        Assert.DoesNotContain("Avalonia.Base", references);
        Assert.DoesNotContain("Avalonia.Controls", references);
        Assert.DoesNotContain("Avalonia.Skia", references);
    }

    [Fact]
    public void Core_cache_bound_applies_to_combined_text_and_rich_entries()
    {
        var previousMaxEntryCount = ProTextCoreCache.MaxEntryCount;
        var style = new ProTextRichStyle(
            ProTextFontDescriptor.DefaultFontFamily,
            12,
            ProTextFontStyle.Normal,
            400,
            5,
            foreground: null,
            textDecorations: [],
            fontFeaturesFingerprint: "none",
            letterSpacing: 0);

        try
        {
            ProTextCoreCache.Clear();
            ProTextCoreCache.MaxEntryCount = 1;

            ProTextCoreCache.GetOrPrepare(new ProTextCacheKey(
                "plain cache entry",
                style.FontDescriptor,
                WhiteSpaceMode.Normal,
                WordBreakMode.Normal));

            var builder = new ProTextRichContentBuilder(style);
            builder.AppendText("rich cache entry", style);
            var content = builder.Build();
            var paragraph = content.Paragraphs[0];
            ProTextCoreCache.GetOrPrepareRich(new ProTextRichCacheKey(paragraph.LayoutFingerprint), paragraph.CreateInlineItems());

            Assert.True(ProTextCoreCache.GetSnapshot().Count <= 1);
        }
        finally
        {
            ProTextCoreCache.MaxEntryCount = previousMaxEntryCount;
            ProTextCoreCache.Clear();
        }
    }

    [Fact]
    public void Core_brush_snapshots_copy_mutable_inputs()
    {
        var stops = new List<ProTextGradientStop>
        {
            new(ProTextColor.FromRgb(255, 0, 0), 0),
        };

        var brush = new ProTextLinearGradientBrush(
            stops,
            opacity: 1,
            ProTextGradientSpreadMethod.Pad,
            new ProTextRelativePoint(0, 0, ProTextRelativeUnit.Relative),
            new ProTextRelativePoint(1, 0, ProTextRelativeUnit.Relative));
        var fingerprint = brush.Fingerprint;

        stops.Add(new ProTextGradientStop(ProTextColor.FromRgb(0, 0, 255), 1));

        Assert.Single(brush.GradientStops);
        Assert.Equal(fingerprint, brush.Fingerprint);
    }

    [Fact]
    public void Core_typeface_resolver_can_return_unowned_typefaces()
    {
        var typeface = SKTypeface.CreateDefault();

        try
        {
            ProTextFontResolver.SetTypefaceResolver(new SharedTypefaceResolver(typeface));

            using (var resolved = ProTextFontResolver.ResolveTypeface(
                ProTextFontDescriptor.DefaultFontFamily,
                weight: 400,
                stretch: 5,
                ProTextFontStyle.Normal))
            {
                Assert.Same(typeface, resolved.Typeface);
            }

            Assert.True(typeface.ContainsGlyphs("A"));
        }
        finally
        {
            ProTextFontResolver.SetTypefaceResolver(null);
            typeface.Dispose();
        }
    }

    [Fact]
    public void Core_text_measurer_does_not_dispose_unowned_typefaces()
    {
        var typeface = SKTypeface.CreateDefault();
        var font = ProTextFontDescriptor.Create(
            ProTextFontDescriptor.DefaultFontFamily,
            12,
            ProTextFontStyle.Normal,
            400);

        try
        {
            ProTextFontResolver.SetTypefaceResolver(new SharedTypefaceResolver(typeface));

            using (var measurer = new ProTextTextMeasurerFactory().Create(font))
            {
                Assert.True(measurer.MeasureText("A") > 0);
            }

            Assert.True(typeface.ContainsGlyphs("A"));
        }
        finally
        {
            ProTextFontResolver.SetTypefaceResolver(null);
            ProTextCoreCache.Clear();
            typeface.Dispose();
        }
    }

    [Fact]
    public void Core_render_font_cache_eviction_does_not_dispose_unowned_typefaces()
    {
        var typeface = SKTypeface.CreateDefault();

        try
        {
            ProTextFontResolver.SetTypefaceResolver(new SharedTypefaceResolver(typeface));

            using var bitmap = new SKBitmap(64, 32);
            using var canvas = new SKCanvas(bitmap);

            for (var i = 0; i < 140; i++)
            {
                var style = CreateStyle(fontSize: 12 + i, letterSpacing: 0);
                var builder = new ProTextRichContentBuilder(style);
                builder.AppendText("A", style);
                var content = builder.Build();
                var prepared = new ProTextPreparedContent(content.Paragraphs
                    .Select(static paragraph => ProTextCoreCache.PrepareRichUncached(paragraph.CreateInlineItems()))
                    .ToArray());
                var snapshot = new ProTextLayoutSnapshot(
                    content,
                    prepared,
                    maxWidth: 64,
                    lineHeight: 16,
                    maxLines: 0,
                    ProTextWrapping.NoWrap,
                    ProTextTrimming.None);

                ProTextSkiaRenderer.Render(canvas, snapshot, new ProTextSkiaRenderOptions(
                    new ProTextRect(0, 0, 64, 32),
                    ProTextTextAlignment.Left,
                    ProTextFlowDirection.LeftToRight,
                    InheritedOpacity: 1));
            }

            ProTextCoreCache.Clear();

            Assert.True(typeface.ContainsGlyphs("A"));
        }
        finally
        {
            ProTextFontResolver.SetTypefaceResolver(null);
            ProTextCoreCache.Clear();
            typeface.Dispose();
        }
    }

    [Fact]
    public void Layout_cache_returns_same_snapshot_for_same_request()
    {
        ProTextCoreCache.Clear();
        var cache = new ProTextLayoutCache();
        var content = CreateContent("cache hit text", CreateStyle(fontSize: 12, letterSpacing: 0));
        var request = CreateLayoutRequest(maxWidth: 160);

        var first = cache.GetSnapshot(content, request);
        var second = cache.GetSnapshot(content, request);

        Assert.Same(first, second);
    }

    [Fact]
    public void Layout_cache_reuses_previous_width_snapshot_when_width_toggles()
    {
        ProTextCoreCache.Clear();
        var cache = new ProTextLayoutCache();
        var content = CreateContent("alpha beta gamma delta", CreateStyle(fontSize: 12, letterSpacing: 0));

        var narrow = cache.GetSnapshot(content, CreateLayoutRequest(maxWidth: 80));
        var wide = cache.GetSnapshot(content, CreateLayoutRequest(maxWidth: 180));
        var narrowAgain = cache.GetSnapshot(content, CreateLayoutRequest(maxWidth: 80));

        Assert.NotSame(narrow, wide);
        Assert.Same(narrow, narrowAgain);
    }

    [Fact]
    public void Layout_cache_render_only_changes_remap_styles_without_preparing_again()
    {
        ProTextCoreCache.Clear();
        var cache = new ProTextLayoutCache();
        var blackStyle = CreateStyle(fontSize: 12, letterSpacing: 0, ProTextColor.FromRgb(0, 0, 0));
        var redStyle = CreateStyle(fontSize: 12, letterSpacing: 0, ProTextColor.FromRgb(255, 0, 0));
        var blackContent = CreateContent("same layout", blackStyle);
        var redContent = CreateContent("same layout", redStyle);
        var request = CreateLayoutRequest(maxWidth: 200);

        var blackSnapshot = cache.GetSnapshot(blackContent, request);
        var counters = ProTextCoreCache.GetSnapshot();
        var redSnapshot = cache.GetSnapshot(redContent, request);
        var countersAfterRenderChange = ProTextCoreCache.GetSnapshot();

        Assert.NotSame(blackSnapshot, redSnapshot);
        Assert.Equal(blackSnapshot.Width, redSnapshot.Width);
        Assert.Equal(counters.Misses, countersAfterRenderChange.Misses);
        Assert.Equal(redStyle.Foreground?.Fingerprint, redSnapshot.Lines[0].Fragments[0].Style.Foreground?.Fingerprint);
    }

    [Fact]
    public void Layout_cache_local_prepared_content_does_not_increment_global_cache_counters()
    {
        ProTextCoreCache.Clear();
        var cache = new ProTextLayoutCache();
        var content = CreateContent("local cache text", CreateStyle(fontSize: 12, letterSpacing: 0));
        var before = ProTextCoreCache.GetSnapshot();

        var first = cache.GetSnapshot(content, CreateLayoutRequest(maxWidth: 160, useGlobalCache: false));
        var second = cache.GetSnapshot(content, CreateLayoutRequest(maxWidth: 160, useGlobalCache: false));
        var after = ProTextCoreCache.GetSnapshot();

        Assert.Same(first, second);
        Assert.Equal(before.Count, after.Count);
        Assert.Equal(before.Hits, after.Hits);
        Assert.Equal(before.Misses, after.Misses);
    }

    [Fact]
    public void Layout_cache_render_only_change_remaps_trimmed_ellipsis_style()
    {
        ProTextCoreCache.Clear();
        var cache = new ProTextLayoutCache();
        var blackStyle = CreateStyle(fontSize: 12, letterSpacing: 0, ProTextColor.FromRgb(0, 0, 0));
        var redStyle = CreateStyle(fontSize: 12, letterSpacing: 0, ProTextColor.FromRgb(255, 0, 0));
        var request = new ProTextLayoutRequest(
            MaxWidth: 16,
            LineHeight: 16,
            MaxLines: 0,
            ProTextWrapping.NoWrap,
            ProTextTrimming.CharacterEllipsis,
            UseGlobalCache: true);

        cache.GetSnapshot(CreateContent("abcdef", blackStyle), request);
        var redSnapshot = cache.GetSnapshot(CreateContent("abcdef", redStyle), request);
        var ellipsis = redSnapshot.Lines.Single().Fragments.Last();

        Assert.Equal("…", ellipsis.Text);
        Assert.Equal(redStyle.Foreground?.Fingerprint, ellipsis.Style.Foreground?.Fingerprint);
    }

    [Fact]
    public void Layout_snapshot_head_trimming_keeps_logical_suffix()
    {
        ProTextCoreCache.Clear();
        const string text = "abcdefghijklmnopqrstuvwxyz";
        var full = CreateNoWrapSnapshot(text, double.PositiveInfinity, ProTextTrimming.None);
        var snapshot = CreateNoWrapSnapshot(text, full.Width * 0.45, ProTextTrimming.HeadCharacterEllipsis);
        var trimmed = FlattenLineText(snapshot);

        Assert.StartsWith("…", trimmed);
        Assert.EndsWith("z", trimmed);
        Assert.NotEqual(text, trimmed);
        Assert.True(snapshot.Width <= full.Width * 0.45 || Math.Abs(snapshot.Width - full.Width * 0.45) < 0.01);
    }

    [Fact]
    public void Layout_snapshot_middle_trimming_keeps_logical_prefix_and_suffix()
    {
        ProTextCoreCache.Clear();
        const string text = "abcdefghijklmnopqrstuvwxyz";
        var full = CreateNoWrapSnapshot(text, double.PositiveInfinity, ProTextTrimming.None);
        var snapshot = CreateNoWrapSnapshot(text, full.Width * 0.45, ProTextTrimming.MiddleCharacterEllipsis);
        var trimmed = FlattenLineText(snapshot);

        Assert.StartsWith("a", trimmed);
        Assert.Contains("…", trimmed, StringComparison.Ordinal);
        Assert.EndsWith("z", trimmed);
        Assert.NotEqual(text, trimmed);
        Assert.False(trimmed.StartsWith("…", StringComparison.Ordinal));
        Assert.False(trimmed.EndsWith("…", StringComparison.Ordinal));
        Assert.True(snapshot.Width <= full.Width * 0.45 || Math.Abs(snapshot.Width - full.Width * 0.45) < 0.01);
    }

    [Fact]
    public void Selection_geometry_cache_reuses_reversed_selection()
    {
        var snapshot = CreateSnapshot("selection geometry", maxWidth: 200);
        var cache = new ProTextSelectionGeometryCache();

        var forward = cache.GetSelectionRects(
            snapshot,
            selectionStart: 0,
            selectionEnd: 9,
            boundsWidth: 200,
            ProTextTextAlignment.Left,
            ProTextFlowDirection.LeftToRight);
        var reversed = cache.GetSelectionRects(
            snapshot,
            selectionStart: 9,
            selectionEnd: 0,
            boundsWidth: 200,
            ProTextTextAlignment.Left,
            ProTextFlowDirection.LeftToRight);

        Assert.NotEmpty(forward);
        Assert.Same(forward, reversed);
    }

    [Fact]
    public void Selection_geometry_cache_invalidates_for_alignment_width_and_flow()
    {
        var snapshot = CreateSnapshot("selection geometry", maxWidth: 200);
        var cache = new ProTextSelectionGeometryCache();

        var left = cache.GetSelectionRects(snapshot, 0, 9, 200, ProTextTextAlignment.Left, ProTextFlowDirection.LeftToRight);
        var centered = cache.GetSelectionRects(snapshot, 0, 9, 200, ProTextTextAlignment.Center, ProTextFlowDirection.LeftToRight);
        var wider = cache.GetSelectionRects(snapshot, 0, 9, 260, ProTextTextAlignment.Center, ProTextFlowDirection.LeftToRight);
        var rtl = cache.GetSelectionRects(snapshot, 0, 9, 260, ProTextTextAlignment.Center, ProTextFlowDirection.RightToLeft);

        Assert.NotSame(left, centered);
        Assert.NotSame(centered, wider);
        Assert.NotSame(wider, rtl);
    }

    [Fact]
    public void Editable_text_masks_password_and_preedit_text()
    {
        var snapshot = ProTextEditableText.CreateSnapshot(new ProTextEditableTextOptions(
            "secret",
            CaretIndex: 3,
            PreeditText: "ime",
            PreeditTextCursorPosition: 2,
            PasswordChar: '*',
            RevealPassword: false));

        Assert.Equal("******", snapshot.DisplayText);
        Assert.Equal("***", snapshot.DisplayPreeditText);
        Assert.Equal(5, snapshot.EffectiveCaretIndex);
        Assert.Equal("secret", snapshot.SourceText);
    }

    [Fact]
    public void Editable_text_content_inserts_preedit_run_at_caret()
    {
        var baseStyle = CreateStyle(fontSize: 12, letterSpacing: 0, ProTextColor.FromRgb(0, 0, 0));
        var preeditStyle = CreateStyle(fontSize: 12, letterSpacing: 0, ProTextColor.FromRgb(255, 0, 0));

        var content = ProTextEditableText.CreateContent(
            new ProTextEditableTextOptions(
                "abcd",
                CaretIndex: 2,
                PreeditText: "XY",
                PreeditTextCursorPosition: null,
                PasswordChar: default,
                RevealPassword: false),
            baseStyle,
            preeditStyle);

        var runs = content.Paragraphs[0].Runs;
        Assert.Equal(["ab", "XY", "cd"], runs.Select(static run => run.Text).ToArray());
        Assert.Equal(preeditStyle.Foreground?.Fingerprint, runs[1].Style.Foreground?.Fingerprint);
        Assert.Equal(4, ProTextEditableText.GetEffectiveCaretIndex(new ProTextEditableTextOptions("abcd", 2, "XY", null, default, false)));
    }

    [Fact]
    public void Brush_fingerprints_are_culture_invariant_and_precise()
    {
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pl-PL");

            var brush = new ProTextLinearGradientBrush(
                [
                    new ProTextGradientStop(ProTextColor.FromRgb(255, 0, 0), 0.125),
                    new ProTextGradientStop(ProTextColor.FromRgb(0, 0, 255), 0.875)
                ],
                opacity: 0.33333333333333331,
                ProTextGradientSpreadMethod.Pad,
                new ProTextRelativePoint(0.125, 0.25, ProTextRelativeUnit.Relative),
                new ProTextRelativePoint(0.875, 1, ProTextRelativeUnit.Relative));

            Assert.Contains("0.3333333333333333", brush.Fingerprint, StringComparison.Ordinal);
            Assert.Contains("0.125", brush.Fingerprint, StringComparison.Ordinal);
            Assert.DoesNotContain("0,125", brush.Fingerprint, StringComparison.Ordinal);

            var descriptor = ProTextFontDescriptor.Create(
                ProTextFontDescriptor.DefaultFontFamily,
                size: 12.0004,
                ProTextFontStyle.Normal,
                weight: 400,
                stretch: 5,
                letterSpacing: 0.0004,
                fontFeaturesFingerprint: "none");

            Assert.Contains("12.0004px", descriptor, StringComparison.Ordinal);
            Assert.Contains("ptb-ls=0.0004", descriptor, StringComparison.Ordinal);
            Assert.DoesNotContain("12,0004px", descriptor, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private static ProTextRichStyle CreateStyle(double fontSize, double letterSpacing)
    {
        return CreateStyle(fontSize, letterSpacing, ProTextColor.FromRgb(0, 0, 0));
    }

    private static ProTextRichStyle CreateStyle(double fontSize, double letterSpacing, ProTextColor foreground)
    {
        return new ProTextRichStyle(
            ProTextFontDescriptor.DefaultFontFamily,
            fontSize,
            ProTextFontStyle.Normal,
            400,
            5,
            new ProTextSolidBrush(foreground, 1),
            textDecorations: [],
            fontFeaturesFingerprint: "none",
            letterSpacing);
    }

    private static ProTextRichContent CreateContent(string text, ProTextRichStyle style)
    {
        var builder = new ProTextRichContentBuilder(style);
        builder.AppendText(text, style);
        return builder.Build();
    }

    private static ProTextLayoutSnapshot CreateSnapshot(string text, double maxWidth)
    {
        var content = CreateContent(text, CreateStyle(fontSize: 12, letterSpacing: 0));
        return new ProTextLayoutCache().GetSnapshot(content, CreateLayoutRequest(maxWidth));
    }

    private static ProTextLayoutSnapshot CreateNoWrapSnapshot(string text, double maxWidth, ProTextTrimming trimming)
    {
        var content = CreateContent(text, CreateStyle(fontSize: 12, letterSpacing: 0));
        return new ProTextLayoutCache().GetSnapshot(content, new ProTextLayoutRequest(
            maxWidth,
            LineHeight: 16,
            MaxLines: 0,
            ProTextWrapping.NoWrap,
            trimming,
            UseGlobalCache: true));
    }

    private static string FlattenLineText(ProTextLayoutSnapshot snapshot)
    {
        return string.Concat(snapshot.Lines.Single().Fragments.Select(static fragment => fragment.Text));
    }

    private static ProTextLayoutRequest CreateLayoutRequest(double maxWidth, bool useGlobalCache = true)
    {
        return new ProTextLayoutRequest(
            maxWidth,
            LineHeight: 16,
            MaxLines: 0,
            ProTextWrapping.Wrap,
            ProTextTrimming.None,
            useGlobalCache);
    }

    private sealed class SharedTypefaceResolver : IProTextTypefaceResolver
    {
        private readonly SKTypeface _typeface;

        public SharedTypefaceResolver(SKTypeface typeface)
        {
            _typeface = typeface;
        }

        public bool TryResolveTypeface(ProTextFontIdentity font, out ProTextResolvedTypeface typeface)
        {
            typeface = new ProTextResolvedTypeface(_typeface, ProTextFontSimulations.None, OwnsTypeface: false);
            return true;
        }
    }
}
