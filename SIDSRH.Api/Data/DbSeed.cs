using SIDSRH.Api.Models;

namespace SIDSRH.Api.Data;

public static class DbSeed
{
    public static void EnsureSeed(AppDbContext db)
    {
        db.Database.EnsureCreated();
        if (db.Users.Any())
        {
            EnsureKnownPasswords(db);
            SeedHistoricalActivity(db);
            return;
        }

        string H(string p) => BCrypt.Net.BCrypt.HashPassword(p);

        db.Locations.AddRange(
            new Location { LocationId = "LOC001", LocationName = "Mthatha Base Hospital", Latitude = -31.5889, Longitude = 28.7844, LocationType = "Hospital" },
            new Location { LocationId = "LOC002", LocationName = "Qumbu Mobile Clinic", Latitude = -31.15, Longitude = 28.8667, LocationType = "Clinic" },
            new Location { LocationId = "LOC003", LocationName = "Tsolo Village Point", Latitude = -31.30, Longitude = 28.75, LocationType = "Village" },
            new Location { LocationId = "LOC004", LocationName = "Port St Johns Clinic", Latitude = -31.6289, Longitude = 29.5369, LocationType = "Clinic" },
            new Location { LocationId = "LOC005", LocationName = "Drone Hub – Mthatha", Latitude = -31.59, Longitude = 28.79, LocationType = "Base" },
            new Location { LocationId = "LOC006", LocationName = "SAPS Mthatha", Latitude = -31.592, Longitude = 28.788, LocationType = "Police" },
            new Location { LocationId = "LOC007", LocationName = "Home Affairs Mthatha", Latitude = -31.595, Longitude = 28.782, LocationType = "Government" },
            new Location { LocationId = "LOC008", LocationName = "Qumbu Village Stand", Latitude = -31.18, Longitude = 28.85, LocationType = "Village" },
            new Location { LocationId = "LOC009", LocationName = "Ngceleni Access Point", Latitude = -31.22, Longitude = 28.90, LocationType = "Village" }
        );

        db.Users.AddRange(
            new User { UserId = "USR001", FirstName = "Thabo", LastName = "Mokoena", Email = "admin@sidsrh.health", PasswordHash = H("Admin@123"), Role = "Administrator" },
            new User { UserId = "USR002", FirstName = "Naledi", LastName = "Dlamini", Email = "manager@sidsrh.health", PasswordHash = H("Manager@123"), Role = "Manager" },
            new User { UserId = "USR003", FirstName = "Sipho", LastName = "Ngcobo", Email = "supply@sidsrh.health", PasswordHash = H("Supply@123"), Role = "Supply Coordinator" },
            new User { UserId = "USR004", FirstName = "Aisha", LastName = "Patel", Email = "emergency@sidsrh.health", PasswordHash = H("Emerg@123"), Role = "Emergency Coordinator" },
            new User { UserId = "USR005", FirstName = "Lindiwe", LastName = "Khumalo", Email = "nurse@sidsrh.health", PasswordHash = H("Staff@123"), Role = "Healthcare Staff" },
            new User { UserId = "USR006", FirstName = "Johan", LastName = "van Wyk", Email = "tech@sidsrh.health", PasswordHash = H("Tech@123"), Role = "Technician" },
            new User { UserId = "USR007", FirstName = "Patient", LastName = "Demo", Email = "patient@demo.health", PasswordHash = H("Patient@123"), Role = "Patient" },
            new User { UserId = "USR008", FirstName = "Nelly", LastName = "Mokoena", Email = "nelly@sidsrh.health", PasswordHash = H("Nelly@123"), Role = "Appointment Clerk" }
        );

        db.MobileClinics.AddRange(
            new MobileClinic { ClinicId = "CLN001", ClinicName = "Qumbu Mobile Unit", LocationId = "LOC002", ContactNumber = "047-555-0101" },
            new MobileClinic { ClinicId = "CLN002", ClinicName = "Port St Johns Unit", LocationId = "LOC004", ContactNumber = "047-555-0102" }
        );

        db.Staff.AddRange(
            new Staff { StaffId = "STF001", UserId = "USR005", FirstName = "Lindiwe", LastName = "Khumalo", ContactNumber = "082-555-1001", Role = "Mobile Clinic Nurse", ClinicId = "CLN001" },
            new Staff { StaffId = "STF002", UserId = "USR003", FirstName = "Sipho", LastName = "Ngcobo", ContactNumber = "082-555-1002", Role = "Supply Coordinator", ClinicId = "CLN001" }
        );

        db.Drones.AddRange(
            new Drone { DroneId = "DRN001", DroneName = "Eagle-01", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5890, Longitude = 28.7912, BatteryLevel = 96 },
            new Drone { DroneId = "DRN002", DroneName = "Falcon-02", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5880, Longitude = 28.7924, BatteryLevel = 92 },
            new Drone { DroneId = "DRN003", DroneName = "MedWing-03", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5870, Longitude = 28.7936, BatteryLevel = 88 },
            new Drone { DroneId = "DRN004", DroneName = "Swift-04", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5860, Longitude = 28.7948, BatteryLevel = 84 },
            new Drone { DroneId = "DRN005", DroneName = "Courier-05", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5850, Longitude = 28.7960, BatteryLevel = 80 },
            new Drone { DroneId = "DRN006", DroneName = "Lifter-06", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5840, Longitude = 28.7972, BatteryLevel = 76 },
            new Drone { DroneId = "DRN007", DroneName = "AirMed-07", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5830, Longitude = 28.7984, BatteryLevel = 72 },
            new Drone { DroneId = "DRN008", DroneName = "Nest-08", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5820, Longitude = 28.7996, BatteryLevel = 68 },
            new Drone { DroneId = "DRN009", DroneName = "Beacon-09", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5810, Longitude = 28.8008, BatteryLevel = 64 },
            new Drone { DroneId = "DRN010", DroneName = "Ranger-10", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5800, Longitude = 28.8020, BatteryLevel = 60 },
            new Drone { DroneId = "DRN011", DroneName = "Pulse-11", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5790, Longitude = 28.8032, BatteryLevel = 56 },
            new Drone { DroneId = "DRN012", DroneName = "Aero-12", Status = "Available", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5780, Longitude = 28.8044, BatteryLevel = 52 },
            new Drone { DroneId = "DRN013", DroneName = "Vital-13", Status = "Charging", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5770, Longitude = 28.8056, BatteryLevel = 48 },
            new Drone { DroneId = "DRN014", DroneName = "Harbor-14", Status = "Maintenance", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5760, Longitude = 28.8068, BatteryLevel = 44 },
            new Drone { DroneId = "DRN015", DroneName = "Summit-15", Status = "Maintenance", CurrentLocation = "Drone Hub – Mthatha", Latitude = -31.5750, Longitude = 28.8080, BatteryLevel = 40 }
        );

        var exp = DateTime.UtcNow.AddMonths(8);
        db.MedicalSupplies.AddRange(
            new MedicalSupply { SupplyId = "SUP001", SupplyName = "Cetirizine tablets", SupplyType = "OTC", Category = "Allergy relief", Quantity = 40, ExpiryDate = exp },
            new MedicalSupply { SupplyId = "SUP002", SupplyName = "Oral Rehydration Salts", SupplyType = "OTC", Category = "Hydration", Quantity = 60, ExpiryDate = exp },
            new MedicalSupply { SupplyId = "SUP003", SupplyName = "Paracetamol tablets", SupplyType = "OTC", Category = "Pain and fever relief", Quantity = 80, ExpiryDate = exp },
            new MedicalSupply { SupplyId = "SUP004", SupplyName = "Diarrhoea treatment pack", SupplyType = "OTC", Category = "Digestive health", Quantity = 35, ExpiryDate = exp },
            new MedicalSupply { SupplyId = "SUP005", SupplyName = "Antacid tablets", SupplyType = "OTC", Category = "Digestive relief", Quantity = 50, ExpiryDate = exp },
            new MedicalSupply { SupplyId = "SUP006", SupplyName = "Wound dressing kit", SupplyType = "OTC", Category = "Wound care", Quantity = 45, ExpiryDate = exp },
            new MedicalSupply { SupplyId = "SUP007", SupplyName = "Calamine lotion", SupplyType = "OTC", Category = "Skin irritation relief", Quantity = 30, ExpiryDate = exp },
            new MedicalSupply { SupplyId = "SUP008", SupplyName = "Saline nasal spray", SupplyType = "OTC", Category = "Cold and flu relief", Quantity = 40, ExpiryDate = exp },
            new MedicalSupply { SupplyId = "SUP009", SupplyName = "O-Negative Blood Units", SupplyType = "Clinical", Category = "Blood product", Quantity = 8, ExpiryDate = exp, ColdChain = true },
            new MedicalSupply { SupplyId = "SUP010", SupplyName = "Basic first-aid pack", SupplyType = "OTC", Category = "Wound care", Quantity = 20, ExpiryDate = exp }
        );

        db.AdministrationGuides.AddRange(
            new AdministrationGuide { GuideId = "G1", Category = "Allergy relief", Situation = "Mild allergic reaction / insect sting", Examples = "Cetirizine / loratadine (OTC where appropriate)", Instructions = "General info only — not a diagnosis. Mild: cool cloth, rest, labelled antihistamine if appropriate. Remove stinger by scraping. Call EMS for breathing difficulty, throat/tongue swelling, collapse or known anaphylaxis. If drone ETA exceeds a safe window or distance > 15 km for urgent supply, prioritise EMS and local help." },
            new AdministrationGuide { GuideId = "G2", Category = "Hydration", Situation = "Dehydration", Examples = "ORS", Instructions = "Mix one sachet in clean water. Sip frequently. Seek care if vomiting persists." },
            new AdministrationGuide { GuideId = "G3", Category = "Pain and fever relief", Situation = "Mild pain or fever", Examples = "Paracetamol", Instructions = "Follow age-appropriate dosing on pack. Do not exceed daily maximum." },
            new AdministrationGuide { GuideId = "G4", Category = "Digestive health", Situation = "Diarrhoea", Examples = "ORS + OTC pack", Instructions = "Rehydrate with ORS. Use OTC only as labelled." },
            new AdministrationGuide { GuideId = "G5", Category = "Digestive relief", Situation = "Heartburn", Examples = "Antacids", Instructions = "Chew/swallow as directed after meals." },
            new AdministrationGuide { GuideId = "G6", Category = "Wound care", Situation = "Minor cuts", Examples = "Dressings, antiseptic", Instructions = "Clean wound, apply dressing. Seek care for deep or dirty wounds." },
            new AdministrationGuide { GuideId = "G7", Category = "Skin irritation relief", Situation = "Insect bites", Examples = "Calamine", Instructions = "Apply thinly to affected skin. Avoid eyes." },
            new AdministrationGuide { GuideId = "G8", Category = "Cold and flu relief", Situation = "Nasal congestion", Examples = "Saline spray", Instructions = "Use as directed. Not a substitute for emergency care." }
        );

        db.Equipment.AddRange(
            new Equipment { EquipmentId = "EQP001", EquipmentName = "Portable Ultrasound", EquipmentType = "Diagnostic", Status = "Available", ClinicId = "CLN001" },
            new Equipment { EquipmentId = "EQP002", EquipmentName = "Defibrillator", EquipmentType = "Emergency", Status = "In Use", ClinicId = "CLN001" },
            new Equipment { EquipmentId = "EQP003", EquipmentName = "Cold-Chain Fridge", EquipmentType = "Storage", Status = "Available", ClinicId = "CLN002" }
        );

        db.Deliveries.Add(new Delivery
        {
            DeliveryId = "DEL001",
            DroneId = "DRN001",
            ClinicId = "CLN001",
            SupplyId = "SUP001",
            OriginLocationId = "LOC005",
            DestinationLocationId = "LOC002",
            DeliveryStatus = "Completed",
            DistanceKm = 8.4,
            CalculatedCost = 150,
            ServiceClass = "Urgent",
            DispatchCode = "112233",
            ArrivalCode = "445566",
            RecipientName = "Nomsa Mbeki",
            RecipientPhone = "0835557788",
            DateCreated = DateTime.UtcNow.AddDays(-1),
            DispatchedAt = DateTime.UtcNow.AddDays(-1).AddMinutes(5),
            EstimatedArrival = DateTime.UtcNow.AddDays(-1).AddMinutes(25),
            DateCompleted = DateTime.UtcNow.AddDays(-1).AddMinutes(28)
        });

        db.PatientRequests.Add(new PatientRequest
        {
            RequestId = "REQ001",
            PatientName = "Nomsa Mbeki",
            ContactNumber = "0835557788",
            LocationId = "LOC008",
            MedicalNeed = "Mild allergic reaction – antihistamine",
            Category = "Allergy relief",
            Urgency = "Urgent",
            ServiceClass = "Urgent",
            Status = "Submitted",
            DistanceKm = 4.2,
            EstimatedCost = 230,
            SituationNotes = "Child logged request; adult helper may assist on arrival."
        });

        db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG001",
            UserId = "USR001",
            ActivityType = "SYSTEM_SEED",
            Description = "HealthcareDB seed loaded."
        });

        db.ChargingStations.AddRange(
            new ChargingStation { StationId = "CHG001", StationName = "Mthatha Tower Hub", Latitude = -31.59, Longitude = 28.79, StationType = "Tower", CoverageRadiusKm = 15 },
            new ChargingStation { StationId = "CHG002", StationName = "Qumbu Cell Tower Pad", Latitude = -31.15, Longitude = 28.8667, StationType = "Tower", CoverageRadiusKm = 15 },
            new ChargingStation { StationId = "CHG003", StationName = "Tsolo Underground Cache", Latitude = -31.30, Longitude = 28.75, StationType = "Underground", CoverageRadiusKm = 15 },
            new ChargingStation { StationId = "CHG004", StationName = "Port St Johns Tower", Latitude = -31.6289, Longitude = 29.5369, StationType = "Tower", CoverageRadiusKm = 15 }
        );
        db.SystemWeather.Add(new SystemWeather { Id = 1, Condition = "Clear", Note = "Default fair weather" });
        db.PatientProfiles.AddRange(
            new PatientProfile { PatientId = "PAT001", FullName = "Nomsa Mbeki", ContactNumber = "0835557788", Address = "Qumbu Village Stand 12", LocationId = "LOC008", Notes = "Chronic hypertension", Source = "Internal", CreatedByUserId = "USR005", CreatedAt = new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc) },
            new PatientProfile { PatientId = "PAT002", FullName = "Thandiwe Nkosi", ContactNumber = "0821112233", Address = "Ngceleni Access Point", LocationId = "LOC009", Notes = "Diabetes follow-up", Source = "Internal", CreatedByUserId = "USR005", CreatedAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Utc) },
            new PatientProfile { PatientId = "PAT003", FullName = "Sipho Dlamini", ContactNumber = "0842223344", Address = "Tsolo Clinic Road", LocationId = "LOC003", Notes = "Asthma management", Source = "Internal", CreatedByUserId = "USR005", CreatedAt = new DateTime(2026, 8, 14, 11, 0, 0, DateTimeKind.Utc) },
            new PatientProfile { PatientId = "PAT004", FullName = "Lerato Molefe", ContactNumber = "0813334455", Address = "Port St Johns", LocationId = "LOC004", Notes = "Antenatal support", Source = "Internal", CreatedByUserId = "USR005", CreatedAt = new DateTime(2026, 8, 15, 8, 30, 0, DateTimeKind.Utc) },
            new PatientProfile { PatientId = "PAT005", FullName = "Bongani Cele", ContactNumber = "0794445566", Address = "Mthatha outer", LocationId = "LOC001", Notes = "Post-surgery follow-up", Source = "Internal", CreatedByUserId = "USR005", CreatedAt = new DateTime(2026, 8, 18, 14, 0, 0, DateTimeKind.Utc) },
            new PatientProfile { PatientId = "PAT006", FullName = "Anele Jacobs", ContactNumber = "0765556677", Address = "Qumbu East", LocationId = "LOC002", Notes = "Hypertension and lipids", Source = "External", CreatedByUserId = "USR002", CreatedAt = new DateTime(2026, 8, 20, 9, 15, 0, DateTimeKind.Utc) },
            new PatientProfile { PatientId = "PAT007", FullName = "Karabo Sithole", ContactNumber = "0726667788", Address = "Tsolo South", LocationId = "LOC003", Notes = "Chronic medication schedule", Source = "External", CreatedByUserId = "USR002", CreatedAt = new DateTime(2026, 8, 22, 16, 0, 0, DateTimeKind.Utc) }
        );
        // Three children under patients 1 and 2 (split)
        db.PatientDependents.AddRange(
            new PatientDependent { DependentId = "DEP001", PatientId = "PAT001", FullName = "Zanele Mbeki", DateOfBirth = new DateTime(2018, 3, 12), Relationship = "Child", Notes = "Paediatric clinic" },
            new PatientDependent { DependentId = "DEP002", PatientId = "PAT001", FullName = "Lindiwe Mbeki", DateOfBirth = new DateTime(2021, 7, 5), Relationship = "Child", Notes = "Immunisation schedule" },
            new PatientDependent { DependentId = "DEP003", PatientId = "PAT002", FullName = "Mpho Nkosi", DateOfBirth = new DateTime(2019, 11, 20), Relationship = "Child", Notes = "School clinic visits" }
        );
        db.ChronicSchedules.Add(new ChronicSchedule {
            ScheduleId = "SCH001", PatientId = "PAT001", SupplyId = "SUP003", ClinicId = "CLN001",
            IntervalDays = 30, NextDueDate = DateTime.UtcNow.Date.AddDays(30), PreferredSlot = "09:00",
            Notes = "Monthly chronic refill"
        });
        db.ClinicAppointments.AddRange(
            new ClinicAppointment { AppointmentId = "APT001", PatientId = "PAT001", PatientName = "Nomsa Mbeki", AppointmentAt = DateTime.UtcNow.Date.AddDays(14).AddHours(9), ClinicId = "CLN001", ReasonBrief = "BP checkup", Status = "Scheduled" },
            new ClinicAppointment { AppointmentId = "APT002", PatientId = "PAT001", PatientName = "Zanele Mbeki", AppointmentAt = DateTime.UtcNow.Date.AddDays(7).AddHours(10), ClinicId = "CLN001", ReasonBrief = "Child wellness", Status = "Scheduled" },
            new ClinicAppointment { AppointmentId = "APT003", PatientId = "PAT002", PatientName = "Mpho Nkosi", AppointmentAt = DateTime.UtcNow.Date.AddDays(10).AddHours(11), ClinicId = "CLN001", ReasonBrief = "Paediatric follow-up", Status = "Scheduled" }
        );
        db.SaveChanges();
        SeedHistoricalActivity(db);
    }

    // Random realistic activity from 12 Aug 2026 through now (demo audit trail)
    
    // Keep demonstration account passwords consistent with documented credentials
        public static void EnsureKnownPasswords(AppDbContext db)
    {
        string H(string p) => BCrypt.Net.BCrypt.HashPassword(p);
        var accounts = new (string Id, string First, string Last, string Email, string Pass, string Role)[]
        {
            ("USR001", "Thabo", "Mokoena", "admin@sidsrh.health", "Admin@123", "Administrator"),
            ("USR002", "Naledi", "Dlamini", "manager@sidsrh.health", "Manager@123", "Manager"),
            ("USR003", "Sipho", "Ngcobo", "supply@sidsrh.health", "Supply@123", "Supply Coordinator"),
            ("USR004", "Aisha", "Patel", "emergency@sidsrh.health", "Emerg@123", "Emergency Coordinator"),
            ("USR005", "Lindiwe", "Khumalo", "nurse@sidsrh.health", "Staff@123", "Healthcare Staff"),
            ("USR006", "Johan", "van Wyk", "tech@sidsrh.health", "Tech@123", "Technician"),
            ("USR007", "Patient", "Demo", "patient@demo.health", "Patient@123", "Patient"),
            ("USR008", "Nelly", "Mokoena", "nelly@sidsrh.health", "Nelly@123", "Appointment Clerk")
        };
        foreach (var a in accounts)
        {
            var u = db.Users.FirstOrDefault(x => x.Email == a.Email);
            if (u == null)
            {
                db.Users.Add(new User
                {
                    UserId = a.Id,
                    FirstName = a.First,
                    LastName = a.Last,
                    Email = a.Email,
                    PasswordHash = H(a.Pass),
                    Role = a.Role,
                    Status = "Active"
                });
            }
            else
            {
                u.PasswordHash = H(a.Pass);
                u.Role = a.Role;
                u.Status = "Active";
                u.FirstName = a.First;
                u.LastName = a.Last;
                u.Email = a.Email;
            }
        }
        db.SaveChanges();
    }

    public static void SeedHistoricalActivity(AppDbContext db)
    {
        if (db.ActivityLogs.Any(a => a.ActivityType == "HIST_SEED")) return;

        var rng = new Random(20260812);
        var users = new[] { "USR001", "USR002", "USR003", "USR004", "USR005", "USR006", "USR007", "SYSTEM" };
        var types = new[]
        {
            "LOGIN", "LOGOUT", "PATIENT_REQUEST", "DELIVERY_CREATED", "DISPATCH_CONFIRMED",
            "ARRIVAL_CONFIRMED", "CLINIC_DISPENSE", "SUPPLY_ADJUST", "DRONE_MAINTENANCE",
            "BACKUP_SYNC", "PASSWORD_RESET_REQUEST", "STAFF_UPDATE", "WEATHER_UPDATE"
        };
        var descs = new[]
        {
            "Signed in from clinic workstation.",
            "Patient request submitted for chronic refill.",
            "Delivery assigned to drone fleet.",
            "Dispatch code confirmed — package in transit.",
            "Arrival code accepted — parcel unlocked.",
            "Clinic counter dispense recorded.",
            "Supply stock level adjusted after delivery.",
            "Drone marked for scheduled maintenance.",
            "Live database backup snapshot created.",
            "Password reset OTP issued.",
            "Staff status set Active.",
            "Weather note updated for flight planning.",
            "Emergency request triaged within 15 km radius.",
            "Charging station status checked.",
            "Patient profile notes updated."
        };

        var start = new DateTime(2026, 8, 12, 6, 0, 0, DateTimeKind.Utc);
        var end = DateTime.UtcNow;
        if (end < start) end = start.AddDays(30);

        var list = new List<ActivityLog>();
        // ~3–6 events per day
        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        {
            var n = rng.Next(3, 7);
            for (var i = 0; i < n; i++)
            {
                var ts = day.AddHours(rng.Next(5, 21)).AddMinutes(rng.Next(0, 60)).AddSeconds(rng.Next(0, 60));
                if (ts > end) ts = end.AddMinutes(-rng.Next(1, 120));
                list.Add(new ActivityLog
                {
                    LogId = "LOG-H" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                    UserId = users[rng.Next(users.Length)],
                    ActivityType = i == 0 && day == start.Date ? "HIST_SEED" : types[rng.Next(types.Length)],
                    Description = descs[rng.Next(descs.Length)],
                    Timestamp = DateTime.SpecifyKind(ts, DateTimeKind.Utc)
                });
            }
        }
        // ensure marker exists
        if (!list.Any(x => x.ActivityType == "HIST_SEED"))
        {
            list.Insert(0, new ActivityLog
            {
                LogId = "LOG-HSEED01",
                UserId = "SYSTEM",
                ActivityType = "HIST_SEED",
                Description = "Historical activity seed from 2026-08-12.",
                Timestamp = start
            });
        }
        db.ActivityLogs.AddRange(list);
        db.SaveChanges();
    }
}


