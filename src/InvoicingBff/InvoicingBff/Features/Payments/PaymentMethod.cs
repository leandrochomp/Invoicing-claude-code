namespace InvoicingBff.Features.Payments;

// Mirrors InvoicingApi's PaymentMethod; both sides serialize it as its number.
public enum PaymentMethod
{
    BankTransfer = 0,
    Card = 1,
    Cash = 2,
    Check = 3,
    Other = 4
}
