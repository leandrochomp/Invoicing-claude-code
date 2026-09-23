namespace Shared.Entities;

public abstract class Entity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public int Version { get; set; }
}

public abstract class SoftDeletableEntity : Entity
{
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public Guid? DeletedBy { get; protected set; }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt)
    {
        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
