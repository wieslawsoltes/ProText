using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ProTextPresenterControl = ProText.Avalonia.ProTextPresenter;

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
[ShortRunJob]
public class PresenterLayoutBenchmarks
{
    private ProTextPresenterControl _presenter = null!;
    private ProTextPresenterControl _selectedPresenter = null!;
    private ProTextPresenterControl _plainFramePresenter = null!;
    private Window _selectedWindow = null!;
    private Window _plainFrameWindow = null!;
    private Window _emptyWindow = null!;

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
        _plainFramePresenter = CreatePresenter();

        _selectedWindow = CreateWindow(_selectedPresenter);
        _plainFrameWindow = CreateWindow(_plainFramePresenter);
        _emptyWindow = CreateWindow(new Border { Background = Brushes.White });
        _selectedWindow.Show();
        _plainFrameWindow.Show();
        _emptyWindow.Show();

        _ = CaptureFrame(_selectedWindow);
        _ = CaptureFrame(_plainFrameWindow);
        _ = CaptureFrame(_emptyWindow);
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
    public object EmptyWindowFrame()
    {
        return CaptureFrame(_emptyWindow);
    }

    [Benchmark]
    public object PresenterPlainFrame()
    {
        _plainFramePresenter.InvalidateVisual();
        return CaptureFrame(_plainFrameWindow);
    }

    [Benchmark]
    public object PresenterSelectedFrame()
    {
        _selectedPresenter.InvalidateVisual();
        return CaptureFrame(_selectedWindow);
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

    private static Window CreateWindow(Control content)
    {
        return new Window
        {
            Width = 720,
            Height = 320,
            Background = Brushes.White,
            Content = new Border
            {
                Padding = new Thickness(20),
                Child = content
            }
        };
    }

    private static object CaptureFrame(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return window.GetLastRenderedFrame()
            ?? throw new InvalidOperationException("The benchmark window did not render a headless frame.");
    }
}
