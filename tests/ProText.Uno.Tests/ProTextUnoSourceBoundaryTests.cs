namespace ProText.Uno.Tests;

public class ProTextUnoSourceBoundaryTests
{
    [Fact]
    public void UnoImplementationDoesNotUseFrameworkTextFallback()
    {
        var root = FindRepositoryRoot();
        var sourceFiles = Directory.GetFiles(Path.Combine(root, "src", "ProText.Uno"), "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.EndsWith("Properties/AssemblyInfo.cs", StringComparison.Ordinal))
            .ToArray();
        var source = string.Join('\n', sourceFiles.Select(File.ReadAllText));

        Assert.DoesNotContain("new TextBlock", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.UI.Xaml.Controls.TextBlock", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Avalonia", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PropertyMetadata(new SolidColorBrush", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UnoPublicControlsDoNotExposeSkiaCanvasAsBaseType()
    {
        Assert.NotEqual("Uno.WinUI.Graphics2DSK.SKCanvasElement", typeof(ProTextBlock).BaseType?.FullName);
        Assert.NotEqual("Uno.WinUI.Graphics2DSK.SKCanvasElement", typeof(ProTextPresenter).BaseType?.FullName);
        Assert.NotEqual("Uno.WinUI.Graphics2DSK.SKCanvasElement", typeof(ProTextBox).BaseType?.FullName);
    }

    [Fact]
    public void CoreProjectStaysFrameworkNeutral()
    {
        var root = FindRepositoryRoot();
        var source = string.Join('\n', Directory.GetFiles(Path.Combine(root, "src", "ProText.Core"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.DoesNotContain("Avalonia", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.UI.Xaml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Uno.", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ProText.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root.");
    }
}
