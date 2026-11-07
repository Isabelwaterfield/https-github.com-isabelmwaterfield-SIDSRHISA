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
    public DbSet<PatientDependent> PatientDependents => Set<PatientDependent>();
    public DbSet<ExternalRegistration> ExternalRegistrations => Set<ExternalRegistration>();
    public DbSet<ChronicSchedule> ChronicSchedules => Set<ChronicSchedule>();
    public DbSet<ChargingStation> ChargingStations => Set<ChargingStation>();
    public DbSet<SystemWeather> SystemWeather => Set<SystemWeather>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<ClinicAppointment> ClinicAppointments => Set<ClinicAppointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(x => x.UserId);
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Location>().HasKey(x => x.LocationId);
        modelBuilder.Entity<MobileClinic>().HasKey(x => x.ClinicId);
        modelBuilder.Entity<Staff>().HasKey(x => x.StaffId);
        modelBuilder.Entity<Drone>().HasKey(x => x.DroneId);
        modelBuilder.Entity<MedicalSupply>().HasKey(x => x.SupplyId);
        modelBuilder.Entity<Delivery>().HasKey(x => x.DeliveryId);
        modelBuilder.Entity<PatientRequest>().HasKey(x => x.RequestId);
        modelBuilder.Entity<Equipment>().HasKey(x => x.EquipmentId);
        modelBuilder.Entity<AdministrationGuide>().HasKey(x => x.GuideId);
        modelBuilder.Entity<ActivityLog>().HasKey(x => x.LogId);
        modelBuilder.Entity<DispenseRecord>().HasKey(x => x.DispenseId);
        modelBuilder.Entity<PatientProfile>().HasKey(x => x.PatientId);
        modelBuilder.Entity<ChronicSchedule>().HasKey(x => x.ScheduleId);
        modelBuilder.Entity<ChargingStation>().HasKey(x => x.StationId);
        modelBuilder.Entity<SystemWeather>().HasKey(x => x.Id);
        modelBuilder.Entity<OtpChallenge>().HasKey(x => x.OtpId);
        modelBuilder.Entity<ClinicAppointment>().HasKey(x => x.AppointmentId);
        modelBuilder.Entity<SystemWeather>().Property(x => x.Id).ValueGeneratedNever();
        modelBuilder.Entity<Delivery>().Property(x => x.ActualCost).HasPrecision(18, 2);
        modelBuilder.Entity<Delivery>().Property(x => x.CalculatedCost).HasPrecision(18, 2);
        modelBuilder.Entity<Delivery>().Property(x => x.DelayFine).HasPrecision(18, 2);
        modelBuilder.Entity<Delivery>().Property(x => x.PatientFee).HasPrecision(18, 2);
        modelBuilder.Entity<PatientRequest>().Property(x => x.EstimatedCost).HasPrecision(18, 2);
    }
}

