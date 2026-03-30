using Avalonia.Controls;
using Avalonia.Interactivity;
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

    private async void OnApplyFiltersClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.ApplyFiltersAsync();
        }
    }

    private async void OnClearFiltersClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.ClearFiltersAsync();
        }
    }

    private async void OnRegisterEntryClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.RegisterEntryAsync();
        }
    }

    private async void OnUpdateSelectedEntryClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.UpdateSelectedEntryAsync();
        }
    }

    private async void OnDeleteSelectedEntryClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.DeleteSelectedEntryAsync();
        }
    }

    private async void OnRegisterCustomerClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.RegisterCustomerAsync();
        }
    }

    private async void OnUpdateSelectedCustomerClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.UpdateSelectedCustomerAsync();
        }
    }

    private async void OnDeleteSelectedCustomerClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.DeleteSelectedCustomerAsync();
        }
    }
}
