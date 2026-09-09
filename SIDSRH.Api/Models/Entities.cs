using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIDSRH.Api.Models;

public class User
{
    [Key] public string UserId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Healthcare Staff";
    public string Status { get; set; } = "Active";
    public int FailedLoginCount { get; set; }
    public bool MfaEnabled { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Location
{
    [Key] public string LocationId { get; set; } = "";
    public string LocationName { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string LocationType { get; set; } = "Clinic";
}

public class MobileClinic
{
    [Key] public string ClinicId { get; set; } = "";
    public string ClinicName { get; set; } = "";
    public string LocationId { get; set; } = "";
    public string Status { get; set; } = "Active";
    public string? ContactNumber { get; set; }
}

public class Staff
{
    [Key] public string StaffId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? ContactNumber { get; set; }
    public string JobTitle { get; set; } = "";
    public string Status { get; set; } = "Active";
    public string? ClinicId { get; set; }
}

public class Drone
{
    [Key] public string DroneId { get; set; } = "";
    public string DroneName { get; set; } = "";
    public string Status { get; set; } = "Available";
    public string? CurrentLocationId { get; set; }
    public string CurrentLocation { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int BatteryLevel { get; set; } = 100;
    public double SpeedKmh { get; set; } = 60;
    public double MaxPayloadKg { get; set; } = 4.5;
    public bool HasCamera { get; set; } = true;
    public bool HasVoice { get; set; } = true;
}

public class MedicalSupply
{
    [Key] public string SupplyId { get; set; } = "";
    public string SupplyName { get; set; } = "";
    public string SupplyType { get; set; } = "";
    public string Category { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool ColdChain { get; set; }
}

public class Delivery
{
    [Key] public string DeliveryId { get; set; } = "";
    public string? DroneId { get; set; }
    public string? ClinicId { get; set; }
    public string? SupplyId { get; set; }
    public string OriginLocationId { get; set; } = "";
    public string DestinationLocationId { get; set; } = "";
    public string DeliveryStatus { get; set; } = "Pending";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DateCompleted { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public DateTime? CollectionDeadline { get; set; }
    public double DistanceKm { get; set; }
    public decimal CalculatedCost { get; set; }
    public string ServiceClass { get; set; } = "Routine";
    public string? DispatchCode { get; set; }
    public string? ArrivalCode { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? BriefReason { get; set; }
    public string? TechnicianName { get; set; }
    public decimal PatientFee { get; set; }
    public decimal DelayFine { get; set; }
    public decimal ActualCost { get; set; }
    public string? PreferredSlot { get; set; }
    public string? WeatherHoldStationId { get; set; }
    public string? DeliveryNote { get; set; }
    public string? PatientRequestId { get; set; }
}

public class Equipment
{
    [Key] public string EquipmentId { get; set; } = "";
    public string EquipmentName { get; set; } = "";
    public string EquipmentType { get; set; } = "";
    public string Status { get; set; } = "Available";
    public string? ClinicId { get; set; }
    public string? LocationId { get; set; }
}

public class EmergencyScenario
{
    [Key] public string ScenarioId { get; set; } = "";
    public string ScenarioName { get; set; } = "";
    public string EmergencyType { get; set; } = "";
    public string Priority { get; set; } = "High";
    public string StartingLocationId { get; set; } = "";
    public string DestinationLocationId { get; set; } = "";
    public string? Description { get; set; }
}

public class Simulation
{
    [Key] public string SimulationId { get; set; } = "";
    public string ScenarioId { get; set; } = "";
    public string? SelectedDroneId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "Pending";
    public double? DistanceKm { get; set; }
    public int? EstimatedResponseTime { get; set; }
    public string? Result { get; set; }
}

public class ActivityLog
{
    [Key] public string LogId { get; set; } = "";
    public string? UserId { get; set; }
    public string ActivityType { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PatientRequest
{
    [Key] public string RequestId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string? ContactNumber { get; set; }
    public string LocationId { get; set; } = "";
    public string MedicalNeed { get; set; } = "";
    public string Category { get; set; } = "";
    public string Urgency { get; set; } = "High";
    public string ServiceClass { get; set; } = "Urgent";
    public string Status { get; set; } = "Submitted";
    public double? DistanceKm { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? AssignedDeliveryId { get; set; }
    public bool RequestResponder { get; set; }
    public string? HelperName { get; set; }
    public string? HelperContact { get; set; }
    public string? SituationNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DispenseRecord
{
    [Key] public string DispenseId { get; set; } = "";
    public string ClinicId { get; set; } = "";
    public string SupplyId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string? PatientId { get; set; }
    public string TargetType { get; set; } = "Patient";
    public string? ToClinicId { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = "PendingVerify";
    public string? StaffUserId { get; set; }
    public string? VerifiedByUserId { get; set; }
    public DateTime DispensedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}

public class PatientProfile
{
    [Key] public string PatientId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string? IdNumber { get; set; }
    public string? LocationId { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChronicSchedule
{
    [Key] public string ScheduleId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string SupplyId { get; set; } = "";
    public string ClinicId { get; set; } = "";
    public int IntervalDays { get; set; } = 30;
    public DateTime NextDueDate { get; set; }
    public string PreferredSlot { get; set; } = "09:00";
    public string Status { get; set; } = "Active";
}

public class ChargingStation
{
    [Key] public string StationId { get; set; } = "";
    public string StationName { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string StationType { get; set; } = "Tower";
    public int CoverageRadiusKm { get; set; } = 15;
    public string Status { get; set; } = "Operational";
}

public class SystemWeather
{
    [Key] public int Id { get; set; } = 1;
    public string Condition { get; set; } = "Clear";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}

public class ClinicAppointment
{
    [Key] public string AppointmentId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public DateTime AppointmentAt { get; set; }
    public string ClinicId { get; set; } = "";
    public string ReasonBrief { get; set; } = "Follow-up checkup";
    public string Status { get; set; } = "Scheduled";
}

public class OtpChallenge
{
    [Key] public string OtpId { get; set; } = "";
    public string Email { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public string Purpose { get; set; } = "Mfa";
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AdministrationGuide
{
    [Key] public string GuideId { get; set; } = "";
    public string Category { get; set; } = "";
    public string Situation { get; set; } = "";
    public string Examples { get; set; } = "";
    public string Instructions { get; set; } = "";
}
