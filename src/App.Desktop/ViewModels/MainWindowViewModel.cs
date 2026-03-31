using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Dashboard;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IFinancialDashboardService _financialDashboardService;

    private DashboardSummaryDto _summary = DashboardSummaryDto.Empty;
    private string _statusMessage = "Preparando dashboard e módulos...";
    private bool _isBusy;

    public MainWindowViewModel(
        IFinancialDashboardService financialDashboardService,
        IFinancialReportsService financialReportsService,
        IFinancialEntryService financialEntryService,
        ICustomerService customerService,
        IProductCatalogService productCatalogService)
    {
        _financialDashboardService = financialDashboardService;

        ReportsModule = new ReportsModuleViewModel(financialReportsService);
        FinancialEntriesModule = new FinancialEntriesModuleViewModel(financialEntryService);
        CustomersModule = new CustomersModuleViewModel(customerService);
        ProductCatalogModule = new ProductCatalogModuleViewModel(productCatalogService);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ReportsModuleViewModel ReportsModule { get; }

    public FinancialEntriesModuleViewModel FinancialEntriesModule { get; }

    public CustomersModuleViewModel CustomersModule { get; }

    public ProductCatalogModuleViewModel ProductCatalogModule { get; }

    public string TotalRevenueDisplay => _summary.TotalRevenue.ToString("C", PortugueseCulture);

    public string TotalExpenseDisplay => _summary.TotalExpense.ToString("C", PortugueseCulture);

    public string NetCashFlowDisplay => _summary.NetCashFlow.ToString("C", PortugueseCulture);

    public string RegistrySummary => $"{_summary.CustomersCount} clientes • {_summary.ProductsCount} itens";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            ReportsModule.SetBusy(value);
            FinancialEntriesModule.SetBusy(value);
            CustomersModule.SetBusy(value);
            ProductCatalogModule.SetBusy(value);
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await LoadDashboardAsync(cancellationToken);
            await ReportsModule.LoadAsync(cancellationToken);
            await FinancialEntriesModule.LoadAsync(cancellationToken);
            await CustomersModule.LoadAsync(cancellationToken);
            await ProductCatalogModule.LoadAsync(cancellationToken);
            SyncFinancialEntriesFormReferences();

            StatusMessage = $"Dashboard e módulos carregados em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao carregar dados iniciais: {exception.Message}";
        }
    }

    public async Task ApplyFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await FinancialEntriesModule.ApplyFiltersAsync(cancellationToken);
            StatusMessage = $"Filtros aplicados em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao aplicar filtros: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task ApplyReportFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await ReportsModule.ApplyFiltersAsync(cancellationToken);
            StatusMessage = $"Relatório atualizado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao atualizar relatório: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await FinancialEntriesModule.ClearFiltersAsync(cancellationToken);
            StatusMessage = $"Filtros limpos em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao limpar filtros: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task ClearReportFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await ReportsModule.ClearFiltersAsync(cancellationToken);
            StatusMessage = $"Filtros de relatório limpos em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao limpar filtros de relatório: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task RegisterEntryAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await FinancialEntriesModule.RegisterAsync(cancellationToken);
            await LoadDashboardAsync(cancellationToken);
            await ReportsModule.LoadAsync(cancellationToken);

            StatusMessage = $"Lançamento cadastrado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao cadastrar lançamento: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task UpdateSelectedEntryAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await FinancialEntriesModule.UpdateSelectedAsync(cancellationToken);
            await LoadDashboardAsync(cancellationToken);
            await ReportsModule.LoadAsync(cancellationToken);

            StatusMessage = $"Lançamento atualizado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao atualizar lançamento: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task DeleteSelectedEntryAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        FinancialEntryDeleteRequest request = FinancialEntriesModule.RequestDeleteSelected();

        if (request.State == FinancialEntryDeleteRequestState.NoSelection)
        {
            StatusMessage = "Selecione um lançamento para excluir.";
            return;
        }

        if (request.State == FinancialEntryDeleteRequestState.ConfirmationRequired)
        {
            StatusMessage = "Confirme a exclusão clicando novamente no botão.";
            return;
        }

        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await FinancialEntriesModule.DeleteConfirmedAsync(request.TargetEntryId, cancellationToken);
            await LoadDashboardAsync(cancellationToken);
            await ReportsModule.LoadAsync(cancellationToken);

            StatusMessage = $"Lançamento excluído em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao excluir lançamento: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task RegisterCustomerAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await CustomersModule.RegisterAsync(cancellationToken);
            SyncFinancialEntriesFormReferences();
            await LoadDashboardAsync(cancellationToken);
            await ReportsModule.LoadAsync(cancellationToken);

            StatusMessage = $"Cliente cadastrado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao cadastrar cliente: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task UpdateSelectedCustomerAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await CustomersModule.UpdateSelectedAsync(cancellationToken);
            SyncFinancialEntriesFormReferences();
            await LoadDashboardAsync(cancellationToken);
            await ReportsModule.LoadAsync(cancellationToken);

            StatusMessage = $"Cliente atualizado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao atualizar cliente: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task DeleteSelectedCustomerAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        CustomerDeleteRequest request = CustomersModule.RequestDeleteSelected();

        if (request.State == CustomerDeleteRequestState.NoSelection)
        {
            StatusMessage = "Selecione um cliente para excluir.";
            return;
        }

        if (request.State == CustomerDeleteRequestState.ConfirmationRequired)
        {
            StatusMessage = "Confirme a exclusão do cliente clicando novamente no botão.";
            return;
        }

        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await CustomersModule.DeleteConfirmedAsync(request.TargetCustomerId, cancellationToken);
            SyncFinancialEntriesFormReferences();
            await LoadDashboardAsync(cancellationToken);
            await ReportsModule.LoadAsync(cancellationToken);

            StatusMessage = $"Cliente excluído em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao excluir cliente: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task RegisterProductServiceAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await ProductCatalogModule.RegisterAsync(cancellationToken);
            SyncFinancialEntriesFormReferences();
            await LoadDashboardAsync(cancellationToken);
            await ReportsModule.LoadAsync(cancellationToken);

            StatusMessage = $"Produto/serviço cadastrado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao cadastrar produto/serviço: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task UpdateSelectedProductServiceAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await ProductCatalogModule.UpdateSelectedAsync(cancellationToken);
            SyncFinancialEntriesFormReferences();
            await LoadDashboardAsync(cancellationToken);
            await ReportsModule.LoadAsync(cancellationToken);

            StatusMessage = $"Produto/serviço atualizado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao atualizar produto/serviço: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task DeleteSelectedProductServiceAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        ProductDeleteRequest request = ProductCatalogModule.RequestDeleteSelected();

        if (request.State == ProductDeleteRequestState.NoSelection)
        {
            StatusMessage = "Selecione um produto/serviço para excluir.";
            return;
        }

        if (request.State == ProductDeleteRequestState.ConfirmationRequired)
        {
            StatusMessage = "Confirme a exclusão do produto/serviço clicando novamente no botão.";
            return;
        }

        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await ProductCatalogModule.DeleteConfirmedAsync(request.TargetProductServiceId, cancellationToken);
            SyncFinancialEntriesFormReferences();
            await LoadDashboardAsync(cancellationToken);
            await ReportsModule.LoadAsync(cancellationToken);

            StatusMessage = $"Produto/serviço excluído em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao excluir produto/serviço: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    private void SyncFinancialEntriesFormReferences()
    {
        FinancialEntriesModule.SyncFormReferences(CustomersModule, ProductCatalogModule);
    }

    private async Task LoadDashboardAsync(CancellationToken cancellationToken)
    {
        _summary = await _financialDashboardService.GetSummaryAsync(cancellationToken: cancellationToken);

        OnPropertyChanged(nameof(TotalRevenueDisplay));
        OnPropertyChanged(nameof(TotalExpenseDisplay));
        OnPropertyChanged(nameof(NetCashFlowDisplay));
        OnPropertyChanged(nameof(RegistrySummary));
    }

    private bool TryBeginBusyOperation()
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        return true;
    }

    private void EndBusyOperation()
    {
        IsBusy = false;
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
