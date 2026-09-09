using Microsoft.EntityFrameworkCore;
using SIDSRH.Api.Models;

namespace SIDSRH.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<MobileClinic> MobileClinics => Set<MobileClinic>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Drone> Drones => Set<Drone>();
    public DbSet<MedicalSupply> MedicalSupplies => Set<MedicalSupply>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<PatientRequest> PatientRequests => Set<PatientRequest>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<AdministrationGuide> AdministrationGuides => Set<AdministrationGuide>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<DispenseRecord> DispenseRecords => Set<DispenseRecord>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<ChronicSchedule> ChronicSchedules => Set<ChronicSchedule>();
    public DbSet<ChargingStation> ChargingStations => Set<ChargingStation>();
    public DbSet<SystemWeather> SystemWeather => Set<SystemWeather>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<ClinicAppointment> ClinicAppointments => Set<ClinicAppointment>();
    public DbSet<EmergencyScenario> EmergencyScenarios => Set<EmergencyScenario>();
    public DbSet<Simulation> Simulations => Set<Simulation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Existing HealthcareDB tables (snake_case) ---
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.FirstName).HasColumnName("first_name");
            e.Property(x => x.LastName).HasColumnName("last_name");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.JobTitle).HasColumnName("job_title");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.FailedLoginCount).HasColumnName("failed_login_count");
            e.Property(x => x.MfaEnabled).HasColumnName("mfa_enabled");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Location>(e =>
        {
            e.ToTable("locations");
            e.HasKey(x => x.LocationId);
            e.Property(x => x.LocationId).HasColumnName("location_id");
            e.Property(x => x.LocationName).HasColumnName("location_name");
            e.Property(x => x.Latitude).HasColumnName("latitude");
            e.Property(x => x.Longitude).HasColumnName("longitude");
            e.Property(x => x.LocationType).HasColumnName("location_type");
        });

        modelBuilder.Entity<MobileClinic>(e =>
        {
            e.ToTable("mobile_clinics");
            e.HasKey(x => x.ClinicId);
            e.Property(x => x.ClinicId).HasColumnName("clinic_id");
            e.Property(x => x.ClinicName).HasColumnName("clinic_name");
            e.Property(x => x.LocationId).HasColumnName("location_id");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.ContactNumber).HasColumnName("contact_number");
        });

        modelBuilder.Entity<Staff>(e =>
        {
            e.ToTable("staff");
            e.HasKey(x => x.StaffId);
            e.Property(x => x.StaffId).HasColumnName("staff_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.FirstName).HasColumnName("first_name");
            e.Property(x => x.LastName).HasColumnName("last_name");
            e.Property(x => x.ContactNumber).HasColumnName("contact_number");
            e.Property(x => x.Role).HasColumnName("role");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.ClinicId).HasColumnName("clinic_id");
        });

        modelBuilder.Entity<Drone>(e =>
        {
            e.ToTable("drones");
            e.HasKey(x => x.DroneId);
            e.Property(x => x.DroneId).HasColumnName("drone_id");
            e.Property(x => x.DroneName).HasColumnName("drone_name");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CurrentLocationId).HasColumnName("current_location_id");
            e.Property(x => x.CurrentLocation).HasColumnName("current_location");
            e.Property(x => x.Latitude).HasColumnName("latitude");
            e.Property(x => x.Longitude).HasColumnName("longitude");
            e.Property(x => x.BatteryLevel).HasColumnName("battery_level");
            e.Property(x => x.MaxPayloadKg).HasColumnName("max_payload_kg");
            e.Property(x => x.SpeedKmh).HasColumnName("speed_kmh");
            e.Property(x => x.HasCamera).HasColumnName("has_camera");
            e.Property(x => x.HasVoice).HasColumnName("has_voice");
        });

        modelBuilder.Entity<MedicalSupply>(e =>
        {
            e.ToTable("medical_supplies");
            e.HasKey(x => x.SupplyId);
            e.Property(x => x.SupplyId).HasColumnName("supply_id");
            e.Property(x => x.SupplyName).HasColumnName("supply_name");
            e.Property(x => x.SupplyType).HasColumnName("supply_type");
            e.Property(x => x.Category).HasColumnName("category");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
            e.Property(x => x.ColdChain).HasColumnName("cold_chain");
        });

        modelBuilder.Entity<Delivery>(e =>
        {
            e.ToTable("deliveries");
            e.HasKey(x => x.DeliveryId);
            e.Property(x => x.DeliveryId).HasColumnName("delivery_id");
            e.Property(x => x.DroneId).HasColumnName("drone_id");
            e.Property(x => x.ClinicId).HasColumnName("clinic_id");
            e.Property(x => x.SupplyId).HasColumnName("supply_id");
            e.Property(x => x.OriginLocationId).HasColumnName("origin_location_id");
            e.Property(x => x.DestinationLocationId).HasColumnName("destination_location_id");
            e.Property(x => x.DeliveryStatus).HasColumnName("delivery_status");
            e.Property(x => x.DistanceKm).HasColumnName("distance_km");
            e.Property(x => x.CalculatedCost).HasColumnName("calculated_cost");
            e.Property(x => x.ServiceClass).HasColumnName("service_class");
            e.Property(x => x.DispatchCode).HasColumnName("dispatch_code");
            e.Property(x => x.ArrivalCode).HasColumnName("arrival_code");
            e.Property(x => x.RecipientPhone).HasColumnName("recipient_phone");
            e.Property(x => x.RecipientName).HasColumnName("recipient_name");
            e.Property(x => x.DateCreated).HasColumnName("date_created");
            e.Property(x => x.DispatchedAt).HasColumnName("dispatched_at");
            e.Property(x => x.EstimatedArrival).HasColumnName("estimated_arrival");
            e.Property(x => x.DateCompleted).HasColumnName("date_completed");
            e.Property(x => x.PatientRequestId).HasColumnName("patient_request_id");
            e.Property(x => x.PatientFee).HasColumnName("patient_fee");
            e.Property(x => x.ActualCost).HasColumnName("actual_cost");
            e.Property(x => x.WeatherHoldStationId).HasColumnName("weather_hold_station_id");
            e.Property(x => x.DeliveryNote).HasColumnName("delivery_note");
            e.Property(x => x.CollectionDeadline).HasColumnName("collection_deadline");
            e.Property(x => x.DelayFine).HasColumnName("delay_fine");
            e.Property(x => x.BriefReason).HasColumnName("brief_reason");
            e.Property(x => x.TechnicianName).HasColumnName("technician_name");
            e.Property(x => x.PreferredSlot).HasColumnName("preferred_slot");
        });

        modelBuilder.Entity<PatientRequest>(e =>
        {
            e.ToTable("patient_requests");
            e.HasKey(x => x.RequestId);
            e.Property(x => x.RequestId).HasColumnName("request_id");
            e.Property(x => x.PatientName).HasColumnName("patient_name");
            e.Property(x => x.ContactNumber).HasColumnName("contact_number");
            e.Property(x => x.LocationId).HasColumnName("location_id");
            e.Property(x => x.MedicalNeed).HasColumnName("medical_need");
            e.Property(x => x.Category).HasColumnName("category");
            e.Property(x => x.Urgency).HasColumnName("urgency");
            e.Property(x => x.ServiceClass).HasColumnName("service_class");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.DistanceKm).HasColumnName("distance_km");
            e.Property(x => x.EstimatedCost).HasColumnName("estimated_cost");
            e.Property(x => x.AssignedDeliveryId).HasColumnName("assigned_delivery_id");
            e.Property(x => x.RequestResponder).HasColumnName("request_responder");
            e.Property(x => x.HelperName).HasColumnName("helper_name");
            e.Property(x => x.HelperContact).HasColumnName("helper_contact");
            e.Property(x => x.SituationNotes).HasColumnName("situation_notes");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Equipment>(e =>
        {
            e.ToTable("equipment");
            e.HasKey(x => x.EquipmentId);
            e.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            e.Property(x => x.EquipmentName).HasColumnName("equipment_name");
            e.Property(x => x.EquipmentType).HasColumnName("equipment_type");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.ClinicId).HasColumnName("clinic_id");
        });

        modelBuilder.Entity<ActivityLog>(e =>
        {
            e.ToTable("activity_logs");
            e.HasKey(x => x.LogId);
            e.Property(x => x.LogId).HasColumnName("log_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.ActivityType).HasColumnName("activity_type");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Timestamp).HasColumnName("timestamp");
        });

        // --- New tables (created by merge script) ---
        modelBuilder.Entity<AdministrationGuide>(e =>
        {
            e.ToTable("administration_guides");
            e.HasKey(x => x.GuideId);
            e.Property(x => x.GuideId).HasColumnName("guide_id");
            e.Property(x => x.Category).HasColumnName("category");
            e.Property(x => x.Situation).HasColumnName("situation");
            e.Property(x => x.Examples).HasColumnName("examples");
            e.Property(x => x.Instructions).HasColumnName("instructions");
        });

        modelBuilder.Entity<DispenseRecord>(e =>
        {
            e.ToTable("dispense_records");
            e.HasKey(x => x.DispenseId);
            e.Property(x => x.DispenseId).HasColumnName("dispense_id");
            e.Property(x => x.ClinicId).HasColumnName("clinic_id");
            e.Property(x => x.SupplyId).HasColumnName("supply_id");
            e.Property(x => x.PatientName).HasColumnName("patient_name");
            e.Property(x => x.PatientId).HasColumnName("patient_id");
            e.Property(x => x.TargetType).HasColumnName("target_type");
            e.Property(x => x.ToClinicId).HasColumnName("to_clinic_id");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.DispensedAt).HasColumnName("dispensed_at");
            e.Property(x => x.StaffUserId).HasColumnName("staff_user_id");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.VerifiedByUserId).HasColumnName("verified_by_user_id");
        });

        modelBuilder.Entity<PatientProfile>(e =>
        {
            e.ToTable("patient_profiles");
            e.HasKey(x => x.PatientId);
            e.Property(x => x.PatientId).HasColumnName("patient_id");
            e.Property(x => x.FullName).HasColumnName("full_name");
            e.Property(x => x.ContactNumber).HasColumnName("contact_number");
            e.Property(x => x.IdNumber).HasColumnName("id_number");
            e.Property(x => x.LocationId).HasColumnName("location_id");
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        });

        modelBuilder.Entity<ChronicSchedule>(e =>
        {
            e.ToTable("chronic_schedules");
            e.HasKey(x => x.ScheduleId);
            e.Property(x => x.ScheduleId).HasColumnName("schedule_id");
            e.Property(x => x.PatientId).HasColumnName("patient_id");
            e.Property(x => x.SupplyId).HasColumnName("supply_id");
            e.Property(x => x.ClinicId).HasColumnName("clinic_id");
            e.Property(x => x.IntervalDays).HasColumnName("interval_days");
            e.Property(x => x.NextDueDate).HasColumnName("next_due_date");
            e.Property(x => x.PreferredSlot).HasColumnName("preferred_slot");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.Notes).HasColumnName("notes");
        });

        modelBuilder.Entity<ChargingStation>(e =>
        {
            e.ToTable("charging_stations");
            e.HasKey(x => x.StationId);
            e.Property(x => x.StationId).HasColumnName("station_id");
            e.Property(x => x.StationName).HasColumnName("station_name");
            e.Property(x => x.Latitude).HasColumnName("latitude");
            e.Property(x => x.Longitude).HasColumnName("longitude");
            e.Property(x => x.StationType).HasColumnName("station_type");
            e.Property(x => x.CoverageRadiusKm).HasColumnName("coverage_radius_km");
            e.Property(x => x.Status).HasColumnName("status");
        });

        modelBuilder.Entity<SystemWeather>(e =>
        {
            e.ToTable("system_weather");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Condition).HasColumnName("condition");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Note).HasColumnName("note");
        });

        modelBuilder.Entity<OtpChallenge>(e =>
        {
            e.ToTable("otp_challenges");
            e.HasKey(x => x.OtpId);
            e.Property(x => x.OtpId).HasColumnName("otp_id");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.CodeHash).HasColumnName("code_hash");
            e.Property(x => x.Purpose).HasColumnName("purpose");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.Used).HasColumnName("used");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<EmergencyScenario>(e =>
        {
            e.ToTable("emergency_scenarios");
            e.HasKey(x => x.ScenarioId);
            e.Property(x => x.ScenarioId).HasColumnName("scenario_id");
            e.Property(x => x.ScenarioName).HasColumnName("scenario_name");
            e.Property(x => x.EmergencyType).HasColumnName("emergency_type");
            e.Property(x => x.Priority).HasColumnName("priority");
            e.Property(x => x.StartingLocationId).HasColumnName("starting_location_id");
            e.Property(x => x.DestinationLocationId).HasColumnName("destination_location_id");
            e.Property(x => x.Description).HasColumnName("description");
        });

        modelBuilder.Entity<Simulation>(e =>
        {
            e.ToTable("simulations");
            e.HasKey(x => x.SimulationId);
            e.Property(x => x.SimulationId).HasColumnName("simulation_id");
            e.Property(x => x.ScenarioId).HasColumnName("scenario_id");
            e.Property(x => x.SelectedDroneId).HasColumnName("selected_drone_id");
            e.Property(x => x.StartTime).HasColumnName("start_time");
            e.Property(x => x.EndTime).HasColumnName("end_time");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.DistanceKm).HasColumnName("distance_km");
            e.Property(x => x.EstimatedResponseTime).HasColumnName("estimated_response_time");
            e.Property(x => x.Result).HasColumnName("result");
        });

        modelBuilder.Entity<ClinicAppointment>(e =>
        {
            e.ToTable("clinic_appointments");
            e.HasKey(x => x.AppointmentId);
            e.Property(x => x.AppointmentId).HasColumnName("appointment_id");
            e.Property(x => x.PatientId).HasColumnName("patient_id");
            e.Property(x => x.PatientName).HasColumnName("patient_name");
            e.Property(x => x.AppointmentAt).HasColumnName("appointment_at");
            e.Property(x => x.ClinicId).HasColumnName("clinic_id");
            e.Property(x => x.ReasonBrief).HasColumnName("reason_brief");
            e.Property(x => x.Status).HasColumnName("status");
        });
    }
}
