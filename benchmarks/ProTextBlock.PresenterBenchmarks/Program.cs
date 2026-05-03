using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ProTextPresenterControl = ProTextBlock.ProTextPresenter;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

internal sealed class PresenterBenchmarkApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}

internal static class PresenterBenchmarkHost
{
    private static int s_started;

    public static void EnsureStarted()
    {
        if (Interlocked.Exchange(ref s_started, 1) == 1)
        {
            return;
        }

        AppBuilder.Configure<PresenterBenchmarkApp>()
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
public class PresenterLayoutBenchmarks
{
    private ProTextPresenterControl _presenter = null!;
    private ProTextPresenterControl _selectedPresenter = null!;
    private Window _window = null!;

    [Params(240, 480, 960)]
    public double Width { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        PresenterBenchmarkHost.EnsureStarted();
        _presenter = CreatePresenter();
        _selectedPresenter = CreatePresenter();
        _selectedPresenter.SelectionStart = 30;
        _selectedPresenter.SelectionEnd = 180;
        _selectedPresenter.SelectionBrush = Brushes.LightSkyBlue;
        _selectedPresenter.CaretIndex = 220;
        _selectedPresenter.ShowCaret();

        _window = new Window
        {
            Width = 720,
            Height = 320,
            Background = Brushes.White,
            Content = new Border
            {
                Padding = new Thickness(20),
                Child = _selectedPresenter
            }
        };
        _window.Show();
    }

    [Benchmark(Baseline = true)]
    public Size PresenterMeasure()
    {
        _presenter.InvalidateMeasure();
        _presenter.Measure(new Size(Width, double.PositiveInfinity));
        return _presenter.DesiredSize;
    }

    [Benchmark]
    public Rect PresenterCaretBounds()
    {
        _presenter.Measure(new Size(Width, double.PositiveInfinity));
        _presenter.Arrange(new Rect(_presenter.DesiredSize));
        return _presenter.GetCaretBounds(128);
    }

    [Benchmark]
    public int PresenterHitTest()
    {
        _presenter.Measure(new Size(Width, double.PositiveInfinity));
        _presenter.Arrange(new Rect(_presenter.DesiredSize));
        return _presenter.GetCharacterIndex(new Point(Width / 2, 36));
    }

    [Benchmark]
    public object PresenterSelectedFrame()
    {
        return _window.CaptureRenderedFrame()!;
    }

    private static ProTextPresenterControl CreatePresenter()
    {
        return new ProTextPresenterControl
        {
            Text = string.Join(' ', Enumerable.Repeat("Presenter text keeps selection, caret, and hit testing on the shared Pretext path.", 16)),
            FontSize = 16,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };
    }
}
