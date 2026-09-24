using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Configuration;

namespace InvoicingApi.Features.Invoices;

public class InvoiceConfiguration : EntityConfiguration<Invoice>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(i => i.ClientId)
            .IsRequired();

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .ValueGeneratedNever();

        // Invoice numbers run 1, 2, 3... separately for each tenant.
        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber })
            .IsUnique();

        // Target of the composite (TenantId, InvoiceId) foreign keys from InvoiceItems and Payments.
        builder.HasAlternateKey(i => new { i.TenantId, i.Id });

        builder.Property(i => i.Status)
            .HasConversion<int>()
            .HasDefaultValue(InvoiceStatus.Draft);

        builder.Property(i => i.IssueDate)
            .IsRequired();

        builder.Property(i => i.DueDate)
            .IsRequired();

        builder.Property(i => i.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(i => i.SubTotal)
            .HasColumnType("numeric(18,2)");

        builder.Property(i => i.TaxTotal)
            .HasColumnType("numeric(18,2)");

        builder.Property(i => i.GrandTotal)
            .HasColumnType("numeric(18,2)");

        builder.Property(i => i.Notes)
            .HasMaxLength(4000);

        // Composite foreign keys that include TenantId, so the database itself rejects a reference
        // across tenants even if a handler forgets to check.
        builder.HasOne(i => i.Client)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => new { i.TenantId, i.ClientId })
            .HasPrincipalKey(c => new { c.TenantId, c.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Items)
            .WithOne(ii => ii.Invoice)
            .HasForeignKey(ii => new { ii.TenantId, ii.InvoiceId })
            .HasPrincipalKey(i => new { i.TenantId, i.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Payments)
            .WithOne(p => p.Invoice)
            .HasForeignKey(p => new { p.TenantId, p.InvoiceId })
            .HasPrincipalKey(i => new { i.TenantId, i.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
