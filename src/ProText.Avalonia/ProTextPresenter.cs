using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Threading;
using ProText.Core;
using ProText.Avalonia.Internal;

namespace ProText.Avalonia;

/// <summary>
/// Presents editable or selectable text through the same PretextSharp layout and Skia rendering path used by <see cref="ProTextBlock"/>.
/// </summary>
public class ProTextPresenter : Control
{
    /// <summary>
    /// Defines the <see cref="UseGlobalCache"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> UseGlobalCacheProperty =
        AvaloniaProperty.Register<ProTextPresenter, bool>(nameof(UseGlobalCache), true);

    /// <summary>
    /// Defines the <see cref="UsePretextRendering"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> UsePretextRenderingProperty =
        AvaloniaProperty.Register<ProTextPresenter, bool>(nameof(UsePretextRendering), true);

    /// <summary>
    /// Defines the <see cref="PretextLineHeightMultiplier"/> property.
    /// </summary>
    public static readonly StyledProperty<double> PretextLineHeightMultiplierProperty =
        AvaloniaProperty.Register<ProTextPresenter, double>(
            nameof(PretextLineHeightMultiplier),
            1.2,
            validate: value => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0);

    /// <summary>
    /// Defines the <see cref="Text"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty =
        TextBlock.TextProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="PreeditText"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> PreeditTextProperty =
        AvaloniaProperty.Register<ProTextPresenter, string?>(nameof(PreeditText));

    /// <summary>
    /// Defines the <see cref="PreeditTextCursorPosition"/> property.
    /// </summary>
    public static readonly StyledProperty<int?> PreeditTextCursorPositionProperty =
        AvaloniaProperty.Register<ProTextPresenter, int?>(nameof(PreeditTextCursorPosition));

    /// <summary>
    /// Defines the <see cref="Background"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="FontFamily"/> property.
    /// </summary>
    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="FontSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="FontStyle"/> property.
    /// </summary>
    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        TextElement.FontStyleProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="FontWeight"/> property.
    /// </summary>
    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextElement.FontWeightProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="FontStretch"/> property.
    /// </summary>
    public static readonly StyledProperty<FontStretch> FontStretchProperty =
        TextElement.FontStretchProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="Foreground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="TextAlignment"/> property.
    /// </summary>
    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        TextBlock.TextAlignmentProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="TextWrapping"/> property.
    /// </summary>
    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        TextBlock.TextWrappingProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="TextTrimming"/> property.
    /// </summary>
    public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
        TextBlock.TextTrimmingProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="LineHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LineHeightProperty =
        TextBlock.LineHeightProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="LineSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LineSpacingProperty =
        TextBlock.LineSpacingProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="LetterSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LetterSpacingProperty =
        TextBlock.LetterSpacingProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="TextDecorations"/> property.
    /// </summary>
    public static readonly StyledProperty<TextDecorationCollection?> TextDecorationsProperty =
        TextBlock.TextDecorationsProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="FontFeatures"/> property.
    /// </summary>
    public static readonly StyledProperty<FontFeatureCollection?> FontFeaturesProperty =
        TextBlock.FontFeaturesProperty.AddOwner<ProTextPresenter>();

    /// <summary>
    /// Defines the <see cref="CaretIndex"/> property.
    /// </summary>
    public static readonly StyledProperty<int> CaretIndexProperty =
        AvaloniaProperty.Register<ProTextPresenter, int>(nameof(CaretIndex));

    /// <summary>
    /// Defines the <see cref="SelectionStart"/> property.
    /// </summary>
    public static readonly StyledProperty<int> SelectionStartProperty =
        AvaloniaProperty.Register<ProTextPresenter, int>(nameof(SelectionStart));

    /// <summary>
    /// Defines the <see cref="SelectionEnd"/> property.
    /// </summary>
    public static readonly StyledProperty<int> SelectionEndProperty =
        AvaloniaProperty.Register<ProTextPresenter, int>(nameof(SelectionEnd));

    /// <summary>
    /// Defines the <see cref="ShowSelectionHighlight"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowSelectionHighlightProperty =
        AvaloniaProperty.Register<ProTextPresenter, bool>(nameof(ShowSelectionHighlight), true);

    /// <summary>
    /// Defines the <see cref="SelectionBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
        AvaloniaProperty.Register<ProTextPresenter, IBrush?>(nameof(SelectionBrush), new SolidColorBrush(Color.FromArgb(96, 0, 120, 215)));

    /// <summary>
    /// Defines the <see cref="SelectionForegroundBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> SelectionForegroundBrushProperty =
        AvaloniaProperty.Register<ProTextPresenter, IBrush?>(nameof(SelectionForegroundBrush));

    /// <summary>
    /// Defines the <see cref="CaretBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> CaretBrushProperty =
        AvaloniaProperty.Register<ProTextPresenter, IBrush?>(nameof(CaretBrush));

    /// <summary>
    /// Defines the <see cref="CaretBlinkInterval"/> property.
    /// </summary>
    public static readonly StyledProperty<TimeSpan> CaretBlinkIntervalProperty =
        AvaloniaProperty.Register<ProTextPresenter, TimeSpan>(nameof(CaretBlinkInterval), TimeSpan.FromMilliseconds(500));

    /// <summary>
    /// Defines the <see cref="PasswordChar"/> property.
    /// </summary>
    public static readonly StyledProperty<char> PasswordCharProperty =
        AvaloniaProperty.Register<ProTextPresenter, char>(nameof(PasswordChar));

    /// <summary>
    /// Defines the <see cref="RevealPassword"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> RevealPasswordProperty =
        AvaloniaProperty.Register<ProTextPresenter, bool>(nameof(RevealPassword));

    /// <summary>
    /// Defines the <see cref="Inlines"/> property.
    /// </summary>
    public static readonly DirectProperty<ProTextPresenter, InlineCollection?> InlinesProperty =
        AvaloniaProperty.RegisterDirect<ProTextPresenter, InlineCollection?>(
            nameof(Inlines),
            control => control.Inlines,
            (control, value) => control.Inlines = value);

    private InlineCollection? _inlines;
    private readonly HashSet<InlineCollection> _observedInlineCollections = new();
    private readonly HashSet<Inline> _observedInlines = new();
    private readonly ProTextLayoutCache _layoutCache = new();
    private readonly ProTextSelectionGeometryCache _selectionGeometryCache = new();
    private ProTextRichContent? _content;
    private IBrush? _selectionBrushSnapshotSource;
    private ProTextBrush? _selectionBrushSnapshot;
    private IBrush? _selectionForegroundSnapshotSource;
    private ProTextBrush? _selectionForegroundSnapshot;
    private ProTextDrawOperation? _drawOperation;
    private ProTextLayoutSnapshot? _drawOperationSnapshot;
    private Rect _drawOperationBounds;
    private TextAlignment _drawOperationTextAlignment;
    private FlowDirection _drawOperationFlowDirection;
    private ProTextBrush? _drawOperationSelectionForeground;
    private ProTextBrush? _drawOperationSelectionBackground;
    private ProTextSelectionRect[]? _drawOperationSelectionRects;
    private int _drawOperationSelectionStart;
    private int _drawOperationSelectionEnd;
    private DispatcherTimer? _caretTimer;
    private bool _showCaret;
    private bool _caretBlink;
    private Rect _caretBounds;

    static ProTextPresenter()
    {
        ClipToBoundsProperty.OverrideDefaultValue<ProTextPresenter>(true);

        AffectsMeasure<ProTextPresenter>(
            UseGlobalCacheProperty,
            UsePretextRenderingProperty,
            PretextLineHeightMultiplierProperty,
            TextProperty,
            PreeditTextProperty,
            PreeditTextCursorPositionProperty,
            TextAlignmentProperty,
            TextWrappingProperty,
            TextTrimmingProperty,
            LineHeightProperty,
            LineSpacingProperty,
            LetterSpacingProperty,
            TextDecorationsProperty,
            FontFeaturesProperty,
            FontFamilyProperty,
            FontSizeProperty,
            FontStyleProperty,
            FontWeightProperty,
            FontStretchProperty,
            ForegroundProperty,
            PasswordCharProperty,
            RevealPasswordProperty,
            InlinesProperty);

        AffectsRender<ProTextPresenter>(
            BackgroundProperty,
            CaretBrushProperty,
            SelectionBrushProperty,
            SelectionForegroundBrushProperty,
            SelectionStartProperty,
            SelectionEndProperty,
            ShowSelectionHighlightProperty);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProTextPresenter"/> class.
    /// </summary>
    public ProTextPresenter()
    {
        _inlines = new InlineCollection();
        AttachInlineCollection(_inlines);
    }

    /// <summary>
    /// Raised when the computed caret bounds change.
    /// </summary>
    public event EventHandler? CaretBoundsChanged;

    /// <summary>
    /// Gets or sets whether shared prepared-text cache entries are used.
    /// </summary>
    public bool UseGlobalCache
    {
        get => GetValue(UseGlobalCacheProperty);
        set => SetValue(UseGlobalCacheProperty, value);
    }

    /// <summary>
    /// Gets or sets whether Pretext rendering is enabled.
    /// </summary>
    public bool UsePretextRendering
    {
        get => GetValue(UsePretextRenderingProperty);
        set => SetValue(UsePretextRenderingProperty, value);
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
    /// Gets or sets the text.
    /// </summary>
    [Content]
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets active IME preedit text inserted at the caret.
    /// </summary>
    public string? PreeditText
    {
        get => GetValue(PreeditTextProperty);
        set => SetValue(PreeditTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the cursor position inside <see cref="PreeditText"/>.
    /// </summary>
    public int? PreeditTextCursorPosition
    {
        get => GetValue(PreeditTextCursorPositionProperty);
        set => SetValue(PreeditTextCursorPositionProperty, value);
    }

    /// <summary>
    /// Gets or sets a brush used to paint the background.
    /// </summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the font style.
    /// </summary>
    public FontStyle FontStyle
    {
        get => GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the font weight.
    /// </summary>
    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the font stretch.
    /// </summary>
    public FontStretch FontStretch
    {
        get => GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush.
    /// </summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
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
    /// Gets or sets text decorations.
    /// </summary>
    public TextDecorationCollection? TextDecorations
    {
        get => GetValue(TextDecorationsProperty);
        set => SetValue(TextDecorationsProperty, value);
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
    /// Gets or sets text wrapping.
    /// </summary>
    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    /// <summary>
    /// Gets or sets text trimming.
    /// </summary>
    public TextTrimming TextTrimming
    {
        get => GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    /// <summary>
    /// Gets or sets line height.
    /// </summary>
    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets extra line spacing.
    /// </summary>
    public double LineSpacing
    {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets letter spacing.
    /// </summary>
    public double LetterSpacing
    {
        get => GetValue(LetterSpacingProperty);
        set => SetValue(LetterSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets rich inline content. Inline content is display-oriented; caret editing APIs are intended for plain text.
    /// </summary>
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
    /// Gets or sets the caret index.
    /// </summary>
    public int CaretIndex
    {
        get => GetValue(CaretIndexProperty);
        set => SetValue(CaretIndexProperty, Math.Max(0, value));
    }

    /// <summary>
    /// Gets or sets the selection start index.
    /// </summary>
    public int SelectionStart
    {
        get => GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, Math.Max(0, value));
    }

    /// <summary>
    /// Gets or sets the selection end index.
    /// </summary>
    public int SelectionEnd
    {
        get => GetValue(SelectionEndProperty);
        set => SetValue(SelectionEndProperty, Math.Max(0, value));
    }

    /// <summary>
    /// Gets or sets whether selection highlight is shown.
    /// </summary>
    public bool ShowSelectionHighlight
    {
        get => GetValue(ShowSelectionHighlightProperty);
        set => SetValue(ShowSelectionHighlightProperty, value);
    }

    /// <summary>
    /// Gets or sets the selection background brush.
    /// </summary>
    public IBrush? SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush used for selected text.
    /// </summary>
    public IBrush? SelectionForegroundBrush
    {
        get => GetValue(SelectionForegroundBrushProperty);
        set => SetValue(SelectionForegroundBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the caret brush.
    /// </summary>
    public IBrush? CaretBrush
    {
        get => GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the caret blink interval.
    /// </summary>
    public TimeSpan CaretBlinkInterval
    {
        get => GetValue(CaretBlinkIntervalProperty);
        set => SetValue(CaretBlinkIntervalProperty, value);
    }

    /// <summary>
    /// Gets or sets the password masking character. The default value disables masking.
    /// </summary>
    public char PasswordChar
    {
        get => GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    /// <summary>
    /// Gets or sets whether password text is revealed.
    /// </summary>
    public bool RevealPassword
    {
        get => GetValue(RevealPasswordProperty);
        set => SetValue(RevealPasswordProperty, value);
    }

    /// <inheritdoc />
    protected override bool BypassFlowDirectionPolicies => true;

    /// <summary>
    /// Shows the caret and starts caret blinking.
    /// </summary>
    public void ShowCaret()
    {
        _showCaret = true;
        _caretBlink = true;
        EnsureCaretTimer();
        _caretTimer?.Start();
        InvalidateVisual();
    }

    /// <summary>
    /// Hides the caret and stops caret blinking.
    /// </summary>
    public void HideCaret()
    {
        _showCaret = false;
        _caretBlink = false;
        _caretTimer?.Stop();
        InvalidateVisual();
    }

    /// <summary>
    /// Moves the caret to a text index.
    /// </summary>
    public void MoveCaretToTextPosition(int textPosition)
    {
        SetCurrentValue(CaretIndexProperty, Math.Clamp(textPosition, 0, GetCurrentTextLength()));
        UpdateCaretBounds();
        InvalidateVisual();
    }

    /// <summary>
    /// Moves the caret to the text position nearest a point in presenter coordinates.
    /// </summary>
    public void MoveCaretToPoint(Point point)
    {
        MoveCaretToTextPosition(GetCharacterIndex(point));
    }

    /// <summary>
    /// Gets the next caret hit relative to the current caret index.
    /// </summary>
    public CharacterHit GetNextCharacterHit(LogicalDirection direction)
    {
        var textLength = GetCurrentTextLength();
        var index = direction == LogicalDirection.Forward
            ? Math.Min(textLength, CaretIndex + 1)
            : Math.Max(0, CaretIndex - 1);

        return new CharacterHit(index, 0);
    }

    /// <summary>
    /// Moves the caret one logical character in the requested direction.
    /// </summary>
    public void MoveCaretHorizontal(LogicalDirection direction)
    {
        MoveCaretToTextPosition(GetNextCharacterHit(direction).FirstCharacterIndex);
    }

    /// <summary>
    /// Moves the caret one rendered line in the requested direction.
    /// </summary>
    public void MoveCaretVertical(LogicalDirection direction)
    {
        if (!TryCreateRichContent(out var content))
        {
            return;
        }

        var snapshot = GetLayoutSnapshot(content, Bounds.Width > 0 ? Bounds.Width : double.PositiveInfinity);
        var bounds = ProTextAvaloniaAdapter.ToAvalonia(ProTextLayoutServices.GetCaretBounds(
            snapshot,
            Math.Clamp(GetEffectiveCaretIndex(), 0, content.Text.Length),
            Bounds.Width,
            ProTextAvaloniaAdapter.ToCore(TextAlignment),
            ProTextAvaloniaAdapter.ToCore(FlowDirection)));
        var y = direction == LogicalDirection.Forward
            ? bounds.Y + snapshot.LineHeight + snapshot.LineHeight / 2
            : bounds.Y - snapshot.LineHeight / 2;
        var index = ProTextLayoutServices.GetCharacterIndex(
            snapshot,
            new ProTextPoint(bounds.X, y),
            Bounds.Width,
            ProTextAvaloniaAdapter.ToCore(TextAlignment),
            ProTextAvaloniaAdapter.ToCore(FlowDirection));
        MoveCaretToTextPosition(index);
    }

    /// <summary>
    /// Gets the text index nearest a point in presenter coordinates.
    /// </summary>
    public int GetCharacterIndex(Point point)
    {
        if (!TryCreateRichContent(out var content))
        {
            return 0;
        }

        var snapshot = GetLayoutSnapshot(content, Bounds.Width > 0 ? Bounds.Width : double.PositiveInfinity);
        return ProTextLayoutServices.GetCharacterIndex(
            snapshot,
            ProTextAvaloniaAdapter.ToCore(point),
            Bounds.Width,
            ProTextAvaloniaAdapter.ToCore(TextAlignment),
            ProTextAvaloniaAdapter.ToCore(FlowDirection));
    }

    /// <summary>
    /// Gets caret bounds for a text index in presenter coordinates.
    /// </summary>
    public Rect GetCaretBounds(int textPosition)
    {
        if (!TryCreateRichContent(out var content))
        {
            return default;
        }

        var snapshot = GetLayoutSnapshot(content, Bounds.Width > 0 ? Bounds.Width : double.PositiveInfinity);
        return ProTextAvaloniaAdapter.ToAvalonia(ProTextLayoutServices.GetCaretBounds(
            snapshot,
            Math.Clamp(textPosition, 0, content.Text.Length),
            Bounds.Width,
            ProTextAvaloniaAdapter.ToCore(TextAlignment),
            ProTextAvaloniaAdapter.ToCore(FlowDirection)));
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

        return ProTextAvaloniaAdapter.ToAvalonia(GetLayoutSnapshot(content, availableWidth).Size);
    }

    /// <summary>
    /// Gets the number of materialized layout lines, or -1 before layout is available.
    /// </summary>
    public int GetLineCount()
    {
        if (!TryCreateRichContent(out var content))
        {
            return -1;
        }

        return GetLayoutSnapshot(content, Bounds.Width > 0 ? Bounds.Width : double.PositiveInfinity).LineCount;
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

        var snapshot = GetLayoutSnapshot(content, Bounds.Width > 0 ? Bounds.Width : double.PositiveInfinity);

        if (snapshot.LineCount == 0)
        {
            return default;
        }

        return ProTextAvaloniaAdapter.ToAvalonia(ProTextLayoutServices.GetLineBounds(snapshot, lineIndex));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (!TryCreateRichContent(out var content))
        {
            return default;
        }

        var snapshot = GetLayoutSnapshot(content, availableSize.Width);

        if (ShouldUpdateCaretBounds())
        {
            UpdateCaretBounds(snapshot, content);
        }

        return ProTextAvaloniaAdapter.ToAvalonia(snapshot.Size);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (TryCreateRichContent(out var content))
        {
            var snapshot = GetLayoutSnapshot(content, finalSize.Width);

            if (ShouldUpdateCaretBounds())
            {
                UpdateCaretBounds(snapshot, content);
            }
        }

        return finalSize;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        if (!TryCreateRichContent(out var content))
        {
            return;
        }

        var bounds = new Rect(Bounds.Size);

        if (Background is { } background)
        {
            context.FillRectangle(background, bounds);
        }

        var snapshot = GetLayoutSnapshot(content, Bounds.Width);

        if (snapshot.LineCount == 0)
        {
            return;
        }

        var selectionRects = GetSelectionRects(snapshot);
        var selectionBackground = selectionRects.Length > 0 ? GetSelectionBrushSnapshot() : null;

        var selectionForeground = selectionRects.Length > 0 && ShouldUseSelectionForeground()
            ? GetSelectionForegroundSnapshot()
            : null;
        var selectionStart = Math.Min(SelectionStart, SelectionEnd);
        var selectionEnd = Math.Max(SelectionStart, SelectionEnd);

        context.Custom(GetDrawOperation(
            bounds,
            snapshot,
            selectionForeground,
            selectionStart,
            selectionEnd,
            selectionBackground,
            selectionRects));

        DrawCaret(context, snapshot, content);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CaretBlinkIntervalProperty)
        {
            ResetCaretTimer();
            return;
        }

        if (change.Property == CaretIndexProperty)
        {
            if (string.IsNullOrEmpty(PreeditText))
            {
                UpdateCaretBounds();
                InvalidateVisual();
                return;
            }
        }

        if (change.Property == SelectionStartProperty
            || change.Property == SelectionEndProperty)
        {
            if (ShouldUpdateCaretBounds())
            {
                UpdateCaretBounds();
            }

            _selectionGeometryCache.Clear();
            InvalidateVisual();
            return;
        }

        if (change.Property == ShowSelectionHighlightProperty
            || change.Property == SelectionBrushProperty
            || change.Property == SelectionForegroundBrushProperty
            || change.Property == CaretBrushProperty
            || change.Property == BackgroundProperty)
        {
            if (change.Property == SelectionBrushProperty)
            {
                _selectionBrushSnapshotSource = null;
                _selectionBrushSnapshot = null;
            }

            if (change.Property == SelectionForegroundBrushProperty)
            {
                _selectionForegroundSnapshotSource = null;
                _selectionForegroundSnapshot = null;
            }

            InvalidateVisual();
            return;
        }

        InvalidateProText();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _caretTimer?.Stop();
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
        _content = null;
        _layoutCache.Clear();
        _selectionGeometryCache.Clear();
        _drawOperation = null;
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

        if (_content is { } cachedContent)
        {
            content = cachedContent;
            return true;
        }

        var baseStyle = CreateBaseStyle(Foreground, TextDecorations);

        if (Inlines is { Count: > 0 })
        {
            if (!ProTextInlineBuilder.TryCreateInlineContent(Inlines, baseStyle, out content))
            {
                return false;
            }

            _content = content;
            return true;
        }

        content = CreateTextContent(baseStyle);
        _content = content;
        return true;
    }

    private ProTextRichContent CreateTextContent(ProTextRichStyle baseStyle)
    {
        var preeditStyle = string.IsNullOrEmpty(PreeditText)
            ? null
            : CreateBaseStyle(Foreground, global::Avalonia.Media.TextDecorations.Underline);

        return ProTextEditableText.CreateContent(CreateEditableTextOptions(), baseStyle, preeditStyle);
    }

    private ProTextRichStyle CreateBaseStyle(IBrush? foreground, TextDecorationCollection? textDecorations)
    {
        return ProTextInlineBuilder.CreateStyle(
            FontFamily,
            FontSize,
            FontStyle,
            FontWeight,
            FontStretch,
            foreground,
            textDecorations,
            FontFeatures,
            LetterSpacing);
    }

    private ProTextLayoutSnapshot GetLayoutSnapshot(ProTextRichContent content, double availableWidth)
    {
        ProTextAvaloniaPlatform.EnsureConfigured();

        var maxWidth = ResolveMaxWidth(availableWidth);
        var lineHeight = GetEffectiveLineHeight(content);
        var textWrapping = ProTextAvaloniaAdapter.ToCore(TextWrapping);
        var textTrimming = ProTextAvaloniaAdapter.ToCore(TextTrimming);

        return _layoutCache.GetSnapshot(
            content,
            new ProTextLayoutRequest(
                maxWidth,
                lineHeight,
                MaxLines: 0,
                textWrapping,
                textTrimming,
                UseGlobalCache));
    }

    private double ResolveMaxWidth(double availableWidth)
    {
        return ProTextLayoutServices.ResolveMaxWidth(
            availableWidth,
            ProTextAvaloniaAdapter.ToCore(TextWrapping),
            ProTextAvaloniaAdapter.ToCore(TextTrimming));
    }

    private double GetEffectiveLineHeight(ProTextRichContent content)
    {
        return ProTextLayoutServices.GetEffectiveLineHeight(FontSize, content.MaxFontSize, LineHeight, LineSpacing, PretextLineHeightMultiplier);
    }

    private int GetCurrentTextLength()
    {
        if (!TryCreateRichContent(out var content))
        {
            return 0;
        }

        return content.Text.Length;
    }

    private void UpdateCaretBounds()
    {
        if (!TryCreateRichContent(out var content))
        {
            return;
        }

        var snapshot = GetLayoutSnapshot(content, Bounds.Width > 0 ? Bounds.Width : double.PositiveInfinity);
        UpdateCaretBounds(snapshot, content);
    }

    private void UpdateCaretBounds(ProTextLayoutSnapshot snapshot, ProTextRichContent content)
    {
        var bounds = ProTextAvaloniaAdapter.ToAvalonia(ProTextLayoutServices.GetCaretBounds(
            snapshot,
            Math.Clamp(GetEffectiveCaretIndex(), 0, content.Text.Length),
            Bounds.Width,
            ProTextAvaloniaAdapter.ToCore(TextAlignment),
            ProTextAvaloniaAdapter.ToCore(FlowDirection)));

        if (bounds != _caretBounds)
        {
            _caretBounds = bounds;
            CaretBoundsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void DrawCaret(DrawingContext context, ProTextLayoutSnapshot snapshot, ProTextRichContent content)
    {
        if (!_showCaret || !_caretBlink || SelectionStart != SelectionEnd)
        {
            return;
        }

        var bounds = ProTextAvaloniaAdapter.ToAvalonia(ProTextLayoutServices.GetCaretBounds(
            snapshot,
            Math.Clamp(GetEffectiveCaretIndex(), 0, content.Text.Length),
            Bounds.Width,
            ProTextAvaloniaAdapter.ToCore(TextAlignment),
            ProTextAvaloniaAdapter.ToCore(FlowDirection)));
        var brush = CaretBrush ?? Foreground ?? Brushes.Black;
        var x = Math.Floor(bounds.X) + 0.5;
        context.DrawLine(new Pen(brush, 1), new Point(x, bounds.Top), new Point(x, bounds.Bottom));
    }

    private int GetEffectiveCaretIndex()
    {
        return ProTextEditableText.GetEffectiveCaretIndex(CreateEditableTextOptions());
    }

    private ProTextEditableTextOptions CreateEditableTextOptions()
    {
        return new ProTextEditableTextOptions(
            Text,
            CaretIndex,
            PreeditText,
            PreeditTextCursorPosition,
            PasswordChar,
            RevealPassword);
    }

    private bool ShouldUpdateCaretBounds()
    {
        return SelectionStart == SelectionEnd || !string.IsNullOrEmpty(PreeditText);
    }

    private bool ShouldUseSelectionForeground()
    {
        return ShowSelectionHighlight && SelectionStart != SelectionEnd && SelectionForegroundBrush is not null;
    }

    private ProTextDrawOperation GetDrawOperation(
        Rect bounds,
        ProTextLayoutSnapshot snapshot,
        ProTextBrush? selectionForeground,
        int selectionStart,
        int selectionEnd,
        ProTextBrush? selectionBackground,
        ProTextSelectionRect[] selectionRects)
    {
        if (_drawOperation is not null
            && ReferenceEquals(_drawOperationSnapshot, snapshot)
            && _drawOperationBounds.Equals(bounds)
            && _drawOperationTextAlignment == TextAlignment
            && _drawOperationFlowDirection == FlowDirection
            && Equals(_drawOperationSelectionForeground, selectionForeground)
            && Equals(_drawOperationSelectionBackground, selectionBackground)
            && ReferenceEquals(_drawOperationSelectionRects, selectionRects)
            && _drawOperationSelectionStart == selectionStart
            && _drawOperationSelectionEnd == selectionEnd)
        {
            return _drawOperation;
        }

        _drawOperation = new ProTextDrawOperation(
            bounds,
            bounds,
            snapshot,
            TextAlignment,
            FlowDirection,
            selectionForeground,
            selectionStart,
            selectionEnd,
            selectionBackground,
            selectionRects);
        _drawOperationSnapshot = snapshot;
        _drawOperationBounds = bounds;
        _drawOperationTextAlignment = TextAlignment;
        _drawOperationFlowDirection = FlowDirection;
        _drawOperationSelectionForeground = selectionForeground;
        _drawOperationSelectionBackground = selectionBackground;
        _drawOperationSelectionRects = selectionRects;
        _drawOperationSelectionStart = selectionStart;
        _drawOperationSelectionEnd = selectionEnd;

        return _drawOperation;
    }

    private ProTextBrush? GetSelectionBrushSnapshot()
    {
        var brush = SelectionBrush;

        if (brush is null)
        {
            _selectionBrushSnapshotSource = null;
            _selectionBrushSnapshot = null;
            return null;
        }

        if (!ReferenceEquals(_selectionBrushSnapshotSource, brush))
        {
            _selectionBrushSnapshotSource = brush;
            _selectionBrushSnapshot = ProTextAvaloniaAdapter.SnapshotBrush(brush);
        }

        return _selectionBrushSnapshot;
    }

    private ProTextBrush? GetSelectionForegroundSnapshot()
    {
        var brush = SelectionForegroundBrush;

        if (brush is null)
        {
            _selectionForegroundSnapshotSource = null;
            _selectionForegroundSnapshot = null;
            return null;
        }

        if (!ReferenceEquals(_selectionForegroundSnapshotSource, brush))
        {
            _selectionForegroundSnapshotSource = brush;
            _selectionForegroundSnapshot = ProTextAvaloniaAdapter.SnapshotBrush(brush);
        }

        return _selectionForegroundSnapshot;
    }

    private ProTextSelectionRect[] GetSelectionRects(ProTextLayoutSnapshot snapshot)
    {
        if (!ShowSelectionHighlight || SelectionStart == SelectionEnd)
        {
            return [];
        }

        return _selectionGeometryCache.GetSelectionRects(
            snapshot,
            SelectionStart,
            SelectionEnd,
            Bounds.Width,
            ProTextAvaloniaAdapter.ToCore(TextAlignment),
            ProTextAvaloniaAdapter.ToCore(FlowDirection));
    }

    private void EnsureCaretTimer()
    {
        if (_caretTimer is null)
        {
            ResetCaretTimer();
        }
    }

    private void ResetCaretTimer()
    {
        var wasEnabled = _caretTimer?.IsEnabled == true;

        if (_caretTimer is not null)
        {
            _caretTimer.Tick -= CaretTimerTick;
            _caretTimer.Stop();
        }

        _caretTimer = null;

        if (CaretBlinkInterval.TotalMilliseconds <= 0)
        {
            return;
        }

        _caretTimer = new DispatcherTimer { Interval = CaretBlinkInterval };
        _caretTimer.Tick += CaretTimerTick;

        if (wasEnabled)
        {
            _caretTimer.Start();
        }
    }

    private void CaretTimerTick(object? sender, EventArgs e)
    {
        _caretBlink = !_caretBlink;
        InvalidateVisual();
    }

}
