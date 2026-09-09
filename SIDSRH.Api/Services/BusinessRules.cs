namespace SIDSRH.Api.Services;

public static class BusinessRules
{
    public const double UrgentMaxKm = 15;
    public const double RoutineMaxKm = 40;
    public const double CruiseSpeedKmh = 60;
    public const int MinBattery = 30;
    public const double DrainPerKm = 1.0;
    public const int MaxFailedLogins = 5;
    public const int OtpMinutes = 6;
    public const decimal OutsideSlotFee = 10m;
    public const decimal DelayFineRand = 3m;
    public const int CollectionGraceMinutes = 8;

    public static readonly string[] FreeSlots = { "06:30", "09:00", "16:00" };

    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }

    public static int EtaMinutes(double distanceKm, double speedKmh = CruiseSpeedKmh)
        => Math.Max(1, (int)Math.Round(distanceKm / Math.Max(speedKmh, 1) * 60));

    public static string NewCode() => Random.Shared.Next(100000, 999999).ToString();

    public static decimal PatientFeeForSlot(string? slot)
    {
        if (string.IsNullOrWhiteSpace(slot)) return OutsideSlotFee;
        return FreeSlots.Contains(slot.Trim()) ? 0m : OutsideSlotFee;
    }

    public static bool IsRaining(string? condition)
        => string.Equals(condition, "Rain", StringComparison.OrdinalIgnoreCase)
        || string.Equals(condition, "Heavy Rain", StringComparison.OrdinalIgnoreCase);
}
