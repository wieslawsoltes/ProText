using ProText.Core;
using SkiaSharp;

namespace ProText.Uno.Internal;

internal static class ProTextUnoSkiaBrushRenderer
{
    public static void DrawRect(SKCanvas canvas, ProTextRect rect, ProTextBrush? brush, double inheritedOpacity = 1)
    {
        if (brush is null || rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var skRect = ToSkRect(rect);
        using var paint = CreatePaint(brush, skRect, inheritedOpacity);
        canvas.DrawRect(skRect, paint);
    }

    public static void DrawCaret(SKCanvas canvas, ProTextRect rect, ProTextBrush? brush, double inheritedOpacity = 1)
    {
        if (brush is null || rect.Height <= 0)
        {
            return;
        }

        var x = (float)Math.Floor(rect.X) + 0.5f;
        using var paint = CreatePaint(brush, ToSkRect(rect), inheritedOpacity);
        paint.StrokeWidth = 1;
        canvas.DrawLine(x, (float)rect.Y, x, (float)rect.Bottom, paint);
    }

    private static SKPaint CreatePaint(ProTextBrush brush, SKRect shaderBounds, double inheritedOpacity)
    {
        var paint = new SKPaint { IsAntialias = true };
        var opacity = brush.Opacity * inheritedOpacity;

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
            var rect = FromSkRect(bounds);
            var start = linear.StartPoint.ToPixels(rect);
            var end = linear.EndPoint.ToPixels(rect);
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

        return null;
    }

    private static (SKColor[] Colors, float[] Positions) GetGradientStops(IReadOnlyList<ProTextGradientStop> gradientStops, double opacity)
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

    private static SKShaderTileMode ToTileMode(ProTextGradientSpreadMethod spreadMethod)
    {
        return spreadMethod switch
        {
            ProTextGradientSpreadMethod.Reflect => SKShaderTileMode.Mirror,
            ProTextGradientSpreadMethod.Repeat => SKShaderTileMode.Repeat,
            _ => SKShaderTileMode.Clamp,
        };
    }

    private static SKRect ToSkRect(ProTextRect rect)
    {
        return SKRect.Create((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
    }

    private static ProTextRect FromSkRect(SKRect rect)
    {
        return new ProTextRect(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    private static SKColor ToSkColor(ProTextColor color, double opacity)
    {
        var alpha = (byte)Math.Clamp(color.A * opacity, 0, 255);
        return new SKColor(color.R, color.G, color.B, alpha);
    }
}
