using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Pretext;
using ProTextBlockCacheApi = ProTextBlock.ProTextBlockCache;
using ProTextBlockControl = ProTextBlock.ProTextBlock;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

internal sealed class BenchmarkApp : Application
{
	public override void Initialize()
	{
		Styles.Add(new FluentTheme());
	}
}

internal static class AvaloniaBenchmarkHost
{
	private static int s_started;

	public static void EnsureStarted()
	{
		if (Interlocked.Exchange(ref s_started, 1) == 1)
		{
			return;
		}

		AppBuilder.Configure<BenchmarkApp>()
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
public class TextLayoutBenchmarks
{
	private const string Font = "400 16px \"Inter\"";
	private readonly string _text = string.Join(' ', Enumerable.Repeat(
		"Text layout pressure comes from repeated measurement, width probes, and redraws across dense interfaces.",
		18));

	private TextBlock _avaloniaTextBlock = null!;
	private TextBlock _avaloniaRichTextBlock = null!;
	private ProTextBlockControl _proTextBlock = null!;
	private ProTextBlockControl _proRichTextBlock = null!;
	private ProTextBlockControl _proTextBlockLocalCache = null!;
	private PreparedTextWithSegments _prepared = null!;

	[Params(160, 320, 640)]
	public double Width { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		AvaloniaBenchmarkHost.EnsureStarted();
		ProTextBlockCacheApi.Clear();

		_avaloniaTextBlock = new TextBlock
		{
			Text = _text,
			FontSize = 16,
			LineHeight = 22,
			TextWrapping = TextWrapping.Wrap,
			Foreground = Brushes.Black
		};

		_proTextBlock = new ProTextBlockControl
		{
			Text = _text,
			FontSize = 16,
			LineHeight = 22,
			TextWrapping = TextWrapping.Wrap,
			Foreground = Brushes.Black
		};

		_proTextBlockLocalCache = new ProTextBlockControl
		{
			Text = _text,
			FontSize = 16,
			LineHeight = 22,
			TextWrapping = TextWrapping.Wrap,
			Foreground = Brushes.Black,
			UseGlobalCache = false
		};

		_avaloniaRichTextBlock = CreateAvaloniaRichTextBlock();
		_proRichTextBlock = CreateProRichTextBlock();

		_prepared = PretextLayout.PrepareWithSegments(_text, Font);
		_proTextBlock.Measure(new Size(Width, double.PositiveInfinity));
	}

	[Benchmark(Baseline = true)]
	public Size AvaloniaTextBlockMeasure()
	{
		_avaloniaTextBlock.InvalidateMeasure();
		_avaloniaTextBlock.Measure(new Size(Width, double.PositiveInfinity));
		return _avaloniaTextBlock.DesiredSize;
	}

	[Benchmark]
	public Size ProTextBlockGlobalCacheMeasure()
	{
		_proTextBlock.InvalidateMeasure();
		_proTextBlock.Measure(new Size(Width, double.PositiveInfinity));
		return _proTextBlock.DesiredSize;
	}

	[Benchmark]
	public Size ProTextBlockLocalCacheMeasure()
	{
		_proTextBlockLocalCache.InvalidateMeasure();
		_proTextBlockLocalCache.Measure(new Size(Width, double.PositiveInfinity));
		return _proTextBlockLocalCache.DesiredSize;
	}

	[Benchmark]
	public Size AvaloniaRichTextBlockMeasure()
	{
		_avaloniaRichTextBlock.InvalidateMeasure();
		_avaloniaRichTextBlock.Measure(new Size(Width, double.PositiveInfinity));
		return _avaloniaRichTextBlock.DesiredSize;
	}

	[Benchmark]
	public Size ProTextBlockRichMeasure()
	{
		_proRichTextBlock.InvalidateMeasure();
		_proRichTextBlock.Measure(new Size(Width, double.PositiveInfinity));
		return _proRichTextBlock.DesiredSize;
	}

	[Benchmark]
	public PreparedTextWithSegments PretextColdPrepare()
	{
		ProTextBlockCacheApi.Clear();
		return PretextLayout.PrepareWithSegments(_text, Font);
	}

	[Benchmark]
	public LineStats PretextMeasureLineStats()
	{
		return PretextLayout.MeasureLineStats(_prepared, Width);
	}

	private static TextBlock CreateAvaloniaRichTextBlock()
	{
		var textBlock = new TextBlock
		{
			FontSize = 16,
			LineHeight = 22,
			TextWrapping = TextWrapping.Wrap,
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextDecorations = TextDecorations.Underline,
			FontFeatures = FontFeatureCollection.Parse("kern, liga"),
			LetterSpacing = 1,
			Foreground = CreateGradient()
		};

		textBlock.Inlines!.Add(new Run("Rich inline content mixes "));
		textBlock.Inlines.Add(new Bold { Inlines = { new Run("bold") } });
		textBlock.Inlines.Add(new Run(", "));
		textBlock.Inlines.Add(new Italic { Inlines = { new Run("italic") } });
		textBlock.Inlines.Add(new Run(", trimming, decoration, font features, and non-solid foreground brushes."));
		return textBlock;
	}

	private static ProTextBlockControl CreateProRichTextBlock()
	{
		var textBlock = new ProTextBlockControl
		{
			FontSize = 16,
			LineHeight = 22,
			TextWrapping = TextWrapping.Wrap,
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextDecorations = TextDecorations.Underline,
			FontFeatures = FontFeatureCollection.Parse("kern, liga"),
			LetterSpacing = 1,
			Foreground = CreateGradient()
		};

		textBlock.Inlines!.Add(new Run("Rich inline content mixes "));
		textBlock.Inlines.Add(new Bold { Inlines = { new Run("bold") } });
		textBlock.Inlines.Add(new Run(", "));
		textBlock.Inlines.Add(new Italic { Inlines = { new Run("italic") } });
		textBlock.Inlines.Add(new Run(", trimming, decoration, font features, and non-solid foreground brushes."));
		return textBlock;
	}

	private static IBrush CreateGradient()
	{
		return new LinearGradientBrush
		{
			StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
			EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
			GradientStops =
			{
				new GradientStop(Colors.MediumVioletRed, 0),
				new GradientStop(Colors.DeepSkyBlue, 1)
			}
		};
	}
}

[MemoryDiagnoser]
public class HeadlessRenderBenchmarks
{
	private readonly string _text = string.Join(' ', Enumerable.Repeat(
		"Headless rendering captures the actual Avalonia frame after layout and draw operations complete.",
		12));

	private Window _avaloniaWindow = null!;
	private Window _proWindow = null!;

	[GlobalSetup]
	public void Setup()
	{
		AvaloniaBenchmarkHost.EnsureStarted();

		_avaloniaWindow = CreateWindow(new TextBlock
		{
			Text = _text,
			TextWrapping = TextWrapping.Wrap,
			FontSize = 18,
			LineHeight = 25,
			Foreground = Brushes.Black
		});

		_proWindow = CreateWindow(new ProTextBlockControl
		{
			Text = _text,
			TextWrapping = TextWrapping.Wrap,
			FontSize = 18,
			LineHeight = 25,
			Foreground = Brushes.Black
		});

		_avaloniaWindow.Show();
		_proWindow.Show();
	}

	[Benchmark(Baseline = true)]
	public object AvaloniaTextBlockFrame()
	{
		return _avaloniaWindow.CaptureRenderedFrame()!;
	}

	[Benchmark]
	public object ProTextBlockFrame()
	{
		return _proWindow.CaptureRenderedFrame()!;
	}

	private static Window CreateWindow(Control content)
	{
		return new Window
		{
			Width = 720,
			Height = 420,
			Background = Brushes.White,
			Content = new Border
			{
				Padding = new Thickness(24),
				Child = content
			}
		};
	}
}
