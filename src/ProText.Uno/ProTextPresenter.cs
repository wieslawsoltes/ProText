using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using ProText.Core;
using ProText.Uno.Internal;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI.Text;

namespace ProText.Uno;

/// <summary>
/// Presents editable or selectable text through the same PretextSharp layout and Skia rendering path used by <see cref="ProTextBlock"/>.
/// </summary>
[ContentProperty(Name = nameof(Text))]
public class ProTextPresenter : ProTextBlock
{
    /// <summary>
    /// Defines the <see cref="PreeditText"/> property.
    /// </summary>
    public static readonly DependencyProperty PreeditTextProperty =
        DependencyProperty.Register(nameof(PreeditText), typeof(string), typeof(ProTextPresenter), new PropertyMetadata(null, OnContentPropertyChanged));

    /// <summary>
    /// Defines the <see cref="PreeditTextCursorPosition"/> property.
    /// </summary>
    public static readonly DependencyProperty PreeditTextCursorPositionProperty =
        DependencyProperty.Register(nameof(PreeditTextCursorPosition), typeof(int?), typeof(ProTextPresenter), new PropertyMetadata(null, OnContentPropertyChanged));

    /// <summary>
    /// Defines the <see cref="CaretIndex"/> property.
    /// </summary>
    public static readonly DependencyProperty CaretIndexProperty =
        DependencyProperty.Register(nameof(CaretIndex), typeof(int), typeof(ProTextPresenter), new PropertyMetadata(0, OnCaretPropertyChanged));

    /// <summary>
    /// Defines the <see cref="SelectionStart"/> property.
    /// </summary>
    public static readonly DependencyProperty SelectionStartProperty =
        DependencyProperty.Register(nameof(SelectionStart), typeof(int), typeof(ProTextPresenter), new PropertyMetadata(0, OnSelectionPropertyChanged));

    /// <summary>
    /// Defines the <see cref="SelectionEnd"/> property.
    /// </summary>
    public static readonly DependencyProperty SelectionEndProperty =
        DependencyProperty.Register(nameof(SelectionEnd), typeof(int), typeof(ProTextPresenter), new PropertyMetadata(0, OnSelectionPropertyChanged));

    /// <summary>
    /// Defines the <see cref="ShowSelectionHighlight"/> property.
    /// </summary>
    public static readonly DependencyProperty ShowSelectionHighlightProperty =
        DependencyProperty.Register(nameof(ShowSelectionHighlight), typeof(bool), typeof(ProTextPresenter), new PropertyMetadata(true, OnRenderPropertyChanged));

    /// <summary>
    /// Defines the <see cref="SelectionBrush"/> property.
    /// </summary>
    public static readonly DependencyProperty SelectionBrushProperty =
        DependencyProperty.Register(nameof(SelectionBrush), typeof(Brush), typeof(ProTextPresenter), new PropertyMetadata(null, OnSelectionBrushChanged));

    /// <summary>
    /// Defines the <see cref="SelectionForegroundBrush"/> property.
    /// </summary>
    public static readonly DependencyProperty SelectionForegroundBrushProperty =
        DependencyProperty.Register(nameof(SelectionForegroundBrush), typeof(Brush), typeof(ProTextPresenter), new PropertyMetadata(null, OnSelectionForegroundBrushChanged));

    /// <summary>
    /// Defines the <see cref="CaretBrush"/> property.
    /// </summary>
    public static readonly DependencyProperty CaretBrushProperty =
        DependencyProperty.Register(nameof(CaretBrush), typeof(Brush), typeof(ProTextPresenter), new PropertyMetadata(null, OnRenderPropertyChanged));

    /// <summary>
    /// Defines the <see cref="CaretBlinkInterval"/> property.
    /// </summary>
    public static readonly DependencyProperty CaretBlinkIntervalProperty =
        DependencyProperty.Register(nameof(CaretBlinkInterval), typeof(TimeSpan), typeof(ProTextPresenter), new PropertyMetadata(TimeSpan.FromMilliseconds(500), OnCaretBlinkIntervalChanged));

    /// <summary>
    /// Defines the <see cref="PasswordChar"/> property.
    /// </summary>
    public static readonly DependencyProperty PasswordCharProperty =
        DependencyProperty.Register(nameof(PasswordChar), typeof(char), typeof(ProTextPresenter), new PropertyMetadata(default(char), OnContentPropertyChanged));

    /// <summary>
    /// Defines the <see cref="RevealPassword"/> property.
    /// </summary>
    public static readonly DependencyProperty RevealPasswordProperty =
        DependencyProperty.Register(nameof(RevealPassword), typeof(bool), typeof(ProTextPresenter), new PropertyMetadata(false, OnContentPropertyChanged));

    private readonly ProTextSelectionGeometryCache _selectionGeometryCache = new();
    private ProTextRichContent? _content;
    private Brush? _selectionBrushSnapshotSource;
    private ProTextBrush? _selectionBrushSnapshot;
    private Brush? _selectionForegroundSnapshotSource;
    private ProTextBrush? _selectionForegroundSnapshot;
    private DispatcherTimer? _caretTimer;
    private bool _showCaret;
    private bool _caretBlink;
    private Rect _caretBounds;

    /// <summary>
    /// Raised when the computed caret bounds change.
    /// </summary>
    public event EventHandler? CaretBoundsChanged;

    /// <summary>
    /// Gets or sets active IME preedit text inserted at the caret.
    /// </summary>
    public string? PreeditText
    {
        get => (string?)GetValue(PreeditTextProperty);
        set => SetValue(PreeditTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the cursor position inside <see cref="PreeditText"/>.
    /// </summary>
    public int? PreeditTextCursorPosition
    {
        get => (int?)GetValue(PreeditTextCursorPositionProperty);
        set => SetValue(PreeditTextCursorPositionProperty, value);
    }

    /// <summary>
    /// Gets or sets the caret index.
    /// </summary>
    public int CaretIndex
    {
        get => (int)GetValue(CaretIndexProperty);
        set => SetValue(CaretIndexProperty, Math.Max(0, value));
    }

    /// <summary>
    /// Gets or sets the selection start index.
    /// </summary>
    public int SelectionStart
    {
        get => (int)GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, Math.Max(0, value));
    }

    /// <summary>
    /// Gets or sets the selection end index.
    /// </summary>
    public int SelectionEnd
    {
        get => (int)GetValue(SelectionEndProperty);
        set => SetValue(SelectionEndProperty, Math.Max(0, value));
    }

    /// <summary>
    /// Gets or sets whether selection highlight is shown.
    /// </summary>
    public bool ShowSelectionHighlight
    {
        get => (bool)GetValue(ShowSelectionHighlightProperty);
        set => SetValue(ShowSelectionHighlightProperty, value);
    }

    /// <summary>
    /// Gets or sets the selection background brush.
    /// </summary>
    public Brush? SelectionBrush
    {
        get => (Brush?)GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush used for selected text.
    /// </summary>
    public Brush? SelectionForegroundBrush
    {
        get => (Brush?)GetValue(SelectionForegroundBrushProperty);
        set => SetValue(SelectionForegroundBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the caret brush.
    /// </summary>
    public Brush? CaretBrush
    {
        get => (Brush?)GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the caret blink interval.
    /// </summary>
    public TimeSpan CaretBlinkInterval
    {
        get => (TimeSpan)GetValue(CaretBlinkIntervalProperty);
        set => SetValue(CaretBlinkIntervalProperty, value);
    }

    /// <summary>
    /// Gets or sets the password masking character. The default value disables masking.
    /// </summary>
    public char PasswordChar
    {
        get => (char)GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    /// <summary>
    /// Gets or sets whether password text is revealed.
    /// </summary>
    public bool RevealPassword
    {
        get => (bool)GetValue(RevealPasswordProperty);
        set => SetValue(RevealPasswordProperty, value);
    }

    /// <summary>
    /// Shows the caret and starts caret blinking.
    /// </summary>
    public void ShowCaret()
    {
        _showCaret = true;
        _caretBlink = true;
        EnsureCaretTimer();
        _caretTimer?.Start();
        InvalidateRender();
    }

    /// <summary>
    /// Hides the caret and stops caret blinking.
    /// </summary>
    public void HideCaret()
    {
        _showCaret = false;
        _caretBlink = false;
        _caretTimer?.Stop();
        InvalidateRender();
    }

    /// <summary>
    /// Moves the caret to a text index.
    /// </summary>
    public void MoveCaretToTextPosition(int textPosition)
    {
        SetValue(CaretIndexProperty, Math.Clamp(textPosition, 0, GetCurrentTextLength()));
        UpdateCaretBounds();
        InvalidateRender();
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
    public ProTextCharacterHit GetNextCharacterHit(LogicalDirection direction)
    {
        var textLength = GetCurrentTextLength();
        var index = direction == LogicalDirection.Forward
            ? Math.Min(textLength, CaretIndex + 1)
            : Math.Max(0, CaretIndex - 1);

        return new ProTextCharacterHit(index, 0);
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

        var width = GetContentWidth();
        var snapshot = GetLayoutSnapshot(content, width);
        var bounds = ProTextLayoutServices.GetCaretBounds(
            snapshot,
            Math.Clamp(GetEffectiveCaretIndex(), 0, content.Text.Length),
            width,
            ProTextUnoAdapter.ToCore(TextAlignment),
            ProTextUnoAdapter.ToCore(FlowDirection));
        var y = direction == LogicalDirection.Forward
            ? bounds.Y + snapshot.LineHeight + snapshot.LineHeight / 2
            : bounds.Y - snapshot.LineHeight / 2;
        var index = ProTextLayoutServices.GetCharacterIndex(
            snapshot,
            new ProTextPoint(bounds.X, y),
            width,
            ProTextUnoAdapter.ToCore(TextAlignment),
            ProTextUnoAdapter.ToCore(FlowDirection));
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

        var padding = Padding;
        var width = GetContentWidth();
        var snapshot = GetLayoutSnapshot(content, width);
        return ProTextLayoutServices.GetCharacterIndex(
            snapshot,
            new ProTextPoint(point.X - padding.Left, point.Y - padding.Top),
            width,
            ProTextUnoAdapter.ToCore(TextAlignment),
            ProTextUnoAdapter.ToCore(FlowDirection));
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

        var padding = Padding;
        var width = GetContentWidth();
        var bounds = ProTextLayoutServices.GetCaretBounds(
            GetLayoutSnapshot(content, width),
            Math.Clamp(textPosition, 0, content.Text.Length),
            width,
            ProTextUnoAdapter.ToCore(TextAlignment),
            ProTextUnoAdapter.ToCore(FlowDirection));

        return ProTextUnoAdapter.ToUno(Offset(bounds, padding.Left, padding.Top));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var measured = base.MeasureOverride(availableSize);

        if (ShouldUpdateCaretBounds())
        {
            UpdateCaretBounds();
        }

        return measured;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);

        if (ShouldUpdateCaretBounds())
        {
            UpdateCaretBounds();
        }

        return arranged;
    }

    /// <summary>
    /// Invalidates ProText layout and rendering state.
    /// </summary>
    protected override void InvalidateProText()
    {
        _content = null;
        _selectionGeometryCache.Clear();
        base.InvalidateProText();
    }

    /// <summary>
    /// Builds current rich text content.
    /// </summary>
    protected override bool TryCreateRichContent(out ProTextRichContent content)
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

        if (Inlines.Count > 0)
        {
            if (!ProTextUnoInlineBuilder.TryCreateInlineContent(Inlines, baseStyle, out content))
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

    /// <summary>
    /// Creates text content when no inlines are present.
    /// </summary>
    protected override ProTextRichContent CreateTextContent(ProTextRichStyle baseStyle)
    {
        var preeditStyle = string.IsNullOrEmpty(PreeditText)
            ? null
            : CreateBaseStyle(Foreground, TextDecorations.Underline);

        return ProTextEditableText.CreateContent(CreateEditableTextOptions(), baseStyle, preeditStyle);
    }

    /// <summary>
    /// Renders text content into the supplied Skia canvas.
    /// </summary>
    protected override void RenderText(SKCanvas canvas, ProTextRichContent content, ProTextLayoutSnapshot snapshot, ProTextRect contentBounds)
    {
        var selectionRects = GetSelectionRects(snapshot, contentBounds);
        var selectionBackground = selectionRects.Length > 0 ? GetSelectionBrushSnapshot() : null;
        var selectionForeground = selectionRects.Length > 0 && ShouldUseSelectionForeground()
            ? GetSelectionForegroundSnapshot()
            : null;
        var selectionStart = Math.Min(SelectionStart, SelectionEnd);
        var selectionEnd = Math.Max(SelectionStart, SelectionEnd);

        ProTextSkiaRenderer.Render(
            canvas,
            snapshot,
            new ProTextSkiaRenderOptions(
                contentBounds,
                ProTextUnoAdapter.ToCore(TextAlignment),
                ProTextUnoAdapter.ToCore(FlowDirection),
                Opacity,
                selectionForeground,
                selectionStart,
                selectionEnd,
                selectionBackground,
                selectionRects));

        DrawCaret(canvas, snapshot, content, contentBounds);
    }

    private int GetCurrentTextLength()
    {
        if (!TryCreateRichContent(out var content))
        {
            return 0;
        }

        return content.Text.Length;
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

    private void UpdateCaretBounds()
    {
        if (!TryCreateRichContent(out var content))
        {
            return;
        }

        var padding = Padding;
        var width = GetContentWidth();
        var snapshot = GetLayoutSnapshot(content, width);
        var bounds = Offset(ProTextLayoutServices.GetCaretBounds(
            snapshot,
            Math.Clamp(GetEffectiveCaretIndex(), 0, content.Text.Length),
            width,
            ProTextUnoAdapter.ToCore(TextAlignment),
            ProTextUnoAdapter.ToCore(FlowDirection)), padding.Left, padding.Top);
        var unoBounds = ProTextUnoAdapter.ToUno(bounds);

        if (!unoBounds.Equals(_caretBounds))
        {
            _caretBounds = unoBounds;
            CaretBoundsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void DrawCaret(SKCanvas canvas, ProTextLayoutSnapshot snapshot, ProTextRichContent content, ProTextRect contentBounds)
    {
        if (!_showCaret || !_caretBlink || SelectionStart != SelectionEnd)
        {
            return;
        }

        var bounds = Offset(ProTextLayoutServices.GetCaretBounds(
            snapshot,
            Math.Clamp(GetEffectiveCaretIndex(), 0, content.Text.Length),
            contentBounds.Width,
            ProTextUnoAdapter.ToCore(TextAlignment),
            ProTextUnoAdapter.ToCore(FlowDirection)), contentBounds.X, contentBounds.Y);
        var brush = ProTextUnoAdapter.SnapshotBrush(CaretBrush ?? Foreground) ?? ProTextUnoAdapter.DefaultForegroundBrush;
        ProTextUnoSkiaBrushRenderer.DrawCaret(canvas, bounds, brush, Opacity);
    }

    private bool ShouldUpdateCaretBounds()
    {
        return SelectionStart == SelectionEnd || !string.IsNullOrEmpty(PreeditText);
    }

    private bool ShouldUseSelectionForeground()
    {
        return ShowSelectionHighlight && SelectionStart != SelectionEnd && SelectionForegroundBrush is not null;
    }

    private ProTextBrush? GetSelectionBrushSnapshot()
    {
        var brush = SelectionBrush;

        if (brush is null)
        {
            _selectionBrushSnapshotSource = null;
            _selectionBrushSnapshot = null;
            return ProTextUnoAdapter.DefaultSelectionBrush;
        }

        if (!ReferenceEquals(_selectionBrushSnapshotSource, brush))
        {
            _selectionBrushSnapshotSource = brush;
            _selectionBrushSnapshot = ProTextUnoAdapter.SnapshotBrush(brush);
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
            _selectionForegroundSnapshot = ProTextUnoAdapter.SnapshotBrush(brush);
        }

        return _selectionForegroundSnapshot;
    }

    private ProTextSelectionRect[] GetSelectionRects(ProTextLayoutSnapshot snapshot, ProTextRect contentBounds)
    {
        if (!ShowSelectionHighlight || SelectionStart == SelectionEnd)
        {
            return [];
        }

        var rects = _selectionGeometryCache.GetSelectionRects(
            snapshot,
            SelectionStart,
            SelectionEnd,
            contentBounds.Width,
            ProTextUnoAdapter.ToCore(TextAlignment),
            ProTextUnoAdapter.ToCore(FlowDirection));

        if (contentBounds.X == 0 && contentBounds.Y == 0)
        {
            return rects;
        }

        var offsetRects = new ProTextSelectionRect[rects.Length];

        for (var i = 0; i < rects.Length; i++)
        {
            var rect = rects[i];
            offsetRects[i] = new ProTextSelectionRect(rect.LineIndex, Offset(rect.Bounds, contentBounds.X, contentBounds.Y));
        }

        return offsetRects;
    }

    private double GetContentWidth()
    {
        var padding = Padding;
        var width = ActualWidth > 0 ? ActualWidth - padding.Left - padding.Right : double.PositiveInfinity;
        return double.IsInfinity(width) ? double.PositiveInfinity : Math.Max(0, width);
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

    private void CaretTimerTick(object? sender, object args)
    {
        _caretBlink = !_caretBlink;
        InvalidateRender();
    }

    private static ProTextRect Offset(ProTextRect rect, double x, double y)
    {
        return new ProTextRect(rect.X + x, rect.Y + y, rect.Width, rect.Height);
    }

    private static void OnContentPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProTextPresenter presenter)
        {
            presenter.InvalidateProText();
        }
    }

    private static void OnCaretPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProTextPresenter presenter)
        {
            if (string.IsNullOrEmpty(presenter.PreeditText))
            {
                presenter.UpdateCaretBounds();
                presenter.InvalidateRender();
                return;
            }

            presenter.InvalidateProText();
        }
    }

    private static void OnSelectionPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProTextPresenter presenter)
        {
            presenter._selectionGeometryCache.Clear();

            if (presenter.ShouldUpdateCaretBounds())
            {
                presenter.UpdateCaretBounds();
            }

            presenter.InvalidateRender();
        }
    }

    private static void OnRenderPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProTextPresenter presenter)
        {
            presenter.InvalidateRender();
        }
    }

    private static void OnSelectionBrushChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProTextPresenter presenter)
        {
            presenter._selectionBrushSnapshotSource = null;
            presenter._selectionBrushSnapshot = null;
            presenter.InvalidateRender();
        }
    }

    private static void OnSelectionForegroundBrushChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProTextPresenter presenter)
        {
            presenter._selectionForegroundSnapshotSource = null;
            presenter._selectionForegroundSnapshot = null;
            presenter.InvalidateRender();
        }
    }

    private static void OnCaretBlinkIntervalChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProTextPresenter presenter)
        {
            presenter.ResetCaretTimer();
        }
    }
}
