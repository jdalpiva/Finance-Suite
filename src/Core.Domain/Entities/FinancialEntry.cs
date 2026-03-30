using SMEFinanceSuite.Core.Domain.Common;
using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.Core.Domain.Entities;

public sealed class FinancialEntry : AuditableEntity
{
    private FinancialEntry()
    {
    }

    public FinancialEntry(
        string description,
        decimal amount,
        DateOnly occurredOn,
        EntryType entryType,
        Guid? customerId = null,
        Guid? productServiceId = null,
        string? notes = null)
    {
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description), 160);
        Amount = Guard.AgainstInvalidMonetaryAmount(amount, nameof(amount));
        OccurredOn = Guard.AgainstDefaultDate(occurredOn, nameof(occurredOn));
        EntryType = entryType;
        CustomerId = customerId;
        ProductServiceId = productServiceId;
        Notes = Guard.NormalizeOptional(notes, 400);
    }

    public string Description { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public DateOnly OccurredOn { get; private set; }

    public EntryType EntryType { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Customer? Customer { get; private set; }

    public Guid? ProductServiceId { get; private set; }

    public ProductService? ProductService { get; private set; }

    public string? Notes { get; private set; }

    public void Update(
        string description,
        decimal amount,
        DateOnly occurredOn,
        EntryType entryType,
        string? notes)
    {
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description), 160);
        Amount = Guard.AgainstInvalidMonetaryAmount(amount, nameof(amount));
        OccurredOn = Guard.AgainstDefaultDate(occurredOn, nameof(occurredOn));
        EntryType = entryType;
        Notes = Guard.NormalizeOptional(notes, 400);
        Touch();
    }

    public void Reclassify(EntryType entryType)
    {
        EntryType = entryType;
        Touch();
    }

    public void UpdateDescription(string description)
    {
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description), 160);
        Touch();
    }
}
