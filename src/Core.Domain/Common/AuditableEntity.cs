namespace SMEFinanceSuite.Core.Domain.Common;

public abstract class AuditableEntity : Entity
{
    protected AuditableEntity()
    {
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public DateTime CreatedAtUtc { get; protected set; }

    public DateTime UpdatedAtUtc { get; protected set; }

    protected void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
