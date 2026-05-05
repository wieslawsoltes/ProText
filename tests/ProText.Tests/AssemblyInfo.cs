using Avalonia.Headless;
using ProText.Tests;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(TestApp))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
