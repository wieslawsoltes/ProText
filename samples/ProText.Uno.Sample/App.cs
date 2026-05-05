using Microsoft.UI.Xaml;

namespace ProText.Uno.Sample;

public sealed class App : Application
{
    private Window? _window;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new Window
        {
            Content = new MainPage()
        };

        _window.Activate();
    }
}
