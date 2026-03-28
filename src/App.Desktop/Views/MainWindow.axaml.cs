using Avalonia.Controls;
using SMEFinanceSuite.App.Desktop.ViewModels;

namespace SMEFinanceSuite.App.Desktop.Views;

public partial class MainWindow : Window
{
    // Required by Avalonia runtime loader/design-time tooling.
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
