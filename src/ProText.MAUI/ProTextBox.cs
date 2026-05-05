using Microsoft.Maui.Controls;

namespace ProText.MAUI;

/// <summary>
/// A lightweight Editor-like host that keeps visible text on the ProText presenter path.
/// </summary>
public class ProTextBox : ProTextPresenter
{
    /// <summary>
    /// Defines the <see cref="AcceptsReturn"/> property.
    /// </summary>
    public static readonly BindableProperty AcceptsReturnProperty =
        BindableProperty.Create(nameof(AcceptsReturn), typeof(bool), typeof(ProTextBox), false);

    /// <summary>
    /// Defines the <see cref="IsReadOnly"/> property.
    /// </summary>
    public static readonly BindableProperty IsReadOnlyProperty =
        BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(ProTextBox), false);

    /// <summary>
    /// Defines the <see cref="MaxLength"/> property.
    /// </summary>
    public static readonly BindableProperty MaxLengthProperty =
        BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(ProTextBox), 0, propertyChanged: OnMaxLengthChanged);

    /// <summary>
    /// Defines the <see cref="Placeholder"/> property.
    /// </summary>
    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(ProTextBox), null);

    /// <summary>
    /// Gets or sets whether newline insertion is allowed by editing helpers.
    /// </summary>
    public bool AcceptsReturn
    {
        get => (bool)GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    /// <summary>
    /// Gets or sets whether editing helpers can mutate <see cref="ProTextBlock.Text"/>.
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum text length. Zero means unlimited.
    /// </summary>
    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, Math.Max(0, value));
    }

    /// <summary>
    /// Gets or sets placeholder text for hosts that choose to surface it.
    /// </summary>
    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>
    /// Gets or sets selected text.
    /// </summary>
    public string SelectedText
    {
        get
        {
            var text = Text ?? string.Empty;
            var (start, end) = GetSelectionRange(text.Length);
            return start == end ? string.Empty : text[start..end];
        }
        set => ReplaceSelection(value ?? string.Empty);
    }

    /// <summary>
    /// Selects a text range.
    /// </summary>
    public void Select(int start, int length)
    {
        var textLength = (Text ?? string.Empty).Length;
        start = Math.Clamp(start, 0, textLength);
        length = Math.Max(0, length);
        SelectionStart = start;
        SelectionEnd = Math.Clamp(start + length, 0, textLength);
        CaretIndex = SelectionEnd;
    }

    /// <summary>
    /// Selects all text.
    /// </summary>
    public void SelectAll()
    {
        Select(0, (Text ?? string.Empty).Length);
    }

    /// <summary>
    /// Clears the active selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectionStart = CaretIndex;
        SelectionEnd = CaretIndex;
    }

    /// <summary>
    /// Clears all text.
    /// </summary>
    public void Clear()
    {
        if (IsReadOnly)
        {
            return;
        }

        Text = string.Empty;
        CaretIndex = 0;
        ClearSelection();
    }

    /// <summary>
    /// Appends text through the ProText text path.
    /// </summary>
    public void AppendText(string? text)
    {
        if (IsReadOnly || string.IsNullOrEmpty(text))
        {
            return;
        }

        var source = Text ?? string.Empty;
        var normalized = NormalizeInput(text);
        SetTextAndCaret(source + normalized, source.Length + normalized.Length);
    }

    /// <summary>
    /// Inserts text at the caret or replaces the selection.
    /// </summary>
    public void InsertTextAtCaret(string? text)
    {
        if (IsReadOnly || string.IsNullOrEmpty(text))
        {
            return;
        }

        ReplaceSelection(text);
    }

    /// <summary>
    /// Deletes the current selection, if any.
    /// </summary>
    public void DeleteSelection()
    {
        if (IsReadOnly)
        {
            return;
        }

        ReplaceSelection(string.Empty);
    }

    private void ReplaceSelection(string text)
    {
        if (IsReadOnly)
        {
            return;
        }

        var source = Text ?? string.Empty;
        var (start, end) = GetSelectionRange(source.Length);
        var replacement = NormalizeInput(text);
        var next = source[..start] + replacement + source[end..];
        SetTextAndCaret(next, start + replacement.Length);
    }

    private void SetTextAndCaret(string text, int caretIndex)
    {
        if (MaxLength > 0 && text.Length > MaxLength)
        {
            text = text[..MaxLength];
        }

        Text = text;
        CaretIndex = Math.Clamp(caretIndex, 0, text.Length);
        SelectionStart = CaretIndex;
        SelectionEnd = CaretIndex;
    }

    private string NormalizeInput(string text)
    {
        return AcceptsReturn ? text : text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
    }

    private (int Start, int End) GetSelectionRange(int textLength)
    {
        var start = Math.Clamp(Math.Min(SelectionStart, SelectionEnd), 0, textLength);
        var end = Math.Clamp(Math.Max(SelectionStart, SelectionEnd), 0, textLength);
        return (start, end);
    }

    private static void OnMaxLengthChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not ProTextBox textBox || textBox.MaxLength <= 0)
        {
            return;
        }

        var text = textBox.Text ?? string.Empty;

        if (text.Length <= textBox.MaxLength)
        {
            return;
        }

        textBox.Text = text[..textBox.MaxLength];
        textBox.CaretIndex = Math.Min(textBox.CaretIndex, textBox.MaxLength);
        textBox.SelectionStart = Math.Min(textBox.SelectionStart, textBox.MaxLength);
        textBox.SelectionEnd = Math.Min(textBox.SelectionEnd, textBox.MaxLength);
    }
}
