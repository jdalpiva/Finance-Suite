using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.FinancialEntries;
using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class FinancialEntryFormViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    private string _newEntryDescription = string.Empty;
    private string _newEntryAmount = string.Empty;
    private string _newEntryOccurredOn = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string _selectedFormEntryType = "Receita";
    private string _newEntryNotes = string.Empty;
    private Guid? _selectedCustomerId;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> FormEntryTypeOptions { get; } = ["Receita", "Despesa"];

    public string NewEntryDescription
    {
        get => _newEntryDescription;
        set => SetProperty(ref _newEntryDescription, value);
    }

    public string NewEntryAmount
    {
        get => _newEntryAmount;
        set => SetProperty(ref _newEntryAmount, value);
    }

    public string NewEntryOccurredOn
    {
        get => _newEntryOccurredOn;
        set => SetProperty(ref _newEntryOccurredOn, value);
    }

    public string SelectedFormEntryType
    {
        get => _selectedFormEntryType;
        set => SetProperty(ref _selectedFormEntryType, value);
    }

    public string NewEntryNotes
    {
        get => _newEntryNotes;
        set => SetProperty(ref _newEntryNotes, value);
    }

    public Guid? SelectedCustomerId
    {
        get => _selectedCustomerId;
        set => SetProperty(ref _selectedCustomerId, value);
    }

    public CreateFinancialEntryCommand BuildCreateCommand()
    {
        (string description, decimal amount, DateOnly occurredOn, EntryType entryType, string? notes) = BuildEntryInput();

        return new CreateFinancialEntryCommand(
            Description: description,
            Amount: amount,
            OccurredOn: occurredOn,
            EntryType: entryType,
            CustomerId: SelectedCustomerId,
            Notes: notes);
    }

    public UpdateFinancialEntryCommand BuildUpdateCommand(Guid id)
    {
        (string description, decimal amount, DateOnly occurredOn, EntryType entryType, string? notes) = BuildEntryInput();

        return new UpdateFinancialEntryCommand(
            Id: id,
            Description: description,
            Amount: amount,
            OccurredOn: occurredOn,
            EntryType: entryType,
            CustomerId: SelectedCustomerId,
            Notes: notes);
    }

    public void FillFromSelection(FinancialEntryListItemViewModel selectedEntry)
    {
        ArgumentNullException.ThrowIfNull(selectedEntry);

        NewEntryDescription = selectedEntry.Description;
        NewEntryAmount = selectedEntry.Amount.ToString("0.00", PortugueseCulture);
        NewEntryOccurredOn = selectedEntry.OccurredOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        SelectedFormEntryType = selectedEntry.EntryTypeDisplay;
        NewEntryNotes = selectedEntry.Notes ?? string.Empty;
        SelectedCustomerId = selectedEntry.CustomerId;
    }

    public void Clear()
    {
        NewEntryDescription = string.Empty;
        NewEntryAmount = string.Empty;
        NewEntryOccurredOn = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        SelectedFormEntryType = FormEntryTypeOptions[0];
        NewEntryNotes = string.Empty;
        SelectedCustomerId = null;
    }

    private (string Description, decimal Amount, DateOnly OccurredOn, EntryType EntryType, string? Notes) BuildEntryInput()
    {
        string description = NewEntryDescription.Trim();

        if (description.Length == 0)
        {
            throw new InvalidOperationException("Informe a descrição do lançamento.");
        }

        if (!TryParseAmount(NewEntryAmount, out decimal amount))
        {
            throw new InvalidOperationException("Informe um valor válido. Exemplo: 1500,50");
        }

        DateOnly occurredOn = ParseRequiredDate(NewEntryOccurredOn, "Data do lançamento");

        EntryType entryType = SelectedFormEntryType == "Despesa"
            ? EntryType.Expense
            : EntryType.Revenue;

        string? notes = string.IsNullOrWhiteSpace(NewEntryNotes)
            ? null
            : NewEntryNotes.Trim();

        return (description, amount, occurredOn, entryType, notes);
    }

    private static bool TryParseAmount(string rawAmount, out decimal amount)
    {
        if (decimal.TryParse(rawAmount, NumberStyles.Number, PortugueseCulture, out amount))
        {
            return true;
        }

        return decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    private static DateOnly ParseRequiredDate(string rawDate, string fieldName)
    {
        if (TryParseDate(rawDate, out DateOnly parsedDate))
        {
            return parsedDate;
        }

        throw new InvalidOperationException($"{fieldName} inválida. Use yyyy-MM-dd.");
    }

    private static bool TryParseDate(string rawDate, out DateOnly parsedDate)
    {
        if (DateOnly.TryParseExact(rawDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
        {
            return true;
        }

        if (DateOnly.TryParse(rawDate, PortugueseCulture, DateTimeStyles.None, out parsedDate))
        {
            return true;
        }

        return DateOnly.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
