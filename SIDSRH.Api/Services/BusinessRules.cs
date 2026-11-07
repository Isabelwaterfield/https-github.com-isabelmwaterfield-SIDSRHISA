namespace SIDSRH.Api.Services;

public static class BusinessRules
{
    public const double UrgentMaxKm = 15;
    public const double RoutineMaxKm = 40;
    public const double RainMaxKm = 15;
    public const decimal OutsideSlotFee = 10m;
    public const decimal DelayFineRand = 3m;
    public const int CollectionGraceMinutes = 8;

    public const int MinTransitMinutes = 5;
    public const int MaxTransitMinutes = 40;

    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public static decimal Tariff(double km)
    {
        if (km < 5) return 90m;
        if (km <= 10) return 150m;
        var bands = (int)Math.Ceiling((km - 10) / 5.0);
        return 150m + bands * 80m;
    }

    public static string NewCode() => Random.Shared.Next(100000, 999999).ToString();

    public static int EtaMinutes(double km, double speedKmh)
    {
        if (speedKmh <= 1) speedKmh = 80;
        var mins = (int)Math.Round(km / speedKmh * 60.0);
        return Math.Clamp(mins, MinTransitMinutes, MaxTransitMinutes);
    }

    public static int TransitMinutes(double km) => EtaMinutes(km, 80);

    public static bool WithinLimit(string serviceClass, double km, bool raining = false)
    {
        if (raining) return km <= RainMaxKm;
        return serviceClass.Equals("Urgent", StringComparison.OrdinalIgnoreCase)
            ? km <= UrgentMaxKm
            : km <= RoutineMaxKm;
    }

    public static readonly string[] FreeSlots = { "06:30", "09:00", "16:00" };

    public static decimal PatientFeeForSlot(string? slot)
    {
        if (string.IsNullOrWhiteSpace(slot)) return 0;
        return FreeSlots.Contains(slot.Trim()) ? 0m : OutsideSlotFee;
    }


    public static double MaxKm(string serviceClass) =>
        serviceClass.Equals("Urgent", StringComparison.OrdinalIgnoreCase) ? UrgentMaxKm : RoutineMaxKm;

    public static object DistanceException(string serviceClass, double km, string? category = null)
    {
        var max = MaxKm(serviceClass);
        var firstAid = FirstAidBridge(category);
        return new
        {
            code = "DISTANCE_LIMIT",
            message = $"Distance {km:F1} km exceeds {max} km limit for {serviceClass} service.",
            distanceKm = Math.Round(km, 2),
            maxKm = max,
            serviceClass,
            alternatives = new[]
            {
                "Switch to Routine delivery if this is non-critical medicine supply (up to 40 km).",
                "Escalate to the nearest clinic or registered responder.",
                "If breathing difficulty, throat/tongue swelling, collapse or severe symptoms: call emergency services immediately.",
                "Follow while-you-wait first-aid guidance below."
            },
            firstAid
        };
    }

    public static object FirstAidBridge(string? category)
    {
        var cat = (category ?? "").ToLowerInvariant();
        if (cat.Contains("allerg"))
        {
            return new
            {
                title = "Allergy / insect sting — general information (not a diagnosis)",
                callEmsIf = new[] { "Difficulty breathing", "Swelling of lips, tongue or throat", "Dizziness, collapse, widespread hives", "Known severe allergy / anaphylaxis" },
                atHome = new[] {
                    "Move away from the insect source if safe",
                    "If a stinger is visible, scrape it off (do not squeeze)",
                    "Apply a cool clean cloth to the area",
                    "Rest; elevate the limb if possible",
                    "Use only medicines already prescribed or labelled for that person, as directed on the pack"
                },
                outdoors = new[] {
                    "Sit or lie down in a safe place",
                    "Loosen tight clothing",
                    "Ask a bystander for help; share contact details if a helper assists",
                    "Call emergency services early if severe signs appear",
                    "Do not run long distances if feeling faint"
                }
            };
        }
        return new
        {
            title = "While help is arranged — general information",
            callEmsIf = new[] { "Severe bleeding", "Chest pain", "Unconsciousness", "Severe breathing difficulty", "Suspected stroke" },
            atHome = new[] { "Keep the person calm and resting", "Use labelled medicines only as directed", "Cool cloth for fever/local irritation if appropriate", "Prepare clear directions for responders" },
            outdoors = new[] { "Move to a safe visible location if possible", "Ask for bystander help", "Call emergency services for severe symptoms", "Stay with the person until help arrives" }
        };
    }
}
