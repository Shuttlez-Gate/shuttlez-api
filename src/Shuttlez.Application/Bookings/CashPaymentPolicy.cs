using Shuttlez.Application.Common;

namespace Shuttlez.Application.Bookings;

/// <summary>
/// Phase 5 — go-live payment policy: CASH is the only payment method for money transfer.
/// Subscription credit is not an online gateway; it remains an existing package path.
/// </summary>
public static class CashPaymentPolicy
{
    public const string Cash = "cash";
    public const string Subscription = "subscription";

    /// <summary>Normalizes client paymentMethod or throws PAYMENT_METHOD_NOT_SUPPORTED.</summary>
    public static string NormalizeOrThrow(string? raw)
    {
        var value = string.IsNullOrWhiteSpace(raw) ? Cash : raw.Trim().ToLowerInvariant();

        return value switch
        {
            "cash" or "نقدا" or "نقدًا" or "نقد" => Cash,
            "subscription" or "package" or "باقة" => Subscription,
            _ => throw new AppException(
                "طريقة الدفع غير مدعومة. الدفع نقدًا للكابتن فقط حالياً.",
                400,
                ErrorCodes.PaymentMethodNotSupported)
        };
    }

    public static bool IsCash(string normalized) =>
        string.Equals(normalized, Cash, StringComparison.OrdinalIgnoreCase);

    public static bool IsOnlineGatewayRejected(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var v = raw.Trim().ToLowerInvariant();
        return v is "tahseel" or "card" or "visa" or "mastercard" or "wallet"
            or "online" or "stripe" or "paymob" or "gateway";
    }
}
