using System.Collections.Specialized;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using Pretext;
using ProText.Core;
using ProText.MAUI.Internal;
using SkiaSharp;

namespace ProText.MAUI;

/// <summary>
/// A high-performance MAUI text display control powered by PretextSharp.
/// </summary>
[ContentProperty(nameof(FormattedText))]
public class ProTextBlock : ContentView
{
    /// <summary>
    /// Defines the <see cref="UseGlobalCache"/> property.
    /// </summary>
    public static readonly BindableProperty UseGlobalCacheProperty =
        BindableProperty.Create(nameof(UseGlobalCache), typeof(bool), typeof(ProTextBlock), true, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="UsePretextRendering"/> property.
    /// </summary>
    public static readonly BindableProperty UsePretextRenderingProperty =
        BindableProperty.Create(nameof(UsePretextRendering), typeof(bool), typeof(ProTextBlock), true, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="PretextWhiteSpace"/> property.
    /// </summary>
    public static readonly BindableProperty PretextWhiteSpaceProperty =
        BindableProperty.Create(nameof(PretextWhiteSpace), typeof(WhiteSpaceMode), typeof(ProTextBlock), WhiteSpaceMode.Normal, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="PretextWordBreak"/> property.
    /// </summary>
    public static readonly BindableProperty PretextWordBreakProperty =
        BindableProperty.Create(nameof(PretextWordBreak), typeof(WordBreakMode), typeof(ProTextBlock), WordBreakMode.Normal, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="PretextLineHeightMultiplier"/> property.
    /// </summary>
    public static readonly BindableProperty PretextLineHeightMultiplierProperty =
        BindableProperty.Create(nameof(PretextLineHeightMultiplier), typeof(double), typeof(ProTextBlock), 1.2d, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="Text"/> property.
    /// </summary>
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(ProTextBlock), null, propertyChanged: OnTextPropertyChanged);

    /// <summary>
    /// Defines the <see cref="FormattedText"/> property.
    /// </summary>
    public static readonly BindableProperty FormattedTextProperty =
        BindableProperty.Create(nameof(FormattedText), typeof(FormattedString), typeof(ProTextBlock), null, propertyChanged: OnFormattedTextPropertyChanged);

    /// <summary>
    /// Defines the <see cref="Foreground"/> property.
    /// </summary>
    public static readonly BindableProperty ForegroundProperty =
        BindableProperty.Create(nameof(Foreground), typeof(Brush), typeof(ProTextBlock), null, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="FontFamily"/> property.
    /// </summary>
    public static readonly BindableProperty FontFamilyProperty =
        BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(ProTextBlock), null, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="FontSize"/> property.
    /// </summary>
    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(ProTextBlock), 14d, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="FontAttributes"/> property.
    /// </summary>
    public static readonly BindableProperty FontAttributesProperty =
        BindableProperty.Create(nameof(FontAttributes), typeof(FontAttributes), typeof(ProTextBlock), FontAttributes.None, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="FontWeight"/> property.
    /// </summary>
    public static readonly BindableProperty FontWeightProperty =
        BindableProperty.Create(nameof(FontWeight), typeof(int), typeof(ProTextBlock), 400, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="FontStretch"/> property.
    /// </summary>
    public static readonly BindableProperty FontStretchProperty =
        BindableProperty.Create(nameof(FontStretch), typeof(int), typeof(ProTextBlock), 5, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="TextDecorations"/> property.
    /// </summary>
    public static readonly BindableProperty TextDecorationsProperty =
        BindableProperty.Create(nameof(TextDecorations), typeof(TextDecorations), typeof(ProTextBlock), TextDecorations.None, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="FontFeatures"/> property.
    /// </summary>
    public static readonly BindableProperty FontFeaturesProperty =
        BindableProperty.Create(nameof(FontFeatures), typeof(string), typeof(ProTextBlock), null, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="LineHeight"/> property.
    /// </summary>
    public static readonly BindableProperty LineHeightProperty =
        BindableProperty.Create(nameof(LineHeight), typeof(double), typeof(ProTextBlock), 0d, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="LineSpacing"/> property.
    /// </summary>
    public static readonly BindableProperty LineSpacingProperty =
        BindableProperty.Create(nameof(LineSpacing), typeof(double), typeof(ProTextBlock), 0d, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="CharacterSpacing"/> property.
    /// </summary>
    public static readonly BindableProperty CharacterSpacingProperty =
        BindableProperty.Create(nameof(CharacterSpacing), typeof(double), typeof(ProTextBlock), 0d, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="LetterSpacing"/> property.
    /// </summary>
    public static readonly BindableProperty LetterSpacingProperty =
        BindableProperty.Create(nameof(LetterSpacing), typeof(double), typeof(ProTextBlock), 0d, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="MaxLines"/> property.
    /// </summary>
    public static readonly BindableProperty MaxLinesProperty =
        BindableProperty.Create(nameof(MaxLines), typeof(int), typeof(ProTextBlock), 0, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="HorizontalTextAlignment"/> property.
    /// </summary>
    public static readonly BindableProperty HorizontalTextAlignmentProperty =
        BindableProperty.Create(nameof(HorizontalTextAlignment), typeof(TextAlignment), typeof(ProTextBlock), TextAlignment.Start, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="LineBreakMode"/> property.
    /// </summary>
    public static readonly BindableProperty LineBreakModeProperty =
        BindableProperty.Create(nameof(LineBreakMode), typeof(LineBreakMode), typeof(ProTextBlock), LineBreakMode.NoWrap, propertyChanged: OnMeasurePropertyChanged);

    /// <summary>
    /// Defines the <see cref="BaselineOffset"/> property.
    /// </summary>
    public static readonly BindableProperty BaselineOffsetProperty =
        BindableProperty.Create(nameof(BaselineOffset), typeof(double), typeof(ProTextBlock), 0d, propertyChanged: OnMeasurePropertyChanged);

    private readonly ProTextLayoutCache _layoutCache = new();
    private readonly ProTextMauiCanvasView _canvasView;
    private readonly HashSet<Span> _observedSpans = new();
    private FormattedString? _observedFormattedText;
    private INotifyCollectionChanged? _observedSpanCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProTextBlock"/> class.
    /// </summary>
    public ProTextBlock()
    {
        ProTextMauiPlatform.EnsureConfigured();
        _canvasView = new ProTextMauiCanvasView(this);
        base.Content = _canvasView;
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
    /// Gets or sets the text.
    /// </summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets rich formatted text made of MAUI spans.
    /// </summary>
    public FormattedString? FormattedText
    {
        get => (FormattedString?)GetValue(FormattedTextProperty);
        set => SetValue(FormattedTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush used to draw text.
    /// </summary>
    public Brush? Foreground
    {
        get => (Brush?)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the font family used to draw text.
    /// </summary>
    public string? FontFamily
    {
        get => (string?)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size used to draw text.
    /// </summary>
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the MAUI font attributes used to draw text.
    /// </summary>
    public FontAttributes FontAttributes
    {
        get => (FontAttributes)GetValue(FontAttributesProperty);
        set => SetValue(FontAttributesProperty, value);
    }

    /// <summary>
    /// Gets or sets the OpenType weight used in the ProText font identity.
    /// </summary>
    public int FontWeight
    {
        get => (int)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the OpenType stretch width used in the ProText font identity.
    /// </summary>
    public int FontStretch
    {
        get => (int)GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
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
    /// Gets or sets MAUI character spacing in device-independent units.
    /// </summary>
    public double CharacterSpacing
    {
        get => (double)GetValue(CharacterSpacingProperty);
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
    /// Gets or sets horizontal text alignment.
    /// </summary>
    public TextAlignment HorizontalTextAlignment
    {
        get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
        set => SetValue(HorizontalTextAlignmentProperty, value);
    }

    /// <summary>
    /// Gets or sets wrapping and trimming behavior.
    /// </summary>
    public LineBreakMode LineBreakMode
    {
        get => (LineBreakMode)GetValue(LineBreakModeProperty);
        set => SetValue(LineBreakModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the baseline offset reserved for API compatibility.
    /// </summary>
    public double BaselineOffset
    {
        get => (double)GetValue(BaselineOffsetProperty);
        set => SetValue(BaselineOffsetProperty, value);
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

        return ProTextMauiAdapter.ToMaui(GetLayoutSnapshot(content, availableWidth).Size);
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
        return ProTextMauiAdapter.ToMaui(ProTextLayoutServices.GetLineBounds(snapshot, lineIndex));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        if (!TryCreateRichContent(out var content))
        {
            return Inflate(default, Padding);
        }

        var padding = Padding;
        var contentSize = DeflateNonNegative(new Size(widthConstraint, heightConstraint), padding);
        var snapshot = GetLayoutSnapshot(content, contentSize.Width);

        return Inflate(ProTextMauiAdapter.ToMaui(snapshot.Size), padding);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Rect bounds)
    {
        _canvasView.Arrange(new Rect(0, 0, bounds.Width, bounds.Height));
        return new Size(bounds.Width, bounds.Height);
    }

    internal void Render(SKCanvas canvas, Size area)
    {
        if (!TryCreateRichContent(out var content))
        {
            return;
        }

        var controlBounds = new ProTextRect(0, 0, Math.Max(0, area.Width), Math.Max(0, area.Height));
        var background = ProTextMauiAdapter.SnapshotBrush(Background);
        ProTextMauiSkiaBrushRenderer.DrawRect(canvas, controlBounds, background, Opacity);

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

        if (FormattedText is { Spans.Count: > 0 } formattedText)
        {
            return ProTextMauiInlineBuilder.TryCreateFormattedContent(formattedText, baseStyle, out content);
        }

        content = CreateTextContent(baseStyle);
        return true;
    }

    /// <summary>
    /// Creates text content when no formatted spans are present.
    /// </summary>
    protected virtual ProTextRichContent CreateTextContent(ProTextRichStyle baseStyle)
    {
        return ProTextMauiInlineBuilder.CreateTextContent(Text, baseStyle);
    }

    /// <summary>
    /// Creates a framework-neutral style from current MAUI properties.
    /// </summary>
    protected ProTextRichStyle CreateBaseStyle(Brush? foreground, TextDecorations textDecorations)
    {
        return ProTextMauiInlineBuilder.CreateStyle(
            FontFamily,
            FontSize,
            FontAttributes,
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
        ProTextMauiPlatform.EnsureConfigured();

        var maxWidth = ResolveMaxWidth(availableWidth);
        var lineHeight = GetEffectiveLineHeight(content);

        return _layoutCache.GetSnapshot(
            content,
            new ProTextLayoutRequest(
                maxWidth,
                lineHeight,
                Math.Max(0, MaxLines),
                ProTextMauiAdapter.ToWrapping(LineBreakMode),
                ProTextMauiAdapter.ToTrimming(LineBreakMode),
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
                ProTextMauiAdapter.ToCore(HorizontalTextAlignment),
                ProTextMauiAdapter.ToCore(FlowDirection),
                Opacity));
    }

    /// <summary>
    /// Gets the current layout width used by hit testing APIs.
    /// </summary>
    protected double GetLayoutWidth()
    {
        return Width > 0 ? Width : double.PositiveInfinity;
    }

    /// <summary>
    /// Resolves a layout width according to wrapping and trimming settings.
    /// </summary>
    protected double ResolveMaxWidth(double availableWidth)
    {
        return ProTextLayoutServices.ResolveMaxWidth(
            availableWidth,
            ProTextMauiAdapter.ToWrapping(LineBreakMode),
            ProTextMauiAdapter.ToTrimming(LineBreakMode));
    }

    /// <summary>
    /// Gets the effective line height used by the core layout engine.
    /// </summary>
    protected double GetEffectiveLineHeight(ProTextRichContent content)
    {
        return ProTextLayoutServices.GetEffectiveLineHeight(
            FontSize,
            content.MaxFontSize,
            ProTextMauiAdapter.NormalizeLineHeight(LineHeight),
            LineSpacing,
            PretextLineHeightMultiplier);
    }

    /// <summary>
    /// Invalidates the internal Skia drawing surface.
    /// </summary>
    protected void InvalidateRender()
    {
        _canvasView.InvalidateSurface();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName is nameof(Padding) or nameof(Background) or nameof(FlowDirection))
        {
            InvalidateProText();
            return;
        }

        if (propertyName is nameof(Opacity) or nameof(Width) or nameof(Height))
        {
            InvalidateRender();
        }
    }

    private void AttachFormattedText(FormattedString? formattedText)
    {
        if (formattedText is null || ReferenceEquals(_observedFormattedText, formattedText))
        {
            return;
        }

        _observedFormattedText = formattedText;
        formattedText.PropertyChanged += OnFormattedTextPartChanged;

        if (formattedText.Spans is INotifyCollectionChanged spanCollection)
        {
            _observedSpanCollection = spanCollection;
            spanCollection.CollectionChanged += OnFormattedTextCollectionChanged;
        }

        foreach (var span in formattedText.Spans)
        {
            AttachSpan(span);
        }
    }

    private void DetachFormattedText()
    {
        if (_observedFormattedText is not null)
        {
            _observedFormattedText.PropertyChanged -= OnFormattedTextPartChanged;
            _observedFormattedText = null;
        }

        if (_observedSpanCollection is not null)
        {
            _observedSpanCollection.CollectionChanged -= OnFormattedTextCollectionChanged;
            _observedSpanCollection = null;
        }

        foreach (var span in _observedSpans)
        {
            span.PropertyChanged -= OnFormattedTextPartChanged;
        }

        _observedSpans.Clear();
    }

    private void AttachSpan(Span span)
    {
        if (!_observedSpans.Add(span))
        {
            return;
        }

        span.PropertyChanged += OnFormattedTextPartChanged;
    }

    private void OnFormattedTextCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var formattedText = _observedFormattedText;
        DetachFormattedText();
        AttachFormattedText(formattedText);
        InvalidateProText();
    }

    private void OnFormattedTextPartChanged(object? sender, EventArgs e)
    {
        InvalidateProText();
    }

    private static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not ProTextBlock block)
        {
            return;
        }

        if (block.FormattedText is not null)
        {
            block.FormattedText = null;
        }

        block.InvalidateProText();
    }

    private static void OnFormattedTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not ProTextBlock block)
        {
            return;
        }

        block.DetachFormattedText();
        block.AttachFormattedText(newValue as FormattedString);
        block.InvalidateProText();
    }

    private static void OnMeasurePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ProTextBlock block)
        {
            block.InvalidateProText();
        }
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
