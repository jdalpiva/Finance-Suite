using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.Core.Infrastructure.Persistence.Configurations;

public sealed class FinancialEntryConfiguration : IEntityTypeConfiguration<FinancialEntry>
{
    public void Configure(EntityTypeBuilder<FinancialEntry> builder)
    {
        builder.ToTable("financial_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Description)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(entry => entry.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(entry => entry.OccurredOn)
            .IsRequired();

        builder.Property(entry => entry.EntryType)
            .HasConversion(
                value => value.ToString(),
                value => Enum.Parse<EntryType>(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(entry => entry.Notes)
            .HasMaxLength(400);

        builder.Property(entry => entry.CreatedAtUtc)
            .IsRequired();

        builder.Property(entry => entry.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(entry => entry.Customer)
            .WithMany()
            .HasForeignKey(entry => entry.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(entry => entry.ProductService)
            .WithMany()
            .HasForeignKey(entry => entry.ProductServiceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
