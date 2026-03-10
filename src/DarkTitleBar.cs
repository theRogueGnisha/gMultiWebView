using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace gMultiWebView;

/// <summary>
/// Applies dark mode to the window title bar (Windows 10 1809+ / 11).
/// </summary>
internal static class DarkTitleBar
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void Apply(Window window)
    {
        if (window == null) return;
        void TryApply()
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            int useDark = 1;
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDark, sizeof(int));
        }

        if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
            TryApply();
        else
            window.SourceInitialized += (_, _) => TryApply();
    }
}
