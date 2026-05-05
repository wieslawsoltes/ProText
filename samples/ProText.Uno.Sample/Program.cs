using Microsoft.UI.Xaml;

namespace ProText.Uno.Sample;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Application.Start(_ => new App());
    }
}
