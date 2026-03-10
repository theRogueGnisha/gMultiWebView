using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace gMultiWebView;

public enum LeftColumnMergeMode
{
    None,
    TopTwo,      // Merge Monitor 1 + 4 into one tall panel
    BottomTwo,   // Merge Monitor 4 + 5 into one tall panel
    AllThree,    // Merge Monitor 1 + 4 + 5 into one tall panel
}

public enum MainLayoutMode
{
    SixPanel,   // Focus + 5 monitors (current 3x3 layout)
    Grid2x2,    // 4 equal panels
}

public partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    private readonly BrowserPanelControl[] _panels;
    private readonly ContentControl[] _slots;
    private bool _isFullscreen;
    private WindowState _savedWindowState;
    private WindowStyle _savedWindowStyle;
    private bool _savedTopmost;
    private double _savedTop, _savedLeft, _savedWidth, _savedHeight;

    private LeftColumnMergeMode _leftMergeMode = LeftColumnMergeMode.None;
    private MainLayoutMode _mainLayoutMode = MainLayoutMode.SixPanel;

    public MainWindow()
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        SetWindowIcon(this);
        _panels = new BrowserPanelControl[6];
        _slots = new[] { SlotFocus, Slot1, Slot2, Slot3, Slot4, Slot5 };

        for (int i = 0; i < 6; i++)
        {
            var panel = new BrowserPanelControl();
            _panels[i] = panel;
            panel.SetPanelLabel(i == 0, i == 0 ? 0 : i);
            var panelIndex = i;
            panel.AddressChanged += (_, url) => PanelUrlChanged?.Invoke(this, (panelIndex, url));
        }

        SlotFocus.Content = _panels[0];
        Slot1.Content = _panels[1];
        Slot2.Content = _panels[2];
        Slot3.Content = _panels[3];
        Slot4.Content = _panels[4];
        Slot5.Content = _panels[5];

        ContentRendered += OnMainWindowContentRendered;
    }

    internal static void SetWindowIcon(Window window)
    {
        try
        {
            window.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/gMultiWebView;component/icon.png", UriKind.Absolute));
        }
        catch { /* icon optional */ }
    }

    private void OnMainWindowContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= OnMainWindowContentRendered;
        var urlControl = new UrlControlWindow(this) { Owner = this };
        urlControl.Left = Left + Width + 10;
        urlControl.Top = Top;
        urlControl.Show();
    }

    /// <summary>Raised when layout mode or left-column merge changes so the URL control window can refresh.</summary>
    public event EventHandler? LayoutChanged;

    /// <summary>Raised when a panel's address changes so the URL control window can update its textbox.</summary>
    public event EventHandler<(int panelIndex, string url)>? PanelUrlChanged;

    /// <summary>One cell in the controller grid (mirrors display layout).</summary>
    public record LayoutCell(int Row, int Col, int RowSpan, int ColSpan, int PanelIndex, string Label);

    /// <summary>Returns grid cells for the controller so its layout matches the display. Row/col/span match the visual grid.</summary>
    public LayoutCell[] GetLayoutGridCells()
    {
        if (_mainLayoutMode == MainLayoutMode.Grid2x2)
            return new[]
            {
                new LayoutCell(0, 0, 1, 1, 1, "Top-Left"),
                new LayoutCell(0, 1, 1, 1, 2, "Top-Right"),
                new LayoutCell(1, 0, 1, 1, 3, "Bottom-Left"),
                new LayoutCell(1, 1, 1, 1, 0, "Bottom-Right")
            };
        return _leftMergeMode switch
        {
            LeftColumnMergeMode.None => new[]
            {
                new LayoutCell(0, 0, 1, 1, 1, "Monitor 1"),
                new LayoutCell(0, 1, 1, 1, 2, "Monitor 2"),
                new LayoutCell(0, 2, 1, 1, 3, "Monitor 3"),
                new LayoutCell(1, 0, 1, 1, 4, "Monitor 4"),
                new LayoutCell(1, 1, 2, 2, 0, "Focus Frame"),
                new LayoutCell(2, 0, 1, 1, 5, "Monitor 5")
            },
            LeftColumnMergeMode.TopTwo => new[]
            {
                new LayoutCell(0, 0, 2, 1, 1, "Left (1+4)"),
                new LayoutCell(0, 1, 1, 1, 2, "Monitor 2"),
                new LayoutCell(0, 2, 1, 1, 3, "Monitor 3"),
                new LayoutCell(1, 1, 2, 2, 0, "Focus Frame"),
                new LayoutCell(2, 0, 1, 1, 5, "Monitor 5")
            },
            LeftColumnMergeMode.BottomTwo => new[]
            {
                new LayoutCell(0, 0, 1, 1, 1, "Monitor 1"),
                new LayoutCell(0, 1, 1, 1, 2, "Monitor 2"),
                new LayoutCell(0, 2, 1, 1, 3, "Monitor 3"),
                new LayoutCell(1, 0, 2, 1, 4, "Left (4+5)"),
                new LayoutCell(1, 1, 2, 2, 0, "Focus Frame")
            },
            _ => new[] // AllThree
            {
                new LayoutCell(0, 0, 3, 1, 1, "Left (1+4+5)"),
                new LayoutCell(0, 1, 1, 1, 2, "Monitor 2"),
                new LayoutCell(0, 2, 1, 1, 3, "Monitor 3"),
                new LayoutCell(1, 1, 2, 2, 0, "Focus Frame")
            }
        };
    }

    /// <summary>Returns (panel index, display label) for each visible slot in the current layout. Merged left column counts as one slot.</summary>
    public (int PanelIndex, string Label)[] GetLayoutSlots()
    {
        if (_mainLayoutMode == MainLayoutMode.Grid2x2)
            return new[] { (1, "Top-Left"), (2, "Top-Right"), (3, "Bottom-Left"), (0, "Bottom-Right") };
        return _leftMergeMode switch
        {
            LeftColumnMergeMode.None => new[] { (0, "Focus Frame"), (1, "Monitor 1"), (2, "Monitor 2"), (3, "Monitor 3"), (4, "Monitor 4"), (5, "Monitor 5") },
            LeftColumnMergeMode.TopTwo => new[] { (0, "Focus Frame"), (1, "Left (1+4)"), (2, "Monitor 2"), (3, "Monitor 3"), (5, "Monitor 5") },
            LeftColumnMergeMode.BottomTwo => new[] { (0, "Focus Frame"), (1, "Monitor 1"), (2, "Monitor 2"), (3, "Monitor 3"), (4, "Left (4+5)") },
            LeftColumnMergeMode.AllThree => new[] { (0, "Focus Frame"), (1, "Left (1+4+5)"), (2, "Monitor 2"), (3, "Monitor 3") },
            _ => new[] { (0, "Focus Frame"), (1, "Monitor 1"), (2, "Monitor 2"), (3, "Monitor 3"), (4, "Monitor 4"), (5, "Monitor 5") }
        };
    }

    public string GetPanelCurrentUrl(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= _panels.Length) return "";
        return _panels[panelIndex].GetCurrentAddress();
    }

    public void NavigateToPanel(int panelIndex, string url)
    {
        if (panelIndex < 0 || panelIndex >= _panels.Length) return;
        _panels[panelIndex].NavigateTo(url?.Trim() ?? "");
    }

    public void GoHome(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= _panels.Length) return;
        _panels[panelIndex].LoadStartPage();
    }

    public MainLayoutMode GetLayoutMode() => _mainLayoutMode;
    public LeftColumnMergeMode GetLeftColumnMergeMode() => _leftMergeMode;

    public void SetLayoutMode(MainLayoutMode mode)
    {
        if (_mainLayoutMode == mode) return;
        _mainLayoutMode = mode;
        if (mode == MainLayoutMode.Grid2x2)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            Grid2x2.Visibility = Visibility.Visible;
            Slot2x2_0.Content = _panels[1];
            Slot2x2_1.Content = _panels[2];
            Slot2x2_2.Content = _panels[3];
            Slot2x2_3.Content = _panels[0];
            SlotFocus.Content = null;
            Slot1.Content = null;
            Slot2.Content = null;
            Slot3.Content = null;
            Slot4.Content = null;
            Slot5.Content = null;
        }
        else
        {
            Grid2x2.Visibility = Visibility.Collapsed;
            Slot2x2_0.Content = null;
            Slot2x2_1.Content = null;
            Slot2x2_2.Content = null;
            Slot2x2_3.Content = null;
            MainGrid.Visibility = Visibility.Visible;
            SlotFocus.Content = _panels[0];
            Slot1.Content = _panels[1];
            Slot2.Content = _panels[2];
            Slot3.Content = _panels[3];
            Slot4.Content = _panels[4];
            Slot5.Content = _panels[5];
            ApplyLeftColumnMerge(_leftMergeMode);
        }
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetLeftColumnMerge(LeftColumnMergeMode mode)
    {
        ApplyLeftColumnMerge(mode);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RequestSwitchToFocus(int monitorPanelIndex)
    {
        if (monitorPanelIndex < 1 || monitorPanelIndex > 5) return;
        var focusPanel = _panels[0];
        var monitorPanel = _panels[monitorPanelIndex];
        var focusUrl = focusPanel.GetCurrentAddress();
        var monitorUrl = monitorPanel.GetCurrentAddress();
        focusPanel.NavigateTo(monitorUrl);
        monitorPanel.NavigateTo(focusUrl);
    }

    private void ApplyLeftColumnMerge(LeftColumnMergeMode mode)
    {
        if (_leftMergeMode == mode) return;
        _leftMergeMode = mode;

        // Reset layout first. Never change row heights – Focus frame (rows 1–2) must keep 2/3 height.
        Grid.SetRowSpan(Slot1, 1);
        Grid.SetRowSpan(Slot4, 1);
        Slot1.Visibility = Visibility.Visible;
        Slot4.Visibility = Visibility.Visible;
        Slot5.Visibility = Visibility.Visible;

        switch (mode)
        {
            case LeftColumnMergeMode.None:
                Slot1.Content = _panels[1];
                Slot4.Content = _panels[4];
                Slot5.Content = _panels[5];
                break;
            case LeftColumnMergeMode.TopTwo:
                Slot4.Visibility = Visibility.Collapsed;
                Slot4.Content = null;
                Grid.SetRowSpan(Slot1, 2);
                Slot1.Content = _panels[1];
                Slot5.Content = _panels[5]; // keep bottom panel when switching from BottomTwo
                break;
            case LeftColumnMergeMode.BottomTwo:
                Slot5.Visibility = Visibility.Collapsed;
                Slot5.Content = null;
                Grid.SetRowSpan(Slot4, 2);
                Slot4.Content = _panels[4];
                break;
            case LeftColumnMergeMode.AllThree:
                Slot4.Visibility = Visibility.Collapsed;
                Slot5.Visibility = Visibility.Collapsed;
                Slot4.Content = null;
                Slot5.Content = null;
                Grid.SetRowSpan(Slot1, 3);
                Slot1.Content = _panels[1];
                break;
        }
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            e.Handled = true;
            ToggleFullscreen();
        }
    }

    private void ToggleFullscreen()
    {
        _isFullscreen = !_isFullscreen;
        if (_isFullscreen)
        {
            _savedWindowState = WindowState;
            _savedWindowStyle = WindowStyle;
            _savedTopmost = Topmost;
            _savedTop = Top;
            _savedLeft = Left;
            _savedWidth = Width;
            _savedHeight = Height;
            var hwnd = new WindowInteropHelper(this).Handle;
            var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref mi))
            {
                var r = mi.rcMonitor;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;
                Left = r.Left;
                Top = r.Top;
                Width = r.Right - r.Left;
                Height = r.Bottom - r.Top;
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;
                Left = _savedLeft;
                Top = _savedTop;
                Width = _savedWidth;
                Height = _savedHeight;
                WindowState = WindowState.Normal;
            }
        }
        else
        {
            Topmost = _savedTopmost;
            WindowStyle = _savedWindowStyle;
            WindowState = _savedWindowState;
            Top = _savedTop;
            Left = _savedLeft;
            Width = _savedWidth;
            Height = _savedHeight;
            ResizeMode = ResizeMode.CanResizeWithGrip;
        }
    }
}
