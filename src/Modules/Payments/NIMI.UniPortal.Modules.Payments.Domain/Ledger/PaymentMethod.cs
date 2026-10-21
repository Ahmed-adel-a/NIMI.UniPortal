namespace NIMI.UniPortal.Modules.Payments.Domain.Ledger;

/// <summary>How a payment reached the university (P5, rule R1).</summary>
public enum PaymentMethod
{
    Cash,
    BankTransfer,
    Online,
}
