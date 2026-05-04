using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using ProTextBlockControl = ProText.ProTextBlock;

namespace ProText.Tests;

public sealed class AvaloniaAdaptedTextBlockTests
{
    [AvaloniaFact]
    public void Calling_Measure_With_Infinite_Space_Should_Set_DesiredSize()
    {
        var textBlock = new ProTextBlockControl { Text = "Hello World" };

        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Assert.True(textBlock.DesiredSize.Width > 0);
        Assert.True(textBlock.DesiredSize.Height > 0);
    }

    [AvaloniaFact]
    public void Should_Measure_MinTextWidth_With_DetectFromContent()
    {
        var textBlock = new ProTextBlockControl
        {
            Text = "Hello\nשלום\nReally really really really long line",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.DetectFromContent,
            TextWrapping = TextWrapping.Wrap
        };

        textBlock.Measure(new Size(1920, 1080));

        Assert.True(textBlock.DesiredSize.Width > 0);
        Assert.True(textBlock.DesiredSize.Width <= 1920);
        Assert.True(textBlock.DesiredSize.Height > 0);
    }

    [AvaloniaFact]
    public void Calling_Arrange_With_Different_Size_Should_Update_Bounds()
    {
        var textBlock = new ProTextBlockControl { Text = "Hello World", TextWrapping = TextWrapping.Wrap };

        textBlock.Measure(Size.Infinity);
        var desired = textBlock.DesiredSize;

        textBlock.Arrange(new Rect(desired));
        Assert.Equal(new Rect(default, desired), textBlock.Bounds);

        var wider = desired + new Size(50, 0);
        textBlock.Arrange(new Rect(wider));

        Assert.Equal(new Rect(default, wider), textBlock.Bounds);
    }

    [AvaloniaFact]
    public void Changing_InlinesCollection_Should_Invalidate_Measure()
    {
        var target = new ProTextBlockControl();

        target.Measure(Size.Infinity);

        Assert.True(target.IsMeasureValid);

        target.Inlines!.Add(new Run("Hello"));

        Assert.False(target.IsMeasureValid);

        target.Measure(Size.Infinity);

        Assert.True(target.IsMeasureValid);
    }

    [AvaloniaFact]
    public void Changing_Inlines_Properties_Should_Invalidate_Measure()
    {
        var target = new ProTextBlockControl();
        var inline = new Run("Hello");

        target.Inlines!.Add(inline);
        target.Measure(Size.Infinity);

        Assert.True(target.IsMeasureValid);

        inline.Foreground = Brushes.Green;

        Assert.False(target.IsMeasureValid);
    }

    [AvaloniaFact]
    public void Changing_Inlines_Should_Invalidate_Measure()
    {
        var target = new ProTextBlockControl();
        var inlines = new InlineCollection { new Run("Hello") };

        target.Measure(Size.Infinity);

        Assert.True(target.IsMeasureValid);

        target.Inlines = inlines;

        Assert.False(target.IsMeasureValid);
    }

    [AvaloniaFact]
    public void Setting_Text_Should_Reset_Inlines()
    {
        var target = new ProTextBlockControl();

        target.Inlines!.Add(new Run("Hello World"));

        Assert.Null(target.Text);
        Assert.Single(target.Inlines);

        target.Text = "1234";

        Assert.Equal("1234", target.Text);
        Assert.Empty(target.Inlines);
    }

    [AvaloniaFact]
    public void TextBlock_With_Infinite_Size_Should_Be_Remeasured_After_Text_Created()
    {
        var target = new ProTextBlockControl { Text = string.Empty };

        target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.Equal(default, target.DesiredSize);

        target.Text = "foo";
        target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Assert.True(target.DesiredSize.Width > 0);
        Assert.True(target.DesiredSize.Height > 0);
    }

    [AvaloniaFact]
    public void Measure_And_Arrange_Should_Use_Trailing_Whitespace_For_Bounds()
    {
        var target = new ProTextBlockControl
        {
            Text = "fy  ",
            FontStyle = FontStyle.Italic,
            FontSize = 48,
            UseLayoutRounding = false,
            Padding = new Thickness(3, 2, 5, 4)
        };

        target.Measure(Size.Infinity);
        target.Arrange(new Rect(default, target.DesiredSize));

        Assert.True(target.DesiredSize.Width > 0);
        Assert.True(target.DesiredSize.Height > 0);
        Assert.Equal(new Rect(default, target.DesiredSize), target.Bounds);
    }

    [AvaloniaFact]
    public void Should_Render_TextDecorations_Headlessly()
    {
        var target = new Border
        {
            Padding = new Thickness(8),
            Width = 220,
            Height = 48,
            Background = Brushes.White,
            Child = new ProTextBlockControl
            {
                FontSize = 14,
                Foreground = Brushes.Black,
                Text = "Neque porro quisquam est qui dolorem",
                TextWrapping = TextWrapping.NoWrap,
                TextDecorations = new TextDecorationCollection
                {
                    new TextDecoration
                    {
                        Location = TextDecorationLocation.Overline,
                        StrokeThickness = 1.5,
                        StrokeThicknessUnit = TextDecorationUnit.Pixel,
                        Stroke = Brushes.Red
                    },
                    new TextDecoration
                    {
                        Location = TextDecorationLocation.Baseline,
                        StrokeThickness = 1.5,
                        StrokeThicknessUnit = TextDecorationUnit.Pixel,
                        Stroke = Brushes.Green
                    },
                    new TextDecoration
                    {
                        Location = TextDecorationLocation.Underline,
                        StrokeThickness = 1.5,
                        StrokeThicknessUnit = TextDecorationUnit.Pixel,
                        Stroke = Brushes.Blue,
                        StrokeOffset = 2,
                        StrokeOffsetUnit = TextDecorationUnit.Pixel
                    }
                }
            }
        };

        var window = CreateWindow(target, 240, 72);
        window.Show();

        Assert.NotNull(window.CaptureRenderedFrame());
    }

    [AvaloniaFact]
    public void Should_Measure_Arrange_TextBlock_Alignment_Cases()
    {
        var target = new StackPanel { Width = 200, Height = 240 };
        var alignments = new[] { TextAlignment.Left, TextAlignment.Center, TextAlignment.Right };
        var horizontalAlignments = new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right };

        foreach (var horizontalAlignment in horizontalAlignments)
        {
            foreach (var textAlignment in alignments)
            {
                target.Children.Add(new ProTextBlockControl
                {
                    Text = "Hello World",
                    Background = Brushes.Red,
                    HorizontalAlignment = horizontalAlignment,
                    TextAlignment = textAlignment,
                    Width = 150,
                    TextWrapping = TextWrapping.NoWrap
                });
            }
        }

        var window = CreateWindow(target, 240, 280);
        window.Show();

        Assert.NotNull(window.CaptureRenderedFrame());
    }

    private static Window CreateWindow(Control content, double width, double height)
    {
        return new Window
        {
            Width = width,
            Height = height,
            Background = Brushes.White,
            Content = content
        };
    }
}
