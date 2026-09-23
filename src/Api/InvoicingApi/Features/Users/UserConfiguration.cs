using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Configuration;

namespace InvoicingApi.Features.Users;

public class UserConfiguration : SoftDeletableEntityConfiguration<User>
{
    protected override void ConfigureSoftDeletableEntity(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.IsDeleted)
            .HasDefaultValue(false);

        // Learning project only: default admin (Admin / P@ssw0rD!) seeded via migrations.
        // Id and hash are constants so the seed doesn't churn on every new migration.
        builder.HasData(new User
        {
            Id = SeedAdminUser.Id,
            Username = SeedAdminUser.Username,
            PasswordHash = SeedAdminUser.PasswordHash,
            Role = UserRole.Admin,
        });
    }
}

public static class SeedAdminUser
{
    public static readonly Guid Id = new("01995c3a-0000-7000-8000-000000000001");
    public const string Username = "Admin";

    // BCrypt hash of "P@ssw0rD!" (work factor 11).
    public const string PasswordHash = "$2a$11$ouzQd7TvlOMBYHCZDzZ0oOXVoJhzDzl7NAM91wOFYS/HGgwR3iHEa";
}
