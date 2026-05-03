using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ProTextBlockControl = ProTextBlock.ProTextBlock;
using ProTextPresenterControl = ProTextBlock.ProTextPresenter;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

internal sealed class InlineBenchmarkApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}

internal static class InlineBenchmarkHost
{
    private static int s_started;

    public static void EnsureStarted()
    {
        if (Interlocked.Exchange(ref s_started, 1) == 1)
        {
            return;
        }

        AppBuilder.Configure<InlineBenchmarkApp>()
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
public class InlineLayoutBenchmarks
{
    private TextBlock _avaloniaInlineTextBlock = null!;
    private ProTextBlockControl _proInlineTextBlock = null!;
    private ProTextPresenterControl _proInlinePresenter = null!;

    [Params(180, 360, 720)]
    public double Width { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        InlineBenchmarkHost.EnsureStarted();
        _avaloniaInlineTextBlock = CreateAvaloniaInlineTextBlock();
        _proInlineTextBlock = CreateProInlineTextBlock();
        _proInlinePresenter = CreateProInlinePresenter();
    }

    [Benchmark(Baseline = true)]
    public Size AvaloniaTextBlockInlineMeasure()
    {
        _avaloniaInlineTextBlock.InvalidateMeasure();
        _avaloniaInlineTextBlock.Measure(new Size(Width, double.PositiveInfinity));
        return _avaloniaInlineTextBlock.DesiredSize;
    }

    [Benchmark]
    public Size ProTextBlockInlineMeasure()
    {
        _proInlineTextBlock.InvalidateMeasure();
        _proInlineTextBlock.Measure(new Size(Width, double.PositiveInfinity));
        return _proInlineTextBlock.DesiredSize;
    }

    [Benchmark]
    public Size ProTextPresenterInlineMeasure()
    {
        _proInlinePresenter.InvalidateMeasure();
        _proInlinePresenter.Measure(new Size(Width, double.PositiveInfinity));
        return _proInlinePresenter.DesiredSize;
    }

    private static TextBlock CreateAvaloniaInlineTextBlock()
    {
        var textBlock = new TextBlock
        {
            FontSize = 16,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        FillInlines(textBlock.Inlines!);
        return textBlock;
    }

    private static ProTextBlockControl CreateProInlineTextBlock()
    {
        var textBlock = new ProTextBlockControl
        {
            FontSize = 16,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        FillInlines(textBlock.Inlines!);
        return textBlock;
    }

    private static ProTextPresenterControl CreateProInlinePresenter()
    {
        var presenter = new ProTextPresenterControl
        {
            FontSize = 16,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        FillInlines(presenter.Inlines!);
        return presenter;
    }

    private static void FillInlines(InlineCollection inlines)
    {
        for (var i = 0; i < 24; i++)
        {
            inlines.Add(new Run($"Inline segment {i:00} "));
            inlines.Add(new Bold { Inlines = { new Run("bold ") } });
            inlines.Add(new Italic { Inlines = { new Run("italic ") } });
            inlines.Add(new Underline { Inlines = { new Run("underline ") } });
        }
    }
}
