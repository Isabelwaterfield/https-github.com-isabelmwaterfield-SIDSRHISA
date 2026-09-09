using SIDSRH.Api.Models;

namespace SIDSRH.Api.Data;

public static class DbSeed
{
    public static void EnsureSeed(AppDbContext db)
    {
        if (db.Users.Any()) return;
        string H(string p) => BCrypt.Net.BCrypt.HashPassword(p);

        db.Locations.AddRange(
            new Location { LocationId = "LOC001", LocationName = "Mthatha Base Hospital", Latitude = -31.5889, Longitude = 28.7844, LocationType = "Hospital" },
            new Location { LocationId = "LOC002", LocationName = "Qumbu Mobile Clinic", Latitude = -31.15, Longitude = 28.8667, LocationType = "Clinic" },
            new Location { LocationId = "LOC003", LocationName = "Tsolo Village Point", Latitude = -31.30, Longitude = 28.75, LocationType = "Remote Site" },
            new Location { LocationId = "LOC004", LocationName = "Port St Johns Clinic", Latitude = -31.6289, Longitude = 29.5369, LocationType = "Clinic" },
            new Location { LocationId = "LOC005", LocationName = "Drone Hub Mthatha", Latitude = -31.59, Longitude = 28.79, LocationType = "Supply Depot" }
        );

        db.Users.AddRange(
            new User { UserId = "USR001", FirstName = "Thabo", LastName = "Mokoena", Email = "admin@sidsrh.health", PasswordHash = H("Admin@123"), Role = "Administrator", MfaEnabled = true },
            new User { UserId = "USR002", FirstName = "Naledi", LastName = "Dlamini", Email = "manager@sidsrh.health", PasswordHash = H("Manager@123"), Role = "Manager", MfaEnabled = true },
            new User { UserId = "USR003", FirstName = "Sipho", LastName = "Ngcobo", Email = "supply@sidsrh.health", PasswordHash = H("Supply@123"), Role = "Supply Coordinator", MfaEnabled = true },
            new User { UserId = "USR004", FirstName = "Aisha", LastName = "Patel", Email = "emergency@sidsrh.health", PasswordHash = H("Emerg@123"), Role = "Emergency Coordinator", MfaEnabled = true },
            new User { UserId = "USR005", FirstName = "Lindiwe", LastName = "Khumalo", Email = "nurse@sidsrh.health", PasswordHash = H("Staff@123"), Role = "Healthcare Staff", MfaEnabled = true },
            new User { UserId = "USR006", FirstName = "Johan", LastName = "Botha", Email = "tech@sidsrh.health", PasswordHash = H("Tech@123"), Role = "Technician", MfaEnabled = true },
            new User { UserId = "USR007", FirstName = "Nomsa", LastName = "Mbeki", Email = "patient@demo.health", PasswordHash = H("Patient@123"), Role = "Patient", MfaEnabled = true }
        );

        db.MobileClinics.AddRange(
            new MobileClinic { ClinicId = "CLN001", ClinicName = "Qumbu Mobile Unit", LocationId = "LOC002", Status = "Active", ContactNumber = "047-555-0101" },
            new MobileClinic { ClinicId = "CLN002", ClinicName = "Port St Johns Unit", LocationId = "LOC004", Status = "Active" }
        );

        db.Staff.AddRange(
            new Staff { StaffId = "STF001", UserId = "USR005", FirstName = "Lindiwe", LastName = "Khumalo", JobTitle = "Professional Nurse", Status = "On Duty", ClinicId = "CLN001", ContactNumber = "083-111-2222" },
            new Staff { StaffId = "STF002", UserId = "USR006", FirstName = "Johan", LastName = "Botha", JobTitle = "Drone Technician", Status = "On Duty", ClinicId = "CLN001" }
        );

        db.Drones.AddRange(
            new Drone { DroneId = "DRN001", DroneName = "AeroMed Winglet", Status = "Available", CurrentLocationId = "LOC005", Latitude = -31.59, Longitude = 28.79, BatteryLevel = 92, SpeedKmh = 80 },
            new Drone { DroneId = "DRN002", DroneName = "AeroRescue HexaShield", Status = "Available", CurrentLocationId = "LOC005", Latitude = -31.59, Longitude = 28.79, BatteryLevel = 98, SpeedKmh = 70 },
            new Drone { DroneId = "DRN003", DroneName = "LifeFlight Courier", Status = "Maintenance", CurrentLocationId = "LOC001", Latitude = -31.5889, Longitude = 28.7844, BatteryLevel = 34, SpeedKmh = 60 }
        );

        db.MedicalSupplies.AddRange(
            new MedicalSupply { SupplyId = "SUP001", SupplyName = "Cetirizine 10mg", SupplyType = "Allergy relief", Category = "Allergy relief", Quantity = 120, ExpiryDate = DateTime.UtcNow.AddMonths(18) },
            new MedicalSupply { SupplyId = "SUP002", SupplyName = "ORS Sachets", SupplyType = "Rehydration", Category = "Hydration", Quantity = 200, ExpiryDate = DateTime.UtcNow.AddMonths(24) },
            new MedicalSupply { SupplyId = "SUP003", SupplyName = "Paracetamol 500mg", SupplyType = "Pain and fever", Category = "Pain and fever relief", Quantity = 300, ExpiryDate = DateTime.UtcNow.AddMonths(20) },
            new MedicalSupply { SupplyId = "SUP004", SupplyName = "Wound dressing kit", SupplyType = "First-aid", Category = "Wound care", Quantity = 80, ExpiryDate = DateTime.UtcNow.AddMonths(12), ColdChain = false }
        );

        db.Equipment.Add(new Equipment { EquipmentId = "EQP001", EquipmentName = "Cold-chain fridge", EquipmentType = "Storage", Status = "Available", ClinicId = "CLN001", LocationId = "LOC002" });

        db.EmergencyScenarios.Add(new EmergencyScenario
        {
            ScenarioId = "SCN001",
            ScenarioName = "Mild allergic reaction - Qumbu",
            EmergencyType = "Allergy medication",
            Priority = "High",
            StartingLocationId = "LOC005",
            DestinationLocationId = "LOC002",
            Description = "Antihistamine request - no diagnosis recorded"
        });

        db.PatientRequests.Add(new PatientRequest
        {
            RequestId = "REQ001",
            PatientName = "Nomsa Mbeki",
            ContactNumber = "0835557788",
            LocationId = "LOC002",
            MedicalNeed = "Mild allergic reaction - antihistamine",
            Category = "Allergy relief",
            ServiceClass = "Urgent",
            Status = "Submitted",
            DistanceKm = 12.5
        });

        db.PatientProfiles.Add(new PatientProfile { PatientId = "PAT001", FullName = "Nomsa Mbeki", ContactNumber = "0835557788", LocationId = "LOC002" });
        db.ChronicSchedules.Add(new ChronicSchedule
        {
            ScheduleId = "SCH001", PatientId = "PAT001", SupplyId = "SUP003", ClinicId = "CLN001",
            IntervalDays = 30, NextDueDate = DateTime.UtcNow.Date.AddDays(28), PreferredSlot = "09:00"
        });

        db.ChargingStations.AddRange(
            new ChargingStation { StationId = "CHG001", StationName = "Mthatha Tower Hub", Latitude = -31.59, Longitude = 28.79, StationType = "Tower", CoverageRadiusKm = 15 },
            new ChargingStation { StationId = "CHG002", StationName = "Qumbu Cell Tower Pad", Latitude = -31.15, Longitude = 28.8667, StationType = "Tower", CoverageRadiusKm = 15 },
            new ChargingStation { StationId = "CHG003", StationName = "Tsolo Underground Cache", Latitude = -31.30, Longitude = 28.75, StationType = "Underground", CoverageRadiusKm = 15 }
        );

        db.SystemWeather.Add(new SystemWeather { Id = 1, Condition = "Clear", Note = "Default" });

        db.ClinicAppointments.Add(new ClinicAppointment
        {
            AppointmentId = "APT001", PatientId = "PAT001", PatientName = "Nomsa Mbeki",
            AppointmentAt = DateTime.UtcNow.Date.AddDays(14).AddHours(9), ClinicId = "CLN001",
            ReasonBrief = "BP checkup", Status = "Scheduled"
        });

        db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG001", UserId = "USR001", ActivityType = "SYSTEM_SEED",
            Description = "Full merged database seeded (Phase 4 + extended features)"
        });

        db.SaveChanges();
    }
}
