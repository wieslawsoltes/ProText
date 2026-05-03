using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace ProTextBlock.Internal;

internal sealed class ProTextBlockDrawOperation : ICustomDrawOperation
{
    private readonly ProTextLayoutSnapshot _snapshot;
    private readonly Rect _contentBounds;
    private readonly TextAlignment _textAlignment;
    private readonly FlowDirection _flowDirection;
    private readonly ProTextBrush? _selectionForeground;
    private readonly int _selectionStart;
    private readonly int _selectionEnd;

    public ProTextBlockDrawOperation(
        Rect bounds,
        Rect contentBounds,
        ProTextLayoutSnapshot snapshot,
        TextAlignment textAlignment,
        FlowDirection flowDirection,
        ProTextBrush? selectionForeground = null,
        int selectionStart = 0,
        int selectionEnd = 0)
    {
        Bounds = bounds;
        _contentBounds = contentBounds;
        _snapshot = snapshot;
        _textAlignment = textAlignment;
        _flowDirection = flowDirection;
        _selectionForeground = selectionForeground;
        _selectionStart = selectionStart;
        _selectionEnd = selectionEnd;
    }

    public Rect Bounds { get; }

    public void Dispose()
    {
    }

    public bool Equals(ICustomDrawOperation? other) => Equals(other as ProTextBlockDrawOperation);

    public bool HitTest(Point p) => Bounds.Contains(p);

    public bool Equals(ProTextBlockDrawOperation? other)
    {
        return other is not null
            && Bounds.Equals(other.Bounds)
            && _contentBounds.Equals(other._contentBounds)
            && ReferenceEquals(_snapshot, other._snapshot)
            && _textAlignment == other._textAlignment
            && _flowDirection == other._flowDirection
            && Equals(_selectionForeground, other._selectionForeground)
            && _selectionStart == other._selectionStart
            && _selectionEnd == other._selectionEnd;
    }

    public override bool Equals(object? obj) => obj is ProTextBlockDrawOperation other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(Bounds, _contentBounds, _snapshot, _textAlignment, _flowDirection, _selectionForeground, _selectionStart, _selectionEnd);
    }

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();

        if (leaseFeature is null || _snapshot.LineCount == 0)
        {
            return;
        }

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;
        var contentClip = ToSkRect(_contentBounds);
        var saveCount = canvas.Save();

        try
        {
            canvas.ClipRect(contentClip);

            for (var lineIndex = 0; lineIndex < _snapshot.Lines.Count; lineIndex++)
            {
                var line = _snapshot.Lines[lineIndex];

                if (line.Fragments.Count == 0)
                {
                    continue;
                }

                var baseline = GetBaseline(line, lineIndex);
                var alignedX = (float)GetAlignedX(line);

                foreach (var fragment in line.Fragments)
                {
                    if (fragment.Text.Length == 0)
                    {
                        continue;
                    }

                    var renderFont = ProTextRenderFontCache.Get(fragment.Style);
                    using var paint = CreatePaint(fragment.Style.Foreground, contentClip, lease.CurrentOpacity);
                    var x = alignedX + (float)fragment.X;

                    DrawFragment(canvas, fragment, x, baseline, renderFont, paint, contentClip, lease.CurrentOpacity);
                    DrawDecorations(canvas, fragment, x, baseline, renderFont.Font, contentClip, lease.CurrentOpacity);
                }
            }
        }
        finally
        {
            canvas.RestoreToCount(saveCount);
        }
    }

    private float GetBaseline(ProTextLayoutLine line, int lineIndex)
    {
        var style = line.Fragments.Count > 0 ? line.Fragments[0].Style : null;

        if (style is null)
        {
            return (float)(_contentBounds.Y + lineIndex * _snapshot.LineHeight);
        }

        var font = ProTextRenderFontCache.Get(style).Font;
        var metrics = font.Metrics;
        var textHeight = metrics.Descent - metrics.Ascent;
        var baselineOffset = -metrics.Ascent + ((float)_snapshot.LineHeight - textHeight) / 2f;

        return (float)_contentBounds.Y + baselineOffset + (float)(lineIndex * _snapshot.LineHeight);
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
            paint.Color = ToSkColor(Colors.Black, opacity);
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

        paint.Color = ToSkColor(Colors.Black, opacity);
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
            return ([ToSkColor(Colors.Transparent, opacity), ToSkColor(Colors.Transparent, opacity)], [0f, 1f]);
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

    private void DrawFragment(
        SKCanvas canvas,
        ProTextLayoutFragment fragment,
        float x,
        float baseline,
        ProTextRenderFont renderFont,
        SKPaint paint,
        SKRect shaderBounds,
        double inheritedOpacity)
    {
        if (!TryGetSelectedTextRange(fragment, out var selectedStart, out var selectedEnd))
        {
            DrawText(canvas, fragment.Text, x, baseline, fragment.Style, renderFont.Family, renderFont.FontStyle, renderFont.Typeface, renderFont.Font, paint);
            return;
        }

        var localStart = selectedStart - fragment.TextStart;
        var localEnd = selectedEnd - fragment.TextStart;
        var currentX = x;

        if (localStart > 0)
        {
            currentX += DrawText(canvas, fragment.Text[..localStart], currentX, baseline, fragment.Style, renderFont.Family, renderFont.FontStyle, renderFont.Typeface, renderFont.Font, paint);
        }

        using (var selectionPaint = CreatePaint(_selectionForeground, shaderBounds, inheritedOpacity))
        {
            currentX += DrawText(canvas, fragment.Text[localStart..localEnd], currentX, baseline, fragment.Style, renderFont.Family, renderFont.FontStyle, renderFont.Typeface, renderFont.Font, selectionPaint);
        }

        if (localEnd < fragment.Text.Length)
        {
            DrawText(canvas, fragment.Text[localEnd..], currentX, baseline, fragment.Style, renderFont.Family, renderFont.FontStyle, renderFont.Typeface, renderFont.Font, paint);
        }
    }

    private bool TryGetSelectedTextRange(ProTextLayoutFragment fragment, out int selectedStart, out int selectedEnd)
    {
        selectedStart = 0;
        selectedEnd = 0;

        if (_selectionForeground is null || _selectionStart == _selectionEnd || _selectionEnd <= fragment.TextStart || _selectionStart >= fragment.TextEnd)
        {
            return false;
        }

        selectedStart = Math.Max(_selectionStart, fragment.TextStart);
        selectedEnd = Math.Min(_selectionEnd, fragment.TextEnd);
        return selectedStart < selectedEnd;
    }

    private static float DrawText(
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
            PenLineCap.Round => SKStrokeCap.Round,
            PenLineCap.Square => SKStrokeCap.Square,
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
            TextDecorationLocation.Overline => metrics.Ascent,
            TextDecorationLocation.Strikethrough => metrics.StrikeoutPosition ?? metrics.Ascent / 2,
            TextDecorationLocation.Underline => metrics.UnderlinePosition ?? (float)(fontSize * 0.08),
            _ => 0,
        };

        offset += decoration.StrokeOffsetUnit switch
        {
            TextDecorationUnit.FontRenderingEmSize => (float)(decoration.StrokeOffset * fontSize),
            TextDecorationUnit.Pixel => (float)decoration.StrokeOffset,
            _ => 0,
        };

        return baseline + offset;
    }

    private static float GetDecorationThickness(ProTextDecoration decoration, SKFontMetrics metrics, double fontSize)
    {
        return decoration.StrokeThicknessUnit switch
        {
            TextDecorationUnit.FontRecommended when decoration.Location == TextDecorationLocation.Underline => metrics.UnderlineThickness ?? Math.Max(1, (float)fontSize / 14f),
            TextDecorationUnit.FontRecommended when decoration.Location == TextDecorationLocation.Strikethrough => metrics.StrikeoutThickness ?? Math.Max(1, (float)fontSize / 14f),
            TextDecorationUnit.FontRenderingEmSize => (float)Math.Max(0, decoration.StrokeThickness * fontSize),
            _ => (float)Math.Max(0, decoration.StrokeThickness),
        };
    }

    private double GetAlignedX(ProTextLayoutLine line)
    {
        var extra = Math.Max(0, _contentBounds.Width - line.Width);

        return ResolveAlignment() switch
        {
            ResolvedTextAlignment.Center => _contentBounds.X + extra / 2,
            ResolvedTextAlignment.Right => _contentBounds.X + extra,
            _ => _contentBounds.X,
        };
    }

    private ResolvedTextAlignment ResolveAlignment()
    {
        return _textAlignment switch
        {
            TextAlignment.Center => ResolvedTextAlignment.Center,
            TextAlignment.Right => ResolvedTextAlignment.Right,
            TextAlignment.End when _flowDirection == FlowDirection.LeftToRight => ResolvedTextAlignment.Right,
            TextAlignment.End => ResolvedTextAlignment.Left,
            TextAlignment.Start when _flowDirection == FlowDirection.RightToLeft => ResolvedTextAlignment.Right,
            TextAlignment.DetectFromContent when _flowDirection == FlowDirection.RightToLeft => ResolvedTextAlignment.Right,
            _ => ResolvedTextAlignment.Left,
        };
    }

    private static SKRect ToSkRect(Rect bounds)
    {
        return new SKRect(
            (float)bounds.X,
            (float)bounds.Y,
            (float)bounds.Right,
            (float)bounds.Bottom);
    }

    private static Rect FromSkRect(SKRect bounds)
    {
        return new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
    }

    private static SKShaderTileMode ToTileMode(GradientSpreadMethod spreadMethod)
    {
        return spreadMethod switch
        {
            GradientSpreadMethod.Reflect => SKShaderTileMode.Mirror,
            GradientSpreadMethod.Repeat => SKShaderTileMode.Repeat,
            _ => SKShaderTileMode.Clamp,
        };
    }

    private static SKColor ToSkColor(Color color, double opacity)
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

    private enum ResolvedTextAlignment
    {
        Left,
        Center,
        Right,
    }
}