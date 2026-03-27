using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMEFinanceSuite.Core.Domain.Entities;

namespace SMEFinanceSuite.Core.Infrastructure.Persistence.Configurations;

public sealed class ProductServiceConfiguration : IEntityTypeConfiguration<ProductService>
{
    public void Configure(EntityTypeBuilder<ProductService> builder)
    {
        builder.ToTable("products_services");

        builder.HasKey(productService => productService.Id);

        builder.Property(productService => productService.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(productService => productService.Category)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(productService => productService.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(productService => productService.IsService)
            .IsRequired();

        builder.Property(productService => productService.IsActive)
            .IsRequired();

        builder.Property(productService => productService.CreatedAtUtc)
            .IsRequired();

        builder.Property(productService => productService.UpdatedAtUtc)
            .IsRequired();
    }
}
