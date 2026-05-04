namespace ProText.Core;

/// <summary>
/// Framework-neutral layout, caret, hit-test, and selection geometry helpers.
/// </summary>
public static class ProTextLayoutServices
{
    public static double ResolveMaxWidth(double availableWidth, ProTextWrapping textWrapping, ProTextTrimming textTrimming)
    {
        if ((textWrapping == ProTextWrapping.NoWrap && textTrimming == ProTextTrimming.None) || double.IsInfinity(availableWidth))
        {
            return double.PositiveInfinity;
        }

        if (double.IsNaN(availableWidth))
        {
            return 0;
        }

        return Math.Max(0, availableWidth);
    }

    public static double GetEffectiveLineHeight(double fontSize, double contentMaxFontSize, double lineHeight, double lineSpacing, double lineHeightMultiplier)
    {
        var effectiveFontSize = Math.Max(fontSize, contentMaxFontSize);
        var baseLineHeight = double.IsNaN(lineHeight) ? effectiveFontSize * lineHeightMultiplier : lineHeight;
        return Math.Max(0, baseLineHeight + lineSpacing);
    }

    public static ProTextRect GetLineBounds(ProTextLayoutSnapshot snapshot, int lineIndex)
    {
        if (snapshot.LineCount == 0)
        {
            return default;
        }

        lineIndex = Math.Clamp(lineIndex, 0, snapshot.LineCount - 1);
        return new ProTextRect(0, lineIndex * snapshot.LineHeight, snapshot.Width, snapshot.LineHeight);
    }

    public static ProTextSelectionRect[] GetSelectionRects(
        ProTextLayoutSnapshot snapshot,
        int selectionStart,
        int selectionEnd,
        double boundsWidth,
        ProTextTextAlignment textAlignment,
        ProTextFlowDirection flowDirection)
    {
        var rects = new List<ProTextSelectionRect>();

        if (selectionStart == selectionEnd)
        {
            return [];
        }

        for (var lineIndex = 0; lineIndex < snapshot.Lines.Count; lineIndex++)
        {
            var line = snapshot.Lines[lineIndex];

            if (line.Fragments.Count == 0 || selectionEnd <= line.StartTextIndex || selectionStart >= line.EndTextIndex)
            {
                continue;
            }

            var start = Math.Max(selectionStart, line.StartTextIndex);
            var end = Math.Min(selectionEnd, line.EndTextIndex);
            var x1 = GetXForTextIndex(snapshot, line, start, boundsWidth, textAlignment, flowDirection);
            var x2 = GetXForTextIndex(snapshot, line, end, boundsWidth, textAlignment, flowDirection);

            if (x2 < x1)
            {
                (x1, x2) = (x2, x1);
            }

            rects.Add(new ProTextSelectionRect(lineIndex, new ProTextRect(x1, lineIndex * snapshot.LineHeight, Math.Max(1, x2 - x1), snapshot.LineHeight)));
        }

        return rects.Count == 0 ? [] : rects.ToArray();
    }

    public static int GetCharacterIndex(
        ProTextLayoutSnapshot snapshot,
        ProTextPoint point,
        double boundsWidth,
        ProTextTextAlignment textAlignment,
        ProTextFlowDirection flowDirection)
    {
        if (snapshot.Lines.Count == 0)
        {
            return 0;
        }

        var lineIndex = Math.Clamp((int)Math.Floor(point.Y / Math.Max(1, snapshot.LineHeight)), 0, snapshot.Lines.Count - 1);
        var line = snapshot.Lines[lineIndex];

        if (line.Fragments.Count == 0)
        {
            return line.StartTextIndex;
        }

        var lineX = GetAlignedX(boundsWidth, line, textAlignment, flowDirection);

        if (point.X <= lineX)
        {
            return line.StartTextIndex;
        }

        foreach (var fragment in line.Fragments)
        {
            var fragmentX = lineX + fragment.X;

            if (point.X > fragmentX + fragment.Width)
            {
                continue;
            }

            return GetCharacterIndexInFragment(fragment, point.X - fragmentX);
        }

        return line.EndTextIndex;
    }

    public static ProTextRect GetCaretBounds(
        ProTextLayoutSnapshot snapshot,
        int textPosition,
        double boundsWidth,
        ProTextTextAlignment textAlignment,
        ProTextFlowDirection flowDirection)
    {
        if (snapshot.Lines.Count == 0)
        {
            return new ProTextRect(0, 0, 0, snapshot.LineHeight);
        }

        for (var lineIndex = 0; lineIndex < snapshot.Lines.Count; lineIndex++)
        {
            var line = snapshot.Lines[lineIndex];

            if (textPosition < line.StartTextIndex || textPosition > line.EndTextIndex)
            {
                continue;
            }

            return new ProTextRect(
                GetXForTextIndex(snapshot, line, textPosition, boundsWidth, textAlignment, flowDirection),
                lineIndex * snapshot.LineHeight,
                0,
                snapshot.LineHeight);
        }

        var lastLine = snapshot.Lines[^1];
        return new ProTextRect(
            GetXForTextIndex(snapshot, lastLine, lastLine.EndTextIndex, boundsWidth, textAlignment, flowDirection),
            (snapshot.Lines.Count - 1) * snapshot.LineHeight,
            0,
            snapshot.LineHeight);
    }

    public static double GetXForTextIndex(
        ProTextLayoutSnapshot snapshot,
        ProTextLayoutLine line,
        int textPosition,
        double boundsWidth,
        ProTextTextAlignment textAlignment,
        ProTextFlowDirection flowDirection)
    {
        var x = GetAlignedX(boundsWidth, line, textAlignment, flowDirection);

        foreach (var fragment in line.Fragments)
        {
            if (textPosition <= fragment.TextStart)
            {
                return x + fragment.X;
            }

            if (textPosition <= fragment.TextEnd)
            {
                var localLength = Math.Clamp(textPosition - fragment.TextStart, 0, fragment.Text.Length);

                if (localLength == fragment.Text.Length)
                {
                    return x + fragment.X + fragment.Width;
                }

                var prefix = fragment.Text[..localLength];
                return x + fragment.X + ProTextRenderFontCache.MeasureText(prefix, fragment.Style);
            }
        }

        return x + line.Width;
    }

    public static double GetAlignedX(
        double boundsWidth,
        ProTextLayoutLine line,
        ProTextTextAlignment textAlignment,
        ProTextFlowDirection flowDirection)
    {
        var extra = Math.Max(0, boundsWidth - line.Width);

        return ResolveAlignment(textAlignment, flowDirection) switch
        {
            ResolvedTextAlignment.Center => extra / 2,
            ResolvedTextAlignment.Right => extra,
            _ => 0,
        };
    }

    public static bool IsRightAligned(ProTextTextAlignment textAlignment, ProTextFlowDirection flowDirection)
    {
        return ResolveAlignment(textAlignment, flowDirection) == ResolvedTextAlignment.Right;
    }

    private static int GetCharacterIndexInFragment(ProTextLayoutFragment fragment, double x)
    {
        var currentX = 0d;
        var lastIndex = fragment.TextStart;

        foreach (var grapheme in ProTextFontResolver.EnumerateGraphemes(fragment.Text))
        {
            var width = ProTextRenderFontCache.MeasureText(grapheme, fragment.Style) + fragment.Style.LetterSpacing;
            var nextIndex = lastIndex + grapheme.Length;

            if (x <= currentX + width / 2)
            {
                return lastIndex;
            }

            currentX += width;
            lastIndex = nextIndex;
        }

        return fragment.TextEnd;
    }

    private static ResolvedTextAlignment ResolveAlignment(ProTextTextAlignment textAlignment, ProTextFlowDirection flowDirection)
    {
        return textAlignment switch
        {
            ProTextTextAlignment.Center => ResolvedTextAlignment.Center,
            ProTextTextAlignment.Right => ResolvedTextAlignment.Right,
            ProTextTextAlignment.End when flowDirection == ProTextFlowDirection.LeftToRight => ResolvedTextAlignment.Right,
            ProTextTextAlignment.End => ResolvedTextAlignment.Left,
            ProTextTextAlignment.Start when flowDirection == ProTextFlowDirection.RightToLeft => ResolvedTextAlignment.Right,
            ProTextTextAlignment.DetectFromContent when flowDirection == ProTextFlowDirection.RightToLeft => ResolvedTextAlignment.Right,
            _ => ResolvedTextAlignment.Left,
        };
    }

    private enum ResolvedTextAlignment
    {
        Left,
        Center,
        Right,
    }
}
