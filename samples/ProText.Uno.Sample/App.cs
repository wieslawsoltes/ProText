using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ProText.Uno.Sample;

public sealed partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Resources.MergedDictionaries.Add(new XamlControlsResources());

        _window = new Window
        {
            Title = "ProText.Uno.Sample",
            Content = new MainPage()
        };

        _window.Activate();
    }
}
