using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using ProText.Core;

namespace ProText.Internal;

internal sealed class ProTextDrawOperation : ICustomDrawOperation
{
    private readonly ProTextLayoutSnapshot _snapshot;
    private readonly ProTextRect _contentBounds;
    private readonly ProTextTextAlignment _textAlignment;
    private readonly ProTextFlowDirection _flowDirection;
    private readonly ProTextBrush? _selectionForeground;
    private readonly int _selectionStart;
    private readonly int _selectionEnd;
    private readonly ProTextBrush? _selectionBackground;
    private readonly IReadOnlyList<ProTextSelectionRect> _selectionRects;

    public ProTextDrawOperation(
        Rect bounds,
        Rect contentBounds,
        ProTextLayoutSnapshot snapshot,
        TextAlignment textAlignment,
        FlowDirection flowDirection,
        ProTextBrush? selectionForeground = null,
        int selectionStart = 0,
        int selectionEnd = 0,
        ProTextBrush? selectionBackground = null,
        IReadOnlyList<ProTextSelectionRect>? selectionRects = null)
    {
        Bounds = bounds;
        _contentBounds = ProTextAvaloniaAdapter.ToCore(contentBounds);
        _snapshot = snapshot;
        _textAlignment = ProTextAvaloniaAdapter.ToCore(textAlignment);
        _flowDirection = ProTextAvaloniaAdapter.ToCore(flowDirection);
        _selectionForeground = selectionForeground;
        _selectionStart = selectionStart;
        _selectionEnd = selectionEnd;
        _selectionBackground = selectionBackground;
        _selectionRects = selectionRects ?? [];
    }

    public Rect Bounds { get; }

    public void Dispose()
    {
    }

    public bool Equals(ICustomDrawOperation? other) => Equals(other as ProTextDrawOperation);

    public bool HitTest(Point p) => Bounds.Contains(p);

    public bool Equals(ProTextDrawOperation? other)
    {
        return other is not null
            && Bounds.Equals(other.Bounds)
            && _contentBounds.Equals(other._contentBounds)
            && ReferenceEquals(_snapshot, other._snapshot)
            && _textAlignment == other._textAlignment
            && _flowDirection == other._flowDirection
            && Equals(_selectionForeground, other._selectionForeground)
            && _selectionStart == other._selectionStart
            && _selectionEnd == other._selectionEnd
            && Equals(_selectionBackground, other._selectionBackground)
            && ReferenceEquals(_selectionRects, other._selectionRects);
    }

    public override bool Equals(object? obj) => obj is ProTextDrawOperation other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Bounds);
        hash.Add(_contentBounds);
        hash.Add(_snapshot);
        hash.Add(_textAlignment);
        hash.Add(_flowDirection);
        hash.Add(_selectionForeground);
        hash.Add(_selectionStart);
        hash.Add(_selectionEnd);
        hash.Add(_selectionBackground);
        hash.Add(_selectionRects);
        return hash.ToHashCode();
    }

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();

        if (leaseFeature is null || _snapshot.LineCount == 0)
        {
            return;
        }

        using var lease = leaseFeature.Lease();
        ProTextSkiaRenderer.Render(
            lease.SkCanvas,
            _snapshot,
            new ProTextSkiaRenderOptions(
                _contentBounds,
                _textAlignment,
                _flowDirection,
                lease.CurrentOpacity,
                _selectionForeground,
                _selectionStart,
                _selectionEnd,
                _selectionBackground,
                _selectionRects));
    }
}
