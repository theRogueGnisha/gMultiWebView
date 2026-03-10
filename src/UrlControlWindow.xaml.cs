using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace gMultiWebView;

public partial class UrlControlWindow : Window
{
    private readonly MainWindow _main;
    private readonly Dictionary<int, TextBox> _panelUrlBoxes = new();

    public UrlControlWindow(MainWindow main)
    {
        _main = main;
        InitializeComponent();
        DarkTitleBar.Apply(this);
        MainWindow.SetWindowIcon(this);
        _main.LayoutChanged += (_, _) => RefreshAll();
        _main.PanelUrlChanged += OnPanelUrlChanged;
        Loaded += (_, _) => RefreshAll();
        Closed += (_, _) => Application.Current.Shutdown();
    }

    private void RefreshAll()
    {
        RefreshLayoutSection();
        RefreshSlotsGrid();
    }

    private void OnPanelUrlChanged(object? sender, (int panelIndex, string url) e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_panelUrlBoxes.TryGetValue(e.panelIndex, out var tb) && tb.Text != e.url)
                tb.Text = e.url;
        });
    }

    private void RefreshLayoutSection()
    {
        var selector = new LayoutSelectorControl
        {
            LayoutMode = _main.GetLayoutMode(),
            LeftColumnMergeMode = _main.GetLeftColumnMergeMode()
        };
        selector.LayoutModeSelected += (_, mode) => _main.SetLayoutMode(mode);
        selector.MergeModeSelected += (_, mode) => _main.SetLeftColumnMerge(mode);
        LayoutSection.Child = selector;
    }

    private void RefreshSlotsGrid()
    {
        _panelUrlBoxes.Clear();
        SlotsGrid.Children.Clear();
        SlotsGrid.RowDefinitions.Clear();
        SlotsGrid.ColumnDefinitions.Clear();

        var cells = _main.GetLayoutGridCells();
        if (cells.Length == 0) return;

        int rowCount = cells.Max(c => c.Row + c.RowSpan);
        int colCount = cells.Max(c => c.Col + c.ColSpan);

        for (int r = 0; r < rowCount; r++)
            SlotsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int c = 0; c < colCount; c++)
            SlotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        foreach (var cell in cells)
        {
            var url = _main.GetPanelCurrentUrl(cell.PanelIndex);
            var panelIndex = cell.PanelIndex;
            var tb = new TextBox
            {
                Text = url,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 300,
                MaxWidth = 300,
                MinWidth = 100
            };
            tb.KeyDown += (s, e) =>
            {
                if (e.Key != Key.Enter) return;
                e.Handled = true;
                if (s is TextBox box)
                    _main.NavigateToPanel(panelIndex, box.Text);
            };
            _panelUrlBoxes[cell.PanelIndex] = tb;

            var go = new Button { Content = "Go", Tag = (panelIndex, tb), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
            go.Click += (s, _) =>
            {
                if (s is Button btn && btn.Tag is (int idx, TextBox box))
                    _main.NavigateToPanel(idx, box.Text);
            };

            var home = new Button
            {
                Content = "🏠",
                Tag = panelIndex,
                ToolTip = "Home (start page)",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            home.Click += (s, _) =>
            {
                if (s is Button btn && btn.Tag is int idx)
                    _main.GoHome(idx);
            };

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            };
            buttonRow.Children.Add(go);
            buttonRow.Children.Add(home);
            if (cell.PanelIndex != 0)
            {
                var swap = new Button
                {
                    Content = "Focus",
                    Tag = panelIndex,
                    ToolTip = "Switch to Focus",
                    VerticalAlignment = VerticalAlignment.Center
                };
                swap.Click += (s, _) =>
                {
                    if (s is Button b && b.Tag is int idx)
                        _main.RequestSwitchToFocus(idx);
                };
                buttonRow.Children.Add(swap);
            }

            var titleBlock = new TextBlock
            {
                Text = cell.Label,
                Foreground = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xcc)),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var inner = new StackPanel();
            inner.Children.Add(titleBlock);
            inner.Children.Add(tb);
            inner.Children.Add(buttonRow);

            var cellContent = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x26)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(2)
            };
            cellContent.Child = inner;

            Grid.SetRow(cellContent, cell.Row);
            Grid.SetColumn(cellContent, cell.Col);
            Grid.SetRowSpan(cellContent, cell.RowSpan);
            Grid.SetColumnSpan(cellContent, cell.ColSpan);
            SlotsGrid.Children.Add(cellContent);
        }
    }
}
