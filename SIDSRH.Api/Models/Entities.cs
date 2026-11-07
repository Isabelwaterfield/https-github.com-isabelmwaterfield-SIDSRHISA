using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SIDSRH.Api.Models;

public class User
{
    public string UserId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Patient";
    public string Status { get; set; } = "Active";
    public bool MfaEnabled { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Location
{
    public string LocationId { get; set; } = "";
    public string LocationName { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string LocationType { get; set; } = "";
}

public class MobileClinic
{
    public string ClinicId { get; set; } = "";
    public string ClinicName { get; set; } = "";
    public string LocationId { get; set; } = "";
    public string Status { get; set; } = "Operational";
    public string? ContactNumber { get; set; }
}

public class Staff
{
    public string StaffId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? ContactNumber { get; set; }
    public string Role { get; set; } = "";
    public string Status { get; set; } = "Active";
    public string? ClinicId { get; set; }
}

public class Drone
{
    public string DroneId { get; set; } = "";
    public string DroneName { get; set; } = "";
    public string Status { get; set; } = "Available";
    public string CurrentLocation { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int BatteryLevel { get; set; } = 100;
    public double MaxPayloadKg { get; set; } = 4.5;
    public double SpeedKmh { get; set; } = 80;
    public bool HasCamera { get; set; } = true;
    public bool HasVoice { get; set; } = true;
}

public class MedicalSupply
{
    public string SupplyId { get; set; } = "";
    public string SupplyName { get; set; } = "";
    public string SupplyType { get; set; } = "";
    public string Category { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool ColdChain { get; set; }
}

public class Delivery
{
    public string DeliveryId { get; set; } = "";
    public string DroneId { get; set; } = "";
    public string ClinicId { get; set; } = "";
    public string SupplyId { get; set; } = "";
    public string OriginLocationId { get; set; } = "";
    public string DestinationLocationId { get; set; } = "";
    public string DeliveryStatus { get; set; } = "Pending";
    public double DistanceKm { get; set; }
    public decimal CalculatedCost { get; set; }
    public string ServiceClass { get; set; } = "Routine"; // Urgent | Routine
    public string? DispatchCode { get; set; }
    public string? ArrivalCode { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientName { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAt { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public DateTime? DateCompleted { get; set; }
    public string? PatientRequestId { get; set; }
    public decimal PatientFee { get; set; }
    public decimal ActualCost { get; set; }
    public string? WeatherHoldStationId { get; set; }
    public string? DeliveryNote { get; set; }
    public DateTime? CollectionDeadline { get; set; }
    public decimal DelayFine { get; set; }
    public string? BriefReason { get; set; }
    public string? TechnicianName { get; set; }
}

public class PatientRequest
{
    public string RequestId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string? ContactNumber { get; set; }
    public string LocationId { get; set; } = "";
    public string MedicalNeed { get; set; } = "";
    public string Category { get; set; } = "";
    public string Urgency { get; set; } = "Urgent";
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

public class Equipment
{
    public string EquipmentId { get; set; } = "";
    public string EquipmentName { get; set; } = "";
    public string EquipmentType { get; set; } = "";
    public string Status { get; set; } = "Available";
    public string? ClinicId { get; set; }
}

public class AdministrationGuide
{
    public string GuideId { get; set; } = "";
    public string Category { get; set; } = "";
    public string Situation { get; set; } = "";
    public string Examples { get; set; } = "";
    public string Instructions { get; set; } = "";
}

public class ActivityLog
{
    public string LogId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string ActivityType { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class DispenseRecord
{
    public string DispenseId { get; set; } = "";
    public string ClinicId { get; set; } = "";
    public string SupplyId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string? PatientId { get; set; }
    public string TargetType { get; set; } = "Patient"; // Patient | ClinicTransfer
    public string? ToClinicId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public DateTime DispensedAt { get; set; } = DateTime.UtcNow;
    public string? StaffUserId { get; set; }
    public string Status { get; set; } = "Completed"; // PendingVerification | Completed
    public string? VerifiedByUserId { get; set; }
}


public class PatientProfile
{
    [Key]
    public string PatientId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string? IdNumber { get; set; }
    public string? Address { get; set; }
    public string? LocationId { get; set; }
    public string? Notes { get; set; }
    public string Source { get; set; } = "Internal"; // Internal | External
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
}

public class PatientDependent
{
    [Key]
    public string DependentId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string FullName { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    public string Relationship { get; set; } = "Child";
    public string? Notes { get; set; }
}

public class ExternalRegistration
{
    [Key]
    public string RegistrationId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? IdNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Address { get; set; } = "";
    public string Status { get; set; } = "Pending"; // Pending | Verified | Rejected
    public string? MatchedPatientId { get; set; }
    public string? Notes { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedByUserId { get; set; }
    public string? PreferredCallDay { get; set; }  // Monday-Friday
    public string? PreferredCallTime { get; set; } // within 08:30-17:00
}

public class ChronicSchedule
{
    [Key]
    public string ScheduleId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string SupplyId { get; set; } = "";
    public string ClinicId { get; set; } = "";
    public int IntervalDays { get; set; } = 30;
    public DateTime NextDueDate { get; set; }
    public string PreferredSlot { get; set; } = "09:00"; // 06:30 | 09:00 | 16:00
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
}

public class ChargingStation
{
    [Key]
    public string StationId { get; set; } = "";
    public string StationName { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string StationType { get; set; } = "Tower"; // Tower | Underground
    public int CoverageRadiusKm { get; set; } = 15;
    public string Status { get; set; } = "Operational";
}

public class SystemWeather
{
    [Key]
    public int Id { get; set; } = 1;
    public string Condition { get; set; } = "Clear"; // Clear | Rain
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}


public class OtpChallenge
{
    [Key]
    public string OtpId { get; set; } = "";
    public string Email { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public string Purpose { get; set; } = "PasswordReset"; // PasswordReset | LoginMfa
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class ClinicAppointment
{
    [Key]
    public string AppointmentId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public DateTime AppointmentAt { get; set; }
    public string ClinicId { get; set; } = "";
    public string ReasonBrief { get; set; } = "Follow-up checkup";
    public string Status { get; set; } = "Scheduled";
}
