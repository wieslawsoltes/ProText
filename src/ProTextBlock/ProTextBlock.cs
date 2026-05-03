using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Pretext;
using ProTextBlock.Internal;

namespace ProTextBlock;

/// <summary>
/// A high-performance text display control for Avalonia 12 powered by PretextSharp.
/// </summary>
public class ProTextBlock : Control
{
    /// <summary>
    /// Defines the <see cref="UseGlobalCache"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> UseGlobalCacheProperty =
        AvaloniaProperty.Register<ProTextBlock, bool>(nameof(UseGlobalCache), true);

    /// <summary>
    /// Defines the <see cref="UsePretextRendering"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> UsePretextRenderingProperty =
        AvaloniaProperty.Register<ProTextBlock, bool>(nameof(UsePretextRendering), true);

    /// <summary>
    /// Defines the <see cref="PretextWhiteSpace"/> property.
    /// </summary>
    public static readonly StyledProperty<WhiteSpaceMode> PretextWhiteSpaceProperty =
        AvaloniaProperty.Register<ProTextBlock, WhiteSpaceMode>(nameof(PretextWhiteSpace), WhiteSpaceMode.Normal);

    /// <summary>
    /// Defines the <see cref="PretextWordBreak"/> property.
    /// </summary>
    public static readonly StyledProperty<WordBreakMode> PretextWordBreakProperty =
        AvaloniaProperty.Register<ProTextBlock, WordBreakMode>(nameof(PretextWordBreak), WordBreakMode.Normal);

    /// <summary>
    /// Defines the <see cref="PretextLineHeightMultiplier"/> property.
    /// </summary>
    public static readonly StyledProperty<double> PretextLineHeightMultiplierProperty =
        AvaloniaProperty.Register<ProTextBlock, double>(
            nameof(PretextLineHeightMultiplier),
            1.2,
            validate: value => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0);

    /// <summary>
    /// Defines the <see cref="Background"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        TextBlock.BackgroundProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="Padding"/> property.
    /// </summary>
    public static readonly StyledProperty<Thickness> PaddingProperty =
        TextBlock.PaddingProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="FontFamily"/> property.
    /// </summary>
    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextBlock.FontFamilyProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="FontSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> FontSizeProperty =
        TextBlock.FontSizeProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="FontStyle"/> property.
    /// </summary>
    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        TextBlock.FontStyleProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="FontWeight"/> property.
    /// </summary>
    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextBlock.FontWeightProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="FontStretch"/> property.
    /// </summary>
    public static readonly StyledProperty<FontStretch> FontStretchProperty =
        TextBlock.FontStretchProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="Foreground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextBlock.ForegroundProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="BaselineOffset"/> property.
    /// </summary>
    public static readonly AttachedProperty<double> BaselineOffsetProperty =
        TextBlock.BaselineOffsetProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="LineHeight"/> property.
    /// </summary>
    public static readonly AttachedProperty<double> LineHeightProperty =
        TextBlock.LineHeightProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="LineSpacing"/> property.
    /// </summary>
    public static readonly AttachedProperty<double> LineSpacingProperty =
        TextBlock.LineSpacingProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="LetterSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LetterSpacingProperty =
        TextBlock.LetterSpacingProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="MaxLines"/> property.
    /// </summary>
    public static readonly AttachedProperty<int> MaxLinesProperty =
        TextBlock.MaxLinesProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="Text"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty =
        TextBlock.TextProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="TextAlignment"/> property.
    /// </summary>
    public static readonly AttachedProperty<TextAlignment> TextAlignmentProperty =
        TextBlock.TextAlignmentProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="TextWrapping"/> property.
    /// </summary>
    public static readonly AttachedProperty<TextWrapping> TextWrappingProperty =
        TextBlock.TextWrappingProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="TextTrimming"/> property.
    /// </summary>
    public static readonly AttachedProperty<TextTrimming> TextTrimmingProperty =
        TextBlock.TextTrimmingProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="TextDecorations"/> property.
    /// </summary>
    public static readonly StyledProperty<TextDecorationCollection?> TextDecorationsProperty =
        TextBlock.TextDecorationsProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="FontFeatures"/> property.
    /// </summary>
    public static readonly StyledProperty<FontFeatureCollection?> FontFeaturesProperty =
        TextBlock.FontFeaturesProperty.AddOwner<ProTextBlock>();

    /// <summary>
    /// Defines the <see cref="Inlines"/> property.
    /// </summary>
    public static readonly DirectProperty<ProTextBlock, InlineCollection?> InlinesProperty =
        AvaloniaProperty.RegisterDirect<ProTextBlock, InlineCollection?>(
            nameof(Inlines),
            control => control.Inlines,
            (control, value) => control.Inlines = value);

    private InlineCollection? _inlines;
    private readonly HashSet<InlineCollection> _observedInlineCollections = new();
    private readonly HashSet<Inline> _observedInlines = new();
    private ProTextLayoutSnapshot? _layoutSnapshot;
    private ProTextRichCacheKey? _localPreparedKey;
    private ProTextPreparedContent? _localPrepared;

    static ProTextBlock()
    {
        ClipToBoundsProperty.OverrideDefaultValue<ProTextBlock>(true);

        AffectsMeasure<ProTextBlock>(
            UseGlobalCacheProperty,
            UsePretextRenderingProperty,
            PretextWhiteSpaceProperty,
            PretextWordBreakProperty,
            PretextLineHeightMultiplierProperty,
            PaddingProperty,
            FontFamilyProperty,
            FontSizeProperty,
            FontStyleProperty,
            FontWeightProperty,
            FontStretchProperty,
            LineHeightProperty,
            LineSpacingProperty,
            LetterSpacingProperty,
            MaxLinesProperty,
            TextProperty,
            TextAlignmentProperty,
            TextWrappingProperty,
            TextTrimmingProperty,
            TextDecorationsProperty,
            FontFeaturesProperty,
            ForegroundProperty,
            InlinesProperty);

        AffectsRender<ProTextBlock>(
            BackgroundProperty,
            ForegroundProperty,
            TextAlignmentProperty,
            TextProperty);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProTextBlock"/> class.
    /// </summary>
    public ProTextBlock()
    {
        _inlines = new InlineCollection();
        AttachInlineCollection(_inlines);
    }

    /// <summary>
    /// Gets or sets whether shared prepared-text cache entries are used by this control.
    /// </summary>
    public bool UseGlobalCache
    {
        get => GetValue(UseGlobalCacheProperty);
        set => SetValue(UseGlobalCacheProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the Pretext rendering path is enabled.
    /// </summary>
    public bool UsePretextRendering
    {
        get => GetValue(UsePretextRenderingProperty);
        set => SetValue(UsePretextRenderingProperty, value);
    }

    /// <summary>
    /// Gets or sets the Pretext whitespace handling mode.
    /// </summary>
    public WhiteSpaceMode PretextWhiteSpace
    {
        get => GetValue(PretextWhiteSpaceProperty);
        set => SetValue(PretextWhiteSpaceProperty, value);
    }

    /// <summary>
    /// Gets or sets the Pretext word-break handling mode.
    /// </summary>
    public WordBreakMode PretextWordBreak
    {
        get => GetValue(PretextWordBreakProperty);
        set => SetValue(PretextWordBreakProperty, value);
    }

    /// <summary>
    /// Gets or sets the fallback multiplier used when <see cref="LineHeight"/> is not explicitly set.
    /// </summary>
    public double PretextLineHeightMultiplier
    {
        get => GetValue(PretextLineHeightMultiplierProperty);
        set => SetValue(PretextLineHeightMultiplierProperty, value);
    }

    /// <summary>
    /// Gets or sets a brush used to paint the control's background.
    /// </summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the padding around the text.
    /// </summary>
    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the font family used to draw text.
    /// </summary>
    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size used to draw text.
    /// </summary>
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the font style used to draw text.
    /// </summary>
    public FontStyle FontStyle
    {
        get => GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the font weight used to draw text.
    /// </summary>
    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the font stretch used to draw text.
    /// </summary>
    public FontStretch FontStretch
    {
        get => GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush used to draw text.
    /// </summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of each text line.
    /// </summary>
    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the extra spacing after each text line.
    /// </summary>
    public double LineSpacing
    {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the letter spacing.
    /// </summary>
    public double LetterSpacing
    {
        get => GetValue(LetterSpacingProperty);
        set => SetValue(LetterSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum number of rendered text lines.
    /// </summary>
    public int MaxLines
    {
        get => GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    /// <summary>
    /// Gets or sets text wrapping behavior.
    /// </summary>
    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    /// <summary>
    /// Gets or sets text trimming behavior.
    /// </summary>
    public TextTrimming TextTrimming
    {
        get => GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    /// <summary>
    /// Gets or sets text alignment.
    /// </summary>
    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    /// <summary>
    /// Gets or sets text decorations.
    /// </summary>
    public TextDecorationCollection? TextDecorations
    {
        get => GetValue(TextDecorationsProperty);
        set => SetValue(TextDecorationsProperty, value);
    }

    /// <summary>
    /// Gets or sets OpenType font features.
    /// </summary>
    public FontFeatureCollection? FontFeatures
    {
        get => GetValue(FontFeaturesProperty);
        set => SetValue(FontFeaturesProperty, value);
    }

    /// <summary>
    /// Gets or sets inline content.
    /// </summary>
    [Content]
    public InlineCollection? Inlines
    {
        get => _inlines;
        set
        {
            if (ReferenceEquals(_inlines, value))
            {
                return;
            }

            DetachInlineObservers();

            SetAndRaise(InlinesProperty, ref _inlines, value);
            AttachInlineCollection(value);
            InvalidateProText();
        }
    }

    /// <summary>
    /// Gets or sets the baseline offset.
    /// </summary>
    public double BaselineOffset
    {
        get => GetValue(BaselineOffsetProperty);
        set => SetValue(BaselineOffsetProperty, value);
    }

    /// <summary>
    /// Gets the baseline offset attached property value from a control.
    /// </summary>
    public static double GetBaselineOffset(Control control) => control.GetValue(BaselineOffsetProperty);

    /// <summary>
    /// Sets the baseline offset attached property value on a control.
    /// </summary>
    public static void SetBaselineOffset(Control control, double value) => control.SetValue(BaselineOffsetProperty, value);

    /// <summary>
    /// Gets the text alignment attached property value from a control.
    /// </summary>
    public static TextAlignment GetTextAlignment(Control control) => control.GetValue(TextAlignmentProperty);

    /// <summary>
    /// Sets the text alignment attached property value on a control.
    /// </summary>
    public static void SetTextAlignment(Control control, TextAlignment value) => control.SetValue(TextAlignmentProperty, value);

    /// <summary>
    /// Gets the text wrapping attached property value from a control.
    /// </summary>
    public static TextWrapping GetTextWrapping(Control control) => control.GetValue(TextWrappingProperty);

    /// <summary>
    /// Sets the text wrapping attached property value on a control.
    /// </summary>
    public static void SetTextWrapping(Control control, TextWrapping value) => control.SetValue(TextWrappingProperty, value);

    /// <summary>
    /// Gets the text trimming attached property value from a control.
    /// </summary>
    public static TextTrimming GetTextTrimming(Control control) => control.GetValue(TextTrimmingProperty);

    /// <summary>
    /// Sets the text trimming attached property value on a control.
    /// </summary>
    public static void SetTextTrimming(Control control, TextTrimming value) => control.SetValue(TextTrimmingProperty, value);

    /// <summary>
    /// Gets the line height attached property value from a control.
    /// </summary>
    public static double GetLineHeight(Control control) => control.GetValue(LineHeightProperty);

    /// <summary>
    /// Sets the line height attached property value on a control.
    /// </summary>
    public static void SetLineHeight(Control control, double value) => control.SetValue(LineHeightProperty, value);

    /// <summary>
    /// Gets the line spacing attached property value from a control.
    /// </summary>
    public static double GetLineSpacing(Control control) => control.GetValue(LineSpacingProperty);

    /// <summary>
    /// Sets the line spacing attached property value on a control.
    /// </summary>
    public static void SetLineSpacing(Control control, double value) => control.SetValue(LineSpacingProperty, value);

    /// <summary>
    /// Gets the letter spacing attached property value from a control.
    /// </summary>
    public static double GetLetterSpacing(Control control) => control.GetValue(LetterSpacingProperty);

    /// <summary>
    /// Sets the letter spacing attached property value on a control.
    /// </summary>
    public static void SetLetterSpacing(Control control, double value) => control.SetValue(LetterSpacingProperty, value);

    /// <summary>
    /// Gets the max lines attached property value from a control.
    /// </summary>
    public static int GetMaxLines(Control control) => control.GetValue(MaxLinesProperty);

    /// <summary>
    /// Sets the max lines attached property value on a control.
    /// </summary>
    public static void SetMaxLines(Control control, int value) => control.SetValue(MaxLinesProperty, value);

    /// <inheritdoc />
    protected override bool BypassFlowDirectionPolicies => true;

    internal bool IsUsingFallback => false;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (TryCreateRichContent(out var content))
        {
            var padding = Padding;
            var contentSize = DeflateNonNegative(availableSize, padding);
            var snapshot = GetLayoutSnapshot(content, contentSize.Width);
            _layoutSnapshot = snapshot;

            return snapshot.Size.Inflate(padding);
        }

        return new Size().Inflate(Padding);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        if (!TryCreateRichContent(out var content))
        {
            return;
        }

        var controlBounds = new Rect(Bounds.Size);

        if (Background is { } background)
        {
            context.FillRectangle(background, controlBounds);
        }

        var padding = Padding;
        var contentSize = DeflateNonNegative(Bounds.Size, padding);

        if (contentSize.Width <= 0 || contentSize.Height <= 0)
        {
            return;
        }

        var snapshot = GetLayoutSnapshot(content, contentSize.Width);

        if (snapshot.LineCount == 0)
        {
            return;
        }

        var contentBounds = new Rect(
            padding.Left,
            padding.Top,
            contentSize.Width,
            contentSize.Height);

        using (context.PushClip(controlBounds))
        {
            context.Custom(new ProTextBlockDrawOperation(
                controlBounds,
                contentBounds,
                snapshot,
                TextAlignment,
                FlowDirection));
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty && _inlines is { Count: > 0 })
        {
            _inlines.Clear();
        }

        InvalidateProText();
    }

    private void OnInlinesInvalidated(object? sender, EventArgs e)
    {
        DetachInlineObservers();
        AttachInlineCollection(_inlines);
        InvalidateProText();
    }

    private void OnInlinePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        InvalidateProText();
    }

    private void AttachInlineCollection(InlineCollection? collection)
    {
        if (collection is null || !_observedInlineCollections.Add(collection))
        {
            return;
        }

        collection.Invalidated += OnInlinesInvalidated;

        foreach (var inline in collection)
        {
            AttachInline(inline);
        }
    }

    private void AttachInline(Inline inline)
    {
        if (!_observedInlines.Add(inline))
        {
            return;
        }

        inline.PropertyChanged += OnInlinePropertyChanged;

        if (inline is Span span)
        {
            AttachInlineCollection(span.Inlines);
        }
    }

    private void DetachInlineObservers()
    {
        foreach (var collection in _observedInlineCollections)
        {
            collection.Invalidated -= OnInlinesInvalidated;
        }

        foreach (var inline in _observedInlines)
        {
            inline.PropertyChanged -= OnInlinePropertyChanged;
        }

        _observedInlineCollections.Clear();
        _observedInlines.Clear();
    }

    private void InvalidateProText()
    {
        _layoutSnapshot = null;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private bool TryCreateRichContent(out ProTextRichContent content)
    {
        content = null!;

        if (!UsePretextRendering)
        {
            return false;
        }

        var baseStyle = CreateBaseStyle();

        if (Inlines is { Count: > 0 })
        {
            return ProTextInlineBuilder.TryCreateInlineContent(Inlines, baseStyle, out content);
        }

        content = ProTextInlineBuilder.CreateTextContent(Text, baseStyle);
        return true;
    }

    private ProTextLayoutSnapshot GetLayoutSnapshot(ProTextRichContent content, double availableWidth)
    {
        var maxWidth = ResolveMaxWidth(availableWidth);
        var lineHeight = GetEffectiveLineHeight(content);
        var maxLines = MaxLines;
        var textWrapping = TextWrapping;
        var textTrimming = TextTrimming;

        if (_layoutSnapshot is { } snapshot && snapshot.Matches(content, maxWidth, lineHeight, maxLines, textWrapping, textTrimming))
        {
            return snapshot;
        }

        var prepared = GetPreparedContent(content);

        snapshot = new ProTextLayoutSnapshot(
            content,
            prepared,
            maxWidth,
            lineHeight,
            maxLines,
            textWrapping,
            textTrimming);

        _layoutSnapshot = snapshot;
        return snapshot;
    }

    private ProTextPreparedContent GetPreparedContent(ProTextRichContent content)
    {
        var key = new ProTextRichCacheKey(content.LayoutFingerprint);

        if (!UseGlobalCache && _localPrepared is not null && _localPreparedKey == key)
        {
            return _localPrepared;
        }

        var preparedParagraphs = new PreparedRichInline[content.Paragraphs.Count];

        for (var i = 0; i < content.Paragraphs.Count; i++)
        {
            var paragraph = content.Paragraphs[i];
            var items = paragraph.CreateInlineItems();

            preparedParagraphs[i] = UseGlobalCache
                ? ProTextBlockCache.GetOrPrepareRich(new ProTextRichCacheKey(paragraph.LayoutFingerprint), items)
                : ProTextBlockCache.PrepareRichUncached(items);
        }

        var prepared = new ProTextPreparedContent(preparedParagraphs);

        if (!UseGlobalCache)
        {
            _localPreparedKey = key;
            _localPrepared = prepared;
        }

        return prepared;
    }

    private ProTextRichStyle CreateBaseStyle()
    {
        return ProTextInlineBuilder.CreateStyle(
            FontFamily,
            FontSize,
            FontStyle,
            FontWeight,
            FontStretch,
            Foreground,
            TextDecorations,
            FontFeatures,
            LetterSpacing);
    }

    private double ResolveMaxWidth(double availableWidth)
    {
        if ((TextWrapping == TextWrapping.NoWrap && ReferenceEquals(TextTrimming, TextTrimming.None)) || double.IsInfinity(availableWidth))
        {
            return double.PositiveInfinity;
        }

        if (double.IsNaN(availableWidth))
        {
            return 0;
        }

        return Math.Max(0, availableWidth);
    }

    private double GetEffectiveLineHeight(ProTextRichContent content)
    {
        var fontSize = Math.Max(FontSize, content.MaxFontSize);
        var baseLineHeight = double.IsNaN(LineHeight) ? fontSize * PretextLineHeightMultiplier : LineHeight;
        return Math.Max(0, baseLineHeight + LineSpacing);
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
}