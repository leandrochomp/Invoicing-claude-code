namespace Shared.Entities;

public abstract class Entity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public int Version { get; set; }
}
