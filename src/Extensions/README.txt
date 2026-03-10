WebView2 extensions folder for gMultiWebView
============================================

Drop unpacked browser extensions here. Each extension must be in its own subfolder that contains a manifest.json file.

Structure:
  Extensions\
    uBlockOrigin\          <- one extension
      manifest.json
      ...
    SomeOtherExtension\    <- another extension
      manifest.json
      ...

- Use unpacked (developer) format: a folder with manifest.json, not a .crx or .zip.
- You can get unpacked extensions from the Chrome Web Store (e.g. "Download" / "Load unpacked" in developer mode) or build from source.
- Extensions load when the app starts. Add or remove folders, then restart the app.

WebView2 uses the same Chromium/Edge engine as Microsoft Edge, so most Chromium extensions are compatible.
