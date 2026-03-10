using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace gMultiWebView;

public partial class App : Application
{
    /// <summary>Shared WebView2 environment (with optional proxy). All panels use this.</summary>
    public static CoreWebView2Environment? WebView2Environment { get; private set; }

    /// <summary>Folder next to the executable where you can drop unpacked extensions (each in its own subfolder with manifest.json).</summary>
    public static string ExtensionsFolderPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Extensions");

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        AppDnsConfig.Load();

        Directory.CreateDirectory(ExtensionsFolderPath);

        var options = new CoreWebView2EnvironmentOptions
        {
            AreBrowserExtensionsEnabled = true
        };
        if (!string.IsNullOrWhiteSpace(AppDnsConfig.ProxyServer))
            options.AdditionalBrowserArguments = "--proxy-server=" + AppDnsConfig.ProxyServer.Trim();

        var userData = Path.Combine(appDir, "WebView2Cache");
        WebView2Environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userData, options: options);

        var main = new MainWindow();
        main.Show();
    }

    private static bool _extensionsLoaded;

    /// <summary>Loads unpacked extensions from the Extensions folder into the profile. Call once when the first CoreWebView2 is ready.</summary>
    public static async void LoadExtensionsFromFolderOnce(CoreWebView2Profile profile)
    {
        if (_extensionsLoaded || profile == null) return;
        _extensionsLoaded = true;
        var folder = ExtensionsFolderPath;
        if (!Directory.Exists(folder)) return;
        try
        {
            foreach (var subDir in Directory.EnumerateDirectories(folder))
            {
                var manifest = Path.Combine(subDir, "manifest.json");
                if (!File.Exists(manifest)) continue;
                try
                {
                    await profile.AddBrowserExtensionAsync(subDir);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Extension load failed for {subDir}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Extensions scan failed: {ex.Message}");
        }
    }
}
