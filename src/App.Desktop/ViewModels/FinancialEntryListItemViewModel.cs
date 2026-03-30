namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class FinancialEntryListItemViewModel
{
    public FinancialEntryListItemViewModel(
        string description,
        string occurredOnDisplay,
        string amountDisplay,
        string entryTypeDisplay,
        string notesDisplay)
    {
        Description = description;
        OccurredOnDisplay = occurredOnDisplay;
        AmountDisplay = amountDisplay;
        EntryTypeDisplay = entryTypeDisplay;
        NotesDisplay = notesDisplay;
    }

    public string Description { get; }

    public string OccurredOnDisplay { get; }

    public string AmountDisplay { get; }

    public string EntryTypeDisplay { get; }

    public string NotesDisplay { get; }
}
