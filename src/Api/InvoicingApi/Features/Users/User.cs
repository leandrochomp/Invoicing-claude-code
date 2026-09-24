using InvoicingApi.Features.Tenants;
using Shared.Entities;

namespace InvoicingApi.Features.Users;

// Global role. Admin is the platform operator: no tenant, and no access to tenant endpoints.
public enum UserRole
{
    User,
    Admin,
}

// Role inside the user's tenant. Only set for UserRole.User (a database check constraint enforces it).
public enum TenantRole
{
    Owner,
    Member,
}

// Users are not ITenantOwned: login and username uniqueness look users up across every tenant.
// A user's tenant is carried into each request by the JWT tenant_id claim instead.
public class User : SoftDeletableEntity
{
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;

    public Guid? TenantId { get; set; }
    public TenantRole? TenantRole { get; set; }

    public Tenant? Tenant { get; set; }
}
