using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Configuration;

namespace InvoicingApi.Features.Invoices;

public class PaymentConfiguration : EntityConfiguration<Payment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.InvoiceId)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnType("numeric(18,2)");

        builder.Property(p => p.PaymentDate)
            .IsRequired();

        builder.Property(p => p.Method)
            .HasConversion<int>();

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.InvoiceId);
    }
}
