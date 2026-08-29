namespace Shuttlez.Application.Bookings;

/// Server-side Shuttle money math. Uses decimal only.
public static class ShuttleFinancialCalculator
{
    public const int MoneyDecimals = 2;

    public static decimal NormalizeMoney(decimal value) =>
        Math.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);

    public static decimal CalculateTotal(decimal pricePerSeat, int seatCount)
    {
        if (seatCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(seatCount));
        if (pricePerSeat < 0)
            throw new ArgumentOutOfRangeException(nameof(pricePerSeat));
        return NormalizeMoney(pricePerSeat * seatCount);
    }

    /// <param name="platformCommissionPercent">0–100 share for Shuttlez.</param>
    public static (decimal CommissionRate, decimal CommissionAmount, decimal CaptainEarnings)
        SplitEarnings(decimal totalAmount, decimal platformCommissionPercent)
    {
        var clamped = Math.Clamp(platformCommissionPercent, 0m, 100m);
        var rate = NormalizeMoney(clamped / 100m);
        var commission = NormalizeMoney(totalAmount * rate);
        var captain = NormalizeMoney(totalAmount - commission);
        return (rate, commission, captain);
    }
}
