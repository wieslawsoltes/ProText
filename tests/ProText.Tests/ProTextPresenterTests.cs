using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using ProTextPresenterControl = ProText.ProTextPresenter;

namespace ProText.Tests;

public sealed class ProTextPresenterTests
{
    [AvaloniaFact]
    public void Presenter_measures_plain_text_with_pretext()
    {
        var presenter = new ProTextPresenterControl
        {
            Text = "Presenter text wraps through shared Pretext layout.",
            FontSize = 16,
            LineHeight = 22,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        presenter.Measure(new Size(160, double.PositiveInfinity));

        Assert.True(presenter.DesiredSize.Width <= 160.1);
        Assert.True(presenter.DesiredSize.Height > 0);
    }

    [AvaloniaFact]
    public void Presenter_caret_bounds_advance_with_text_index()
    {
        var presenter = new ProTextPresenterControl
        {
            Text = "abcdef",
            FontSize = 20,
            LineHeight = 28,
            TextWrapping = TextWrapping.NoWrap,
            Foreground = Brushes.Black
        };

        presenter.Measure(new Size(300, double.PositiveInfinity));
        presenter.Arrange(new Rect(presenter.DesiredSize));

        var start = presenter.GetCaretBounds(0);
        var middle = presenter.GetCaretBounds(3);
        var end = presenter.GetCaretBounds(6);

        Assert.True(middle.X > start.X);
        Assert.True(end.X > middle.X);
        Assert.Equal(28, start.Height, precision: 3);
    }

    [AvaloniaFact]
    public void Presenter_point_hit_testing_returns_nearest_index()
    {
        var presenter = new ProTextPresenterControl
        {
            Text = "abcdef",
            FontSize = 20,
            LineHeight = 28,
            TextWrapping = TextWrapping.NoWrap,
            Foreground = Brushes.Black
        };

        presenter.Measure(new Size(300, double.PositiveInfinity));
        presenter.Arrange(new Rect(presenter.DesiredSize));

        var end = presenter.GetCaretBounds(6);
        var index = presenter.GetCharacterIndex(new Point(end.X - 1, 10));

        Assert.InRange(index, 4, 6);
    }

    [AvaloniaFact]
    public void Presenter_moves_caret_horizontally_without_textlayout()
    {
        var presenter = CreatePresenter("abcdef");
        presenter.CaretIndex = 2;

        presenter.MoveCaretHorizontal(LogicalDirection.Forward);
        Assert.Equal(3, presenter.CaretIndex);

        var previous = presenter.GetNextCharacterHit(LogicalDirection.Backward);
        Assert.Equal(2, previous.FirstCharacterIndex);
    }

    [AvaloniaFact]
    public void Presenter_moves_caret_vertically_on_pretext_layout()
    {
        var presenter = CreatePresenter("one\ntwo\nthree");
        presenter.TextWrapping = TextWrapping.Wrap;
        presenter.CaretIndex = 1;

        presenter.Measure(new Size(240, double.PositiveInfinity));
        presenter.Arrange(new Rect(presenter.DesiredSize));
        presenter.MoveCaretVertical(LogicalDirection.Forward);

        Assert.True(presenter.CaretIndex > 1);
        Assert.Equal(3, presenter.GetLineCount());
    }

    [AvaloniaFact]
    public void Presenter_preedit_text_participates_in_measurement()
    {
        var normal = CreatePresenter("input");
        var preedit = CreatePresenter("input");
        preedit.CaretIndex = 2;
        preedit.PreeditText = "漢字";
        preedit.PreeditTextCursorPosition = 1;

        normal.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        preedit.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Assert.True(preedit.DesiredSize.Width > normal.DesiredSize.Width);
    }

    [AvaloniaFact]
    public void Presenter_password_mode_preserves_caret_positions()
    {
        var presenter = CreatePresenter("secret");
        presenter.PasswordChar = '*';

        presenter.Measure(new Size(300, double.PositiveInfinity));
        presenter.Arrange(new Rect(presenter.DesiredSize));

        Assert.True(presenter.GetCaretBounds(6).X > presenter.GetCaretBounds(0).X);
    }

    [AvaloniaFact]
    public void Presenter_renders_inline_content_on_pretext_path()
    {
        var presenter = new ProTextPresenterControl
        {
            FontSize = 18,
            LineHeight = 26,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        presenter.Inlines!.Add(new Run("Presenter inlines: "));
        presenter.Inlines.Add(new Bold { Inlines = { new Run("bold") } });
        presenter.Inlines.Add(new Run(", "));
        presenter.Inlines.Add(new Italic { Inlines = { new Run("italic") } });
        presenter.Inlines.Add(new Run(", "));
        presenter.Inlines.Add(new Underline { Inlines = { new Run("underline") } });

        var window = new Window
        {
            Width = 420,
            Height = 160,
            Background = Brushes.White,
            Content = new Border
            {
                Padding = new Thickness(16),
                Child = presenter
            }
        };

        window.Show();

        Assert.NotNull(window.CaptureRenderedFrame());
        Assert.True(presenter.DesiredSize.Width > 0);
    }

    [AvaloniaFact]
    public void Presenter_observes_nested_inline_changes()
    {
        var run = new Run("short");
        var presenter = new ProTextPresenterControl
        {
            FontSize = 18,
            LineHeight = 26,
            TextWrapping = TextWrapping.NoWrap,
            Foreground = Brushes.Black
        };
        presenter.Inlines!.Add(new Bold { Inlines = { run } });

        presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var shortWidth = presenter.DesiredSize.Width;

        run.Text = "a much longer nested inline run";
        presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Assert.True(presenter.DesiredSize.Width > shortWidth);
    }

    [AvaloniaFact]
    public void Presenter_renders_selection_and_caret()
    {
        var presenter = CreatePresenter("selectable presenter text");
        presenter.SelectionStart = 2;
        presenter.SelectionEnd = 12;
        presenter.SelectionBrush = Brushes.LightSkyBlue;
        presenter.ShowCaret();

        var window = new Window
        {
            Width = 360,
            Height = 120,
            Background = Brushes.White,
            Content = new Border
            {
                Padding = new Thickness(16),
                Child = presenter
            }
        };

        window.Show();

        Assert.NotNull(window.CaptureRenderedFrame());
    }

    private static ProTextPresenterControl CreatePresenter(string text)
    {
        return new ProTextPresenterControl
        {
            Text = text,
            FontSize = 18,
            LineHeight = 26,
            TextWrapping = TextWrapping.NoWrap,
            Foreground = Brushes.Black
        };
    }
}