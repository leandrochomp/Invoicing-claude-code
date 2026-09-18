using Shared.Entities;

namespace InvoicingApi.Features.Users;

public enum UserRole
{
    User,
    Admin,
}

public class User : SoftDeletableEntity
{
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
}
