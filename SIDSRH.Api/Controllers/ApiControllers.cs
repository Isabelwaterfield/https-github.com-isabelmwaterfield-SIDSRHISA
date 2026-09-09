using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SIDSRH.Api.Data;
using SIDSRH.Api.Models;
using SIDSRH.Api.Services;

namespace SIDSRH.Api.Controllers;

static class LogX
{
    public static void Add(AppDbContext db, string? userId, string type, string desc)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = userId,
            ActivityType = type,
            Description = desc
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    public AuthController(AppDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    string JwtFor(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var mins = int.TryParse(_cfg["BusinessRules:TokenMinutes"], out var t) ? t : 60;
        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"], audience: _cfg["Jwt:Audience"],
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("name", user.FirstName + " " + user.LastName)
            },
            expires: DateTime.UtcNow.AddMinutes(mins),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Dictionary<string, string> body)
    {
        var email = body.GetValueOrDefault("email")?.Trim() ?? "";
        var password = body.GetValueOrDefault("password") ?? "";
        var otp = (body.GetValueOrDefault("otp") ?? "").Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            if (user != null && user.Status == "Active")
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= BusinessRules.MaxFailedLogins) user.Status = "Locked";
                await _db.SaveChangesAsync();
            }
            return Unauthorized(new { message = "Invalid email or password" });
        }
        if (user.Status != "Active")
            return Unauthorized(new { message = "Account is locked or inactive." });

        if (user.MfaEnabled)
        {
            if (string.IsNullOrWhiteSpace(otp))
            {
                var code = BusinessRules.NewCode();
                _db.OtpChallenges.Add(new OtpChallenge
                {
                    OtpId = "OTP-" + Guid.NewGuid().ToString("N")[..10],
                    Email = user.Email,
                    CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
                    Purpose = "Mfa",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(BusinessRules.OtpMinutes)
                });
                await _db.SaveChangesAsync();
                return Ok(new { mfaRequired = true, demoOtp = code, message = $"MFA required. Code valid {BusinessRules.OtpMinutes} minutes." });
            }
            var challenge = await _db.OtpChallenges
                .Where(o => o.Email == user.Email && o.Purpose == "Mfa" && !o.Used && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
            if (challenge == null || !BCrypt.Net.BCrypt.Verify(otp, challenge.CodeHash))
                return Unauthorized(new { message = "Invalid or expired MFA code" });
            challenge.Used = true;
        }

        user.FailedLoginCount = 0;
        LogX.Add(_db, user.UserId, "LOGIN", "Login succeeded");
        await _db.SaveChangesAsync();
        return Ok(new { token = JwtFor(user), user = new { user.UserId, user.FirstName, user.LastName, user.Email, user.Role } });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Dictionary<string, string> body)
    {
        var email = body.GetValueOrDefault("email")?.Trim() ?? "";
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return BadRequest(new { message = "Email already registered" });
        var user = new User
        {
            UserId = "USR-" + Guid.NewGuid().ToString("N")[..8],
            FirstName = body.GetValueOrDefault("firstName") ?? "",
            LastName = body.GetValueOrDefault("lastName") ?? "",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.GetValueOrDefault("password") ?? "Patient@123"),
            Role = "Patient",
            MfaEnabled = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Patient account created. Staff accounts are created by an administrator." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot([FromBody] Dictionary<string, string> body)
    {
        var email = body.GetValueOrDefault("email")?.Trim() ?? "";
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Ok(new { message = "If the account exists, a code was issued." });
        var code = BusinessRules.NewCode();
        _db.OtpChallenges.Add(new OtpChallenge
        {
            OtpId = "OTP-" + Guid.NewGuid().ToString("N")[..10],
            Email = email,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
            Purpose = "Reset",
            ExpiresAt = DateTime.UtcNow.AddMinutes(BusinessRules.OtpMinutes)
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Reset code issued (valid {BusinessRules.OtpMinutes} min).", demoOtp = code });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> Reset([FromBody] Dictionary<string, string> body)
    {
        var email = body.GetValueOrDefault("email")?.Trim() ?? "";
        var otp = body.GetValueOrDefault("otp") ?? "";
        var pass = body.GetValueOrDefault("password") ?? "";
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return BadRequest(new { message = "Invalid request" });
        var ch = await _db.OtpChallenges
            .Where(o => o.Email == email && o.Purpose == "Reset" && !o.Used && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
        if (ch == null || !BCrypt.Net.BCrypt.Verify(otp, ch.CodeHash))
            return BadRequest(new { message = "Invalid or expired code" });
        ch.Used = true;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(pass);
        user.FailedLoginCount = 0;
        user.Status = "Active";
        LogX.Add(_db, user.UserId, "PASSWORD_RESET", "Password reset via OTP");
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password updated" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var weather = await _db.SystemWeather.AsNoTracking().FirstOrDefaultAsync(w => w.Id == 1);
            return Ok(new
            {
                dronesAvailable = await _db.Drones.CountAsync(d => d.Status == "Available"),
                dronesTotal = await _db.Drones.CountAsync(),
                deliveriesActive = await _db.Deliveries.CountAsync(d => d.DeliveryStatus == "In Transit" || d.DeliveryStatus == "Assigned"),
                deliveriesTotal = await _db.Deliveries.CountAsync(),
                requestsOpen = await _db.PatientRequests.CountAsync(r => r.Status == "Submitted"),
                supplies = await _db.MedicalSupplies.CountAsync(),
                scenarios = await _db.EmergencyScenarios.CountAsync(),
                weather = weather?.Condition ?? "Clear"
            });
        }
        catch (Exception ex)
        {
            return Ok(new { dronesAvailable = 0, dronesTotal = 0, deliveriesActive = 0, deliveriesTotal = 0, requestsOpen = 0, supplies = 0, scenarios = 0, weather = "Clear", schemaWarning = ex.Message });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public LocationsController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.Locations.AsNoTracking().ToListAsync());
}

[ApiController]
[Route("api/[controller]")]
public class DronesController : ControllerBase
{
    private readonly AppDbContext _db;
    public DronesController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.Drones.AsNoTracking().ToListAsync());

    [HttpPost("{id}/maintenance")]
    public async Task<IActionResult> Maintenance(string id, [FromBody] Dictionary<string, string> body)
    {
        var d = await _db.Drones.FindAsync(id);
        if (d == null) return NotFound();
        var on = body.GetValueOrDefault("on") == "true" || body.GetValueOrDefault("status") == "Maintenance";
        d.Status = on ? "Maintenance" : "Available";
        LogX.Add(_db, null, "DRONE_MAINTENANCE", $"{id} -> {d.Status}");
        await _db.SaveChangesAsync();
        return Ok(d);
    }
}

[ApiController]
[Route("api/[controller]")]
public class SuppliesController : ControllerBase
{
    private readonly AppDbContext _db;
    public SuppliesController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.MedicalSupplies.AsNoTracking().ToListAsync());
}

[ApiController]
[Route("api/[controller]")]
public class ClinicsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClinicsController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.MobileClinics.AsNoTracking().ToListAsync());
}

[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly AppDbContext _db;
    public StaffController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.Staff.AsNoTracking().ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Dictionary<string, string> body)
    {
        var s = new Staff
        {
            StaffId = "STF-" + Guid.NewGuid().ToString("N")[..6],
            UserId = body.GetValueOrDefault("userId") ?? "",
            FirstName = body.GetValueOrDefault("firstName") ?? "",
            LastName = body.GetValueOrDefault("lastName") ?? "",
            JobTitle = body.GetValueOrDefault("jobTitle") ?? "Staff",
            Status = body.GetValueOrDefault("status") ?? "Active",
            ClinicId = body.GetValueOrDefault("clinicId"),
            ContactNumber = body.GetValueOrDefault("contactNumber")
        };
        _db.Staff.Add(s);
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var s = await _db.Staff.FindAsync(id);
        if (s == null) return NotFound();
        _db.Staff.Remove(s);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly AppDbContext _db;
    public EquipmentController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.Equipment.AsNoTracking().ToListAsync());
}

[ApiController]
[Route("api/[controller]")]
public class ActivityLogsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ActivityLogsController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() =>
        Ok(await _db.ActivityLogs.AsNoTracking().OrderByDescending(x => x.Timestamp).Take(150).ToListAsync());
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() =>
        Ok(await _db.Users.AsNoTracking().Select(u => new { u.UserId, u.FirstName, u.LastName, u.Email, u.Role, u.Status, u.MfaEnabled }).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Dictionary<string, string> body)
    {
        var role = body.GetValueOrDefault("role") ?? "Healthcare Staff";
        var email = body.GetValueOrDefault("email")?.Trim() ?? "";
        if (await _db.Users.AnyAsync(u => u.Email == email)) return BadRequest(new { message = "Email exists" });
        var u = new User
        {
            UserId = "USR-" + Guid.NewGuid().ToString("N")[..8],
            FirstName = body.GetValueOrDefault("firstName") ?? "",
            LastName = body.GetValueOrDefault("lastName") ?? "",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.GetValueOrDefault("password") ?? "ChangeMe@123"),
            Role = role,
            MfaEnabled = true
        };
        _db.Users.Add(u);
        LogX.Add(_db, null, "USER_CREATE", email + " " + role);
        await _db.SaveChangesAsync();
        return Ok(u);
    }
}

[ApiController]
[Route("api/[controller]")]
public class PatientRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PatientRequestsController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() =>
        Ok(await _db.PatientRequests.AsNoTracking().OrderByDescending(r => r.CreatedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Dictionary<string, string> body)
    {
        var locId = body.GetValueOrDefault("locationId") ?? "LOC002";
        var service = body.GetValueOrDefault("serviceClass") ?? "Urgent";
        var hub = await _db.Locations.FindAsync("LOC005");
        var loc = await _db.Locations.FindAsync(locId);
        double? km = null;
        if (hub != null && loc != null)
            km = Math.Round(BusinessRules.HaversineKm(hub.Latitude, hub.Longitude, loc.Latitude, loc.Longitude), 2);
        var max = service == "Urgent" ? BusinessRules.UrgentMaxKm : BusinessRules.RoutineMaxKm;
        var weather = await _db.SystemWeather.FirstOrDefaultAsync(w => w.Id == 1);
        if (BusinessRules.IsRaining(weather?.Condition) && km > BusinessRules.UrgentMaxKm)
            return BadRequest(new { message = "Rain: only deliveries within 15 km are allowed." });
        if (km > max)
            return BadRequest(new { message = $"Distance {km} km exceeds {service} limit ({max} km)." });

        var req = new PatientRequest
        {
            RequestId = "REQ-" + Guid.NewGuid().ToString("N")[..8],
            PatientName = body.GetValueOrDefault("patientName") ?? "",
            ContactNumber = body.GetValueOrDefault("contactNumber"),
            LocationId = locId,
            MedicalNeed = body.GetValueOrDefault("medicalNeed") ?? "",
            Category = body.GetValueOrDefault("category") ?? "",
            ServiceClass = service,
            DistanceKm = km,
            Status = "Submitted"
        };
        _db.PatientRequests.Add(req);
        LogX.Add(_db, null, "PATIENT_REQUEST", req.RequestId);
        await _db.SaveChangesAsync();
        return Ok(req);
    }

    [HttpPost("{id}/accept")]
    public async Task<IActionResult> Accept(string id, [FromBody] Dictionary<string, string> body)
    {
        var req = await _db.PatientRequests.FindAsync(id);
        if (req == null) return NotFound();
        var droneId = body.GetValueOrDefault("droneId");
        var supplyId = body.GetValueOrDefault("supplyId") ?? "SUP001";
        var clinicId = body.GetValueOrDefault("clinicId") ?? "CLN001";
        var origin = body.GetValueOrDefault("originLocationId") ?? "LOC005";
        var supply = await _db.MedicalSupplies.FindAsync(supplyId);
        if (supply == null || supply.Quantity < 1) return BadRequest(new { message = "Supply unavailable" });
        supply.Quantity -= 1;
        var o = await _db.Locations.FindAsync(origin);
        var d = await _db.Locations.FindAsync(req.LocationId);
        var km = (o != null && d != null) ? BusinessRules.HaversineKm(o.Latitude, o.Longitude, d.Latitude, d.Longitude) : req.DistanceKm ?? 0;
        var slot = body.GetValueOrDefault("preferredSlot") ?? "09:00";
        var del = new Delivery
        {
            DeliveryId = "DEL-" + Guid.NewGuid().ToString("N")[..8],
            DroneId = droneId,
            ClinicId = clinicId,
            SupplyId = supplyId,
            Quantity = 1,
            OriginLocationId = origin,
            DestinationLocationId = req.LocationId,
            DeliveryStatus = string.IsNullOrEmpty(droneId) ? "Pending" : "Assigned",
            DistanceKm = Math.Round(km, 2),
            ServiceClass = req.ServiceClass,
            RecipientName = req.PatientName,
            RecipientPhone = req.ContactNumber,
            BriefReason = req.Category.Length > 40 ? req.Category[..40] : req.Category,
            PatientFee = BusinessRules.PatientFeeForSlot(slot),
            PreferredSlot = slot,
            ActualCost = (decimal)(km * 2.5)
        };
        if (!string.IsNullOrEmpty(droneId))
        {
            var drone = await _db.Drones.FindAsync(droneId);
            if (drone == null || drone.Status != "Available") return BadRequest(new { message = "Drone not available" });
            drone.Status = "Assigned";
            del.DispatchCode = BusinessRules.NewCode();
        }
        req.Status = "Accepted";
        req.AssignedDeliveryId = del.DeliveryId;
        _db.Deliveries.Add(del);
        LogX.Add(_db, null, "REQUEST_ACCEPTED", id + " -> " + del.DeliveryId);
        await _db.SaveChangesAsync();
        return Ok(new { request = req, delivery = del });
    }
}

[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public DeliveriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.Deliveries.AsNoTracking().OrderByDescending(d => d.DateCreated).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Dictionary<string, string> body)
    {
        var supplyId = body.GetValueOrDefault("supplyId") ?? "";
        var origin = body.GetValueOrDefault("originLocationId") ?? "LOC005";
        var dest = body.GetValueOrDefault("destinationLocationId") ?? "LOC002";
        if (!int.TryParse(body.GetValueOrDefault("quantity"), out var qty) || qty < 1) qty = 1;
        var supply = await _db.MedicalSupplies.FindAsync(supplyId);
        if (supply == null) return BadRequest(new { message = "Supply not found" });
        if (supply.Quantity < qty) return BadRequest(new { message = "Insufficient stock" });
        var o = await _db.Locations.FindAsync(origin);
        var dloc = await _db.Locations.FindAsync(dest);
        if (o == null || dloc == null) return BadRequest(new { message = "Invalid locations" });
        var km = BusinessRules.HaversineKm(o.Latitude, o.Longitude, dloc.Latitude, dloc.Longitude);
        var weather = await _db.SystemWeather.FirstOrDefaultAsync(w => w.Id == 1);
        if (BusinessRules.IsRaining(weather?.Condition) && km > BusinessRules.UrgentMaxKm)
            return BadRequest(new { message = "Rain limit: max 15 km" });
        var slot = body.GetValueOrDefault("preferredSlot") ?? "09:00";
        supply.Quantity -= qty;
        var del = new Delivery
        {
            DeliveryId = "DEL-" + Guid.NewGuid().ToString("N")[..8],
            ClinicId = body.GetValueOrDefault("clinicId") ?? "CLN001",
            SupplyId = supplyId,
            Quantity = qty,
            OriginLocationId = origin,
            DestinationLocationId = dest,
            DeliveryStatus = "Pending",
            DistanceKm = Math.Round(km, 2),
            ServiceClass = body.GetValueOrDefault("serviceClass") ?? "Routine",
            RecipientName = body.GetValueOrDefault("recipientName"),
            BriefReason = body.GetValueOrDefault("briefReason"),
            PatientFee = BusinessRules.PatientFeeForSlot(slot),
            PreferredSlot = slot,
            ActualCost = (decimal)(km * 2.5)
        };
        _db.Deliveries.Add(del);
        LogX.Add(_db, null, "DeliveryCreated", del.DeliveryId);
        await _db.SaveChangesAsync();
        return Ok(del);
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> Assign(string id, [FromBody] Dictionary<string, string> body)
    {
        var del = await _db.Deliveries.FindAsync(id);
        if (del == null) return NotFound();
        if (del.DeliveryStatus != "Pending") return BadRequest(new { message = "Only Pending can be assigned" });
        var droneId = body.GetValueOrDefault("droneId") ?? "";
        var drone = await _db.Drones.FindAsync(droneId);
        if (drone == null || drone.Status != "Available" || drone.BatteryLevel < BusinessRules.MinBattery)
            return BadRequest(new { message = "Drone not available" });
        drone.Status = "Assigned";
        del.DroneId = droneId;
        del.DeliveryStatus = "Assigned";
        del.DispatchCode = BusinessRules.NewCode();
        del.TechnicianName = body.GetValueOrDefault("technicianName") ?? "On-duty technician";
        await _db.SaveChangesAsync();
        return Ok(del);
    }

    [HttpPost("{id}/dispatch")]
    public async Task<IActionResult> Dispatch(string id)
    {
        var del = await _db.Deliveries.FindAsync(id);
        if (del == null) return NotFound();
        if (del.DeliveryStatus != "Assigned") return BadRequest(new { message = "Assign first" });
        var weather = await _db.SystemWeather.FirstOrDefaultAsync(w => w.Id == 1);
        if (BusinessRules.IsRaining(weather?.Condition) && del.DistanceKm > BusinessRules.UrgentMaxKm)
            return BadRequest(new { message = "Cannot dispatch beyond 15 km in rain" });
        double spd = BusinessRules.CruiseSpeedKmh;
        if (del.DroneId != null)
        {
            var drone = await _db.Drones.FindAsync(del.DroneId);
            if (drone != null)
            {
                drone.Status = "In Transit";
                if (drone.SpeedKmh > 0) spd = drone.SpeedKmh;
            }
        }
        var mins = BusinessRules.EtaMinutes(del.DistanceKm, spd);
        del.DeliveryStatus = "In Transit";
        del.DispatchedAt = DateTime.UtcNow;
        del.EstimatedArrival = DateTime.UtcNow.AddMinutes(mins);
        del.ArrivalCode = BusinessRules.NewCode();
        await _db.SaveChangesAsync();
        return Ok(new { delivery = del, etaMinutes = mins, message = "In transit. Arrival code for recipient." });
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(string id, [FromBody] Dictionary<string, string>? body)
    {
        var del = await _db.Deliveries.FindAsync(id);
        if (del == null) return NotFound();
        var code = body?.GetValueOrDefault("arrivalCode") ?? "";
        if (!string.IsNullOrEmpty(del.ArrivalCode) && del.ArrivalCode != code)
            return BadRequest(new { message = "Invalid arrival code" });
        del.DeliveryStatus = "Completed";
        del.DateCompleted = DateTime.UtcNow;
        if (del.DroneId != null)
        {
            var drone = await _db.Drones.FindAsync(del.DroneId);
            if (drone != null)
            {
                drone.Status = "Available";
                drone.BatteryLevel = Math.Max(0, drone.BatteryLevel - (int)Math.Ceiling(del.DistanceKm * BusinessRules.DrainPerKm));
                drone.CurrentLocationId = del.DestinationLocationId;
            }
        }
        await _db.SaveChangesAsync();
        return Ok(del);
    }
}

[ApiController]
[Route("api/[controller]")]
public class DispenseController : ControllerBase
{
    private readonly AppDbContext _db;
    public DispenseController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() =>
        Ok(await _db.DispenseRecords.AsNoTracking().OrderByDescending(d => d.DispensedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Dictionary<string, string> body)
    {
        if (!int.TryParse(body.GetValueOrDefault("quantity"), out var qty) || qty < 1) qty = 1;
        var supplyId = body.GetValueOrDefault("supplyId") ?? "";
        var supply = await _db.MedicalSupplies.FindAsync(supplyId);
        if (supply == null || supply.Quantity < qty) return BadRequest(new { message = "Stock unavailable" });
        supply.Quantity -= qty;
        var rec = new DispenseRecord
        {
            DispenseId = "DSP-" + Guid.NewGuid().ToString("N")[..8],
            ClinicId = body.GetValueOrDefault("clinicId") ?? "CLN001",
            SupplyId = supplyId,
            PatientName = body.GetValueOrDefault("patientName") ?? "",
            PatientId = body.GetValueOrDefault("patientId"),
            TargetType = body.GetValueOrDefault("targetType") ?? "Patient",
            ToClinicId = body.GetValueOrDefault("toClinicId"),
            Quantity = qty,
            Status = "PendingVerify",
            Notes = body.GetValueOrDefault("notes")
        };
        _db.DispenseRecords.Add(rec);
        LogX.Add(_db, null, "DISPENSE", rec.DispenseId);
        await _db.SaveChangesAsync();
        return Ok(rec);
    }

    [HttpPost("{id}/verify")]
    public async Task<IActionResult> Verify(string id)
    {
        var r = await _db.DispenseRecords.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = "Verified";
        r.VerifiedByUserId = "MANAGER";
        await _db.SaveChangesAsync();
        return Ok(r);
    }
}

[ApiController]
[Route("api/[controller]")]
public class SimulationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SimulationsController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() =>
        Ok(await _db.Simulations.AsNoTracking().OrderByDescending(s => s.StartTime).Take(50).ToListAsync());
    [HttpGet("scenarios")] public async Task<IActionResult> Scenarios() =>
        Ok(await _db.EmergencyScenarios.AsNoTracking().ToListAsync());

    [HttpPost("run/{scenarioId}")]
    public async Task<IActionResult> Run(string scenarioId)
    {
        var scenario = await _db.EmergencyScenarios.FindAsync(scenarioId);
        if (scenario == null) return NotFound();
        var start = await _db.Locations.FindAsync(scenario.StartingLocationId);
        var dest = await _db.Locations.FindAsync(scenario.DestinationLocationId);
        var sim = new Simulation { SimulationId = "SIM-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), ScenarioId = scenarioId, Status = "Running", StartTime = DateTime.UtcNow };
        if (start == null || dest == null)
        {
            sim.Status = "Failed"; sim.Result = "SIMULATED: invalid locations"; sim.EndTime = DateTime.UtcNow;
            _db.Simulations.Add(sim); await _db.SaveChangesAsync(); return Ok(sim);
        }
        var distance = BusinessRules.HaversineKm(start.Latitude, start.Longitude, dest.Latitude, dest.Longitude);
        if (distance > BusinessRules.UrgentMaxKm)
        {
            sim.Status = "Failed"; sim.DistanceKm = Math.Round(distance, 2);
            sim.Result = $"SIMULATED: more than {BusinessRules.UrgentMaxKm} km";
            sim.EndTime = DateTime.UtcNow;
            _db.Simulations.Add(sim); await _db.SaveChangesAsync(); return Ok(sim);
        }
        var drones = await _db.Drones.Where(d => d.Status == "Available" && d.BatteryLevel >= BusinessRules.MinBattery).ToListAsync();
        Drone? best = null; double bestTo = double.MaxValue;
        foreach (var drone in drones)
        {
            var dloc = drone.CurrentLocationId != null ? await _db.Locations.FindAsync(drone.CurrentLocationId) : null;
            var lat = dloc?.Latitude ?? drone.Latitude; var lng = dloc?.Longitude ?? drone.Longitude;
            var toStart = BusinessRules.HaversineKm(lat, lng, start.Latitude, start.Longitude);
            if (drone.BatteryLevel >= (toStart + distance) * BusinessRules.DrainPerKm + 10 && toStart < bestTo)
            { bestTo = toStart; best = drone; }
        }
        if (best == null) { sim.Status = "Failed"; sim.Result = "SIMULATED: no suitable drone"; }
        else
        {
            var flight = (bestTo + distance) / (best.SpeedKmh > 0 ? best.SpeedKmh : BusinessRules.CruiseSpeedKmh) * 60;
            var prep = scenario.Priority switch { "Critical" => 2, "High" => 5, "Medium" => 10, _ => 15 };
            sim.SelectedDroneId = best.DroneId; sim.DistanceKm = Math.Round(distance, 2);
            sim.EstimatedResponseTime = (int)Math.Round(prep + flight); sim.Status = "Completed";
            sim.Result = $"SIMULATED: {best.DroneName} could deliver in about {sim.EstimatedResponseTime} minutes. Planning use only.";
        }
        sim.EndTime = DateTime.UtcNow;
        _db.Simulations.Add(sim);
        LogX.Add(_db, null, "SimulationRun", sim.SimulationId);
        await _db.SaveChangesAsync();
        return Ok(sim);
    }
}

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly AppDbContext _db;
    public WeatherController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.SystemWeather.FirstOrDefaultAsync(w => w.Id == 1) ?? new SystemWeather());
    [HttpPost]
    public async Task<IActionResult> Set([FromBody] Dictionary<string, string> body)
    {
        var w = await _db.SystemWeather.FirstOrDefaultAsync(x => x.Id == 1);
        if (w == null) { w = new SystemWeather { Id = 1 }; _db.SystemWeather.Add(w); }
        w.Condition = body.GetValueOrDefault("condition") ?? "Clear";
        w.UpdatedAt = DateTime.UtcNow;
        w.Note = body.GetValueOrDefault("note");
        await _db.SaveChangesAsync();
        return Ok(w);
    }
}

[ApiController]
[Route("api/[controller]")]
public class StationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public StationsController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.ChargingStations.AsNoTracking().ToListAsync());
}

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AppointmentsController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() =>
        Ok(await _db.ClinicAppointments.AsNoTracking().OrderBy(a => a.AppointmentAt).ToListAsync());
}

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PatientsController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.PatientProfiles.AsNoTracking().ToListAsync());
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Dictionary<string, string> body)
    {
        var p = new PatientProfile
        {
            PatientId = "PAT-" + Guid.NewGuid().ToString("N")[..6],
            FullName = body.GetValueOrDefault("fullName") ?? "",
            ContactNumber = body.GetValueOrDefault("contactNumber") ?? "",
            LocationId = body.GetValueOrDefault("locationId")
        };
        _db.PatientProfiles.Add(p);
        await _db.SaveChangesAsync();
        return Ok(p);
    }
}

[ApiController]
[Route("api/[controller]")]
public class HelpController : ControllerBase
{
    private readonly AppDbContext _db;
    public HelpController(AppDbContext db) => _db = db;

    [HttpPost("code-assist")]
    public async Task<IActionResult> CodeAssist([FromBody] Dictionary<string, string> body)
    {
        var deliveryId = body.GetValueOrDefault("deliveryId") ?? "";
        var del = string.IsNullOrWhiteSpace(deliveryId) ? null : await _db.Deliveries.FindAsync(deliveryId);
        LogX.Add(_db, "SYSTEM", "CODE_HELP_CHAT", "Assist " + deliveryId);
        await _db.SaveChangesAsync();
        return Ok(new
        {
            title = "Emergency code help",
            steps = new object[]
            {
                new { step = 1, check = "Collection point", result = del == null ? "Confirm delivery ID" : $"Status {del.DeliveryStatus}, dest {del.DestinationLocationId}" },
                new { step = 2, check = "Correct account", result = del == null ? "N/A" : $"Recipient {del.RecipientName}" },
                new { step = 3, check = "Network", result = "Reconnect and reload if offline" },
                new { step = 4, check = "Resolve", result = "Re-show arrival code or nurse verifies next-of-kin on audit" }
            },
            returnPolicy = $"If not collected within {BusinessRules.CollectionGraceMinutes} min after arrival, R{BusinessRules.DelayFineRand} fine; then drone may return with parcel.",
            nextOfKinOption = "Nurse/technician may verify registered patient or next-of-kin and release with activity log."
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    public ReportsController(AppDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    [HttpGet("info")]
    public IActionResult Info()
    {
        var p = _cfg["Database:Provider"] ?? "Sqlite";
        return Ok(new
        {
            provider = p,
            mode = p.Equals("Sqlite", StringComparison.OrdinalIgnoreCase) ? "Offline / local SQLite" : "Online database",
            urgentMaxKm = BusinessRules.UrgentMaxKm,
            routineMaxKm = BusinessRules.RoutineMaxKm,
            otpMinutes = BusinessRules.OtpMinutes,
            phase4Plus = true
        });
    }

    [HttpGet("deliveries.csv")]
    public async Task<IActionResult> DeliveriesCsv()
    {
        var list = await _db.Deliveries.AsNoTracking().ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("DeliveryId,Status,DistanceKm,PatientFee,DelayFine,ActualCost,Slot,BriefReason");
        foreach (var d in list)
            sb.AppendLine($"{d.DeliveryId},{d.DeliveryStatus},{d.DistanceKm},{d.PatientFee:0.00},{d.DelayFine:0.00},{d.ActualCost:0.00},{d.PreferredSlot},{d.BriefReason}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "deliveries.csv");
    }

    [HttpGet("manager-weekly.csv")]
    public async Task<IActionResult> ManagerWeekly()
    {
        var staff = await _db.Staff.AsNoTracking().ToListAsync();
        var disp = await _db.DispenseRecords.AsNoTracking().Where(x => x.DispensedAt >= DateTime.UtcNow.AddDays(-7)).ToListAsync();
        var dels = await _db.Deliveries.AsNoTracking().Where(x => x.DateCreated >= DateTime.UtcNow.AddDays(-7)).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Section,Id,Name,Detail,Status");
        foreach (var s in staff) sb.AppendLine($"Staff,{s.StaffId},{s.FirstName} {s.LastName},{s.JobTitle},{s.Status}");
        foreach (var d in disp) sb.AppendLine($"Dispense,{d.DispenseId},{d.PatientName},qty {d.Quantity},{d.Status}");
        foreach (var d in dels) sb.AppendLine($"Delivery,{d.DeliveryId},{d.RecipientName},fee {d.PatientFee + d.DelayFine:0.00},{d.DeliveryStatus}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "manager-weekly.csv");
    }

    [HttpGet("technician.csv")]
    public async Task<IActionResult> TechCsv()
    {
        var drones = await _db.Drones.AsNoTracking().ToListAsync();
        var active = await _db.Deliveries.AsNoTracking().Where(d => d.DeliveryStatus == "In Transit" || d.DeliveryStatus == "Assigned").ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Type,Id,Name,Status,Battery");
        foreach (var d in drones) sb.AppendLine($"Drone,{d.DroneId},{d.DroneName},{d.Status},{d.BatteryLevel}");
        foreach (var d in active) sb.AppendLine($"ActiveTask,{d.DeliveryId},{d.RecipientName},{d.DeliveryStatus},");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "technician-report.csv");
    }

    [HttpGet("patient-receipt/{deliveryId}")]
    public async Task<IActionResult> PatientReceipt(string deliveryId)
    {
        var d = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.DeliveryId == deliveryId);
        if (d == null) return NotFound();
        var brief = string.IsNullOrWhiteSpace(d.BriefReason) ? d.ServiceClass : d.BriefReason;
        if (brief.Length > 40) brief = brief[..40];
        var charge = d.PatientFee + d.DelayFine;
        var chargeLine = charge <= 0 ? "R0.00" : "R" + charge.ToString("0.00");
        var when = d.DateCompleted ?? d.EstimatedArrival ?? d.DateCreated;
        var html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'/><title>SIDSRH Receipt</title>
<style>body{{font-family:Segoe UI,Arial;color:#082c60;max-width:480px;margin:24px auto;border:1px solid #d7e3ec;padding:20px}}
h1{{font-size:18px}} .muted{{color:#556;font-size:12px}} table{{width:100%;font-size:13px}} td{{padding:4px 0}}</style></head><body>
<img src='/assets/sidsrh-logo.png' height='48' alt='SIDSRH'/>
<h1>Patient delivery receipt</h1>
<p class='muted'>Confidential - brief record only. Not full clinical history.</p>
<table>
<tr><td>Date / time</td><td><b>{when:yyyy-MM-dd HH:mm} UTC</b></td></tr>
<tr><td>Delivery ID</td><td>{d.DeliveryId}</td></tr>
<tr><td>Recipient</td><td>{System.Net.WebUtility.HtmlEncode(d.RecipientName ?? "")}</td></tr>
<tr><td>Technician</td><td>{System.Net.WebUtility.HtmlEncode(d.TechnicianName ?? "On-duty technician")}</td></tr>
<tr><td>Brief reason</td><td>{System.Net.WebUtility.HtmlEncode(brief)}</td></tr>
<tr><td>Distance</td><td>{d.DistanceKm} km</td></tr>
<tr><td>Patient charge</td><td><b>{chargeLine}</b></td></tr>
<tr><td>Status</td><td>{d.DeliveryStatus}</td></tr>
</table>
<p class='muted'>Free in-slot (06:30 / 09:00 / 16:00) shows R0.00. Outside slot R10. Delay fine R3 after 8 minutes.</p>
</body></html>";
        return Content(html, "text/html");
    }
}

[ApiController]
[Route("api/[controller]")]
public class BackupController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly IWebHostEnvironment _env;
    public BackupController(IConfiguration cfg, IWebHostEnvironment env) { _cfg = cfg; _env = env; }

    [HttpPost("sqlite")]
    public IActionResult BackupSqlite()
    {
        var provider = _cfg["Database:Provider"] ?? "Sqlite";
        var folder = Path.Combine(_env.ContentRootPath, _cfg["Backup:Folder"] ?? "Backups");
        Directory.CreateDirectory(folder);
        if (!provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            return Ok(new { message = "Provider is not Sqlite. Use SQL Server backup tools or switch Provider to Sqlite for file backup.", provider });
        var src = Path.Combine(_env.ContentRootPath, "sidsrh_offline.db");
        if (!System.IO.File.Exists(src))
            return NotFound(new { message = "sidsrh_offline.db not found yet. Run the app once to create it." });
        var name = "sidsrh_backup_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".db";
        var dest = Path.Combine(folder, name);
        System.IO.File.Copy(src, dest, true);
        // retain
        var retain = 10;
        int.TryParse(_cfg["Backup:RetainCount"], out retain);
        var files = new DirectoryInfo(folder).GetFiles("sidsrh_backup_*.db").OrderByDescending(f => f.CreationTimeUtc).Skip(Math.Max(1, retain)).ToList();
        foreach (var f in files) f.Delete();
        return Ok(new { message = "Backup created", file = name, path = dest });
    }

    [HttpGet("list")]
    public IActionResult List()
    {
        var folder = Path.Combine(_env.ContentRootPath, _cfg["Backup:Folder"] ?? "Backups");
        Directory.CreateDirectory(folder);
        var files = new DirectoryInfo(folder).GetFiles("sidsrh_backup_*.db")
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => new { f.Name, SizeKb = f.Length / 1024, f.CreationTimeUtc })
            .ToList();
        return Ok(files);
    }

    [HttpGet("instructions")]
    public IActionResult Instructions()
    {
        return Ok(new
        {
            sqlite = "POST /api/Backup/sqlite copies sidsrh_offline.db into Backups/",
            sqlServer = "SSMS: Right-click HealthcareDB > Tasks > Back Up. Or: BACKUP DATABASE HealthcareDB TO DISK = 'C:\\Backups\\HealthcareDB.bak'",
            mysql = "mysqldump -u root -p HealthcareDB > HealthcareDB_backup.sql",
            codeZip = "Zip entire SIDSRH.Api folder excluding bin/obj for source backup"
        });
    }
}
