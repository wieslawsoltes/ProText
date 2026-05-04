namespace ProText.Core;

/// <summary>
/// Framework-neutral font style.
/// </summary>
public enum ProTextFontStyle
{
    Normal,
    Italic,
    Oblique,
}

/// <summary>
/// Framework-neutral text wrapping mode.
/// </summary>
public enum ProTextWrapping
{
    NoWrap,
    Wrap,
    WrapWithOverflow,
}

/// <summary>
/// Framework-neutral trimming mode.
/// </summary>
public enum ProTextTrimming
{
    None,
    CharacterEllipsis,
    WordEllipsis,
}

/// <summary>
/// Framework-neutral horizontal text alignment.
/// </summary>
public enum ProTextTextAlignment
{
    Left,
    Center,
    Right,
    Justify,
    Start,
    End,
    DetectFromContent,
}

/// <summary>
/// Framework-neutral flow direction.
/// </summary>
public enum ProTextFlowDirection
{
    LeftToRight,
    RightToLeft,
}

/// <summary>
/// Framework-neutral gradient spread mode.
/// </summary>
public enum ProTextGradientSpreadMethod
{
    Pad,
    Reflect,
    Repeat,
}

/// <summary>
/// Framework-neutral relative coordinate unit.
/// </summary>
public enum ProTextRelativeUnit
{
    Relative,
    Absolute,
}

/// <summary>
/// Framework-neutral text decoration location.
/// </summary>
public enum ProTextDecorationLocation
{
    Underline,
    Strikethrough,
    Overline,
    Baseline,
}

/// <summary>
/// Framework-neutral text decoration unit.
/// </summary>
public enum ProTextDecorationUnit
{
    FontRecommended,
    FontRenderingEmSize,
    Pixel,
}

/// <summary>
/// Framework-neutral line cap.
/// </summary>
public enum ProTextPenLineCap
{
    Flat,
    Square,
    Round,
}

/// <summary>
/// Font simulations needed when the host font manager resolves a nearby face.
/// </summary>
[Flags]
public enum ProTextFontSimulations
{
    None = 0,
    Bold = 1,
    Oblique = 2,
}

/// <summary>
/// Framework-neutral ARGB color.
/// </summary>
public readonly record struct ProTextColor(byte A, byte R, byte G, byte B)
{
    public static ProTextColor Black { get; } = FromRgb(0, 0, 0);

    public static ProTextColor Transparent { get; } = new(0, 0, 0, 0);

    public static ProTextColor FromRgb(byte r, byte g, byte b) => new(byte.MaxValue, r, g, b);

    public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";
}

/// <summary>
/// Framework-neutral point.
/// </summary>
public readonly record struct ProTextPoint(double X, double Y);

/// <summary>
/// Framework-neutral size.
/// </summary>
public readonly record struct ProTextSize(double Width, double Height);

/// <summary>
/// Framework-neutral rectangle.
/// </summary>
public readonly record struct ProTextRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;

    public double Bottom => Y + Height;

    public bool Contains(ProTextPoint point)
    {
        return point.X >= X && point.X <= Right && point.Y >= Y && point.Y <= Bottom;
    }
}

/// <summary>
/// Framework-neutral relative point.
/// </summary>
public readonly record struct ProTextRelativePoint(double X, double Y, ProTextRelativeUnit Unit)
{
    public ProTextPoint ToPixels(ProTextRect bounds)
    {
        return Unit == ProTextRelativeUnit.Relative
            ? new ProTextPoint(bounds.X + bounds.Width * X, bounds.Y + bounds.Height * Y)
            : new ProTextPoint(bounds.X + X, bounds.Y + Y);
    }
}

/// <summary>
/// Framework-neutral relative scalar.
/// </summary>
public readonly record struct ProTextRelativeScalar(double Scalar, ProTextRelativeUnit Unit)
{
    public double ToValue(double size)
    {
        return Unit == ProTextRelativeUnit.Relative ? Scalar * size : Scalar;
    }
}
