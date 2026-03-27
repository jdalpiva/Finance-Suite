using SMEFinanceSuite.Core.Domain.Common;

namespace SMEFinanceSuite.Core.Domain.Entities;

public sealed class Customer : AuditableEntity
{
    private Customer()
    {
    }

    public Customer(string name, string? email = null, string? phone = null)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 120);
        Email = Guard.NormalizeOptional(email, 150);
        Phone = Guard.NormalizeOptional(phone, 30);
    }

    public string Name { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public void Rename(string name)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 120);
        Touch();
    }

    public void UpdateContact(string? email, string? phone)
    {
        Email = Guard.NormalizeOptional(email, 150);
        Phone = Guard.NormalizeOptional(phone, 30);
        Touch();
    }
}
