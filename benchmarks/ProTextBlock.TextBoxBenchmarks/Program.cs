using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ProTextBoxControl = ProTextBlock.ProTextBox;
using ProTextPresenterControl = ProTextBlock.ProTextPresenter;

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
    private Window _avaloniaTextBoxWindow = null!;
    private Window _proTextBoxWindow = null!;
    private Window _avaloniaSelectedTextBoxWindow = null!;
    private Window _proSelectedTextBoxWindow = null!;
    private int _measureTick;

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

        _avaloniaTextBoxWindow = ShowBenchmarkedTextBox(_avaloniaTextBox, expectProPresenter: false);
        _proTextBoxWindow = ShowBenchmarkedTextBox(_proTextBox, expectProPresenter: true);
        _avaloniaSelectedTextBoxWindow = ShowBenchmarkedTextBox(_avaloniaSelectedTextBox, expectProPresenter: false);
        _proSelectedTextBoxWindow = ShowBenchmarkedTextBox(_proSelectedTextBox, expectProPresenter: true);
    }

    [Benchmark(Baseline = true)]
    public Size AvaloniaTextBoxMeasure()
    {
        return MeasureThemedTextBox(_avaloniaTextBox);
    }

    [Benchmark]
    public Size ProTextBoxMeasure()
    {
        return MeasureThemedTextBox(_proTextBox);
    }

    [Benchmark]
    public Size AvaloniaTextBoxSelectedMeasure()
    {
        return MeasureThemedTextBox(_avaloniaSelectedTextBox);
    }

    [Benchmark]
    public Size ProTextBoxSelectedMeasure()
    {
        return MeasureThemedTextBox(_proSelectedTextBox);
    }

    private Size MeasureThemedTextBox(Control textBox)
    {
        var width = Width + ((++_measureTick & 1) == 0 ? 0 : 0.5);

        textBox.InvalidateMeasure();
        textBox.Measure(new Size(width, double.PositiveInfinity));
        textBox.Arrange(new Rect(0, 0, width, textBox.DesiredSize.Height));

        return textBox.DesiredSize;
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

    private static Window ShowBenchmarkedTextBox(Control textBox, bool expectProPresenter)
    {
        var window = new Window
        {
            Width = 960,
            Height = 360,
            Background = Brushes.White,
            Content = new Border
            {
                Padding = new Thickness(20),
                Child = textBox
            }
        };

        window.Show();
        textBox.ApplyTemplate();

        ValidateThemeApplied(window, expectProPresenter);

        return window;
    }

    private static void ValidateThemeApplied(Window window, bool expectProPresenter)
    {
        var hasPresenter = expectProPresenter
            ? window.GetVisualDescendants().Any(visual => visual is ProTextPresenterControl)
            : window.GetVisualDescendants().Any(visual => visual is TextPresenter);

        if (!hasPresenter)
        {
            throw new InvalidOperationException(expectProPresenter
                ? "ProTextBox benchmark did not apply the ProText Fluent theme: ProTextPresenter was not found."
                : "Avalonia TextBox benchmark did not apply the Fluent TextBox theme: TextPresenter was not found.");
        }

        _ = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("The benchmark window did not render a headless frame.");
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
    private TextBox _avaloniaTextBox = null!;
    private ProTextBoxControl _proTextBox = null!;
    private TextPresenter _avaloniaPresenter = null!;
    private ProTextPresenterControl _proPresenter = null!;
    private Window _emptyWindow = null!;
    private Window _directProWindow = null!;
    private ProTextPresenterControl _directProPresenter = null!;

    [GlobalSetup]
    public void Setup()
    {
        TextBoxBenchmarkHost.EnsureStarted();
        _avaloniaTextBox = CreateAvaloniaTextBox();
        _proTextBox = CreateProTextBox();
        _directProPresenter = CreateProTextPresenter();
        _emptyWindow = CreateWindow(new Border { Background = Brushes.White });
        _avaloniaWindow = CreateWindow(_avaloniaTextBox);
        _proWindow = CreateWindow(_proTextBox);
        _directProWindow = CreateWindow(_directProPresenter);
        _emptyWindow.Show();
        _avaloniaWindow.Show();
        _proWindow.Show();
        _directProWindow.Show();
        _avaloniaTextBox.ApplyTemplate();
        _proTextBox.ApplyTemplate();
        _avaloniaPresenter = FindPresenter<TextPresenter>(_avaloniaWindow,
            "Avalonia TextBox benchmark did not apply the Fluent TextBox theme: TextPresenter was not found.");
        _proPresenter = FindPresenter<ProTextPresenterControl>(_proWindow,
            "ProTextBox benchmark did not apply the ProText Fluent theme: ProTextPresenter was not found.");
        _ = CaptureFrame(_emptyWindow);
        _ = CaptureFrame(_avaloniaWindow);
        _ = CaptureFrame(_proWindow);
        _ = CaptureFrame(_directProWindow);
    }

    [Benchmark]
    public object EmptyWindowFrame()
    {
        return CaptureFrame(_emptyWindow);
    }

    [Benchmark]
    public object AvaloniaTextBoxCaptureOnly()
    {
        return CaptureFrame(_avaloniaWindow);
    }

    [Benchmark]
    public object ProTextBoxCaptureOnly()
    {
        return CaptureFrame(_proWindow);
    }

    [Benchmark(Baseline = true)]
    public object AvaloniaTextBoxFrame()
    {
        _avaloniaPresenter.InvalidateVisual();
        return CaptureFrame(_avaloniaWindow);
    }

    [Benchmark]
    public object ProTextBoxFrame()
    {
        _proPresenter.InvalidateVisual();
        return CaptureFrame(_proWindow);
    }

    [Benchmark]
    public object DirectProTextPresenterFrame()
    {
        _directProPresenter.InvalidateVisual();
        return CaptureFrame(_directProWindow);
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

    private ProTextPresenterControl CreateProTextPresenter()
    {
        return new ProTextPresenterControl
        {
            Text = _text,
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

    private static T FindPresenter<T>(Window window, string message)
        where T : Control
    {
        return window.GetVisualDescendants().OfType<T>().FirstOrDefault()
            ?? throw new InvalidOperationException(message);
    }

    private static object CaptureFrame(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return window.GetLastRenderedFrame()
            ?? throw new InvalidOperationException("The benchmark window did not render a headless frame.");
    }
}
