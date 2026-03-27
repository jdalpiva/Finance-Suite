using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMEFinanceSuite.Core.Domain.Entities;

namespace SMEFinanceSuite.Core.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(150);

        builder.Property(customer => customer.Phone)
            .HasMaxLength(30);

        builder.Property(customer => customer.CreatedAtUtc)
            .IsRequired();

        builder.Property(customer => customer.UpdatedAtUtc)
            .IsRequired();
    }
}
