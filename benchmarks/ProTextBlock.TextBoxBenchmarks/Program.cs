using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ProTextBoxControl = ProTextBlock.ProTextBox;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

internal sealed class TextBoxBenchmarkApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://ProTextBlock.TextBoxBenchmarks"))
        {
            Source = new Uri("avares://ProTextBlock/Themes/Fluent.axaml")
        });
    }
}

internal static class TextBoxBenchmarkHost
{
    private static int s_started;

    public static void EnsureStarted()
    {
        if (Interlocked.Exchange(ref s_started, 1) == 1)
        {
            return;
        }

        AppBuilder.Configure<TextBoxBenchmarkApp>()
            .UseSkia()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false
            })
            .SetupWithoutStarting();
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class TextBoxLayoutBenchmarks
{
    private readonly string _text = string.Join(' ', Enumerable.Repeat(
        "Editable dense surfaces measure text boxes repeatedly while caret, selection, wrapping, and placeholder state stay synchronized.",
        18));

    private TextBox _avaloniaTextBox = null!;
    private ProTextBoxControl _proTextBox = null!;
    private TextBox _avaloniaSelectedTextBox = null!;
    private ProTextBoxControl _proSelectedTextBox = null!;

    [Params(220, 440, 880)]
    public double Width { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        TextBoxBenchmarkHost.EnsureStarted();

        _avaloniaTextBox = CreateAvaloniaTextBox(selection: false);
        _proTextBox = CreateProTextBox(selection: false);
        _avaloniaSelectedTextBox = CreateAvaloniaTextBox(selection: true);
        _proSelectedTextBox = CreateProTextBox(selection: true);
    }

    [Benchmark(Baseline = true)]
    public Size AvaloniaTextBoxMeasure()
    {
        _avaloniaTextBox.InvalidateMeasure();
        _avaloniaTextBox.Measure(new Size(Width, double.PositiveInfinity));
        return _avaloniaTextBox.DesiredSize;
    }

    [Benchmark]
    public Size ProTextBoxMeasure()
    {
        _proTextBox.InvalidateMeasure();
        _proTextBox.Measure(new Size(Width, double.PositiveInfinity));
        return _proTextBox.DesiredSize;
    }

    [Benchmark]
    public Size AvaloniaTextBoxSelectedMeasure()
    {
        _avaloniaSelectedTextBox.InvalidateMeasure();
        _avaloniaSelectedTextBox.Measure(new Size(Width, double.PositiveInfinity));
        return _avaloniaSelectedTextBox.DesiredSize;
    }

    [Benchmark]
    public Size ProTextBoxSelectedMeasure()
    {
        _proSelectedTextBox.InvalidateMeasure();
        _proSelectedTextBox.Measure(new Size(Width, double.PositiveInfinity));
        return _proSelectedTextBox.DesiredSize;
    }

    private TextBox CreateAvaloniaTextBox(bool selection)
    {
        var textBox = new TextBox
        {
            Text = _text,
            AcceptsReturn = true,
            FontSize = 16,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black,
            SelectionBrush = Brushes.LightSkyBlue,
            SelectionForegroundBrush = Brushes.White,
            CaretBrush = Brushes.Black
        };

        if (selection)
        {
            textBox.SelectionStart = 32;
            textBox.SelectionEnd = 220;
            textBox.CaretIndex = 220;
        }

        return textBox;
    }

    private ProTextBoxControl CreateProTextBox(bool selection)
    {
        var textBox = new ProTextBoxControl
        {
            Text = _text,
            AcceptsReturn = true,
            FontSize = 16,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black,
            SelectionBrush = Brushes.LightSkyBlue,
            SelectionForegroundBrush = Brushes.White,
            CaretBrush = Brushes.Black
        };

        if (selection)
        {
            textBox.SelectionStart = 32;
            textBox.SelectionEnd = 220;
            textBox.CaretIndex = 220;
        }

        return textBox;
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class TextBoxRenderBenchmarks
{
    private readonly string _text = string.Join(' ', Enumerable.Repeat(
        "Headless TextBox rendering compares Avalonia TextPresenter with ProTextPresenter inside a copied Fluent TextBox template.",
        10));

    private Window _avaloniaWindow = null!;
    private Window _proWindow = null!;

    [GlobalSetup]
    public void Setup()
    {
        TextBoxBenchmarkHost.EnsureStarted();
        _avaloniaWindow = CreateWindow(CreateAvaloniaTextBox());
        _proWindow = CreateWindow(CreateProTextBox());
        _avaloniaWindow.Show();
        _proWindow.Show();
    }

    [Benchmark(Baseline = true)]
    public object AvaloniaTextBoxFrame()
    {
        return _avaloniaWindow.CaptureRenderedFrame()!;
    }

    [Benchmark]
    public object ProTextBoxFrame()
    {
        return _proWindow.CaptureRenderedFrame()!;
    }

    private TextBox CreateAvaloniaTextBox()
    {
        return new TextBox
        {
            Text = _text,
            AcceptsReturn = true,
            FontSize = 18,
            LineHeight = 26,
            TextWrapping = TextWrapping.Wrap,
            SelectionStart = 24,
            SelectionEnd = 180,
            CaretIndex = 180,
            Foreground = Brushes.Black,
            SelectionBrush = Brushes.LightSkyBlue,
            SelectionForegroundBrush = Brushes.White,
            CaretBrush = Brushes.Black
        };
    }

    private ProTextBoxControl CreateProTextBox()
    {
        return new ProTextBoxControl
        {
            Text = _text,
            AcceptsReturn = true,
            FontSize = 18,
            LineHeight = 26,
            TextWrapping = TextWrapping.Wrap,
            SelectionStart = 24,
            SelectionEnd = 180,
            CaretIndex = 180,
            Foreground = Brushes.Black,
            SelectionBrush = Brushes.LightSkyBlue,
            SelectionForegroundBrush = Brushes.White,
            CaretBrush = Brushes.Black
        };
    }

    private static Window CreateWindow(Control content)
    {
        return new Window
        {
            Width = 720,
            Height = 260,
            Background = Brushes.White,
            Content = new Border
            {
                Padding = new Thickness(20),
                Child = content
            }
        };
    }
}
