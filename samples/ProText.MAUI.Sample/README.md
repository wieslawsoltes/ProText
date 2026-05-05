# ProText.MAUI.Sample

Minimal .NET MAUI sample for the `ProText.MAUI` implementation.

By default the project references `src/ProText.MAUI/ProText.MAUI.csproj` when that project exists, defines `PROTEXT_MAUI_AVAILABLE`, and activates the ProText columns through `ProTextBlock`, `ProTextPresenter`, `ProTextBox`, and `ProTextCache`.

The default target is `net10.0-android` so solution builds can validate the sample on a broadly available MAUI workload. Pass `-p:ProTextMauiSampleTargetFrameworks=net10.0-maccatalyst` or another MAUI target framework when a matching platform toolchain is available.
