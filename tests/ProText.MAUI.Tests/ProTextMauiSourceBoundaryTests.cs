namespace ProText.MAUI.Tests;

public class ProTextMauiSourceBoundaryTests
{
    [Fact]
    public void MauiImplementationDoesNotUseFrameworkTextFallback()
    {
        var root = ProTextMauiTestAssembly.FindRepositoryRoot();
        var mauiSourceRoot = Path.Combine(root, "src", "ProText.MAUI");

        Assert.True(Directory.Exists(mauiSourceRoot), "Expected src/ProText.MAUI to exist.");

        var sourceFiles = Directory.GetFiles(mauiSourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.EndsWith("Properties/AssemblyInfo.cs", StringComparison.Ordinal))
            .ToArray();
        var source = string.Join('\n', sourceFiles.Select(File.ReadAllText));

        Assert.DoesNotContain("new Label", source, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(Label)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Maui.Controls.Label", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new Entry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(Entry)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Maui.Controls.Entry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new Editor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(Editor)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Maui.Controls.Editor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new TextBlock", source, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(TextBlock)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.UI.Xaml.Controls.TextBlock", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new TextPresenter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(TextPresenter)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.UI.Xaml.Controls.TextPresenter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Avalonia", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.UI.Xaml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Uno.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MauiImplementationDoesNotUseMutableBrushDefaults()
    {
        var root = ProTextMauiTestAssembly.FindRepositoryRoot();
        var mauiSourceRoot = Path.Combine(root, "src", "ProText.MAUI");

        Assert.True(Directory.Exists(mauiSourceRoot), "Expected src/ProText.MAUI to exist.");

        var source = string.Join('\n', Directory.GetFiles(mauiSourceRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.DoesNotContain("defaultValue: new SolidColorBrush", source, StringComparison.Ordinal);
        Assert.DoesNotContain("defaultValue: new LinearGradientBrush", source, StringComparison.Ordinal);
        Assert.DoesNotContain("defaultValue: new RadialGradientBrush", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MauiPublicControlsDoNotExposeSkiaCanvasAsBaseType()
    {
        AssertNotSkiaCanvasBase(ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextBlock"));
        AssertNotSkiaCanvasBase(ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextPresenter"));
        AssertNotSkiaCanvasBase(ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextBox"));
    }

    [Fact]
    public void CoreProjectStaysFrameworkNeutral()
    {
        var root = ProTextMauiTestAssembly.FindRepositoryRoot();
        var source = string.Join('\n', Directory.GetFiles(Path.Combine(root, "src", "ProText.Core"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.DoesNotContain("Avalonia", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.UI.Xaml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Uno.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Maui", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SkiaSharp.Views.Maui", source, StringComparison.Ordinal);
    }

    private static void AssertNotSkiaCanvasBase(Type controlType)
    {
        var baseTypeName = controlType.BaseType?.FullName;

        Assert.NotEqual("SkiaSharp.Views.Maui.Controls.SKCanvasView", baseTypeName);
        Assert.NotEqual("SkiaSharp.Views.Maui.Controls.SKGLView", baseTypeName);
    }
}
