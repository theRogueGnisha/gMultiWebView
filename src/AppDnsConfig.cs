using System.IO;

namespace gMultiWebView;

/// <summary>
/// App-wide DNS/proxy config read from dns.config next to the executable.
/// Applies to all browser panels. Default: Comodo Secure DNS (DoH).
/// </summary>
public static class AppDnsConfig
{
    public const string ConfigFileName = "dns.config";

    /// <summary>Comodo Secure DNS (8.26.56.26 / 8.20.247.20) – security, ad blocking, malware/phishing filtering. DoH endpoint.</summary>
    public const string DefaultDoHTemplate = "https://securedns.dnsbycomodo.com/dns-query";

    public static string? DoHTemplate { get; private set; }
    public static string? ProxyServer { get; private set; }

    public static void Load()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        if (File.Exists(path))
            LoadFromFile(path);
        else
            DoHTemplate = DefaultDoHTemplate;
    }

    private static void LoadFromFile(string path)
    {

        foreach (var line in File.ReadAllLines(path))
        {
            var s = line.Trim();
            if (s.Length == 0 || s.StartsWith("#"))
                continue;
            var eq = s.IndexOf('=');
            if (eq <= 0)
                continue;
            var key = s[..eq].Trim();
            var value = s[(eq + 1)..].Trim();
            if (key.Equals("DoH", StringComparison.OrdinalIgnoreCase))
                DoHTemplate = string.IsNullOrWhiteSpace(value) ? DefaultDoHTemplate : value;
            else if (key.Equals("Proxy", StringComparison.OrdinalIgnoreCase))
                ProxyServer = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        if (DoHTemplate == null)
            DoHTemplate = DefaultDoHTemplate;
    }
}
