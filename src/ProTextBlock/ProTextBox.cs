using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;

namespace ProTextBlock;

/// <summary>
/// A lightweight TextBox-like control that presents text through <see cref="ProTextPresenter"/>.
/// </summary>
[TemplatePart("PART_TextPresenter", typeof(ProTextPresenter), IsRequired = true)]
[TemplatePart("PART_ScrollViewer", typeof(ScrollViewer))]
[PseudoClasses(EmptyPseudoClass)]
[PseudoClasses(TouchModePseudoClass)]
public class ProTextBox : TemplatedControl
{
    private const string EmptyPseudoClass = ":empty";
    private const string TouchModePseudoClass = ":touch-mode";

    /// <summary>
    /// Gets a platform-specific key gesture for the cut action.
    /// </summary>
    public static KeyGesture? CutGesture => Application.Current?.PlatformSettings?.HotkeyConfiguration.Cut.FirstOrDefault();

    /// <summary>
    /// Gets a platform-specific key gesture for the copy action.
    /// </summary>
    public static KeyGesture? CopyGesture => Application.Current?.PlatformSettings?.HotkeyConfiguration.Copy.FirstOrDefault();

    /// <summary>
    /// Gets a platform-specific key gesture for the paste action.
    /// </summary>
    public static KeyGesture? PasteGesture => Application.Current?.PlatformSettings?.HotkeyConfiguration.Paste.FirstOrDefault();

    /// <summary>
    /// Defines the <see cref="UseGlobalCache"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> UseGlobalCacheProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(UseGlobalCache), true);

    /// <summary>
    /// Defines the <see cref="UsePretextRendering"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> UsePretextRenderingProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(UsePretextRendering), true);

    /// <summary>
    /// Defines the <see cref="PretextLineHeightMultiplier"/> property.
    /// </summary>
    public static readonly StyledProperty<double> PretextLineHeightMultiplierProperty =
        ProTextPresenter.PretextLineHeightMultiplierProperty.AddOwner<ProTextBox>();

    /// <summary>
    /// Defines the <see cref="Text"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty =
        TextBlock.TextProperty.AddOwner<ProTextBox>(new(
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true));

    /// <summary>
    /// Defines the <see cref="AcceptsReturn"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> AcceptsReturnProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(AcceptsReturn));

    /// <summary>
    /// Defines the <see cref="AcceptsTab"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> AcceptsTabProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(AcceptsTab));

    /// <summary>
    /// Defines the <see cref="NewLine"/> property.
    /// </summary>
    public static readonly StyledProperty<string> NewLineProperty =
        AvaloniaProperty.Register<ProTextBox, string>(nameof(NewLine), Environment.NewLine);

    /// <summary>
    /// Defines the <see cref="IsReadOnly"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(IsReadOnly));

    /// <summary>
    /// Defines the <see cref="CaretIndex"/> property.
    /// </summary>
    public static readonly StyledProperty<int> CaretIndexProperty =
        AvaloniaProperty.Register<ProTextBox, int>(nameof(CaretIndex), coerce: CoerceCaretIndex);

    /// <summary>
    /// Defines the <see cref="SelectionStart"/> property.
    /// </summary>
    public static readonly StyledProperty<int> SelectionStartProperty =
        AvaloniaProperty.Register<ProTextBox, int>(nameof(SelectionStart), coerce: CoerceCaretIndex);

    /// <summary>
    /// Defines the <see cref="SelectionEnd"/> property.
    /// </summary>
    public static readonly StyledProperty<int> SelectionEndProperty =
        AvaloniaProperty.Register<ProTextBox, int>(nameof(SelectionEnd), coerce: CoerceCaretIndex);

    /// <summary>
    /// Defines the <see cref="MaxLength"/> property.
    /// </summary>
    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<ProTextBox, int>(nameof(MaxLength));

    /// <summary>
    /// Defines the <see cref="UndoLimit"/> property.
    /// </summary>
    public static readonly StyledProperty<int> UndoLimitProperty =
        AvaloniaProperty.Register<ProTextBox, int>(nameof(UndoLimit), 100);

    /// <summary>
    /// Defines the <see cref="PasswordChar"/> property.
    /// </summary>
    public static readonly StyledProperty<char> PasswordCharProperty =
        AvaloniaProperty.Register<ProTextBox, char>(nameof(PasswordChar));

    /// <summary>
    /// Defines the <see cref="RevealPassword"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> RevealPasswordProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(RevealPassword));

    /// <summary>
    /// Defines the <see cref="PlaceholderText"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<ProTextBox, string?>(nameof(PlaceholderText));

    /// <summary>
    /// Defines the <see cref="UseFloatingPlaceholder"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> UseFloatingPlaceholderProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(UseFloatingPlaceholder));

    /// <summary>
    /// Defines the <see cref="PlaceholderForeground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> PlaceholderForegroundProperty =
        AvaloniaProperty.Register<ProTextBox, IBrush?>(nameof(PlaceholderForeground));

    /// <summary>
    /// Defines the <see cref="InnerLeftContent"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> InnerLeftContentProperty =
        AvaloniaProperty.Register<ProTextBox, object?>(nameof(InnerLeftContent));

    /// <summary>
    /// Defines the <see cref="InnerRightContent"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> InnerRightContentProperty =
        AvaloniaProperty.Register<ProTextBox, object?>(nameof(InnerRightContent));

    /// <summary>
    /// Defines the <see cref="TextAlignment"/> property.
    /// </summary>
    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        TextBlock.TextAlignmentProperty.AddOwner<ProTextBox>();

    /// <summary>
    /// Defines the <see cref="TextWrapping"/> property.
    /// </summary>
    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        TextBlock.TextWrappingProperty.AddOwner<ProTextBox>();

    /// <summary>
    /// Defines the <see cref="TextDecorations"/> property.
    /// </summary>
    public static readonly StyledProperty<TextDecorationCollection?> TextDecorationsProperty =
        TextBlock.TextDecorationsProperty.AddOwner<ProTextBox>();

    /// <summary>
    /// Defines the <see cref="LineHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LineHeightProperty =
        TextBlock.LineHeightProperty.AddOwner<ProTextBox>();

    /// <summary>
    /// Defines the <see cref="HorizontalContentAlignment"/> property.
    /// </summary>
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<ProTextBox>();

    /// <summary>
    /// Defines the <see cref="VerticalContentAlignment"/> property.
    /// </summary>
    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<ProTextBox>();

    /// <summary>
    /// Defines the <see cref="SelectionBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
        AvaloniaProperty.Register<ProTextBox, IBrush?>(nameof(SelectionBrush));

    /// <summary>
    /// Defines the <see cref="SelectionForegroundBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> SelectionForegroundBrushProperty =
        AvaloniaProperty.Register<ProTextBox, IBrush?>(nameof(SelectionForegroundBrush));

    /// <summary>
    /// Defines the <see cref="CaretBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> CaretBrushProperty =
        AvaloniaProperty.Register<ProTextBox, IBrush?>(nameof(CaretBrush));

    /// <summary>
    /// Defines the <see cref="CaretBlinkInterval"/> property.
    /// </summary>
    public static readonly StyledProperty<TimeSpan> CaretBlinkIntervalProperty =
        AvaloniaProperty.Register<ProTextBox, TimeSpan>(nameof(CaretBlinkInterval), TimeSpan.FromMilliseconds(500));

    private ProTextPresenter? _presenter;
    private readonly Stack<TextEditState> _undoStack = new();
    private readonly Stack<TextEditState> _redoStack = new();

    static ProTextBox()
    {
        FocusableProperty.OverrideDefaultValue<ProTextBox>(true);
        ClipToBoundsProperty.OverrideDefaultValue<ProTextBox>(true);

        AffectsMeasure<ProTextBox>(
            TextProperty,
            PlaceholderTextProperty,
            UseFloatingPlaceholderProperty,
            PaddingProperty,
            BorderThicknessProperty,
            FontFamilyProperty,
            FontSizeProperty,
            FontStyleProperty,
            FontWeightProperty,
            FontStretchProperty,
            FontFeaturesProperty,
            TextAlignmentProperty,
            TextWrappingProperty,
            TextDecorationsProperty,
            LineHeightProperty,
            LetterSpacingProperty,
            InnerLeftContentProperty,
            InnerRightContentProperty,
            PasswordCharProperty,
            RevealPasswordProperty,
            UseGlobalCacheProperty,
            UsePretextRenderingProperty,
            PretextLineHeightMultiplierProperty);

        AffectsRender<ProTextBox>(
            BackgroundProperty,
            BorderBrushProperty,
            ForegroundProperty,
            PlaceholderForegroundProperty,
            CaretBrushProperty,
            SelectionBrushProperty,
            SelectionForegroundBrushProperty,
            SelectionStartProperty,
            SelectionEndProperty);
    }

    /// <summary>
    /// Gets or sets whether shared prepared-text cache entries are used by the presenter.
    /// </summary>
    public bool UseGlobalCache
    {
        get => GetValue(UseGlobalCacheProperty);
        set => SetValue(UseGlobalCacheProperty, value);
    }

    /// <summary>
    /// Gets or sets whether Pretext rendering is enabled by the presenter.
    /// </summary>
    public bool UsePretextRendering
    {
        get => GetValue(UsePretextRenderingProperty);
        set => SetValue(UsePretextRenderingProperty, value);
    }

    /// <summary>
    /// Gets or sets the fallback line-height multiplier used by the presenter.
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
    /// Gets or sets whether return characters are accepted.
    /// </summary>
    public bool AcceptsReturn
    {
        get => GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    /// <summary>
    /// Gets or sets whether tab characters are accepted.
    /// </summary>
    public bool AcceptsTab
    {
        get => GetValue(AcceptsTabProperty);
        set => SetValue(AcceptsTabProperty, value);
    }

    /// <summary>
    /// Gets or sets the newline text inserted when Enter is accepted.
    /// </summary>
    public string NewLine
    {
        get => GetValue(NewLineProperty);
        set => SetValue(NewLineProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the text is read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// Gets or sets the caret index.
    /// </summary>
    public int CaretIndex
    {
        get => GetValue(CaretIndexProperty);
        set => SetValue(CaretIndexProperty, value);
    }

    /// <summary>
    /// Gets or sets the selection start.
    /// </summary>
    public int SelectionStart
    {
        get => GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, value);
    }

    /// <summary>
    /// Gets or sets the selection end.
    /// </summary>
    public int SelectionEnd
    {
        get => GetValue(SelectionEndProperty);
        set => SetValue(SelectionEndProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum text length. Zero means unlimited.
    /// </summary>
    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of undo states retained by the control.
    /// </summary>
    public int UndoLimit
    {
        get => GetValue(UndoLimitProperty);
        set => SetValue(UndoLimitProperty, Math.Max(0, value));
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

    /// <summary>
    /// Gets or sets placeholder text.
    /// </summary>
    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the placeholder floats above text once content exists.
    /// </summary>
    public bool UseFloatingPlaceholder
    {
        get => GetValue(UseFloatingPlaceholderProperty);
        set => SetValue(UseFloatingPlaceholderProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder foreground brush.
    /// </summary>
    public IBrush? PlaceholderForeground
    {
        get => GetValue(PlaceholderForegroundProperty);
        set => SetValue(PlaceholderForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets content shown inside the left edge of the control.
    /// </summary>
    public object? InnerLeftContent
    {
        get => GetValue(InnerLeftContentProperty);
        set => SetValue(InnerLeftContentProperty, value);
    }

    /// <summary>
    /// Gets or sets content shown inside the right edge of the control.
    /// </summary>
    public object? InnerRightContent
    {
        get => GetValue(InnerRightContentProperty);
        set => SetValue(InnerRightContentProperty, value);
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
    /// Gets or sets text decorations.
    /// </summary>
    public TextDecorationCollection? TextDecorations
    {
        get => GetValue(TextDecorationsProperty);
        set => SetValue(TextDecorationsProperty, value);
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
    /// Gets or sets horizontal content alignment.
    /// </summary>
    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    /// <summary>
    /// Gets or sets vertical content alignment.
    /// </summary>
    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
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
    /// Gets or sets the selected text foreground brush.
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
    /// Gets or sets selected text.
    /// </summary>
    public string SelectedText
    {
        get => GetSelection();
        set => ReplaceSelection(value ?? string.Empty, recordUndo: true);
    }

    /// <summary>
    /// Gets whether an undo operation can currently execute.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets whether a redo operation can currently execute.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Gets whether cut can currently execute.
    /// </summary>
    public bool CanCut => !IsReadOnly && !IsPasswordBox && SelectionStart != SelectionEnd;

    /// <summary>
    /// Gets whether copy can currently execute.
    /// </summary>
    public bool CanCopy => !IsPasswordBox && SelectionStart != SelectionEnd;

    /// <summary>
    /// Gets whether paste can currently execute.
    /// </summary>
    public bool CanPaste => !IsReadOnly;

    private bool IsPasswordBox => PasswordChar != default && !RevealPassword;

    /// <summary>
    /// Selects all text.
    /// </summary>
    public void SelectAll()
    {
        var length = Text?.Length ?? 0;
        SetCurrentValue(SelectionStartProperty, 0);
        SetCurrentValue(SelectionEndProperty, length);
        SetCurrentValue(CaretIndexProperty, length);
    }

    /// <summary>
    /// Clears the text.
    /// </summary>
    public void Clear()
    {
        if (IsReadOnly)
        {
            return;
        }

        SnapshotUndoRedo();
        SetCurrentValue(TextProperty, string.Empty);
        SetCaretAndSelection(0);
    }

    /// <summary>
    /// Restores the previous edit state when available.
    /// </summary>
    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        _redoStack.Push(CaptureState());
        RestoreState(_undoStack.Pop());
    }

    /// <summary>
    /// Restores the next edit state when available.
    /// </summary>
    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        _undoStack.Push(CaptureState());
        RestoreState(_redoStack.Pop());
    }

    /// <summary>
    /// Cuts the selected text to the clipboard.
    /// </summary>
    public async void Cut()
    {
        if (!CanCut)
        {
            return;
        }

        var text = GetSelection();
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }

        SnapshotUndoRedo();
        DeleteSelection(recordUndo: false);
    }

    /// <summary>
    /// Copies the selected text to the clipboard.
    /// </summary>
    public async void Copy()
    {
        if (!CanCopy)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(GetSelection());
        }
    }

    /// <summary>
    /// Pastes text from the clipboard.
    /// </summary>
    public async void Paste()
    {
        if (!CanPaste)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        var text = clipboard is null ? null : await clipboard.TryGetTextAsync();

        if (!string.IsNullOrEmpty(text))
        {
            InsertText(text, recordUndo: true);
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _presenter = e.NameScope.Find<ProTextPresenter>("PART_TextPresenter");

        if (_presenter is not null)
        {
            if (IsFocused)
            {
                _presenter.ShowCaret();
            }
            else
            {
                _presenter.HideCaret();
            }
        }
    }

    /// <inheritdoc />
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        _presenter?.ShowCaret();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        SetCurrentValue(RevealPasswordProperty, false);
        _presenter?.HideCaret();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            Focus();

            if (_presenter is not null)
            {
                var point = e.GetPosition(_presenter);
                var index = _presenter.GetCharacterIndex(point);
                SetCaretAndSelection(index);
            }

            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (IsReadOnly || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        InsertText(e.Text, recordUndo: true);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        var keymap = Application.Current?.PlatformSettings?.HotkeyConfiguration;

        if (keymap is not null && keymap.SelectAll.Any(gesture => gesture.Matches(e)))
        {
            SelectAll();
            e.Handled = true;
            return;
        }

        if (keymap is not null && keymap.Copy.Any(gesture => gesture.Matches(e)))
        {
            Copy();
            e.Handled = true;
            return;
        }

        if (keymap is not null && keymap.Cut.Any(gesture => gesture.Matches(e)))
        {
            Cut();
            e.Handled = true;
            return;
        }

        if (keymap is not null && keymap.Paste.Any(gesture => gesture.Matches(e)))
        {
            Paste();
            e.Handled = true;
            return;
        }

        if (keymap is not null && keymap.Undo.Any(gesture => gesture.Matches(e)))
        {
            Undo();
            e.Handled = true;
            return;
        }

        if (keymap is not null && keymap.Redo.Any(gesture => gesture.Matches(e)))
        {
            Redo();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
                MoveCaret(-1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;
            case Key.Right:
                MoveCaret(1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;
            case Key.Home:
                SetCaretAndSelection(0, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;
            case Key.End:
                SetCaretAndSelection(Text?.Length ?? 0, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;
            case Key.Back:
                if (!IsReadOnly)
                {
                    Backspace(e.KeyModifiers.HasFlag(KeyModifiers.Control) && !IsPasswordBox);
                    e.Handled = true;
                }
                break;
            case Key.Delete:
                if (!IsReadOnly)
                {
                    Delete(e.KeyModifiers.HasFlag(KeyModifiers.Control) && !IsPasswordBox);
                    e.Handled = true;
                }
                break;
            case Key.Enter:
                if (AcceptsReturn && !IsReadOnly)
                {
                    InsertText(NewLine, recordUndo: true);
                    e.Handled = true;
                }
                break;
            case Key.Tab:
                if (AcceptsTab && !IsReadOnly)
                {
                    InsertText("\t", recordUndo: true);
                    e.Handled = true;
                }
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            CoerceValue(CaretIndexProperty);
            CoerceValue(SelectionStartProperty);
            CoerceValue(SelectionEndProperty);
            UpdatePseudoClasses();
        }
        else if (change.Property == SelectionStartProperty && SelectionStart == SelectionEnd)
        {
            SetCurrentValue(CaretIndexProperty, SelectionStart);
        }
        else if (change.Property == SelectionEndProperty && SelectionStart == SelectionEnd)
        {
            SetCurrentValue(CaretIndexProperty, SelectionEnd);
        }
        else if (change.Property == CaretIndexProperty)
        {
            var index = CaretIndex;

            if (SelectionStart == SelectionEnd)
            {
                SetCurrentValue(SelectionStartProperty, index);
                SetCurrentValue(SelectionEndProperty, index);
            }
        }
    }

    private static int CoerceCaretIndex(AvaloniaObject sender, int value)
    {
        if (sender is not ProTextBox textBox)
        {
            return Math.Max(0, value);
        }

        return Math.Clamp(value, 0, textBox.Text?.Length ?? 0);
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(EmptyPseudoClass, string.IsNullOrEmpty(Text));
    }

    private void InsertText(string input, bool recordUndo)
    {
        input = SanitizeInput(input);

        if (input.Length == 0)
        {
            return;
        }

        var text = Text ?? string.Empty;
        var (selectionStart, selectionEnd) = GetSelectionRange();
        var remaining = MaxLength > 0 ? Math.Max(0, MaxLength - (text.Length - (selectionEnd - selectionStart))) : int.MaxValue;

        if (remaining == 0)
        {
            return;
        }

        if (input.Length > remaining)
        {
            input = input[..remaining];
        }

        ReplaceSelection(input, recordUndo);
    }

    private string SanitizeInput(string input)
    {
        if (!AcceptsReturn)
        {
            input = input.Replace("\r", string.Empty).Replace("\n", string.Empty);
        }

        if (!AcceptsTab)
        {
            input = input.Replace("\t", string.Empty);
        }

        return input;
    }

    private void Backspace(bool wholeWord)
    {
        if (DeleteSelection(recordUndo: true))
        {
            return;
        }

        var text = Text ?? string.Empty;
        var caretIndex = CaretIndex;

        if (caretIndex <= 0 || text.Length == 0)
        {
            return;
        }

        var start = wholeWord ? FindPreviousWordDeletionStart(text, caretIndex) : Math.Max(0, caretIndex - 1);
        SnapshotUndoRedo();
        SetCurrentValue(TextProperty, text.Remove(start, caretIndex - start));
        SetCaretAndSelection(start);
    }

    private void Delete(bool wholeWord)
    {
        if (DeleteSelection(recordUndo: true))
        {
            return;
        }

        var text = Text ?? string.Empty;
        var caretIndex = CaretIndex;

        if (caretIndex >= text.Length)
        {
            return;
        }

        var end = wholeWord ? FindNextWordDeletionEnd(text, caretIndex) : caretIndex + 1;
        SnapshotUndoRedo();
        SetCurrentValue(TextProperty, text.Remove(caretIndex, end - caretIndex));
        SetCaretAndSelection(caretIndex);
    }

    private bool DeleteSelection(bool recordUndo)
    {
        var text = Text ?? string.Empty;
        var (selectionStart, selectionEnd) = GetSelectionRange();

        if (selectionStart == selectionEnd)
        {
            return false;
        }

        if (recordUndo)
        {
            SnapshotUndoRedo();
        }

        SetCurrentValue(TextProperty, text.Remove(selectionStart, selectionEnd - selectionStart));
        SetCaretAndSelection(selectionStart);
        return true;
    }

    private void ReplaceSelection(string replacement, bool recordUndo)
    {
        if (IsReadOnly)
        {
            return;
        }

        replacement = SanitizeInput(replacement);

        var text = Text ?? string.Empty;
        var (selectionStart, selectionEnd) = GetSelectionRange();
        var remaining = MaxLength > 0 ? Math.Max(0, MaxLength - (text.Length - (selectionEnd - selectionStart))) : int.MaxValue;

        if (remaining == 0 && replacement.Length > 0)
        {
            return;
        }

        if (replacement.Length > remaining)
        {
            replacement = replacement[..remaining];
        }

        if (recordUndo)
        {
            SnapshotUndoRedo();
        }

        var newText = text.Remove(selectionStart, selectionEnd - selectionStart).Insert(selectionStart, replacement);
        SetCurrentValue(TextProperty, newText);
        SetCaretAndSelection(selectionStart + replacement.Length);
    }

    private void MoveCaret(int delta, bool selecting)
    {
        SetCaretAndSelection(CaretIndex + delta, selecting);
    }

    private void SetCaretAndSelection(int index, bool selecting = false)
    {
        index = Math.Clamp(index, 0, Text?.Length ?? 0);

        if (selecting)
        {
            if (SelectionStart == SelectionEnd)
            {
                SetCurrentValue(SelectionStartProperty, CaretIndex);
            }

            SetCurrentValue(SelectionEndProperty, index);
        }
        else
        {
            SetCurrentValue(SelectionStartProperty, index);
            SetCurrentValue(SelectionEndProperty, index);
        }

        SetCurrentValue(CaretIndexProperty, index);
        _presenter?.MoveCaretToTextPosition(index);
    }

    private (int Start, int End) GetSelectionRange()
    {
        var start = Math.Clamp(Math.Min(SelectionStart, SelectionEnd), 0, Text?.Length ?? 0);
        var end = Math.Clamp(Math.Max(SelectionStart, SelectionEnd), 0, Text?.Length ?? 0);
        return (start, end);
    }

    private string GetSelection()
    {
        var text = Text ?? string.Empty;
        var (start, end) = GetSelectionRange();
        return start == end ? string.Empty : text[start..end];
    }

    private void SnapshotUndoRedo()
    {
        var limit = UndoLimit;

        if (limit == 0)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            return;
        }

        var state = CaptureState();

        if (_undoStack.TryPeek(out var previous) && previous == state)
        {
            return;
        }

        _undoStack.Push(state);
        _redoStack.Clear();

        while (_undoStack.Count > limit)
        {
            var states = _undoStack.Reverse().TakeLast(limit).ToArray();
            _undoStack.Clear();

            foreach (var retained in states)
            {
                _undoStack.Push(retained);
            }
        }
    }

    private TextEditState CaptureState()
    {
        return new TextEditState(Text, CaretIndex, SelectionStart, SelectionEnd);
    }

    private void RestoreState(TextEditState state)
    {
        SetCurrentValue(TextProperty, state.Text);
        SetCurrentValue(SelectionStartProperty, state.SelectionStart);
        SetCurrentValue(SelectionEndProperty, state.SelectionEnd);
        SetCurrentValue(CaretIndexProperty, state.CaretIndex);
        _presenter?.MoveCaretToTextPosition(state.CaretIndex);
    }

    private static int FindPreviousWordDeletionStart(string text, int caretIndex)
    {
        var index = Math.Clamp(caretIndex, 0, text.Length);

        if (index == 0)
        {
            return 0;
        }

        index--;

        while (index > 0 && char.IsWhiteSpace(text[index]))
        {
            index--;
        }

        while (index > 0 && !char.IsWhiteSpace(text[index - 1]))
        {
            index--;
        }

        return index;
    }

    private static int FindNextWordDeletionEnd(string text, int caretIndex)
    {
        var index = Math.Clamp(caretIndex, 0, text.Length);

        if (index >= text.Length)
        {
            return text.Length;
        }

        if (char.IsWhiteSpace(text[index]))
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            return index;
        }

        while (index < text.Length && !char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        return index;
    }

    private readonly record struct TextEditState(string? Text, int CaretIndex, int SelectionStart, int SelectionEnd);
}
