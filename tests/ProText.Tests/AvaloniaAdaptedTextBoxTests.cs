using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ProTextBoxControl = ProText.Avalonia.ProTextBox;
using ProTextPresenterControl = ProText.Avalonia.ProTextPresenter;

namespace ProText.Tests;

public sealed class AvaloniaAdaptedTextBoxTests
{
    [AvaloniaFact]
    public void DefaultBindingMode_Should_Be_TwoWay()
    {
        Assert.Equal(BindingMode.TwoWay, ProTextBoxControl.TextProperty.GetMetadata(typeof(ProTextBoxControl)).DefaultBindingMode);
    }

    [AvaloniaFact]
    public void CaretIndex_Can_Move_To_Position_After_The_End_Of_Text_With_Arrow_Key()
    {
        var target = CreateTextBox("1234");
        target.CaretIndex = 3;

        RaiseKeyEvent(target, Key.Right, KeyModifiers.None);

        Assert.Equal(4, target.CaretIndex);
    }

    [AvaloniaFact]
    public void Shift_Right_Extends_Selection_From_Caret()
    {
        var target = CreateTextBox("1234");
        target.CaretIndex = 1;

        RaiseKeyEvent(target, Key.Right, KeyModifiers.Shift);

        Assert.Equal(1, target.SelectionStart);
        Assert.Equal(2, target.SelectionEnd);
        Assert.Equal(2, target.CaretIndex);
    }

    [AvaloniaFact]
    public void Control_Shift_Right_Extends_Selection_To_Next_Word()
    {
        var target = CreateTextBox("First Second Third");
        target.CaretIndex = 0;

        RaiseKeyEvent(target, Key.Right, KeyModifiers.Control | KeyModifiers.Shift);

        Assert.Equal(0, target.SelectionStart);
        Assert.Equal(6, target.SelectionEnd);
        Assert.Equal(6, target.CaretIndex);
    }

    [AvaloniaFact]
    public void Down_Key_Moves_Caret_To_Next_Rendered_Line()
    {
        var target = CreateTextBox("one\ntwo\nthree");
        target.Width = 260;
        target.Height = 90;
        target.CaretIndex = 1;
        var window = new Window { Width = 300, Height = 120, Content = target };
        window.Show();

        RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

        Assert.True(target.CaretIndex > 1);
        Assert.Equal(1, target.SelectionStart);
        Assert.Equal(target.CaretIndex, target.SelectionEnd);
    }

    [AvaloniaFact]
    public void Press_Ctrl_A_Select_All_Text()
    {
        var target = CreateTextBox("1234");

        RaiseKeyEvent(target, Key.A, KeyModifiers.Control);
        Assert.Equal(0, target.SelectionStart);
        Assert.Equal(4, target.SelectionEnd);
    }

    [AvaloniaFact]
    public void Press_Ctrl_A_Select_All_Null_Text()
    {
        var target = CreateTextBox(null);

        RaiseKeyEvent(target, Key.A, KeyModifiers.Control);
        Assert.Equal(0, target.SelectionStart);
        Assert.Equal(0, target.SelectionEnd);
    }

    [AvaloniaFact]
    public void ClearSelection_Collapses_Selection_To_Caret()
    {
        var target = CreateTextBox("012345");
        target.SelectionStart = 1;
        target.SelectionEnd = 4;
        target.CaretIndex = 4;

        target.ClearSelection();

        Assert.Equal(4, target.SelectionStart);
        Assert.Equal(4, target.SelectionEnd);
        Assert.Equal(4, target.CaretIndex);
    }

    [AvaloniaFact]
    public void Can_Properties_Update_When_Selection_Changes()
    {
        var target = CreateTextBox("012345");

        Assert.False(target.CanCopy);
        Assert.False(target.CanCut);
        Assert.True(target.CanPaste);

        target.SelectionStart = 1;
        target.SelectionEnd = 4;

        Assert.True(target.CanCopy);
        Assert.True(target.CanCut);

        target.PasswordChar = '*';

        Assert.False(target.CanCopy);
        Assert.False(target.CanCut);
    }

    [AvaloniaFact]
    public void Press_Ctrl_Z_Will_Not_Modify_Text_When_Undo_Stack_Is_Empty()
    {
        var target = CreateTextBox("1234");

        RaiseKeyEvent(target, Key.Z, KeyModifiers.Control);
        Assert.Equal("1234", target.Text);
    }

    [AvaloniaFact]
    public void IsUndoEnabled_Disables_Edit_History()
    {
        var target = CreateTextBox("0123");
        target.IsUndoEnabled = false;
        target.SelectionStart = 1;
        target.SelectionEnd = 3;

        RaiseTextEvent(target, "A");
        RaiseKeyEvent(target, Key.Z, KeyModifiers.Control);

        Assert.Equal("0A3", target.Text);
        Assert.False(target.CanUndo);
    }

    [AvaloniaFact]
    public void TextChanging_And_TextChanged_Fire_For_Text_Input()
    {
        var target = CreateTextBox("0123");
        var changing = 0;
        var changed = 0;
        target.TextChanging += (_, _) => changing++;
        target.TextChanged += (_, _) => changed++;

        RaiseTextEvent(target, "A");

        Assert.Equal(1, changing);
        Assert.Equal(1, changed);
    }

    [AvaloniaFact]
    public void Setting_SelectionStart_To_SelectionEnd_Sets_CaretPosition_To_SelectionStart()
    {
        var textBox = CreateTextBox("0123456789");

        textBox.SelectionStart = 2;
        textBox.SelectionEnd = 2;

        Assert.Equal(2, textBox.CaretIndex);
    }

    [AvaloniaFact]
    public void Setting_Text_Updates_CaretPosition()
    {
        var target = CreateTextBox("Initial Text");
        target.CaretIndex = 11;
        var invoked = false;

        var skipFirst = true;
        target.GetObservable(ProTextBoxControl.TextProperty).Subscribe(new ActionObserver<string?>(_ =>
        {
            if (skipFirst)
            {
                skipFirst = false;
                return;
            }

            Assert.Equal(7, target.CaretIndex);
            invoked = true;
        }));

        target.Text = "Changed";

        Assert.True(invoked);
    }

    [AvaloniaFact]
    public void Press_Enter_Does_Not_Accept_Return()
    {
        var target = CreateTextBox("1234");
        target.AcceptsReturn = false;

        RaiseKeyEvent(target, Key.Enter, KeyModifiers.None);

        Assert.Equal("1234", target.Text);
    }

    [AvaloniaFact]
    public void Press_Enter_Add_Default_Newline()
    {
        var target = CreateTextBox(null);
        target.AcceptsReturn = true;

        RaiseKeyEvent(target, Key.Enter, KeyModifiers.None);

        Assert.Equal(Environment.NewLine, target.Text);
    }

    [AvaloniaFact]
    public void Press_Enter_Add_Custom_Newline()
    {
        var target = CreateTextBox(null);
        target.AcceptsReturn = true;
        target.NewLine = "Test";

        RaiseKeyEvent(target, Key.Enter, KeyModifiers.None);

        Assert.Equal("Test", target.Text);
    }

    [AvaloniaFact]
    public void SelectionEnd_Doesnt_Cause_Exception()
    {
        var target = CreateTextBox("0123456789");

        target.SelectionStart = 0;
        target.SelectionEnd = 9;
        target.Text = "123";
        RaiseTextEvent(target, "456");

        Assert.True(target.SelectionEnd <= target.Text!.Length);
    }

    [AvaloniaFact]
    public void SelectionStartEnd_Are_Valid_After_TextChange()
    {
        var target = CreateTextBox("0123456789");

        target.SelectionStart = 8;
        target.SelectionEnd = 9;
        target.Text = "123";

        Assert.True(target.SelectionStart <= "123".Length);
        Assert.True(target.SelectionEnd <= "123".Length);
    }

    [AvaloniaFact]
    public void SelectedText_Changes_OnSelectionChange()
    {
        var target = CreateTextBox("0123456789");

        Assert.Equal(string.Empty, target.SelectedText);

        target.SelectionStart = 2;
        target.SelectionEnd = 4;

        Assert.Equal("23", target.SelectedText);
    }

    [AvaloniaFact]
    public void SelectedText_EditsText()
    {
        var target = CreateTextBox("0123");

        target.SelectedText = "AA";
        Assert.Equal("AA0123", target.Text);

        target.SelectionStart = 1;
        target.SelectionEnd = 3;
        target.SelectedText = "BB";

        Assert.Equal("ABB123", target.Text);
    }

    [AvaloniaFact]
    public void SelectedText_CanClearText()
    {
        var target = CreateTextBox("0123");
        target.SelectionStart = 1;
        target.SelectionEnd = 3;

        target.SelectedText = string.Empty;

        Assert.Equal("03", target.Text);
    }

    [AvaloniaFact]
    public void SelectedText_NullClearsText()
    {
        var target = CreateTextBox("0123");
        target.SelectionStart = 1;
        target.SelectionEnd = 3;

        target.SelectedText = null!;

        Assert.Equal("03", target.Text);
    }

    [AvaloniaFact]
    public void CoerceCaretIndex_Doesnt_Cause_Exception_With_Malformed_Line_Ending()
    {
        var target = CreateTextBox("0123456789\r");
        target.CaretIndex = 11;

        Assert.True(target.CaretIndex <= target.Text!.Length);
    }

    [AvaloniaTheory]
    [InlineData(Key.Up)]
    [InlineData(Key.Down)]
    [InlineData(Key.Home)]
    [InlineData(Key.End)]
    public void Textbox_Doesnt_Crash_When_Receives_Input_And_Template_Not_Applied(Key key)
    {
        var target = new ProTextBoxControl { Text = "1234" };

        RaiseKeyEvent(target, key, KeyModifiers.None);

        Assert.True(target.CaretIndex >= 0);
    }

    [AvaloniaFact]
    public void TextBox_CaretIndex_Persists_When_Focus_Lost()
    {
        var target1 = CreateTextBox("1234");
        var target2 = CreateTextBox("5678");
        var panel = new StackPanel { Children = { target1, target2 } };
        var window = new Window { Width = 300, Height = 120, Content = panel };
        window.Show();

        target2.Focus();
        target2.CaretIndex = 2;
        target1.Focus();

        Assert.Equal(2, target2.CaretIndex);
    }

    [AvaloniaFact]
    public void TextBox_Reveal_Password_Reset_When_Lost_Focus()
    {
        var target1 = CreateTextBox("1234");
        target1.PasswordChar = '*';
        var target2 = CreateTextBox("5678");
        var panel = new StackPanel { Children = { target1, target2 } };
        var window = new Window { Width = 300, Height = 120, Content = panel };
        window.Show();

        target1.Focus();
        target1.RevealPassword = true;
        target2.Focus();

        Assert.False(target1.RevealPassword);
    }

    [AvaloniaFact]
    public void Mouse_Drag_Selects_Text()
    {
        var target = CreateTextBox("abcdef");
        target.Width = 260;
        target.Height = 60;
        var window = new Window { Width = 300, Height = 90, Content = target };
        window.Show();

        var presenter = target.GetVisualDescendants().OfType<ProTextPresenterControl>().Single();
        var start = ToWindowPoint(presenter, window, presenter.GetCaretBounds(0));
        var end = ToWindowPoint(presenter, window, presenter.GetCaretBounds(4));

        window.MouseDown(start, MouseButton.Left, RawInputModifiers.None);
        window.MouseMove(end, RawInputModifiers.LeftMouseButton);
        window.MouseUp(end, MouseButton.Left, RawInputModifiers.None);

        var selectionStart = Math.Min(target.SelectionStart, target.SelectionEnd);
        var selectionEnd = Math.Max(target.SelectionStart, target.SelectionEnd);
        Assert.Equal(0, selectionStart);
        Assert.True(selectionEnd >= 3);
    }

    [AvaloniaFact]
    public void GetLineCount_Uses_ProTextPresenter_Layout()
    {
        var target = CreateTextBox("one\ntwo\nthree");
        target.AcceptsReturn = true;
        target.Width = 260;
        target.Height = 90;
        var window = new Window { Width = 300, Height = 120, Content = target };
        window.Show();

        Assert.Equal(3, target.GetLineCount());
    }

    [AvaloniaTheory]
    [InlineData("abc", "d", 3, 0, 0, "abc")]
    [InlineData("abc", "dd", 4, 3, 3, "abcd")]
    [InlineData("abc", "ddd", 3, 0, 2, "ddc")]
    [InlineData("abc", "dddd", 4, 1, 3, "addd")]
    [InlineData("abc", "ddddd", 5, 3, 3, "abcdd")]
    public void MaxLength_Works_Properly(string initialText, string textInput, int maxLength, int selectionStart, int selectionEnd, string expected)
    {
        var target = CreateTextBox(initialText);
        target.MaxLength = maxLength;
        target.SelectionStart = selectionStart;
        target.SelectionEnd = selectionEnd;

        RaiseTextEvent(target, textInput);

        Assert.Equal(expected, target.Text);
    }

    [AvaloniaFact]
    public void Keys_Allow_Undo()
    {
        var target = CreateTextBox("0123");
        target.SelectionStart = 1;
        target.SelectionEnd = 3;

        RaiseKeyEvent(target, Key.Delete, KeyModifiers.None);
        RaiseKeyEvent(target, Key.Z, KeyModifiers.Control);

        Assert.Equal("0123", target.Text);
    }

    [AvaloniaFact]
    public void Entering_Text_With_SelectedText_Should_Fire_Single_Text_Changed_Notification()
    {
        var target = CreateTextBox("0123");
        target.AcceptsReturn = true;
        target.AcceptsTab = true;
        target.SelectionStart = 1;
        target.SelectionEnd = 3;
        var values = new List<string?>();
        target.GetObservable(ProTextBoxControl.TextProperty).Subscribe(new ActionObserver<string?>(value => values.Add(value)));

        RaiseTextEvent(target, "A");

        Assert.Equal(new[] { "0123", "0A3" }, values);
    }

    [AvaloniaFact]
    public void Control_Backspace_Should_Remove_The_Word_Before_The_Caret_If_There_Is_No_Selection()
    {
        var textBox = CreateTextBox("First Second Third Fourth");
        textBox.SelectionStart = 5;
        textBox.SelectionEnd = 5;

        RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
        Assert.Equal(" Second Third Fourth", textBox.Text);

        textBox.CaretIndex = 8;
        RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
        Assert.Equal(" Third Fourth", textBox.Text);

        textBox.CaretIndex = 4;
        RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
        Assert.Equal(" rd Fourth", textBox.Text);

        textBox.SelectionStart = 5;
        textBox.SelectionEnd = 7;
        RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
        Assert.Equal(" rd Frth", textBox.Text);

        textBox.CaretIndex = 1;
        RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
        Assert.Equal("rd Frth", textBox.Text);
    }

    [AvaloniaFact]
    public void Control_Delete_Should_Remove_The_Word_After_The_Caret_If_There_Is_No_Selection()
    {
        var textBox = CreateTextBox("First Second Third Fourth");
        textBox.CaretIndex = 19;

        RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
        Assert.Equal("First Second Third ", textBox.Text);

        textBox.CaretIndex = 13;
        RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
        Assert.Equal("First Second ", textBox.Text);

        textBox.CaretIndex = 9;
        RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
        Assert.Equal("First Sec", textBox.Text);

        textBox.SelectionStart = 2;
        textBox.SelectionEnd = 4;
        RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
        Assert.Equal("Fit Sec", textBox.Text);

        textBox.Text += " ";
        textBox.CaretIndex = 7;
        RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
        Assert.Equal("Fit Sec", textBox.Text);
    }

    [AvaloniaFact]
    public void Placeholder_With_Red_Foreground_Renders()
    {
        var target = new Border
        {
            Padding = new Thickness(8),
            Width = 200,
            Height = 50,
            Background = Brushes.White,
            Child = new ProTextBoxControl
            {
                FontSize = 12,
                Background = Brushes.White,
                PlaceholderText = "Red placeholder",
                PlaceholderForeground = Brushes.Red
            }
        };

        var window = new Window { Width = 220, Height = 80, Content = target };
        window.Show();

        Assert.NotNull(window.CaptureRenderedFrame());
    }

    private static ProTextBoxControl CreateTextBox(string? text)
    {
        var textBox = new ProTextBoxControl
        {
            Text = text,
            FontSize = 16,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black,
            SelectionBrush = Brushes.LightSkyBlue,
            SelectionForegroundBrush = Brushes.White,
            CaretBrush = Brushes.Black
        };

        textBox.ApplyTemplate();
        textBox.Measure(Size.Infinity);
        return textBox;
    }

    private static void RaiseKeyEvent(ProTextBoxControl textBox, Key key, KeyModifiers inputModifiers)
    {
        textBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            KeyModifiers = inputModifiers,
            Key = key
        });
    }

    private static void RaiseTextEvent(ProTextBoxControl textBox, string text)
    {
        textBox.RaiseEvent(new TextInputEventArgs
        {
            RoutedEvent = InputElement.TextInputEvent,
            Text = text
        });
    }

    private static Point ToWindowPoint(ProTextPresenterControl presenter, Window window, Rect caretBounds)
    {
        var point = new Point(caretBounds.X + 1, caretBounds.Y + Math.Max(1, caretBounds.Height / 2));
        return presenter.TranslatePoint(point, window) ?? point;
    }

    private sealed class ActionObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            throw error;
        }

        public void OnNext(T value)
        {
            onNext(value);
        }
    }
}
