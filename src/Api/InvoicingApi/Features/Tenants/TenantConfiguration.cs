using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Configuration;

namespace InvoicingApi.Features.Tenants;

public class TenantConfiguration : SoftDeletableEntityConfiguration<Tenant>
{
    protected override void ConfigureSoftDeletableEntity(EntityTypeBuilder<Tenant> builder)
    {
        // Entity already assigns Id on construction. Without this, EF would borrow Tenant.Id's value generator
        // to invent a random TenantId for a new client (TenantId is both a foreign key to Tenants and part of
        // the clients' alternate key), instead of leaving it for InvoicingDbContext to stamp.
        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        // Learning project only: a dev tenant with a seeded Owner (see SeedDevTenant) so the local
        // stack and the E2E suite have a tenant user to sign in as. The Admin has no tenant.
        builder.HasData(new Tenant
        {
            Id = SeedDevTenant.Id,
            Name = SeedDevTenant.Name,
        });
    }
}

public static class SeedDevTenant
{
    public static readonly Guid Id = new("01995c3a-0000-7000-8000-000000000101");
    public const string Name = "Dev Tenant";

    public static readonly Guid OwnerId = new("01995c3a-0000-7000-8000-000000000102");
    public const string OwnerUsername = "Owner";

    // BCrypt hash of "P@ssw0rD!" (work factor 11).
    public const string OwnerPasswordHash = "$2a$11$NHMaMrzhmc1drSqcTEHR/eBxertBvf6M9r5EqsLfIX60qP6zJXDGi";
}
