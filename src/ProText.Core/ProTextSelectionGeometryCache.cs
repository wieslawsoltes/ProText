namespace ProText.Core;

/// <summary>
/// Framework-neutral cache for selection rectangles derived from a layout snapshot.
/// </summary>
public sealed class ProTextSelectionGeometryCache
{
    private ProTextSelectionRect[] _selectionRects = [];
    private ProTextLayoutSnapshot? _snapshot;
    private int _selectionStart;
    private int _selectionEnd;
    private double _boundsWidth;
    private ProTextTextAlignment _textAlignment;
    private ProTextFlowDirection _flowDirection;

    public void Clear()
    {
        _selectionRects = [];
        _snapshot = null;
        _selectionStart = 0;
        _selectionEnd = 0;
        _boundsWidth = 0;
        _textAlignment = ProTextTextAlignment.Left;
        _flowDirection = ProTextFlowDirection.LeftToRight;
    }

    public ProTextSelectionRect[] GetSelectionRects(
        ProTextLayoutSnapshot snapshot,
        int selectionStart,
        int selectionEnd,
        double boundsWidth,
        ProTextTextAlignment textAlignment,
        ProTextFlowDirection flowDirection)
    {
        if (selectionStart == selectionEnd)
        {
            return [];
        }

        var start = Math.Min(selectionStart, selectionEnd);
        var end = Math.Max(selectionStart, selectionEnd);

        if (ReferenceEquals(_snapshot, snapshot)
            && _selectionStart == start
            && _selectionEnd == end
            && _boundsWidth.Equals(boundsWidth)
            && _textAlignment == textAlignment
            && _flowDirection == flowDirection)
        {
            return _selectionRects;
        }

        _selectionRects = ProTextLayoutServices.GetSelectionRects(
            snapshot,
            start,
            end,
            boundsWidth,
            textAlignment,
            flowDirection);
        _snapshot = snapshot;
        _selectionStart = start;
        _selectionEnd = end;
        _boundsWidth = boundsWidth;
        _textAlignment = textAlignment;
        _flowDirection = flowDirection;
        return _selectionRects;
    }
}
