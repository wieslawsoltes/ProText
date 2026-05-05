using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Pretext;
using ProText.Core;
using ProText.Uno.Internal;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI.Text;

namespace ProText.Uno;

/// <summary>
/// A high-performance text display control for Uno Platform powered by PretextSharp.
/// </summary>
[ContentProperty(Name = nameof(Inlines))]
public class ProTextBlock : ContentControl
{
    /// <summary>
    /// Defines the <see cref="UseGlobalCache"/> property.
    /// </summary>
    public static readonly DependencyProperty UseGlobalCacheProperty =
        DependencyProperty.Register(nameof(UseGlobalCache), typeof(bool), typeof(ProTextBlock), new PropertyMetadata(true, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="UsePretextRendering"/> property.
    /// </summary>
    public static readonly DependencyProperty UsePretextRenderingProperty =
        DependencyProperty.Register(nameof(UsePretextRendering), typeof(bool), typeof(ProTextBlock), new PropertyMetadata(true, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="PretextWhiteSpace"/> property.
    /// </summary>
    public static readonly DependencyProperty PretextWhiteSpaceProperty =
        DependencyProperty.Register(nameof(PretextWhiteSpace), typeof(WhiteSpaceMode), typeof(ProTextBlock), new PropertyMetadata(WhiteSpaceMode.Normal, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="PretextWordBreak"/> property.
    /// </summary>
    public static readonly DependencyProperty PretextWordBreakProperty =
        DependencyProperty.Register(nameof(PretextWordBreak), typeof(WordBreakMode), typeof(ProTextBlock), new PropertyMetadata(WordBreakMode.Normal, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="PretextLineHeightMultiplier"/> property.
    /// </summary>
    public static readonly DependencyProperty PretextLineHeightMultiplierProperty =
        DependencyProperty.Register(nameof(PretextLineHeightMultiplier), typeof(double), typeof(ProTextBlock), new PropertyMetadata(1.2d, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="Background"/> property.
    /// </summary>
    public static readonly new DependencyProperty BackgroundProperty =
        DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(ProTextBlock), new PropertyMetadata(null, OnRenderPropertyChanged));

    /// <summary>
    /// Defines the <see cref="Padding"/> property.
    /// </summary>
    public static readonly new DependencyProperty PaddingProperty =
        DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(ProTextBlock), new PropertyMetadata(default(Thickness), OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="FontFamily"/> property.
    /// </summary>
    public static readonly new DependencyProperty FontFamilyProperty =
        DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(ProTextBlock), new PropertyMetadata(FontFamily.Default, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="FontSize"/> property.
    /// </summary>
    public static readonly new DependencyProperty FontSizeProperty =
        DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(ProTextBlock), new PropertyMetadata(14d, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="FontStyle"/> property.
    /// </summary>
    public static readonly new DependencyProperty FontStyleProperty =
        DependencyProperty.Register(nameof(FontStyle), typeof(FontStyle), typeof(ProTextBlock), new PropertyMetadata(FontStyle.Normal, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="FontWeight"/> property.
    /// </summary>
    public static readonly new DependencyProperty FontWeightProperty =
        DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(ProTextBlock), new PropertyMetadata(new FontWeight(400), OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="FontStretch"/> property.
    /// </summary>
    public static readonly new DependencyProperty FontStretchProperty =
        DependencyProperty.Register(nameof(FontStretch), typeof(FontStretch), typeof(ProTextBlock), new PropertyMetadata(FontStretch.Normal, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="Foreground"/> property.
    /// </summary>
    public static readonly new DependencyProperty ForegroundProperty =
        DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(ProTextBlock), new PropertyMetadata(null, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="LineHeight"/> property.
    /// </summary>
    public static readonly DependencyProperty LineHeightProperty =
        DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(ProTextBlock), new PropertyMetadata(0d, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="LineSpacing"/> property.
    /// </summary>
    public static readonly DependencyProperty LineSpacingProperty =
        DependencyProperty.Register(nameof(LineSpacing), typeof(double), typeof(ProTextBlock), new PropertyMetadata(0d, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="CharacterSpacing"/> property.
    /// </summary>
    public static readonly new DependencyProperty CharacterSpacingProperty =
        DependencyProperty.Register(nameof(CharacterSpacing), typeof(int), typeof(ProTextBlock), new PropertyMetadata(0, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="LetterSpacing"/> property.
    /// </summary>
    public static readonly DependencyProperty LetterSpacingProperty =
        DependencyProperty.Register(nameof(LetterSpacing), typeof(double), typeof(ProTextBlock), new PropertyMetadata(0d, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="MaxLines"/> property.
    /// </summary>
    public static readonly DependencyProperty MaxLinesProperty =
        DependencyProperty.Register(nameof(MaxLines), typeof(int), typeof(ProTextBlock), new PropertyMetadata(0, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="Text"/> property.
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(ProTextBlock), new PropertyMetadata(null, OnTextPropertyChanged));

    /// <summary>
    /// Defines the <see cref="TextAlignment"/> property.
    /// </summary>
    public static readonly DependencyProperty TextAlignmentProperty =
        DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(ProTextBlock), new PropertyMetadata(TextAlignment.Left, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="TextWrapping"/> property.
    /// </summary>
    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(ProTextBlock), new PropertyMetadata(TextWrapping.NoWrap, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="TextTrimming"/> property.
    /// </summary>
    public static readonly DependencyProperty TextTrimmingProperty =
        DependencyProperty.Register(nameof(TextTrimming), typeof(TextTrimming), typeof(ProTextBlock), new PropertyMetadata(TextTrimming.None, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="TextDecorations"/> property.
    /// </summary>
    public static readonly DependencyProperty TextDecorationsProperty =
        DependencyProperty.Register(nameof(TextDecorations), typeof(TextDecorations), typeof(ProTextBlock), new PropertyMetadata(TextDecorations.None, OnMeasurePropertyChanged));

    /// <summary>
    /// Defines the <see cref="FontFeatures"/> property.
    /// </summary>
    public static readonly DependencyProperty FontFeaturesProperty =
        DependencyProperty.Register(nameof(FontFeatures), typeof(string), typeof(ProTextBlock), new PropertyMetadata(null, OnMeasurePropertyChanged));

    private readonly ProTextLayoutCache _layoutCache = new();
    private readonly ProTextUnoCanvasElement? _canvasElement;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProTextBlock"/> class.
    /// </summary>
    public ProTextBlock()
    {
        Inlines = ProTextUnoInlineCollectionFactory.Create(this);

        if (ProTextUnoCanvasElement.IsSkiaSupportedOnCurrentPlatform())
        {
            _canvasElement = new ProTextUnoCanvasElement(this);
            Content = _canvasElement;
        }
    }

    /// <summary>
    /// Gets or sets whether shared prepared-text cache entries are used by this control.
    /// </summary>
    public bool UseGlobalCache
    {
        get => (bool)GetValue(UseGlobalCacheProperty);
        set => SetValue(UseGlobalCacheProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the Pretext rendering path is enabled.
    /// </summary>
    public bool UsePretextRendering
    {
        get => (bool)GetValue(UsePretextRenderingProperty);
        set => SetValue(UsePretextRenderingProperty, value);
    }

    /// <summary>
    /// Gets or sets the Pretext whitespace handling mode.
    /// </summary>
    public WhiteSpaceMode PretextWhiteSpace
    {
        get => (WhiteSpaceMode)GetValue(PretextWhiteSpaceProperty);
        set => SetValue(PretextWhiteSpaceProperty, value);
    }

    /// <summary>
    /// Gets or sets the Pretext word-break handling mode.
    /// </summary>
    public WordBreakMode PretextWordBreak
    {
        get => (WordBreakMode)GetValue(PretextWordBreakProperty);
        set => SetValue(PretextWordBreakProperty, value);
    }

    /// <summary>
    /// Gets or sets the fallback multiplier used when <see cref="LineHeight"/> is not explicitly set.
    /// </summary>
    public double PretextLineHeightMultiplier
    {
        get => (double)GetValue(PretextLineHeightMultiplierProperty);
        set => SetValue(PretextLineHeightMultiplierProperty, value);
    }

    /// <summary>
    /// Gets or sets a brush used to paint the control background.
    /// </summary>
    public new Brush? Background
    {
        get => (Brush?)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the padding around text.
    /// </summary>
    public new Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets rich inline content.
    /// </summary>
    public InlineCollection Inlines { get; }

    /// <summary>
    /// Gets or sets the font family used to draw text.
    /// </summary>
    public new FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size used to draw text.
    /// </summary>
    public new double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the font style used to draw text.
    /// </summary>
    public new FontStyle FontStyle
    {
        get => (FontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the font weight used to draw text.
    /// </summary>
    public new FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the font stretch used to draw text.
    /// </summary>
    public new FontStretch FontStretch
    {
        get => (FontStretch)GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush used to draw text.
    /// </summary>
    public new Brush? Foreground
    {
        get => (Brush?)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of each text line. A value less than or equal to zero uses automatic line height.
    /// </summary>
    public double LineHeight
    {
        get => (double)GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets extra spacing after each text line.
    /// </summary>
    public double LineSpacing
    {
        get => (double)GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets WinUI character spacing in 1/1000 em units.
    /// </summary>
    public new int CharacterSpacing
    {
        get => (int)GetValue(CharacterSpacingProperty);
        set => SetValue(CharacterSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets additional pixel letter spacing applied after <see cref="CharacterSpacing"/>.
    /// </summary>
    public double LetterSpacing
    {
        get => (double)GetValue(LetterSpacingProperty);
        set => SetValue(LetterSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum number of rendered text lines. Zero means unlimited.
    /// </summary>
    public int MaxLines
    {
        get => (int)GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    /// <summary>
    /// Gets or sets text wrapping behavior.
    /// </summary>
    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    /// <summary>
    /// Gets or sets text trimming behavior.
    /// </summary>
    public TextTrimming TextTrimming
    {
        get => (TextTrimming)GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    /// <summary>
    /// Gets or sets text alignment.
    /// </summary>
    public TextAlignment TextAlignment
    {
        get => (TextAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    /// <summary>
    /// Gets or sets text decorations.
    /// </summary>
    public TextDecorations TextDecorations
    {
        get => (TextDecorations)GetValue(TextDecorationsProperty);
        set => SetValue(TextDecorationsProperty, value);
    }

    /// <summary>
    /// Gets or sets an OpenType font-feature fingerprint used in layout cache identity.
    /// </summary>
    public string? FontFeatures
    {
        get => (string?)GetValue(FontFeaturesProperty);
        set => SetValue(FontFeaturesProperty, value);
    }

    internal bool IsUsingFallback => false;

    /// <summary>
    /// Invalidates cached ProText content, layout, and rendering.
    /// </summary>
    public void InvalidateText()
    {
        InvalidateProText();
    }

    /// <summary>
    /// Measures the current text using a supplied width.
    /// </summary>
    public Size MeasureText(double availableWidth)
    {
        if (!TryCreateRichContent(out var content))
        {
            return default;
        }

        return ProTextUnoAdapter.ToUno(GetLayoutSnapshot(content, availableWidth).Size);
    }

    /// <summary>
    /// Gets the number of materialized layout lines, or -1 when layout is unavailable.
    /// </summary>
    public int GetLineCount()
    {
        if (!TryCreateRichContent(out var content))
        {
            return -1;
        }

        return GetLayoutSnapshot(content, GetLayoutWidth()).LineCount;
    }

    /// <summary>
    /// Gets rendered bounds for a materialized layout line.
    /// </summary>
    public Rect GetLineBounds(int lineIndex)
    {
        if (!TryCreateRichContent(out var content))
        {
            return default;
        }

        var snapshot = GetLayoutSnapshot(content, GetLayoutWidth());
        return ProTextUnoAdapter.ToUno(ProTextLayoutServices.GetLineBounds(snapshot, lineIndex));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (!TryCreateRichContent(out var content))
        {
            return Inflate(default, Padding);
        }

        var padding = Padding;
        var contentSize = DeflateNonNegative(availableSize, padding);
        var snapshot = GetLayoutSnapshot(content, contentSize.Width);

        return Inflate(ProTextUnoAdapter.ToUno(snapshot.Size), padding);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _canvasElement?.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        return finalSize;
    }

    internal void Render(SKCanvas canvas, Size area)
    {
        if (!TryCreateRichContent(out var content))
        {
            return;
        }

        var controlBounds = new ProTextRect(0, 0, Math.Max(0, area.Width), Math.Max(0, area.Height));
        var background = ProTextUnoAdapter.SnapshotBrush(Background);
        ProTextUnoSkiaBrushRenderer.DrawRect(canvas, controlBounds, background, Opacity);

        var padding = Padding;
        var contentSize = DeflateNonNegative(area, padding);

        if (contentSize.Width <= 0 || contentSize.Height <= 0)
        {
            return;
        }

        var snapshot = GetLayoutSnapshot(content, contentSize.Width);

        if (snapshot.LineCount == 0)
        {
            return;
        }

        var contentBounds = new ProTextRect(
            padding.Left,
            padding.Top,
            contentSize.Width,
            contentSize.Height);

        RenderText(canvas, content, snapshot, contentBounds);
    }

    /// <summary>
    /// Invalidates ProText layout and rendering state.
    /// </summary>
    protected virtual void InvalidateProText()
    {
        _layoutCache.Clear();
        InvalidateMeasure();
        InvalidateRender();
    }

    /// <summary>
    /// Builds current rich text content.
    /// </summary>
    protected virtual bool TryCreateRichContent(out ProTextRichContent content)
    {
        content = null!;

        if (!UsePretextRendering)
        {
            return false;
        }

        var baseStyle = CreateBaseStyle(Foreground, TextDecorations);

        if (Inlines.Count > 0)
        {
            return ProTextUnoInlineBuilder.TryCreateInlineContent(Inlines, baseStyle, out content);
        }

        content = CreateTextContent(baseStyle);
        return true;
    }

    /// <summary>
    /// Creates text content when no inlines are present.
    /// </summary>
    protected virtual ProTextRichContent CreateTextContent(ProTextRichStyle baseStyle)
    {
        return ProTextUnoInlineBuilder.CreateTextContent(Text, baseStyle);
    }

    /// <summary>
    /// Creates a framework-neutral style from current Uno properties.
    /// </summary>
    protected ProTextRichStyle CreateBaseStyle(Brush? foreground, TextDecorations textDecorations)
    {
        return ProTextUnoInlineBuilder.CreateStyle(
            FontFamily,
            FontSize,
            FontStyle,
            FontWeight,
            FontStretch,
            foreground,
            textDecorations,
            FontFeatures,
            CharacterSpacing,
            LetterSpacing);
    }

    /// <summary>
    /// Gets or creates a layout snapshot for current content and width.
    /// </summary>
    protected ProTextLayoutSnapshot GetLayoutSnapshot(ProTextRichContent content, double availableWidth)
    {
        ProTextUnoPlatform.EnsureConfigured();

        var maxWidth = ResolveMaxWidth(availableWidth);
        var lineHeight = GetEffectiveLineHeight(content);
        var textWrapping = ProTextUnoAdapter.ToCore(TextWrapping);
        var textTrimming = ProTextUnoAdapter.ToCore(TextTrimming);

        return _layoutCache.GetSnapshot(
            content,
            new ProTextLayoutRequest(
                maxWidth,
                lineHeight,
                Math.Max(0, MaxLines),
                textWrapping,
                textTrimming,
                UseGlobalCache));
    }

    /// <summary>
    /// Renders text content into the supplied Skia canvas.
    /// </summary>
    protected virtual void RenderText(SKCanvas canvas, ProTextRichContent content, ProTextLayoutSnapshot snapshot, ProTextRect contentBounds)
    {
        ProTextSkiaRenderer.Render(
            canvas,
            snapshot,
            new ProTextSkiaRenderOptions(
                contentBounds,
                ProTextUnoAdapter.ToCore(TextAlignment),
                ProTextUnoAdapter.ToCore(FlowDirection),
                Opacity));
    }

    /// <summary>
    /// Gets the current layout width used by hit testing APIs.
    /// </summary>
    protected double GetLayoutWidth()
    {
        return ActualWidth > 0 ? ActualWidth : double.PositiveInfinity;
    }

    /// <summary>
    /// Resolves a layout width according to wrapping and trimming settings.
    /// </summary>
    protected double ResolveMaxWidth(double availableWidth)
    {
        return ProTextLayoutServices.ResolveMaxWidth(
            availableWidth,
            ProTextUnoAdapter.ToCore(TextWrapping),
            ProTextUnoAdapter.ToCore(TextTrimming));
    }

    /// <summary>
    /// Gets the effective line height used by the core layout engine.
    /// </summary>
    protected double GetEffectiveLineHeight(ProTextRichContent content)
    {
        return ProTextLayoutServices.GetEffectiveLineHeight(
            FontSize,
            content.MaxFontSize,
            ProTextUnoAdapter.NormalizeLineHeight(LineHeight),
            LineSpacing,
            PretextLineHeightMultiplier);
    }

    private static void OnTextPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProTextBlock block)
        {
            if (block.Inlines.Count > 0)
            {
                block.Inlines.Clear();
            }

            block.InvalidateProText();
        }
    }

    private static void OnMeasurePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProTextBlock block)
        {
            block.InvalidateProText();
        }
    }

    private static void OnRenderPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProTextBlock block)
        {
            block.InvalidateRender();
        }
    }

    /// <summary>
    /// Invalidates the internal Skia drawing surface when one is available.
    /// </summary>
    protected void InvalidateRender()
    {
        _canvasElement?.Invalidate();
    }

    private static Size DeflateNonNegative(Size size, Thickness thickness)
    {
        var width = double.IsInfinity(size.Width)
            ? double.PositiveInfinity
            : Math.Max(0, size.Width - thickness.Left - thickness.Right);
        var height = double.IsInfinity(size.Height)
            ? double.PositiveInfinity
            : Math.Max(0, size.Height - thickness.Top - thickness.Bottom);

        return new Size(width, height);
    }

    private static Size Inflate(Size size, Thickness thickness)
    {
        return new Size(
            size.Width + thickness.Left + thickness.Right,
            size.Height + thickness.Top + thickness.Bottom);
    }
}
