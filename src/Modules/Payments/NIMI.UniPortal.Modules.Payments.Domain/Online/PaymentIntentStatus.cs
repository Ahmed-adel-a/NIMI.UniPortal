namespace NIMI.UniPortal.Modules.Payments.Domain.Online;

/// <summary>The persisted lifecycle of an online checkout (PAYGW). Expiry is computed by time.</summary>
public enum PaymentIntentStatus
{
    Pending,
    Succeeded,
    Failed,
    Expired,
}
