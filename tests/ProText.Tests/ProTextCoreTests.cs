using Pretext;
using ProText.Core;
using SkiaSharp;

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

    private static ProTextRichStyle CreateStyle(double fontSize, double letterSpacing)
    {
        return new ProTextRichStyle(
            ProTextFontDescriptor.DefaultFontFamily,
            fontSize,
            ProTextFontStyle.Normal,
            400,
            5,
            new ProTextSolidBrush(ProTextColor.FromRgb(0, 0, 0), 1),
            textDecorations: [],
            fontFeaturesFingerprint: "none",
            letterSpacing);
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
