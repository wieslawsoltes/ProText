using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using ProTextBoxControl = ProText.Avalonia.ProTextBox;
using ProTextPresenterControl = ProText.Avalonia.ProTextPresenter;

namespace ProText.Tests;

public sealed class ProTextBoxTests
{
    [AvaloniaFact]
    public void ProTextBox_theme_uses_pro_text_presenter()
    {
        var textBox = new ProTextBoxControl
        {
            Text = "The copied Fluent TextBox theme hosts ProTextPresenter.",
            FontSize = 16,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            SelectionStart = 11,
            SelectionEnd = 29,
            Foreground = Brushes.Black,
            SelectionBrush = Brushes.LightSkyBlue,
            SelectionForegroundBrush = Brushes.White
        };

        var window = new Window
        {
            Width = 420,
            Height = 140,
            Background = Brushes.White,
            Content = new Border
            {
                Padding = new Thickness(16),
                Child = textBox
            }
        };

        window.Show();

        Assert.NotNull(window.CaptureRenderedFrame());
        Assert.Contains(window.GetVisualDescendants(), visual => visual is ProTextPresenterControl);
    }
}
