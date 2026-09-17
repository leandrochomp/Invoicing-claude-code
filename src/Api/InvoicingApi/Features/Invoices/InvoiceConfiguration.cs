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

        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique();

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

        builder.HasOne(i => i.Client)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Items)
            .WithOne(ii => ii.Invoice)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Payments)
            .WithOne(p => p.Invoice)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
