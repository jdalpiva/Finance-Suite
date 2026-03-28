using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Domain.Enums;
using Xunit;

namespace SMEFinanceSuite.Tests.Unit.Domain;

public sealed class FinancialEntryTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenAmountHasMoreThanTwoDecimalPlaces()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FinancialEntry(
                description: "Receita inválida",
                amount: 10.123m,
                occurredOn: DateOnly.FromDateTime(DateTime.Today),
                entryType: EntryType.Revenue));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOccurredOnIsDefault()
    {
        Assert.Throws<ArgumentException>(() =>
            new FinancialEntry(
                description: "Receita inválida",
                amount: 100m,
                occurredOn: default,
                entryType: EntryType.Revenue));
    }
}
