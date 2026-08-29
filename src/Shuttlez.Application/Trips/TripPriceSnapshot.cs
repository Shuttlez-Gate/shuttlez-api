using Shuttlez.Application.Common;

namespace Shuttlez.Application.Trips;

/// <summary>
/// Trip.PricePerSeat is a launch-time snapshot. Admin updates must not rewrite it.
/// </summary>
public static class TripPriceSnapshot
{
    public static void EnsureImmutableOnUpdate(decimal existingPricePerSeat, decimal requestedPricePerSeat)
    {
        if (existingPricePerSeat != requestedPricePerSeat)
        {
            throw new AppException(
                "سعر الرحلة ثابت بعد الإنشاء ولا يمكن تعديله. غيّر قاعدة التسعير للرحلات الجديدة فقط.",
                400,
                ErrorCodes.TripPriceImmutable);
        }
    }
}
