using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Configuration;

namespace InvoicingApi.Features.Clients;

public class ClientConfiguration : EntityConfiguration<Client>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Client> builder)
    {
        builder.Property(c => c.CompanyName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.ContactName)
            .HasMaxLength(255);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.AddressLine1)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.AddressLine2)
            .HasMaxLength(255);

        builder.Property(c => c.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.StateOrRegion)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.PostalCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.PreferredCurrency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.HasMany(c => c.Invoices)
            .WithOne(i => i.Client)
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
