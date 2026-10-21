using NIMI.UniPortal.Modules.Payments.Domain.Ledger;

namespace NIMI.UniPortal.Modules.Payments.Application.Common;

/// <summary>Maps the API's <c>method</c> strings to <see cref="PaymentMethod"/> and back.</summary>
internal static class PaymentMethodNames
{
    public static bool TryParse(string? value, out PaymentMethod method)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "cash":
                method = PaymentMethod.Cash;
                return true;
            case "bank_transfer":
                method = PaymentMethod.BankTransfer;
                return true;
            case "online":
                method = PaymentMethod.Online;
                return true;
            default:
                method = default;
                return false;
        }
    }

    public static string ToWire(PaymentMethod method) => EnumWire.ToWire(method);
}
