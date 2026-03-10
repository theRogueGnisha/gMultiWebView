using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace gMultiWebView;

public partial class BrowserPanelControl : IDisposable
{
    /// <summary>Fired when the browser's address changes (so Controller can stay in sync).</summary>
    public event EventHandler<string>? AddressChanged;

    private WebView2? _webView2;

    public int PanelIndex { get; set; }
    public bool IsFocusPanel { get; set; }

    public string StartPagePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "start.html");

    public BrowserPanelControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_webView2 != null) return;
        _webView2 = new WebView2 { Focusable = true };
        BrowserHost.Child = _webView2;

        var env = App.WebView2Environment;
        await _webView2.EnsureCoreWebView2Async(env);

        var core = _webView2.CoreWebView2;
        if (core == null) return;

        App.LoadExtensionsFromFolderOnce(core.Profile);

        core.NavigationCompleted += (_, _) => UpdateTitleAndUrl();

        LoadStartPage();
    }

    private void UpdateTitleAndUrl()
    {
        if (_webView2?.CoreWebView2 == null) return;
        var url = _webView2.CoreWebView2.Source ?? "";
        SafeUpdateUi(() => AddressChanged?.Invoke(this, url));
    }

    private void SafeUpdateUi(Action action)
    {
        if (Dispatcher.CheckAccess())
            action();
        else
            Dispatcher.Invoke(action);
    }

    private void BrowserHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _webView2?.Focus();
    }

    private void BrowserHost_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_webView2 != null && !_webView2.IsKeyboardFocusWithin)
            _webView2.Focus();
    }

    public void LoadStartPage()
    {
        if (_webView2?.CoreWebView2 == null) return;
        var path = StartPagePath;
        if (File.Exists(path))
        {
            var fileUri = "file:///" + Path.GetFullPath(path).Replace("\\", "/");
            _webView2.CoreWebView2.Navigate(fileUri);
        }
        else
            _webView2.CoreWebView2.Navigate("about:blank");
    }

    public void SetPanelLabel(bool isFocus, int monitorIndex)
    {
        IsFocusPanel = isFocus;
        PanelIndex = isFocus ? 0 : monitorIndex;
    }

    public string GetCurrentAddress()
    {
        return _webView2?.CoreWebView2?.Source ?? "";
    }

    public void NavigateTo(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            LoadStartPage();
            return;
        }
        var normalized = NormalizeNavigateUrl(url.Trim());
        _webView2?.CoreWebView2?.Navigate(normalized);
    }

    /// <summary>Ensures the URL has a scheme so WebView2.Navigate doesn't crash (e.g. "yahoo.com" -> "https://yahoo.com").</summary>
    private static string NormalizeNavigateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        var u = url.Trim();
        if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            return u;
        if (u.Contains(" ") || (!u.Contains(".") && !u.Contains(":")))
            return "https://www.google.com/search?q=" + Uri.EscapeDataString(u);
        return "https://" + u;
    }

    public void Dispose() => _webView2?.Dispose();
}
