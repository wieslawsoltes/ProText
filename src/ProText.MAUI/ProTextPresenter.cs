using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using ProText.Core;
using ProText.MAUI.Internal;
using SkiaSharp;

namespace ProText.MAUI;

/// <summary>
/// Presents editable or selectable text through the same PretextSharp layout and Skia rendering path used by <see cref="ProTextBlock"/>.
/// </summary>
public class ProTextPresenter : ProTextBlock
{
    /// <summary>
    /// Defines the <see cref="PreeditText"/> property.
    /// </summary>
    public static readonly BindableProperty PreeditTextProperty =
        BindableProperty.Create(nameof(PreeditText), typeof(string), typeof(ProTextPresenter), null, propertyChanged: OnContentPropertyChanged);

    /// <summary>
    /// Defines the <see cref="PreeditTextCursorPosition"/> property.
    /// </summary>
    public static readonly BindableProperty PreeditTextCursorPositionProperty =
        BindableProperty.Create(nameof(PreeditTextCursorPosition), typeof(int?), typeof(ProTextPresenter), null, propertyChanged: OnContentPropertyChanged);

    /// <summary>
    /// Defines the <see cref="CaretIndex"/> property.
    /// </summary>
    public static readonly BindableProperty CaretIndexProperty =
        BindableProperty.Create(nameof(CaretIndex), typeof(int), typeof(ProTextPresenter), 0, propertyChanged: OnCaretPropertyChanged);

    /// <summary>
    /// Defines the <see cref="SelectionStart"/> property.
    /// </summary>
    public static readonly BindableProperty SelectionStartProperty =
        BindableProperty.Create(nameof(SelectionStart), typeof(int), typeof(ProTextPresenter), 0, propertyChanged: OnSelectionPropertyChanged);

    /// <summary>
    /// Defines the <see cref="SelectionEnd"/> property.
    /// </summary>
    public static readonly BindableProperty SelectionEndProperty =
        BindableProperty.Create(nameof(SelectionEnd), typeof(int), typeof(ProTextPresenter), 0, propertyChanged: OnSelectionPropertyChanged);

    /// <summary>
    /// Defines the <see cref="ShowSelectionHighlight"/> property.
    /// </summary>
    public static readonly BindableProperty ShowSelectionHighlightProperty =
        BindableProperty.Create(nameof(ShowSelectionHighlight), typeof(bool), typeof(ProTextPresenter), true, propertyChanged: OnRenderPropertyChanged);

    /// <summary>
    /// Defines the <see cref="SelectionBrush"/> property.
    /// </summary>
    public static readonly BindableProperty SelectionBrushProperty =
        BindableProperty.Create(nameof(SelectionBrush), typeof(Brush), typeof(ProTextPresenter), null, propertyChanged: OnSelectionBrushChanged);

    /// <summary>
    /// Defines the <see cref="SelectionForeground"/> property.
    /// </summary>
    public static readonly BindableProperty SelectionForegroundProperty =
        BindableProperty.Create(nameof(SelectionForeground), typeof(Brush), typeof(ProTextPresenter), null, propertyChanged: OnSelectionForegroundChanged);

    /// <summary>
    /// Defines the <see cref="SelectionForegroundBrush"/> property.
    /// </summary>
    public static readonly BindableProperty SelectionForegroundBrushProperty =
        BindableProperty.Create(nameof(SelectionForegroundBrush), typeof(Brush), typeof(ProTextPresenter), null, propertyChanged: OnSelectionForegroundChanged);

    /// <summary>
    /// Defines the <see cref="CaretBrush"/> property.
    /// </summary>
    public static readonly BindableProperty CaretBrushProperty =
        BindableProperty.Create(nameof(CaretBrush), typeof(Brush), typeof(ProTextPresenter), null, propertyChanged: OnRenderPropertyChanged);

    /// <summary>
    /// Defines the <see cref="CaretBlinkInterval"/> property.
    /// </summary>
    public static readonly BindableProperty CaretBlinkIntervalProperty =
        BindableProperty.Create(nameof(CaretBlinkInterval), typeof(TimeSpan), typeof(ProTextPresenter), TimeSpan.FromMilliseconds(500), propertyChanged: OnCaretBlinkIntervalChanged);

    /// <summary>
    /// Defines the <see cref="PasswordChar"/> property.
    /// </summary>
    public static readonly BindableProperty PasswordCharProperty =
        BindableProperty.Create(nameof(PasswordChar), typeof(char), typeof(ProTextPresenter), default(char), propertyChanged: OnContentPropertyChanged);

    /// <summary>
    /// Defines the <see cref="RevealPassword"/> property.
    /// </summary>
    public static readonly BindableProperty RevealPasswordProperty =
        BindableProperty.Create(nameof(RevealPassword), typeof(bool), typeof(ProTextPresenter), false, propertyChanged: OnContentPropertyChanged);

    private readonly ProTextSelectionGeometryCache _selectionGeometryCache = new();
    private ProTextRichContent? _content;
    private Brush? _selectionBrushSnapshotSource;
    private ProTextBrush? _selectionBrushSnapshot;
    private Brush? _selectionForegroundSnapshotSource;
    private ProTextBrush? _selectionForegroundSnapshot;
    private IDispatcherTimer? _caretTimer;
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
    public Brush? SelectionForeground
    {
        get => (Brush?)GetValue(SelectionForegroundProperty);
        set => SetValue(SelectionForegroundProperty, value);
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
    /// Shows the caret.
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
    /// Hides the caret.
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
    public ProTextCharacterHit GetNextCharacterHit(ProTextLogicalDirection direction)
    {
        var textLength = GetCurrentTextLength();
        var index = direction == ProTextLogicalDirection.Forward
            ? Math.Min(textLength, CaretIndex + 1)
            : Math.Max(0, CaretIndex - 1);

        return new ProTextCharacterHit(index, 0);
    }

    /// <summary>
    /// Moves the caret one logical character in the requested direction.
    /// </summary>
    public void MoveCaretHorizontal(ProTextLogicalDirection direction)
    {
        MoveCaretToTextPosition(GetNextCharacterHit(direction).FirstCharacterIndex);
    }

    /// <summary>
    /// Moves the caret one rendered line in the requested direction.
    /// </summary>
    public void MoveCaretVertical(ProTextLogicalDirection direction)
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
            ProTextMauiAdapter.ToCore(HorizontalTextAlignment),
            ProTextMauiAdapter.ToCore(FlowDirection));
        var y = direction == ProTextLogicalDirection.Forward
            ? bounds.Y + snapshot.LineHeight + snapshot.LineHeight / 2
            : bounds.Y - snapshot.LineHeight / 2;
        var index = ProTextLayoutServices.GetCharacterIndex(
            snapshot,
            new ProTextPoint(bounds.X, y),
            width,
            ProTextMauiAdapter.ToCore(HorizontalTextAlignment),
            ProTextMauiAdapter.ToCore(FlowDirection));
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
            ProTextMauiAdapter.ToCore(HorizontalTextAlignment),
            ProTextMauiAdapter.ToCore(FlowDirection));
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
            ProTextMauiAdapter.ToCore(HorizontalTextAlignment),
            ProTextMauiAdapter.ToCore(FlowDirection));

        return ProTextMauiAdapter.ToMaui(Offset(bounds, padding.Left, padding.Top));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        var measured = base.MeasureOverride(widthConstraint, heightConstraint);

        if (ShouldUpdateCaretBounds())
        {
            UpdateCaretBounds();
        }

        return measured;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Rect bounds)
    {
        var arranged = base.ArrangeOverride(bounds);

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

        if (FormattedText is { Spans.Count: > 0 } formattedText)
        {
            if (!ProTextMauiInlineBuilder.TryCreateFormattedContent(formattedText, baseStyle, out content))
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
    /// Creates text content when no formatted spans are present.
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
                ProTextMauiAdapter.ToCore(HorizontalTextAlignment),
                ProTextMauiAdapter.ToCore(FlowDirection),
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
            ProTextMauiAdapter.ToCore(HorizontalTextAlignment),
            ProTextMauiAdapter.ToCore(FlowDirection)), padding.Left, padding.Top);
        var mauiBounds = ProTextMauiAdapter.ToMaui(bounds);

        if (!mauiBounds.Equals(_caretBounds))
        {
            _caretBounds = mauiBounds;
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
            ProTextMauiAdapter.ToCore(HorizontalTextAlignment),
            ProTextMauiAdapter.ToCore(FlowDirection)), contentBounds.X, contentBounds.Y);
        var brush = ProTextMauiAdapter.SnapshotBrush(CaretBrush ?? Foreground) ?? ProTextMauiAdapter.DefaultForegroundBrush;
        ProTextMauiSkiaBrushRenderer.DrawCaret(canvas, bounds, brush, Opacity);
    }

    private bool ShouldUpdateCaretBounds()
    {
        return SelectionStart == SelectionEnd || !string.IsNullOrEmpty(PreeditText);
    }

    private bool ShouldUseSelectionForeground()
    {
        return ShowSelectionHighlight && SelectionStart != SelectionEnd && GetSelectionForegroundBrush() is not null;
    }

    private ProTextBrush? GetSelectionBrushSnapshot()
    {
        var brush = SelectionBrush;

        if (brush is null)
        {
            _selectionBrushSnapshotSource = null;
            _selectionBrushSnapshot = null;
            return ProTextMauiAdapter.DefaultSelectionBrush;
        }

        if (!ReferenceEquals(_selectionBrushSnapshotSource, brush))
        {
            _selectionBrushSnapshotSource = brush;
            _selectionBrushSnapshot = ProTextMauiAdapter.SnapshotBrush(brush);
        }

        return _selectionBrushSnapshot;
    }

    private ProTextBrush? GetSelectionForegroundSnapshot()
    {
        var brush = GetSelectionForegroundBrush();

        if (brush is null)
        {
            _selectionForegroundSnapshotSource = null;
            _selectionForegroundSnapshot = null;
            return null;
        }

        if (!ReferenceEquals(_selectionForegroundSnapshotSource, brush))
        {
            _selectionForegroundSnapshotSource = brush;
            _selectionForegroundSnapshot = ProTextMauiAdapter.SnapshotBrush(brush);
        }

        return _selectionForegroundSnapshot;
    }

    private Brush? GetSelectionForegroundBrush()
    {
        return SelectionForegroundBrush ?? SelectionForeground;
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
            ProTextMauiAdapter.ToCore(HorizontalTextAlignment),
            ProTextMauiAdapter.ToCore(FlowDirection));

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
        var width = Width > 0 ? Width - padding.Left - padding.Right : double.PositiveInfinity;
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
        var wasRunning = _caretTimer?.IsRunning == true;

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

        var dispatcher = TryGetDispatcher();

        if (dispatcher is null)
        {
            return;
        }

        _caretTimer = dispatcher.CreateTimer();
        _caretTimer.Interval = CaretBlinkInterval;
        _caretTimer.IsRepeating = true;
        _caretTimer.Tick += CaretTimerTick;

        if (wasRunning)
        {
            _caretTimer.Start();
        }
    }

    private IDispatcher? TryGetDispatcher()
    {
        try
        {
            return Dispatcher;
        }
        catch (InvalidOperationException)
        {
            return DispatcherProvider.Current.GetForCurrentThread();
        }
    }

    private void CaretTimerTick(object? sender, EventArgs args)
    {
        _caretBlink = !_caretBlink;
        InvalidateRender();
    }

    private static ProTextRect Offset(ProTextRect rect, double x, double y)
    {
        return new ProTextRect(rect.X + x, rect.Y + y, rect.Width, rect.Height);
    }

    private static void OnContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ProTextPresenter presenter)
        {
            presenter.InvalidateProText();
        }
    }

    private static void OnCaretPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ProTextPresenter presenter)
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

    private static void OnSelectionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ProTextPresenter presenter)
        {
            presenter._selectionGeometryCache.Clear();

            if (presenter.ShouldUpdateCaretBounds())
            {
                presenter.UpdateCaretBounds();
            }

            presenter.InvalidateRender();
        }
    }

    private static void OnRenderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ProTextPresenter presenter)
        {
            presenter.InvalidateRender();
        }
    }

    private static void OnCaretBlinkIntervalChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ProTextPresenter presenter)
        {
            presenter.ResetCaretTimer();
        }
    }

    private static void OnSelectionBrushChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ProTextPresenter presenter)
        {
            presenter._selectionBrushSnapshotSource = null;
            presenter._selectionBrushSnapshot = null;
            presenter.InvalidateRender();
        }
    }

    private static void OnSelectionForegroundChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ProTextPresenter presenter)
        {
            presenter._selectionForegroundSnapshotSource = null;
            presenter._selectionForegroundSnapshot = null;
            presenter.InvalidateRender();
        }
    }
}
