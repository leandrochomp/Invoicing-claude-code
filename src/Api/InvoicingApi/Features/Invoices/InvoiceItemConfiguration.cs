using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Configuration;

namespace InvoicingApi.Features.Invoices;

public class InvoiceItemConfiguration : EntityConfiguration<InvoiceItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.Property(ii => ii.InvoiceId)
            .IsRequired();

        builder.Property(ii => ii.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(ii => ii.Quantity)
            .HasColumnType("numeric(18,4)");

        builder.Property(ii => ii.UnitPrice)
            .HasColumnType("numeric(18,2)");

        builder.Property(ii => ii.TaxRate)
            .HasColumnType("numeric(18,4)");

        builder.Property(ii => ii.LineTotal)
            .HasColumnType("numeric(18,2)");

        builder.Property(ii => ii.SortOrder)
            .HasDefaultValue(0);

        // The relationship to Invoice, a composite (TenantId, InvoiceId) key, is configured in InvoiceConfiguration.
        // This index leads with the foreign key's columns, so it also serves as the foreign key's index.
        builder.HasIndex(ii => new { ii.TenantId, ii.InvoiceId, ii.SortOrder });
    }
}
