using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace ProText.MAUI.Internal;

internal sealed class ProTextMauiCanvasView : SKCanvasView
{
    private readonly ProTextBlock _owner;

    public ProTextMauiCanvasView(ProTextBlock owner)
    {
        _owner = owner;
        IgnorePixelScaling = true;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        InputTransparent = true;
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var width = Width > 0 ? Width : e.Info.Width;
        var height = Height > 0 ? Height : e.Info.Height;
        _owner.Render(canvas, new Size(width, height));
    }
}
