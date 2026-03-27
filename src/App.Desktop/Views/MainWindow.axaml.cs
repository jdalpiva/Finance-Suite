using Avalonia.Controls;
using SMEFinanceSuite.App.Desktop.ViewModels;

namespace SMEFinanceSuite.App.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
