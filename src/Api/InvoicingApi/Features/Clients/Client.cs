using InvoicingApi.Features.Invoices;
using Shared.Entities;

namespace InvoicingApi.Features.Clients;

public class Client : SoftDeletableEntity
{
    public required string CompanyName { get; set; }
    public string? ContactName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }

    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public required string StateOrRegion { get; set; }
    public required string PostalCode { get; set; }
    public required string Country { get; set; }

    public required string PreferredCurrency { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
