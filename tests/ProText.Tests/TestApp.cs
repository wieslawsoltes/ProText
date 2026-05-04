using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace ProText.Tests;

public sealed class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://ProText.Tests"))
        {
            Source = new Uri("avares://ProText/Themes/Fluent.axaml")
        });
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<TestApp>()
            .UseSkia()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false
            });
    }
}