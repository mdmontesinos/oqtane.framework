namespace Oqtane.Maui;

public partial class MainPage : ContentPage
{
    private HttpClient _httpClient;
    private string _apiUrl;

    public MainPage()
	{
		InitializeComponent();

        HandlerChanged += (s, e) =>
        {
            _httpClient = Handler.MauiContext.Services.GetRequiredService<HttpClient>();
            _apiUrl = Handler.MauiContext.Services.GetService<ConfiguredApiUrl>()?.Url ?? MauiConstants.ApiUrl;
        };
    }

    private static readonly byte[] _htmlPlaceholder = System.Text.Encoding.UTF8.GetBytes("<html><body>Asset Redirect</body></html>");

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".html" or ".htm" => "text/html",
        ".css" => "text/css",
        ".js" => "application/javascript",
        ".json" => "application/json",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".woff" => "font/woff",
        ".woff2" => "font/woff2",
        ".ttf" => "font/ttf",
        ".eot" => "application/vnd.ms-fontobject",
        ".ico" => "image/x-icon",
        ".webp" => "image/webp",
        _ => "application/octet-stream",
    };

    private void BlazorWebView_WebResourceRequested(object sender, WebViewWebResourceRequestedEventArgs e)
    {
        if (e.Uri.Host != "0.0.0.1") return;

        var path = e.Uri.AbsolutePath;

        if (!path.Contains('.') || path.Contains("_framework")) return;

        e.Handled = true;

        var redirectUrl = $"{_apiUrl.TrimEnd('/')}{e.Uri.PathAndQuery}";

#if ANDROID
        var contentType = GetContentType(Path.GetExtension(path));
        e.SetResponse(200, "OK", contentType, _httpClient.GetStreamAsync(redirectUrl));
#else
        // Create a minimal HTML body (some engines require non-empty content)
        using var stream = new MemoryStream(_htmlPlaceholder);

        var headers = new Dictionary<string, string>(e.Headers)
        {
            ["Location"] = redirectUrl
        };

        e.SetResponse(code: 302, reason: "Found", headers: headers, content: stream);
#endif
    }
}
