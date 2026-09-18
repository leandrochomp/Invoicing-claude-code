using InvoicingApi.Features.Clients;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace InvoicingApi.Tests.Infrastructure.Data;

[Collection(PostgresCollection.Name)]
public class RepositoryAndUnitOfWorkTests(PostgresFixture postgres)
{
    private async Task<InvoicingDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        var context = new InvoicingDbContext(options);
        await context.Database.MigrateAsync();

        return context;
    }

    private static Client NewClient(string companyName) => new()
    {
        CompanyName = companyName,
        Email = $"{Guid.NewGuid()}@acme.test",
        AddressLine1 = "1 Main St",
        City = "Springfield",
        StateOrRegion = "IL",
        PostalCode = "62701",
        Country = "US",
        PreferredCurrency = "USD",
    };

    [Fact]
    public async Task Adding_an_entity_and_saving_persists_it()
    {
        await using var context = await CreateContextAsync();
        var repository = new Repository<Client>(context);
        var unitOfWork = new EfUnitOfWork(context);
        var client = NewClient("Acme");

        await repository.AddAsync(client);
        await unitOfWork.SaveChangesAsync();

        var persisted = await repository.GetByIdAsync(client.Id);

        persisted.ShouldNotBeNull();
        persisted.CompanyName.ShouldBe("Acme");
    }

    [Fact]
    public async Task Listing_returns_all_persisted_entities()
    {
        await using var context = await CreateContextAsync();
        var repository = new Repository<Client>(context);
        var unitOfWork = new EfUnitOfWork(context);
        var first = NewClient("First");
        var second = NewClient("Second");

        await repository.AddAsync(first);
        await repository.AddAsync(second);
        await unitOfWork.SaveChangesAsync();

        var all = await repository.ListAsync();

        all.ShouldContain(c => c.Id == first.Id);
        all.ShouldContain(c => c.Id == second.Id);
    }

    [Fact]
    public async Task Removing_an_entity_and_saving_deletes_it()
    {
        await using var context = await CreateContextAsync();
        var repository = new Repository<Client>(context);
        var unitOfWork = new EfUnitOfWork(context);
        var client = NewClient("Temp");
        await repository.AddAsync(client);
        await unitOfWork.SaveChangesAsync();

        repository.Remove(client);
        await unitOfWork.SaveChangesAsync();

        var persisted = await repository.GetByIdAsync(client.Id);

        persisted.ShouldBeNull();
    }
}
