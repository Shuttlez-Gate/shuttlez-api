namespace Shuttlez.Application.Subscriptions;

public enum SubscriptionCreditDecision
{
    /// No package — cash booking without credits.
    CashNoPackage,

    /// Explicit cash payment — never consume credits.
    CashExplicit,

    /// Consume subscription credits.
    UseCredit,

    /// Active limited package without enough remaining credits.
    LimitReached,

    /// Package present but expired.
    Expired
}

/// Pure subscription credit math (no DB). Used by handlers and unit tests.
public static class SubscriptionUsageCalculator
{
    public static bool IsUnlimited(int packageTripCount) => packageTripCount <= 0;

    public static int ComputeRemaining(int packageTripCount, int usedSeatCredits)
    {
        if (IsUnlimited(packageTripCount))
            return int.MaxValue;
        return Math.Max(0, packageTripCount - Math.Max(0, usedSeatCredits));
    }

    public static bool IsExplicitCash(string? paymentMethod) =>
        string.Equals(paymentMethod?.Trim(), "cash", StringComparison.OrdinalIgnoreCase);

    public static bool IsSubscriptionPayment(string? paymentMethod) =>
        string.Equals(paymentMethod?.Trim(), "subscription", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves credit intent. Exhausted/expired subscription payments fail explicitly —
    /// never silently convert subscription → cash.
    /// </summary>
    public static SubscriptionCreditDecision Resolve(
        string? paymentMethod,
        bool hasActivePackage,
        bool isExpired,
        int packageTripCount,
        int usedSeatCredits,
        int seatCount)
    {
        if (seatCount <= 0)
            return SubscriptionCreditDecision.CashNoPackage;

        if (IsExplicitCash(paymentMethod))
            return SubscriptionCreditDecision.CashExplicit;

        // subscription (or legacy non-cash) path
        if (!hasActivePackage)
            return SubscriptionCreditDecision.CashNoPackage;

        if (isExpired)
            return SubscriptionCreditDecision.Expired;

        if (IsUnlimited(packageTripCount))
            return SubscriptionCreditDecision.UseCredit;

        if (ComputeRemaining(packageTripCount, usedSeatCredits) >= seatCount)
            return SubscriptionCreditDecision.UseCredit;

        return SubscriptionCreditDecision.LimitReached;
    }

    [Obsolete("Use Resolve instead")]
    public static bool ShouldUseCredit(
        bool hasActivePackage,
        bool isExpired,
        int packageTripCount,
        int usedSeatCredits,
        int seatCount)
    {
        return Resolve(
            paymentMethod: "subscription",
            hasActivePackage,
            isExpired,
            packageTripCount,
            usedSeatCredits,
            seatCount) == SubscriptionCreditDecision.UseCredit;
    }
}
