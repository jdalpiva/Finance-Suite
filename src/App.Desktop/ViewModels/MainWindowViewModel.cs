using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Dashboard;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IFinancialDashboardService _financialDashboardService;
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("pt-BR");
    private DashboardSummaryDto _summary = DashboardSummaryDto.Empty;
    private string _statusMessage = "Carregando dados iniciais...";

    public MainWindowViewModel(IFinancialDashboardService financialDashboardService)
    {
        _financialDashboardService = financialDashboardService;
        _ = LoadAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string TotalRevenueDisplay => _summary.TotalRevenue.ToString("C", _culture);

    public string TotalExpenseDisplay => _summary.TotalExpense.ToString("C", _culture);

    public string NetCashFlowDisplay => _summary.NetCashFlow.ToString("C", _culture);

    public string RegistrySummary => $"{_summary.CustomersCount} clientes • {_summary.ProductsCount} itens";

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            _summary = await _financialDashboardService.GetSummaryAsync();
            OnPropertyChanged(nameof(TotalRevenueDisplay));
            OnPropertyChanged(nameof(TotalExpenseDisplay));
            OnPropertyChanged(nameof(NetCashFlowDisplay));
            OnPropertyChanged(nameof(RegistrySummary));

            StatusMessage = $"Resumo carregado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao carregar o dashboard: {exception.Message}";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
