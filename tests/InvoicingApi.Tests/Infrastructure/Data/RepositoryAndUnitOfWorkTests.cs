using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;

namespace InvoicingApi.Tests.Infrastructure.Data;

file sealed class TestEntity : Entity
{
    public string Name { get; set; } = string.Empty;
}

file sealed class TestDbContext(DbContextOptions<InvoicingDbContext> options, string schema)
    : InvoicingDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(schema);
        modelBuilder.Entity<TestEntity>();
    }
}

[Collection(PostgresCollection.Name)]
public class RepositoryAndUnitOfWorkTests(PostgresFixture postgres)
{
    private InvoicingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .Options;

        var context = new TestDbContext(options, $"test_{Guid.NewGuid():N}");
        context.Database.EnsureCreated();

        return context;
    }

    [Fact]
    public async Task Adding_an_entity_and_saving_persists_it()
    {
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var unitOfWork = new EfUnitOfWork(context);
        var entity = new TestEntity { Name = "Acme" };

        await repository.AddAsync(entity);
        await unitOfWork.SaveChangesAsync();

        var persisted = await repository.GetByIdAsync(entity.Id);

        Assert.NotNull(persisted);
        Assert.Equal("Acme", persisted.Name);
    }

    [Fact]
    public async Task Listing_returns_all_persisted_entities()
    {
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var unitOfWork = new EfUnitOfWork(context);

        await repository.AddAsync(new TestEntity { Name = "First" });
        await repository.AddAsync(new TestEntity { Name = "Second" });
        await unitOfWork.SaveChangesAsync();

        var all = await repository.ListAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Removing_an_entity_and_saving_deletes_it()
    {
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var unitOfWork = new EfUnitOfWork(context);
        var entity = new TestEntity { Name = "Temp" };
        await repository.AddAsync(entity);
        await unitOfWork.SaveChangesAsync();

        repository.Remove(entity);
        await unitOfWork.SaveChangesAsync();

        var persisted = await repository.GetByIdAsync(entity.Id);

        Assert.Null(persisted);
    }
}
