using Shared.Entities;
using Shouldly;

namespace Shared.Tests.Entities;

public class EntityTests
{
    private sealed class TestEntity : Entity;

    [Fact]
    public void Entity_has_default_version_of_zero()
    {
        var entity = new TestEntity();

        entity.Version.ShouldBe(0);
    }

    [Fact]
    public void Entity_generates_a_non_empty_id_by_default()
    {
        var entity = new TestEntity();

        entity.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Two_entities_get_distinct_ids()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        first.Id.ShouldNotBe(second.Id);
    }
}
