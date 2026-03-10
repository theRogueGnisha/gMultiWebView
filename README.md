# gMultiWebView

A WPF application that displays multiple browser panels in one window—ideal for 4K TVs or large monitors. One **Focus Frame** (large panel) plus several **Monitor** panels are arranged in a configurable grid. All browsing is controlled from a separate **Controller** window, so the display window stays clean and full-screen friendly.

**Tech:** C# .NET 10, WPF, Microsoft WebView2.

---

## Features

- **Two layout modes**
  - **6-panel:** Focus Frame (bottom-right, large) + 5 Monitor panels (top row + left column). Left column can be merged (split, top 2, bottom 2, or all 3).
  - **2×2:** Four equal panels; Focus Frame is bottom-right.
- **Controller window** – Single place to control everything: layout, URLs, Go, Home, and “Switch to Focus” per panel. Closing the Controller exits the app.
- **Display window** – Browser panels only (no per-panel toolbars). Resize and move the window; **F11** toggles fullscreen on the current monitor (covers taskbar).
- **Custom start page** (`start.html`) – Google search, YouTube, and Twitch links; used as the home page for every panel.
- **Dark theme** – App and start page use a dark color scheme; window title bars use Windows dark mode where supported.
- **WebView2** – Full Chromium/Edge engine: YouTube, Twitch, and normal web content work. Optional unpacked extensions via the `Extensions` folder.
- **App-wide DNS/proxy** – Optional `dns.config` for DoH (default: Comodo Secure DNS) and/or proxy for all panels.

---

## Requirements

- **Windows** (64-bit)
- **.NET 10.0 SDK** (to build)
- **Microsoft Edge WebView2 Runtime** (usually already installed with Edge; [download](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) if needed)

---

## Build and run

```bash
cd src
dotnet restore
dotnet run
```

Or open the solution in Visual Studio / Rider and run the **gMultiWebView** project.

On startup you get:
1. **Main window** – Display of browser panels (resize/move as needed; F11 for fullscreen).
2. **Controller window** – Opens beside it; use this for all URL and layout control.

---

## Controller window

- **Layout (top, centered)** – Choose **6-panel** or **2×2**. In 6-panel mode, choose how the left column is merged: Split (1+4+5), Top 2 (1+4), Bottom 2 (4+5), or All 3 (1+4+5).
- **Per-panel block** – Each visible panel has:
  - **Frame name** (e.g. Focus Frame, Monitor 1, Left (1+4))
  - **URL bar** (300px; press **Enter** to navigate)
  - **Go** – Navigate to the URL in the bar.
  - **Home** – Load the start page.
  - **Focus** – (Monitor panels only) Swap this panel’s page with the Focus Frame.
- Closing the Controller window (X) closes the entire application.

---

## Display window

- **F11** – Toggle fullscreen on the **current monitor** (hides taskbar, uses full monitor area). Press F11 again to restore.
- Panels have no individual toolbars; all input is via the Controller. Scroll and interact with pages as usual inside each panel.

---

## Configuration

### DNS and proxy

Copy `dns.config.example` to `dns.config` next to the executable and edit:

- **DoH** – DNS-over-HTTPS URL. Default: Comodo Secure DNS (`https://securedns.dnsbycomodo.com/dns-query`).
- **Proxy** – Optional proxy for all browser traffic, e.g. `Proxy=127.0.0.1:8080`.

If `dns.config` is missing, the app uses the default DoH endpoint only.

### Extensions

Place **unpacked** WebView2/Chromium extensions in the `Extensions` folder (each in its own subfolder with a `manifest.json`). They load at startup. See `Extensions/README.txt` for details.

---

## Project structure

| Path | Description |
|------|-------------|
| `MultiBrowserTV/MultiBrowserTV/` | Main WPF project (output: **gMultiWebView**) |
| `App.xaml(.cs)` | Startup, WebView2 environment, extensions loader |
| `MainWindow.xaml(.cs)` | Display window: 6-panel / 2×2 grid, fullscreen (F11), panel API for Controller |
| `UrlControlWindow.xaml(.cs)` | Controller window: layout selector, URL rows, Go/Home/Focus |
| `BrowserPanelControl.xaml(.cs)` | Single browser panel (WebView2, no toolbar) |
| `LayoutSelectorControl.xaml(.cs)` | Graphical layout and left-column merge selector |
| `DarkTitleBar.cs` | Dark title bar for windows |
| `AppDnsConfig.cs` | Reads `dns.config` (DoH, proxy) |
| `start.html` | Start page (Google, YouTube, Twitch) |
| `dns.config.example` | Example DNS/proxy config |
| `Extensions/README.txt` | How to add unpacked extensions |
| `icon.png` | App icon (optional; remove from `.csproj` if not used) |

---

## Layout overview

**6-panel (default)**  
- Top: Monitor 1, Monitor 2, Monitor 3.  
- Bottom-right: **Focus Frame** (large).  
- Left column: Monitor 4, Monitor 5 (or merged as chosen in the Controller).

**2×2**  
- Four equal quadrants; Focus Frame is the bottom-right quadrant.

---

## License

Use and modify as you like. No warranty.
