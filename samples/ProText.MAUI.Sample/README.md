# ProText.MAUI.Sample

Minimal .NET MAUI sample for the `ProText.MAUI` implementation.

By default the project references `src/ProText.MAUI/ProText.MAUI.csproj` when that project exists, defines `PROTEXT_MAUI_AVAILABLE`, and activates the ProText columns through `ProTextBlock`, `ProTextPresenter`, `ProTextBox`, and `ProTextCache`.

On macOS the sample defaults to `net10.0-maccatalyst` so `dotnet run` launches a desktop app without requiring an Android device. On other hosts it defaults to `net10.0-android`.

Run it with:

```bash
dotnet run --project samples/ProText.MAUI.Sample/ProText.MAUI.Sample.csproj
```

Pass `-p:ProTextMauiSampleTargetFrameworks=net10.0-android` or another MAUI target framework when running against a specific platform toolchain.
