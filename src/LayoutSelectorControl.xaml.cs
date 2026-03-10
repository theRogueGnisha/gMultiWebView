using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace gMultiWebView;

public partial class LayoutSelectorControl : UserControl
{
    public static readonly DependencyProperty LayoutModeProperty = DependencyProperty.Register(
        nameof(LayoutMode), typeof(MainLayoutMode), typeof(LayoutSelectorControl),
        new PropertyMetadata(MainLayoutMode.SixPanel, (d, _) => ((LayoutSelectorControl)d).UpdateLayoutModeVisual()));

    public static readonly DependencyProperty LeftColumnMergeModeProperty = DependencyProperty.Register(
        nameof(LeftColumnMergeMode), typeof(LeftColumnMergeMode), typeof(LayoutSelectorControl),
        new PropertyMetadata(LeftColumnMergeMode.None, (d, _) => ((LayoutSelectorControl)d).UpdateMergeSelectionVisual()));

    public MainLayoutMode LayoutMode
    {
        get => (MainLayoutMode)GetValue(LayoutModeProperty);
        set => SetValue(LayoutModeProperty, value);
    }

    public LeftColumnMergeMode LeftColumnMergeMode
    {
        get => (LeftColumnMergeMode)GetValue(LeftColumnMergeModeProperty);
        set => SetValue(LeftColumnMergeModeProperty, value);
    }

    public event EventHandler<MainLayoutMode>? LayoutModeSelected;
    public event EventHandler<LeftColumnMergeMode>? MergeModeSelected;

    private static readonly Brush UnselectedMergeBrush = new SolidColorBrush(Color.FromRgb(45, 45, 48));
    private static readonly Brush SelectedMergeBrush = new SolidColorBrush(Color.FromRgb(14, 99, 156));

    public LayoutSelectorControl()
    {
        InitializeComponent();
        UpdateLayoutModeVisual();
        UpdateMergeSelectionVisual();
    }

    private void BtnSixPanel_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        LayoutModeSelected?.Invoke(this, MainLayoutMode.SixPanel);
    }

    private void Btn2x2_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        LayoutModeSelected?.Invoke(this, MainLayoutMode.Grid2x2);
    }

    private void MergeOption_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is not Border border) return;
        var mode = border == MergeSplit ? LeftColumnMergeMode.None
            : border == MergeTopTwo ? LeftColumnMergeMode.TopTwo
            : border == MergeBottomTwo ? LeftColumnMergeMode.BottomTwo
            : LeftColumnMergeMode.AllThree;
        MergeModeSelected?.Invoke(this, mode);
    }

    private void UpdateLayoutModeVisual()
    {
        var sixBrush = LayoutMode == MainLayoutMode.SixPanel ? new SolidColorBrush(Color.FromRgb(14, 99, 156)) : new SolidColorBrush(Color.FromRgb(45, 45, 48));
        var twoBrush = LayoutMode == MainLayoutMode.Grid2x2 ? new SolidColorBrush(Color.FromRgb(14, 99, 156)) : new SolidColorBrush(Color.FromRgb(45, 45, 48));
        BtnSixPanel.Background = sixBrush;
        Btn2x2.Background = twoBrush;
        MergeOptionsPanel.Visibility = LayoutMode == MainLayoutMode.SixPanel ? Visibility.Visible : Visibility.Collapsed;
        LeftColLabel.Visibility = LayoutMode == MainLayoutMode.SixPanel ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateMergeSelectionVisual()
    {
        MergeSplit.Background = LeftColumnMergeMode == LeftColumnMergeMode.None ? SelectedMergeBrush : UnselectedMergeBrush;
        MergeTopTwo.Background = LeftColumnMergeMode == LeftColumnMergeMode.TopTwo ? SelectedMergeBrush : UnselectedMergeBrush;
        MergeBottomTwo.Background = LeftColumnMergeMode == LeftColumnMergeMode.BottomTwo ? SelectedMergeBrush : UnselectedMergeBrush;
        MergeAllThree.Background = LeftColumnMergeMode == LeftColumnMergeMode.AllThree ? SelectedMergeBrush : UnselectedMergeBrush;
    }
}
