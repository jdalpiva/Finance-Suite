using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SMEFinanceSuite.App.Desktop.ViewModels;

namespace SMEFinanceSuite.App.Desktop.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType CsvFileType = new("Arquivo CSV")
    {
        Patterns = ["*.csv"],
        MimeTypes = ["text/csv"]
    };

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

    private async void OnApplyReportFiltersClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.ApplyReportFiltersAsync();
        }
    }

    private async void OnClearReportFiltersClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.ClearReportFiltersAsync();
        }
    }

    private async void OnExportReportCsvClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        try
        {
            FinancialReportCsvExport export = viewModel.CreateReportCsvExport();

            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Exportar relatório financeiro (CSV)",
                SuggestedFileName = export.SuggestedFileName,
                DefaultExtension = "csv",
                ShowOverwritePrompt = true,
                FileTypeChoices = [CsvFileType]
            });

            if (file is null)
            {
                viewModel.NotifyReportExportCancelled();
                return;
            }

            await using Stream stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            await writer.WriteAsync(export.Content);

            viewModel.NotifyReportExportSucceeded(file.Name);
        }
        catch (Exception exception)
        {
            viewModel.NotifyReportExportFailed(exception);
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

    private async void OnRegisterProductServiceClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.RegisterProductServiceAsync();
        }
    }

    private async void OnUpdateSelectedProductServiceClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.UpdateSelectedProductServiceAsync();
        }
    }

    private async void OnDeleteSelectedProductServiceClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.DeleteSelectedProductServiceAsync();
        }
    }
}
