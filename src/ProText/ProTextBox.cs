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
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace ProText;

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
    /// Defines the <see cref="IsInactiveSelectionHighlightEnabled"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsInactiveSelectionHighlightEnabledProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(IsInactiveSelectionHighlightEnabled), true);

    /// <summary>
    /// Defines the <see cref="ClearSelectionOnLostFocus"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ClearSelectionOnLostFocusProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(ClearSelectionOnLostFocus), true);

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
    /// Defines the <see cref="MaxLines"/> property.
    /// </summary>
    public static readonly StyledProperty<int> MaxLinesProperty =
        AvaloniaProperty.Register<ProTextBox, int>(nameof(MaxLines));

    /// <summary>
    /// Defines the <see cref="MinLines"/> property.
    /// </summary>
    public static readonly StyledProperty<int> MinLinesProperty =
        AvaloniaProperty.Register<ProTextBox, int>(nameof(MinLines));

    /// <summary>
    /// Defines the <see cref="IsUndoEnabled"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsUndoEnabledProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(IsUndoEnabled), true);

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
    /// Defines the obsolete <see cref="Watermark"/> property alias.
    /// </summary>
    [Obsolete("Use PlaceholderTextProperty instead.", false)]
    public static readonly StyledProperty<string?> WatermarkProperty = PlaceholderTextProperty;

    /// <summary>
    /// Defines the <see cref="UseFloatingPlaceholder"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> UseFloatingPlaceholderProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(UseFloatingPlaceholder));

    /// <summary>
    /// Defines the obsolete <see cref="UseFloatingWatermark"/> property alias.
    /// </summary>
    [Obsolete("Use UseFloatingPlaceholderProperty instead.", false)]
    public static readonly StyledProperty<bool> UseFloatingWatermarkProperty = UseFloatingPlaceholderProperty;

    /// <summary>
    /// Defines the <see cref="PlaceholderForeground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> PlaceholderForegroundProperty =
        AvaloniaProperty.Register<ProTextBox, IBrush?>(nameof(PlaceholderForeground));

    /// <summary>
    /// Defines the obsolete <see cref="WatermarkForeground"/> property alias.
    /// </summary>
    [Obsolete("Use PlaceholderForegroundProperty instead.", false)]
    public static readonly StyledProperty<IBrush?> WatermarkForegroundProperty = PlaceholderForegroundProperty;

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

    /// <summary>
    /// Defines the <see cref="CanCut"/> property.
    /// </summary>
    public static readonly DirectProperty<ProTextBox, bool> CanCutProperty =
        AvaloniaProperty.RegisterDirect<ProTextBox, bool>(nameof(CanCut), control => control.CanCut);

    /// <summary>
    /// Defines the <see cref="CanCopy"/> property.
    /// </summary>
    public static readonly DirectProperty<ProTextBox, bool> CanCopyProperty =
        AvaloniaProperty.RegisterDirect<ProTextBox, bool>(nameof(CanCopy), control => control.CanCopy);

    /// <summary>
    /// Defines the <see cref="CanPaste"/> property.
    /// </summary>
    public static readonly DirectProperty<ProTextBox, bool> CanPasteProperty =
        AvaloniaProperty.RegisterDirect<ProTextBox, bool>(nameof(CanPaste), control => control.CanPaste);

    /// <summary>
    /// Defines the <see cref="CanUndo"/> property.
    /// </summary>
    public static readonly DirectProperty<ProTextBox, bool> CanUndoProperty =
        AvaloniaProperty.RegisterDirect<ProTextBox, bool>(nameof(CanUndo), control => control.CanUndo);

    /// <summary>
    /// Defines the <see cref="CanRedo"/> property.
    /// </summary>
    public static readonly DirectProperty<ProTextBox, bool> CanRedoProperty =
        AvaloniaProperty.RegisterDirect<ProTextBox, bool>(nameof(CanRedo), control => control.CanRedo);

    /// <summary>
    /// Defines the <see cref="TextChanged"/> event.
    /// </summary>
    public static readonly RoutedEvent<TextChangedEventArgs> TextChangedEvent =
        RoutedEvent.Register<ProTextBox, TextChangedEventArgs>(nameof(TextChanged), RoutingStrategies.Bubble);

    /// <summary>
    /// Defines the <see cref="TextChanging"/> event.
    /// </summary>
    public static readonly RoutedEvent<TextChangingEventArgs> TextChangingEvent =
        RoutedEvent.Register<ProTextBox, TextChangingEventArgs>(nameof(TextChanging), RoutingStrategies.Bubble);

    /// <summary>
    /// Defines the <see cref="CopyingToClipboard"/> event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> CopyingToClipboardEvent =
        RoutedEvent.Register<ProTextBox, RoutedEventArgs>(nameof(CopyingToClipboard), RoutingStrategies.Bubble);

    /// <summary>
    /// Defines the <see cref="CuttingToClipboard"/> event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> CuttingToClipboardEvent =
        RoutedEvent.Register<ProTextBox, RoutedEventArgs>(nameof(CuttingToClipboard), RoutingStrategies.Bubble);

    /// <summary>
    /// Defines the <see cref="PastingFromClipboard"/> event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> PastingFromClipboardEvent =
        RoutedEvent.Register<ProTextBox, RoutedEventArgs>(nameof(PastingFromClipboard), RoutingStrategies.Bubble);

    private ProTextPresenter? _presenter;
    private ScrollViewer? _scrollViewer;
    private readonly Stack<TextEditState> _undoStack = new();
    private readonly Stack<TextEditState> _redoStack = new();
    private bool _canCut;
    private bool _canCopy;
    private bool _canPaste;
    private bool _canUndo;
    private bool _canRedo;
    private bool _isSettingTextWithEvents;
    private bool _isSelectingWithPointer;
    private int _selectionAnchor;

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
            MaxLinesProperty,
            MinLinesProperty,
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
    /// Initializes a new instance of the <see cref="ProTextBox"/> class.
    /// </summary>
    public ProTextBox()
    {
        UpdateCanExecuteProperties();
    }

    /// <summary>
    /// Raised after text content changes.
    /// </summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged
    {
        add => AddHandler(TextChangedEvent, value);
        remove => RemoveHandler(TextChangedEvent, value);
    }

    /// <summary>
    /// Raised immediately before text content changes through ProTextBox editing APIs.
    /// </summary>
    public event EventHandler<TextChangingEventArgs>? TextChanging
    {
        add => AddHandler(TextChangingEvent, value);
        remove => RemoveHandler(TextChangingEvent, value);
    }

    /// <summary>
    /// Raised before selected text is copied to the clipboard.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? CopyingToClipboard
    {
        add => AddHandler(CopyingToClipboardEvent, value);
        remove => RemoveHandler(CopyingToClipboardEvent, value);
    }

    /// <summary>
    /// Raised before selected text is cut to the clipboard.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? CuttingToClipboard
    {
        add => AddHandler(CuttingToClipboardEvent, value);
        remove => RemoveHandler(CuttingToClipboardEvent, value);
    }

    /// <summary>
    /// Raised before clipboard text is pasted.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? PastingFromClipboard
    {
        add => AddHandler(PastingFromClipboardEvent, value);
        remove => RemoveHandler(PastingFromClipboardEvent, value);
    }

    /// <summary>
    /// Gets or sets a value that determines whether selection remains highlighted when the control is not focused.
    /// </summary>
    public bool IsInactiveSelectionHighlightEnabled
    {
        get => GetValue(IsInactiveSelectionHighlightEnabledProperty);
        set => SetValue(IsInactiveSelectionHighlightEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets a value that determines whether the selection is cleared when focus is lost.
    /// </summary>
    public bool ClearSelectionOnLostFocus
    {
        get => GetValue(ClearSelectionOnLostFocusProperty);
        set => SetValue(ClearSelectionOnLostFocusProperty, value);
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
    /// Gets or sets the maximum number of visible lines to size to.
    /// </summary>
    public int MaxLines
    {
        get => GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum number of visible lines to size to.
    /// </summary>
    public int MinLines
    {
        get => GetValue(MinLinesProperty);
        set => SetValue(MinLinesProperty, value);
    }

    /// <summary>
    /// Gets or sets whether undo and redo operations are enabled.
    /// </summary>
    public bool IsUndoEnabled
    {
        get => GetValue(IsUndoEnabledProperty);
        set => SetValue(IsUndoEnabledProperty, value);
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
    /// Gets or sets placeholder text.
    /// </summary>
    [Obsolete("Use PlaceholderText instead.", false)]
    public string? Watermark
    {
        get => PlaceholderText;
        set => PlaceholderText = value;
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
    /// Gets or sets whether the placeholder floats above text once content exists.
    /// </summary>
    [Obsolete("Use UseFloatingPlaceholder instead.", false)]
    public bool UseFloatingWatermark
    {
        get => UseFloatingPlaceholder;
        set => UseFloatingPlaceholder = value;
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
    /// Gets or sets the placeholder foreground brush.
    /// </summary>
    [Obsolete("Use PlaceholderForeground instead.", false)]
    public IBrush? WatermarkForeground
    {
        get => PlaceholderForeground;
        set => PlaceholderForeground = value;
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
    public bool CanUndo
    {
        get => _canUndo;
        private set => SetAndRaise(CanUndoProperty, ref _canUndo, value);
    }

    /// <summary>
    /// Gets whether a redo operation can currently execute.
    /// </summary>
    public bool CanRedo
    {
        get => _canRedo;
        private set => SetAndRaise(CanRedoProperty, ref _canRedo, value);
    }

    /// <summary>
    /// Gets whether cut can currently execute.
    /// </summary>
    public bool CanCut
    {
        get => _canCut;
        private set => SetAndRaise(CanCutProperty, ref _canCut, value);
    }

    /// <summary>
    /// Gets whether copy can currently execute.
    /// </summary>
    public bool CanCopy
    {
        get => _canCopy;
        private set => SetAndRaise(CanCopyProperty, ref _canCopy, value);
    }

    /// <summary>
    /// Gets whether paste can currently execute.
    /// </summary>
    public bool CanPaste
    {
        get => _canPaste;
        private set => SetAndRaise(CanPasteProperty, ref _canPaste, value);
    }

    private bool IsPasswordBox => PasswordChar != default && !RevealPassword;

    /// <summary>
    /// Selects all text.
    /// </summary>
    public void SelectAll()
    {
        var length = Text?.Length ?? 0;
        SetSelectionRange(0, length);
    }

    /// <summary>
    /// Clears the current selection while preserving the caret position.
    /// </summary>
    public void ClearSelection()
    {
        SetCaretAndSelection(CaretIndex);
    }

    /// <summary>
    /// Gets the number of rendered lines in the presenter, or -1 before layout is available.
    /// </summary>
    public int GetLineCount()
    {
        return _presenter?.GetLineCount() ?? -1;
    }

    /// <summary>
    /// Scrolls the text presenter to the specified rendered line when a scroll viewer is available.
    /// </summary>
    public void ScrollToLine(int lineIndex)
    {
        if (_scrollViewer is null || _presenter is null)
        {
            return;
        }

        var bounds = _presenter.GetLineBounds(Math.Max(0, lineIndex));
        _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, Math.Max(0, bounds.Y));
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
        SetTextWithEvents(string.Empty);
        SetCaretAndSelection(0);
    }

    /// <summary>
    /// Restores the previous edit state when available.
    /// </summary>
    public void Undo()
    {
        if (!IsUndoEnabled || _undoStack.Count == 0)
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
        if (!IsUndoEnabled || _redoStack.Count == 0)
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

        if (RaiseCancelableEvent(CuttingToClipboardEvent))
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

        if (RaiseCancelableEvent(CopyingToClipboardEvent))
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

        if (RaiseCancelableEvent(PastingFromClipboardEvent))
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
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        UpdateLineConstraints();

        if (_presenter is not null)
        {
            _presenter.ShowSelectionHighlight = IsFocused || IsInactiveSelectionHighlightEnabled;

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
        if (_presenter is not null)
        {
            _presenter.ShowSelectionHighlight = true;
        }
    }

    /// <inheritdoc />
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        SetCurrentValue(RevealPasswordProperty, false);
        _presenter?.HideCaret();

        if (ClearSelectionOnLostFocus)
        {
            ClearSelection();
        }
        else if (_presenter is not null)
        {
            _presenter.ShowSelectionHighlight = IsInactiveSelectionHighlightEnabled;
        }
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

                if (e.ClickCount >= 3)
                {
                    SelectLineAt(index);
                    _selectionAnchor = SelectionStart;
                }
                else if (e.ClickCount == 2 && !IsPasswordBox)
                {
                    SelectWordAt(index);
                    _selectionAnchor = SelectionStart;
                }
                else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    _selectionAnchor = SelectionStart == SelectionEnd ? CaretIndex : SelectionStart;
                    SetSelectionRange(_selectionAnchor, index);
                }
                else
                {
                    _selectionAnchor = index;
                    SetCaretAndSelection(index);
                }

                _isSelectingWithPointer = true;
                e.Pointer.Capture(this);
            }

            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isSelectingWithPointer || _presenter is null)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isSelectingWithPointer = false;
            e.Pointer.Capture(null);
            return;
        }

        var index = _presenter.GetCharacterIndex(e.GetPosition(_presenter));
        SetSelectionRange(_selectionAnchor, index);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isSelectingWithPointer)
        {
            _isSelectingWithPointer = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isSelectingWithPointer = false;
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
                MoveCaretHorizontal(
                    LogicalDirection.Backward,
                    e.KeyModifiers.HasFlag(KeyModifiers.Control) && !IsPasswordBox,
                    e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;
            case Key.Right:
                MoveCaretHorizontal(
                    LogicalDirection.Forward,
                    e.KeyModifiers.HasFlag(KeyModifiers.Control) && !IsPasswordBox,
                    e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;
            case Key.Up:
                MoveCaretVertical(LogicalDirection.Backward, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;
            case Key.Down:
                MoveCaretVertical(LogicalDirection.Forward, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;
            case Key.Home:
                SetCaretAndSelection(GetHomeIndex(e.KeyModifiers.HasFlag(KeyModifiers.Control)), e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;
            case Key.End:
                SetCaretAndSelection(GetEndIndex(e.KeyModifiers.HasFlag(KeyModifiers.Control)), e.KeyModifiers.HasFlag(KeyModifiers.Shift));
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
            UpdateCanExecuteProperties();

            if (!_isSettingTextWithEvents)
            {
                RaiseTextChangedEvent();
            }
        }
        else if (change.Property == SelectionStartProperty || change.Property == SelectionEndProperty)
        {
            if (SelectionStart == SelectionEnd)
            {
                SetCurrentValue(CaretIndexProperty, SelectionStart);
            }

            UpdateCanExecuteProperties();
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
        else if (change.Property == IsReadOnlyProperty
            || change.Property == PasswordCharProperty
            || change.Property == RevealPasswordProperty
            || change.Property == IsUndoEnabledProperty)
        {
            if (change.Property == IsUndoEnabledProperty && !IsUndoEnabled)
            {
                _undoStack.Clear();
                _redoStack.Clear();
            }

            UpdateCanExecuteProperties();
        }
        else if (change.Property == MinLinesProperty || change.Property == MaxLinesProperty || change.Property == LineHeightProperty)
        {
            UpdateLineConstraints();
        }
        else if (change.Property == IsInactiveSelectionHighlightEnabledProperty && _presenter is not null && !IsFocused)
        {
            _presenter.ShowSelectionHighlight = IsInactiveSelectionHighlightEnabled;
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        UpdateLineConstraints();
        return base.MeasureOverride(availableSize);
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
        SetTextWithEvents(text.Remove(start, caretIndex - start));
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
        SetTextWithEvents(text.Remove(caretIndex, end - caretIndex));
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

        SetTextWithEvents(text.Remove(selectionStart, selectionEnd - selectionStart));
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
        SetTextWithEvents(newText);
        SetCaretAndSelection(selectionStart + replacement.Length);
    }

    private void MoveCaretHorizontal(LogicalDirection direction, bool wholeWord, bool selecting)
    {
        var text = Text ?? string.Empty;
        var index = direction == LogicalDirection.Forward
            ? (wholeWord ? FindNextWordNavigationIndex(text, CaretIndex) : Math.Min(text.Length, CaretIndex + 1))
            : (wholeWord ? FindPreviousWordDeletionStart(text, CaretIndex) : Math.Max(0, CaretIndex - 1));

        SetCaretAndSelection(index, selecting);
    }

    private void MoveCaretVertical(LogicalDirection direction, bool selecting)
    {
        if (_presenter is null)
        {
            return;
        }

        var previous = CaretIndex;
        _presenter.MoveCaretToTextPosition(previous);
        _presenter.MoveCaretVertical(direction);
        var index = _presenter.CaretIndex;
        SetCaretAndSelection(index, selecting);
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
        UpdateCanExecuteProperties();
    }

    private void SetSelectionRange(int anchor, int caret)
    {
        var length = Text?.Length ?? 0;
        anchor = Math.Clamp(anchor, 0, length);
        caret = Math.Clamp(caret, 0, length);

        SetCurrentValue(SelectionStartProperty, anchor);
        SetCurrentValue(SelectionEndProperty, caret);
        SetCurrentValue(CaretIndexProperty, caret);
        _presenter?.MoveCaretToTextPosition(caret);
        UpdateCanExecuteProperties();
    }

    private void SelectWordAt(int index)
    {
        var text = Text ?? string.Empty;
        var (start, end) = GetWordRangeAt(text, index);
        SetSelectionRange(start, end);
    }

    private void SelectLineAt(int index)
    {
        var text = Text ?? string.Empty;
        var (start, end) = GetLineRangeAt(text, index);
        SetSelectionRange(start, end);
    }

    private int GetHomeIndex(bool documentStart)
    {
        if (documentStart)
        {
            return 0;
        }

        var text = Text ?? string.Empty;
        var index = Math.Clamp(CaretIndex, 0, text.Length);
        while (index > 0 && text[index - 1] != '\n' && text[index - 1] != '\r')
        {
            index--;
        }

        return index;
    }

    private int GetEndIndex(bool documentEnd)
    {
        var text = Text ?? string.Empty;

        if (documentEnd)
        {
            return text.Length;
        }

        var index = Math.Clamp(CaretIndex, 0, text.Length);
        while (index < text.Length && text[index] != '\r' && text[index] != '\n')
        {
            index++;
        }

        return index;
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
        if (!IsUndoEnabled)
        {
            return;
        }

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
            UpdateCanExecuteProperties();
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

        UpdateCanExecuteProperties();
    }

    private TextEditState CaptureState()
    {
        return new TextEditState(Text, CaretIndex, SelectionStart, SelectionEnd);
    }

    private void RestoreState(TextEditState state)
    {
        SetTextWithEvents(state.Text);
        SetCurrentValue(SelectionStartProperty, state.SelectionStart);
        SetCurrentValue(SelectionEndProperty, state.SelectionEnd);
        SetCurrentValue(CaretIndexProperty, state.CaretIndex);
        _presenter?.MoveCaretToTextPosition(state.CaretIndex);
        UpdateCanExecuteProperties();
    }

    private void SetTextWithEvents(string? text)
    {
        if (Text == text)
        {
            return;
        }

        RaiseEvent(new TextChangingEventArgs(TextChangingEvent, this));
        _isSettingTextWithEvents = true;

        try
        {
            SetCurrentValue(TextProperty, text);
        }
        finally
        {
            _isSettingTextWithEvents = false;
        }

        RaiseTextChangedEvent();
    }

    private void RaiseTextChangedEvent()
    {
        RaiseEvent(new TextChangedEventArgs(TextChangedEvent, this));
    }

    private bool RaiseCancelableEvent(RoutedEvent<RoutedEventArgs> routedEvent)
    {
        var args = new RoutedEventArgs(routedEvent, this);
        RaiseEvent(args);
        return args.Handled;
    }

    private void UpdateCanExecuteProperties()
    {
        var hasSelection = SelectionStart != SelectionEnd;
        CanCopy = !IsPasswordBox && hasSelection;
        CanCut = !IsReadOnly && CanCopy;
        CanPaste = !IsReadOnly;
        CanUndo = IsUndoEnabled && _undoStack.Count > 0;
        CanRedo = IsUndoEnabled && _redoStack.Count > 0;
    }

    private void UpdateLineConstraints()
    {
        if (_scrollViewer is null)
        {
            return;
        }

        var lineHeight = ResolveLineHeight();
        _scrollViewer.MinHeight = MinLines > 0 ? lineHeight * MinLines : 0;
        _scrollViewer.MaxHeight = MaxLines > 0 ? lineHeight * MaxLines : double.PositiveInfinity;
    }

    private double ResolveLineHeight()
    {
        if (!double.IsNaN(LineHeight) && LineHeight > 0)
        {
            return LineHeight;
        }

        return Math.Max(1, FontSize * PretextLineHeightMultiplier);
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

    private static int FindNextWordNavigationIndex(string text, int caretIndex)
    {
        var index = Math.Clamp(caretIndex, 0, text.Length);

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

    private static (int Start, int End) GetWordRangeAt(string text, int index)
    {
        if (text.Length == 0)
        {
            return (0, 0);
        }

        index = Math.Clamp(index, 0, text.Length - 1);

        if (index > 0 && (index == text.Length || char.IsWhiteSpace(text[index])))
        {
            index--;
        }

        if (char.IsWhiteSpace(text[index]))
        {
            var whitespaceStart = index;
            var whitespaceEnd = index + 1;

            while (whitespaceStart > 0 && char.IsWhiteSpace(text[whitespaceStart - 1]))
            {
                whitespaceStart--;
            }

            while (whitespaceEnd < text.Length && char.IsWhiteSpace(text[whitespaceEnd]))
            {
                whitespaceEnd++;
            }

            return (whitespaceStart, whitespaceEnd);
        }

        var start = index;
        var end = index + 1;

        while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
        {
            start--;
        }

        while (end < text.Length && !char.IsWhiteSpace(text[end]))
        {
            end++;
        }

        return (start, end);
    }

    private static (int Start, int End) GetLineRangeAt(string text, int index)
    {
        index = Math.Clamp(index, 0, text.Length);
        var start = index;
        var end = index;

        while (start > 0 && text[start - 1] != '\n' && text[start - 1] != '\r')
        {
            start--;
        }

        while (end < text.Length && text[end] != '\r' && text[end] != '\n')
        {
            end++;
        }

        return (start, end);
    }

    private readonly record struct TextEditState(string? Text, int CaretIndex, int SelectionStart, int SelectionEnd);
}
