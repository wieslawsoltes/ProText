using System.Runtime.CompilerServices;
using SkiaSharp;

namespace ProText.Core;

public readonly record struct ProTextSkiaRenderOptions(
    ProTextRect ContentBounds,
    ProTextTextAlignment TextAlignment,
    ProTextFlowDirection FlowDirection,
    double InheritedOpacity,
    ProTextBrush? SelectionForeground = null,
    int SelectionStart = 0,
    int SelectionEnd = 0,
    ProTextBrush? SelectionBackground = null,
    IReadOnlyList<ProTextSelectionRect>? SelectionRects = null);

/// <summary>
/// Draws prepared ProText layout snapshots to a Skia canvas without any UI-framework dependency.
/// </summary>
public static class ProTextSkiaRenderer
{
    private static readonly ConditionalWeakTable<ProTextLayoutSnapshot, CachedPicture> s_pictureCache = new();

    public static void Render(SKCanvas canvas, ProTextLayoutSnapshot snapshot, ProTextSkiaRenderOptions options)
    {
        if (snapshot.LineCount == 0)
        {
            return;
        }

        var contentClip = ToSkRect(options.ContentBounds);
        var cachedPicture = s_pictureCache.GetOrCreateValue(snapshot);
        var key = new PictureKey(
            options.ContentBounds,
            options.TextAlignment,
            options.FlowDirection,
            options.SelectionBackground?.Fingerprint,
            options.SelectionForeground?.Fingerprint,
            GetSelectionRectsFingerprint(options.SelectionRects),
            options.SelectionStart,
            options.SelectionEnd,
            options.InheritedOpacity);
        var picture = cachedPicture.GetOrCreate(key, snapshot, options, contentClip);
        var saveCount = canvas.Save();

        try
        {
            canvas.ClipRect(contentClip);
            canvas.DrawPicture(picture);
        }
        finally
        {
            canvas.RestoreToCount(saveCount);
        }
    }

    private static SKPicture RecordPicture(ProTextLayoutSnapshot snapshot, ProTextSkiaRenderOptions options, SKRect contentClip)
    {
        using var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(contentClip);

        DrawTextContent(canvas, snapshot, options, contentClip);

        return recorder.EndRecording();
    }

    private static void DrawTextContent(SKCanvas canvas, ProTextLayoutSnapshot snapshot, ProTextSkiaRenderOptions options, SKRect contentClip)
    {
        using var paintCache = new PaintCache(contentClip, options.InheritedOpacity);
        var selectionRects = options.SelectionRects ?? [];

        if (options.SelectionBackground is not null && selectionRects.Count > 0)
        {
            using var selectionBackgroundPaint = CreatePaint(options.SelectionBackground, contentClip, options.InheritedOpacity);

            foreach (var rect in selectionRects)
            {
                canvas.DrawRect(ToSkRect(rect.Bounds), selectionBackgroundPaint);
            }
        }

        using var selectionPaint = options.SelectionForeground is null ? null : CreatePaint(options.SelectionForeground, contentClip, options.InheritedOpacity);

        for (var lineIndex = 0; lineIndex < snapshot.Lines.Count; lineIndex++)
        {
            var line = snapshot.Lines[lineIndex];

            if (line.Fragments.Count == 0)
            {
                continue;
            }

            var baseline = GetBaseline(snapshot, options.ContentBounds, line, lineIndex);
            var alignedX = (float)(options.ContentBounds.X + ProTextLayoutServices.GetAlignedX(options.ContentBounds.Width, line, options.TextAlignment, options.FlowDirection));
            DrawLine(canvas, line, alignedX, baseline, paintCache, selectionPaint, options);
        }
    }

    private static float GetBaseline(ProTextLayoutSnapshot snapshot, ProTextRect contentBounds, ProTextLayoutLine line, int lineIndex)
    {
        var style = line.Fragments.Count > 0 ? line.Fragments[0].Style : null;

        if (style is null)
        {
            return (float)(contentBounds.Y + lineIndex * snapshot.LineHeight);
        }

        using var renderFontLease = ProTextRenderFontCache.Get(style);
        var font = renderFontLease.Font.Font;
        var metrics = font.Metrics;
        var textHeight = metrics.Descent - metrics.Ascent;
        var baselineOffset = -metrics.Ascent + ((float)snapshot.LineHeight - textHeight) / 2f;

        return (float)contentBounds.Y + baselineOffset + (float)(lineIndex * snapshot.LineHeight);
    }

    private static SKPaint CreatePaint(ProTextBrush? brush, SKRect shaderBounds, double inheritedOpacity)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
        };

        var opacity = (brush?.Opacity ?? 1) * inheritedOpacity;

        if (brush is null)
        {
            paint.Color = ToSkColor(ProTextColor.Black, opacity);
            return paint;
        }

        if (brush is ProTextSolidBrush solid)
        {
            paint.Color = ToSkColor(solid.Color, opacity);
            return paint;
        }

        var shader = CreateShader(brush, shaderBounds, opacity);

        if (shader is not null)
        {
            paint.Shader = shader;
            return paint;
        }

        paint.Color = ToSkColor(ProTextColor.Black, opacity);
        return paint;
    }

    private static SKShader? CreateShader(ProTextBrush brush, SKRect bounds, double opacity)
    {
        if (brush is ProTextLinearGradientBrush linear)
        {
            var start = linear.StartPoint.ToPixels(FromSkRect(bounds));
            var end = linear.EndPoint.ToPixels(FromSkRect(bounds));
            var (colors, positions) = GetGradientStops(linear.GradientStops, opacity);

            return SKShader.CreateLinearGradient(
                new SKPoint((float)start.X, (float)start.Y),
                new SKPoint((float)end.X, (float)end.Y),
                colors,
                positions,
                ToTileMode(linear.SpreadMethod));
        }

        if (brush is ProTextRadialGradientBrush radial)
        {
            var rect = FromSkRect(bounds);
            var center = radial.Center.ToPixels(rect);
            var radiusX = radial.RadiusX.ToValue(rect.Width);
            var radiusY = radial.RadiusY.ToValue(rect.Height);
            var radius = Math.Max(1, Math.Max(radiusX, radiusY));
            var (colors, positions) = GetGradientStops(radial.GradientStops, opacity);

            return SKShader.CreateRadialGradient(
                new SKPoint((float)center.X, (float)center.Y),
                (float)radius,
                colors,
                positions,
                ToTileMode(radial.SpreadMethod));
        }

        if (brush is ProTextConicGradientBrush conic)
        {
            var rect = FromSkRect(bounds);
            var center = conic.Center.ToPixels(rect);
            var (colors, positions) = GetGradientStops(conic.GradientStops, opacity);
            var start = (float)conic.Angle;

            return SKShader.CreateSweepGradient(
                new SKPoint((float)center.X, (float)center.Y),
                colors,
                positions,
                SKShaderTileMode.Clamp,
                start,
                start + 360);
        }

        return null;
    }

    private static (SKColor[] Colors, float[]? Positions) GetGradientStops(IReadOnlyList<ProTextGradientStop> gradientStops, double opacity)
    {
        if (gradientStops.Count == 0)
        {
            return ([ToSkColor(ProTextColor.Transparent, opacity), ToSkColor(ProTextColor.Transparent, opacity)], [0f, 1f]);
        }

        var colors = new SKColor[gradientStops.Count];
        var positions = new float[gradientStops.Count];

        for (var i = 0; i < gradientStops.Count; i++)
        {
            var stop = gradientStops[i];
            colors[i] = ToSkColor(stop.Color, opacity);
            positions[i] = (float)Math.Clamp(stop.Offset, 0, 1);
        }

        return (colors, positions);
    }

    private static void DrawLine(
        SKCanvas canvas,
        ProTextLayoutLine line,
        float alignedX,
        float baseline,
        PaintCache paintCache,
        SKPaint? selectionPaint,
        ProTextSkiaRenderOptions options)
    {
        foreach (var fragment in line.Fragments)
        {
            if (fragment.Text.Length == 0)
            {
                continue;
            }

            using var renderFontLease = ProTextRenderFontCache.Get(fragment.Style);
            var renderFont = renderFontLease.Font;
            var paint = paintCache.Get(fragment.Style.Foreground);
            var x = alignedX + (float)fragment.X;

            DrawFragment(canvas, fragment, x, baseline, renderFont, paint, selectionPaint, options);
            DrawDecorations(canvas, fragment, x, baseline, renderFont.Font, paintCache.ShaderBounds, paintCache.InheritedOpacity);
        }
    }

    private static void DrawFragment(
        SKCanvas canvas,
        ProTextLayoutFragment fragment,
        float x,
        float baseline,
        ProTextRenderFont renderFont,
        SKPaint paint,
        SKPaint? selectionPaint,
        ProTextSkiaRenderOptions options)
    {
        if (selectionPaint is null || !TryGetSelectedTextRange(fragment, options, out var selectedStart, out var selectedEnd))
        {
            DrawText(canvas, fragment.Text, x, baseline, fragment.Style, renderFont.Family, renderFont.FontStyle, renderFont.Typeface, renderFont.Font, paint);
            return;
        }

        var localStart = selectedStart - fragment.TextStart;
        var localEnd = selectedEnd - fragment.TextStart;
        var currentX = x;

        if (localStart > 0)
        {
            currentX += DrawTextAndMeasure(canvas, fragment.Text[..localStart], currentX, baseline, fragment.Style, renderFont.Family, renderFont.FontStyle, renderFont.Typeface, renderFont.Font, paint);
        }

        if (localEnd < fragment.Text.Length)
        {
            currentX += DrawTextAndMeasure(canvas, fragment.Text[localStart..localEnd], currentX, baseline, fragment.Style, renderFont.Family, renderFont.FontStyle, renderFont.Typeface, renderFont.Font, selectionPaint);
            DrawText(canvas, fragment.Text[localEnd..], currentX, baseline, fragment.Style, renderFont.Family, renderFont.FontStyle, renderFont.Typeface, renderFont.Font, paint);
        }
        else
        {
            DrawText(canvas, fragment.Text[localStart..localEnd], currentX, baseline, fragment.Style, renderFont.Family, renderFont.FontStyle, renderFont.Typeface, renderFont.Font, selectionPaint);
        }
    }

    private static bool TryGetSelectedTextRange(ProTextLayoutFragment fragment, ProTextSkiaRenderOptions options, out int selectedStart, out int selectedEnd)
    {
        selectedStart = 0;
        selectedEnd = 0;

        if (options.SelectionForeground is null || options.SelectionStart == options.SelectionEnd || options.SelectionEnd <= fragment.TextStart || options.SelectionStart >= fragment.TextEnd)
        {
            return false;
        }

        selectedStart = Math.Max(options.SelectionStart, fragment.TextStart);
        selectedEnd = Math.Min(options.SelectionEnd, fragment.TextEnd);
        return selectedStart < selectedEnd;
    }

    private static void DrawText(
        SKCanvas canvas,
        string text,
        float x,
        float baseline,
        ProTextRichStyle style,
        string family,
        SKFontStyle fontStyle,
        SKTypeface typeface,
        SKFont font,
        SKPaint paint)
    {
        if (style.LetterSpacing.Equals(0d) && typeface.ContainsGlyphs(text))
        {
            canvas.DrawText(text, x, baseline, font, paint);
            return;
        }

        foreach (var grapheme in ProTextFontResolver.EnumerateGraphemes(text))
        {
            using var resolved = ProTextFontResolver.ResolveTypeface(typeface, family, fontStyle, grapheme);

            if (ReferenceEquals(resolved.Typeface, typeface))
            {
                canvas.DrawText(grapheme, x, baseline, font, paint);
                x += font.MeasureText(grapheme) + (float)style.LetterSpacing;
            }
            else
            {
                using var fallbackFont = ProTextFontResolver.CreateFont(resolved.Typeface, style.FontSize);
                canvas.DrawText(grapheme, x, baseline, fallbackFont, paint);
                x += fallbackFont.MeasureText(grapheme) + (float)style.LetterSpacing;
            }
        }
    }

    private static float DrawTextAndMeasure(
        SKCanvas canvas,
        string text,
        float x,
        float baseline,
        ProTextRichStyle style,
        string family,
        SKFontStyle fontStyle,
        SKTypeface typeface,
        SKFont font,
        SKPaint paint)
    {
        if (style.LetterSpacing.Equals(0d) && typeface.ContainsGlyphs(text))
        {
            canvas.DrawText(text, x, baseline, font, paint);
            return font.MeasureText(text);
        }

        var startX = x;

        foreach (var grapheme in ProTextFontResolver.EnumerateGraphemes(text))
        {
            using var resolved = ProTextFontResolver.ResolveTypeface(typeface, family, fontStyle, grapheme);

            if (ReferenceEquals(resolved.Typeface, typeface))
            {
                canvas.DrawText(grapheme, x, baseline, font, paint);
                x += font.MeasureText(grapheme) + (float)style.LetterSpacing;
            }
            else
            {
                using var fallbackFont = ProTextFontResolver.CreateFont(resolved.Typeface, style.FontSize);
                canvas.DrawText(grapheme, x, baseline, fallbackFont, paint);
                x += fallbackFont.MeasureText(grapheme) + (float)style.LetterSpacing;
            }
        }

        return x - startX;
    }

    private static void DrawDecorations(
        SKCanvas canvas,
        ProTextLayoutFragment fragment,
        float x,
        float baseline,
        SKFont font,
        SKRect shaderBounds,
        double inheritedOpacity)
    {
        var decorations = fragment.Style.TextDecorations;

        if (decorations is null || decorations.Count == 0)
        {
            return;
        }

        var metrics = font.Metrics;

        foreach (var decoration in decorations)
        {
            using var paint = CreateDecorationPaint(decoration, fragment.Style.Foreground, shaderBounds, inheritedOpacity, metrics, fragment.Style.FontSize);
            var y = GetDecorationY(decoration, baseline, metrics, fragment.Style.FontSize);
            canvas.DrawLine(x, y, x + (float)fragment.Width, y, paint);
        }
    }

    private static SKPaint CreateDecorationPaint(
        ProTextDecoration decoration,
        ProTextBrush? defaultBrush,
        SKRect shaderBounds,
        double inheritedOpacity,
        SKFontMetrics metrics,
        double fontSize)
    {
        var paint = CreatePaint(decoration.Stroke ?? defaultBrush, shaderBounds, inheritedOpacity);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = GetDecorationThickness(decoration, metrics, fontSize);
        paint.StrokeCap = decoration.StrokeLineCap switch
        {
            ProTextPenLineCap.Round => SKStrokeCap.Round,
            ProTextPenLineCap.Square => SKStrokeCap.Square,
            _ => SKStrokeCap.Butt,
        };

        if (decoration.StrokeDashArray is { Count: > 0 } dashArray)
        {
            var intervals = new float[dashArray.Count];

            for (var i = 0; i < dashArray.Count; i++)
            {
                intervals[i] = (float)Math.Max(0, dashArray[i] * paint.StrokeWidth);
            }

            paint.PathEffect = SKPathEffect.CreateDash(intervals, (float)decoration.StrokeDashOffset);
        }

        return paint;
    }

    private static float GetDecorationY(ProTextDecoration decoration, float baseline, SKFontMetrics metrics, double fontSize)
    {
        var offset = decoration.Location switch
        {
            ProTextDecorationLocation.Overline => metrics.Ascent,
            ProTextDecorationLocation.Strikethrough => metrics.StrikeoutPosition ?? metrics.Ascent / 2,
            ProTextDecorationLocation.Underline => metrics.UnderlinePosition ?? (float)(fontSize * 0.08),
            _ => 0,
        };

        offset += decoration.StrokeOffsetUnit switch
        {
            ProTextDecorationUnit.FontRenderingEmSize => (float)(decoration.StrokeOffset * fontSize),
            ProTextDecorationUnit.Pixel => (float)decoration.StrokeOffset,
            _ => 0,
        };

        return baseline + offset;
    }

    private static float GetDecorationThickness(ProTextDecoration decoration, SKFontMetrics metrics, double fontSize)
    {
        return decoration.StrokeThicknessUnit switch
        {
            ProTextDecorationUnit.FontRecommended when decoration.Location == ProTextDecorationLocation.Underline => metrics.UnderlineThickness ?? Math.Max(1, (float)fontSize / 14f),
            ProTextDecorationUnit.FontRecommended when decoration.Location == ProTextDecorationLocation.Strikethrough => metrics.StrikeoutThickness ?? Math.Max(1, (float)fontSize / 14f),
            ProTextDecorationUnit.FontRenderingEmSize => (float)Math.Max(0, decoration.StrokeThickness * fontSize),
            _ => (float)Math.Max(0, decoration.StrokeThickness),
        };
    }

    private static SKRect ToSkRect(ProTextRect bounds)
    {
        return new SKRect(
            (float)bounds.X,
            (float)bounds.Y,
            (float)bounds.Right,
            (float)bounds.Bottom);
    }

    private static ProTextRect FromSkRect(SKRect bounds)
    {
        return new ProTextRect(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
    }

    private static SKShaderTileMode ToTileMode(ProTextGradientSpreadMethod spreadMethod)
    {
        return spreadMethod switch
        {
            ProTextGradientSpreadMethod.Reflect => SKShaderTileMode.Mirror,
            ProTextGradientSpreadMethod.Repeat => SKShaderTileMode.Repeat,
            _ => SKShaderTileMode.Clamp,
        };
    }

    private static SKColor ToSkColor(ProTextColor color, double opacity)
    {
        var alpha = ClampToByte(color.A * opacity);
        return new SKColor(color.R, color.G, color.B, alpha);
    }

    private static byte ClampToByte(double value)
    {
        if (double.IsNaN(value) || value <= 0)
        {
            return 0;
        }

        return value >= 255 ? byte.MaxValue : (byte)Math.Round(value);
    }

    private static int GetSelectionRectsFingerprint(IReadOnlyList<ProTextSelectionRect>? selectionRects)
    {
        if (selectionRects is null || selectionRects.Count == 0)
        {
            return 0;
        }

        var hash = new HashCode();
        hash.Add(selectionRects.Count);

        foreach (var rect in selectionRects)
        {
            hash.Add(rect.LineIndex);
            hash.Add(rect.Bounds);
        }

        return hash.ToHashCode();
    }

    private sealed class PaintCache : IDisposable
    {
        private SKPaint? _paint;
        private ProTextBrush? _brush;

        public PaintCache(SKRect shaderBounds, double inheritedOpacity)
        {
            ShaderBounds = shaderBounds;
            InheritedOpacity = inheritedOpacity;
        }

        public SKRect ShaderBounds { get; }

        public double InheritedOpacity { get; }

        public SKPaint Get(ProTextBrush? brush)
        {
            if (_paint is not null && Equals(_brush, brush))
            {
                return _paint;
            }

            _paint?.Dispose();
            _paint = CreatePaint(brush, ShaderBounds, InheritedOpacity);
            _brush = brush;
            return _paint;
        }

        public void Dispose()
        {
            _paint?.Dispose();
        }
    }

    private readonly record struct PictureKey(
        ProTextRect ContentBounds,
        ProTextTextAlignment TextAlignment,
        ProTextFlowDirection FlowDirection,
        string? SelectionBackgroundFingerprint,
        string? SelectionForegroundFingerprint,
        int SelectionRectsFingerprint,
        int SelectionStart,
        int SelectionEnd,
        double InheritedOpacity);

    private sealed class CachedPicture
    {
        private PictureKey _key;
        private SKPicture? _picture;

        public SKPicture GetOrCreate(PictureKey key, ProTextLayoutSnapshot snapshot, ProTextSkiaRenderOptions options, SKRect contentClip)
        {
            if (_picture is not null && _key.Equals(key))
            {
                return _picture;
            }

            _picture?.Dispose();
            _key = key;
            _picture = RecordPicture(snapshot, options, contentClip);
            return _picture;
        }
    }
}
