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

        builder.HasOne(ii => ii.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ii => new { ii.InvoiceId, ii.SortOrder });
    }
}
