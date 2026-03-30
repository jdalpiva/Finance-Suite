namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class FinancialEntryListItemViewModel
{
    public FinancialEntryListItemViewModel(
        Guid id,
        string description,
        decimal amount,
        DateOnly occurredOn,
        Guid? customerId,
        string occurredOnDisplay,
        string amountDisplay,
        string entryTypeDisplay,
        string? notes,
        string notesDisplay)
    {
        Id = id;
        Description = description;
        Amount = amount;
        OccurredOn = occurredOn;
        CustomerId = customerId;
        OccurredOnDisplay = occurredOnDisplay;
        AmountDisplay = amountDisplay;
        EntryTypeDisplay = entryTypeDisplay;
        Notes = notes;
        NotesDisplay = notesDisplay;
    }

    public Guid Id { get; }

    public string Description { get; }

    public decimal Amount { get; }

    public DateOnly OccurredOn { get; }

    public Guid? CustomerId { get; }

    public string OccurredOnDisplay { get; }

    public string AmountDisplay { get; }

    public string EntryTypeDisplay { get; }

    public string? Notes { get; }

    public string NotesDisplay { get; }
}
