using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;

namespace ProText.Uno.Internal;

internal sealed class ProTextUnoCanvasElement : SKCanvasElement
{
    private readonly ProTextBlock _owner;

    public ProTextUnoCanvasElement(ProTextBlock owner)
    {
        _owner = owner;
    }

    public static bool IsSkiaSupportedOnCurrentPlatform() => SKCanvasElement.IsSupportedOnCurrentPlatform();

    protected override void RenderOverride(SKCanvas canvas, Size area)
    {
        _owner.Render(canvas, area);
    }
}
