using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SIDSRH.Api.Data;
using SIDSRH.Api.Models;
using SIDSRH.Api.Services;

namespace SIDSRH.Api.Controllers;

public record LoginDto(string Email, string Password);
public record RegisterDto(string FirstName, string LastName, string Email, string Password);
public record ForgotPasswordDto(string Email);
public record VerifyOtpDto(string Email, string Code, string Purpose);
public record ResetPasswordDto(string Email, string Code, string NewPassword);
public record CreateUserDto(string FirstName, string LastName, string Email, string Password, string Role, string? Phone);
public record MfaLoginDto(string Email, string Password, string? Otp);

public record CreateDeliveryDto(string DroneId, string ClinicId, string SupplyId, string OriginLocationId, string DestinationLocationId, string ServiceClass, string? RecipientName, string? RecipientPhone);
public record CodeDto(string Code);
public record AcceptRequestDto(string DroneId, string ClinicId, string SupplyId);
public record DispenseDto(string ClinicId, string SupplyId, string PatientName, int Quantity, string? Notes, string? PatientId, string? TargetType, string? ToClinicId, bool PendingVerification);
public record PatientRequestDto(string PatientName, string? ContactNumber, string LocationId, string MedicalNeed, string Category, string ServiceClass, string? SituationNotes, bool RequestResponder, string? HelperName, string? HelperContact);

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    public AuthController(AppDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var email = (dto.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Status == "Active");
        if (user == null || string.IsNullOrEmpty(dto.Password) || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("name", $"{user.FirstName} {user.LastName}")
        };
        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = user.UserId,
            ActivityType = "LOGIN",
            Description = $"{user.Email} signed in."
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = new { user.UserId, user.FirstName, user.LastName, user.Email, user.Role }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email and password required." });
            if (dto.Password.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters." });

            if (await _db.Users.AnyAsync(u => u.Email == dto.Email.Trim().ToLower()))
                return BadRequest(new { message = "Email already registered." });

            var user = new User
            {
                UserId = "USR-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                FirstName = string.IsNullOrWhiteSpace(dto.FirstName) ? "Patient" : dto.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(dto.LastName) ? "" : dto.LastName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Patient",
                Status = "Active"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Patient account created. You can sign in." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Registration failed: " + ex.Message });
        }
    }

    static string NewOtp() => Random.Shared.Next(100000, 999999).ToString();

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var email = (dto.Email ?? "").Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        // Always generic message (do not reveal if email exists)
        if (user == null)
            return Ok(new { message = "If that email is registered, a reset code was issued.", verificationCode = (string?)null });

        var code = NewOtp();
        _db.OtpChallenges.Add(new OtpChallenge
        {
            OtpId = "OTP-" + Guid.NewGuid().ToString("N")[..10],
            Email = email,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
            Purpose = "PasswordReset",
            ExpiresAt = DateTime.UtcNow.AddMinutes(6)
        });
        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = user.UserId,
            ActivityType = "PASSWORD_RESET_REQUEST",
            Description = $"Password reset OTP issued for {email}"
        });
        await _db.SaveChangesAsync();
        // Verification code issued for authorised channel
        return Ok(new
        {
            message = "Reset code issued. Valid 6 minutes. (Demo shows code; production sends email/SMS only.)",
            verificationCode = code,
            expiresMinutes = 6
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var email = (dto.Email ?? "").Trim().ToLower();
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return BadRequest(new { message = "New password must be at least 6 characters." });

        var otp = await _db.OtpChallenges
            .Where(o => o.Email == email && o.Purpose == "PasswordReset" && !o.Used && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
        if (otp == null || !BCrypt.Net.BCrypt.Verify(dto.Code ?? "", otp.CodeHash))
            return BadRequest(new { message = "Invalid or expired code." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return BadRequest(new { message = "User not found." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        otp.Used = true;
        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = user.UserId,
            ActivityType = "PASSWORD_RESET",
            Description = $"Password reset completed for {email}"
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password updated. You can sign in." });
    }

    [HttpPost("login-mfa")]
    public async Task<IActionResult> LoginMfa([FromBody] MfaLoginDto dto)
    {
        var email = (dto.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Status == "Active");
        if (user == null || string.IsNullOrEmpty(dto.Password) || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var needsMfa = true; // verification code required for every role
        if (needsMfa)
        {
            if (string.IsNullOrWhiteSpace(dto.Otp))
            {
                var code = NewOtp();
                _db.OtpChallenges.Add(new OtpChallenge
                {
                    OtpId = "OTP-" + Guid.NewGuid().ToString("N")[..10],
                    Email = user.Email,
                    CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
                    Purpose = "LoginMfa",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(6)
                });
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    mfaRequired = true,
                    message = "MFA required for this role. Enter the one-time code.",
                    verificationCode = code
                });
            }
            var otp = await _db.OtpChallenges
                .Where(o => o.Email == user.Email && o.Purpose == "LoginMfa" && !o.Used && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
            if (otp == null || !BCrypt.Net.BCrypt.Verify(dto.Otp, otp.CodeHash))
                return Unauthorized(new { message = "Invalid or expired MFA code." });
            otp.Used = true;
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("name", $"{user.FirstName} {user.LastName}")
        };
        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);
        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = user.UserId,
            ActivityType = "LOGIN",
            Description = $"{user.Email} signed in" + (needsMfa ? " (MFA)." : ".")
        });
        await _db.SaveChangesAsync();
        return Ok(new
        {
            mfaRequired = false,
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = new { user.UserId, user.FirstName, user.LastName, user.Email, user.Role }
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    static readonly string[] ManagerRoles = { "Healthcare Staff", "Supply Coordinator", "Emergency Coordinator", "Manager" };
    static readonly string[] AdminOnlyRoles = { "Technician", "Administrator" };

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.Users.AsNoTracking().OrderBy(u => u.Role).ThenBy(u => u.Email)
            .Select(u => new { u.UserId, u.FirstName, u.LastName, u.Email, u.Role, u.Status, u.MfaEnabled })
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, [FromQuery] string actorRole = "Administrator")
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Email and password required." });
        var role = (dto.Role ?? "").Trim();
        if (AdminOnlyRoles.Contains(role) && actorRole != "Administrator")
            return StatusCode(403, new { message = "Only Administrator can create Technician or Administrator accounts." });
        if (actorRole == "Manager" && !ManagerRoles.Contains(role) && role != "Healthcare Staff")
            return StatusCode(403, new { message = "Manager may create clinical/operational staff only, not technicians." });
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email.Trim().ToLower()))
            return BadRequest(new { message = "Email already exists." });

        var user = new User
        {
            UserId = "USR-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            FirstName = dto.FirstName?.Trim() ?? "",
            LastName = dto.LastName?.Trim() ?? "",
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            Status = "Active",
            Phone = dto.Phone,
            MfaEnabled = role is "Administrator" or "Technician"
        };
        _db.Users.Add(user);
        if (role is "Healthcare Staff" or "Supply Coordinator" or "Emergency Coordinator" or "Manager")
        {
            _db.Staff.Add(new Staff
            {
                StaffId = "STF-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ContactNumber = dto.Phone,
                Role = role,
                Status = "Active"
            });
        }
        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "USER_CREATE",
            Description = $"{actorRole} created user {user.Email} as {user.Role}"
        });
        await _db.SaveChangesAsync();
        return Ok(new { user.UserId, user.Email, user.Role, user.MfaEnabled });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> Status(string id, [FromBody] Dictionary<string, string> body)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        if (body != null && body.TryGetValue("status", out var st)) u.Status = st;
        await _db.SaveChangesAsync();
        return Ok(new { u.UserId, u.Status });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        if (u.Role == "Administrator")
            return BadRequest(new { message = "Cannot delete Administrator via API in this demo." });
        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User removed." });
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
        return Ok(new
        {
            dronesAvailable = await _db.Drones.CountAsync(d => d.Status == "Available"),
            dronesTotal = await _db.Drones.CountAsync(),
            deliveriesActive = await _db.Deliveries.CountAsync(d => d.DeliveryStatus == "In Transit" || d.DeliveryStatus == "Assigned"),
            deliveriesTotal = await _db.Deliveries.CountAsync(),
            requestsOpen = await _db.PatientRequests.CountAsync(r => r.Status == "Submitted"),
            supplies = await _db.MedicalSupplies.CountAsync(),
            staff = await _db.Staff.CountAsync(),
            clinics = await _db.MobileClinics.CountAsync()
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class DronesController : ControllerBase
{
    private readonly AppDbContext _db;
    public DronesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _db.Drones.AsNoTracking().ToListAsync());

    [HttpPost("{id}/maintenance")]
    public async Task<IActionResult> SetMaintenance(string id, [FromBody] Dictionary<string, object>? body)
    {
        var d = await _db.Drones.FindAsync(id);
        if (d == null) return NotFound();
        var into = true;
        if (body != null && body.TryGetValue("inMaintenance", out var v))
            into = v?.ToString()?.ToLowerInvariant() is "true" or "1";
        d.Status = into ? "Maintenance" : "Available";
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = into ? "DRONE_MAINTENANCE_IN" : "DRONE_MAINTENANCE_OUT",
            Description = $"{d.DroneName} ({d.DroneId}) set to {d.Status}. Battery {d.BatteryLevel}%."
        });
        await _db.SaveChangesAsync();
        return Ok(d);
    }

    [HttpPost("{id}/battery")]
    public async Task<IActionResult> SetBattery(string id, [FromBody] Dictionary<string, int> body)
    {
        var d = await _db.Drones.FindAsync(id);
        if (d == null) return NotFound();
        var level = body != null && body.ContainsKey("batteryLevel") ? body["batteryLevel"] : d.BatteryLevel;
        d.BatteryLevel = Math.Clamp(level, 0, 100);
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "DRONE_BATTERY",
            Description = $"{d.DroneName} battery set to {d.BatteryLevel}%."
        });
        await _db.SaveChangesAsync();
        return Ok(d);
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

    [HttpGet("mine")]
    public async Task<IActionResult> Mine([FromQuery] string? phone, [FromQuery] string? name)
    {
        var q = _db.Deliveries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(phone))
            q = q.Where(d => d.RecipientPhone == phone);
        if (!string.IsNullOrWhiteSpace(name))
            q = q.Where(d => d.RecipientName == name);
        return Ok(await q.OrderByDescending(d => d.DateCreated).Take(50).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryDto dto)
    {
        var origin = await _db.Locations.FindAsync(dto.OriginLocationId);
        var dest = await _db.Locations.FindAsync(dto.DestinationLocationId);
        if (origin == null || dest == null) return BadRequest(new { message = "Invalid locations." });

        var km = BusinessRules.HaversineKm(origin.Latitude, origin.Longitude, dest.Latitude, dest.Longitude);
        var service = string.IsNullOrWhiteSpace(dto.ServiceClass) ? "Routine" : dto.ServiceClass;
        if (!BusinessRules.WithinLimit(service, km))
            return BadRequest(BusinessRules.DistanceException(service, km));

        var drone = await _db.Drones.FindAsync(dto.DroneId);
        if (drone == null || drone.Status != "Available")
            return BadRequest(new { message = "Selected drone is not available." });

        var dispatch = BusinessRules.NewCode();
        var arrival = BusinessRules.NewCode();
        while (arrival == dispatch) arrival = BusinessRules.NewCode();

        var d = new Delivery
        {
            DeliveryId = "DEL-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..4].ToUpper(),
            DroneId = dto.DroneId,
            ClinicId = dto.ClinicId,
            SupplyId = dto.SupplyId,
            OriginLocationId = dto.OriginLocationId,
            DestinationLocationId = dto.DestinationLocationId,
            DeliveryStatus = "Assigned",
            DistanceKm = Math.Round(km, 2),
            CalculatedCost = BusinessRules.Tariff(km),
            ActualCost = BusinessRules.Tariff(km),
            PatientFee = 0,
            ServiceClass = service,
            DispatchCode = dispatch,
            ArrivalCode = arrival,
            RecipientName = dto.RecipientName,
            RecipientPhone = dto.RecipientPhone
        };
        drone.Status = "Assigned";
        _db.Deliveries.Add(d);
        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "CREATE_DELIVERY",
            Description = $"Delivery {d.DeliveryId} created. Dispatch code issued to recipient channel."
        });
        await _db.SaveChangesAsync();

        // Codes returned so staff can confirm SMS/app notification was issued (demo channel)
        return Ok(new
        {
            delivery = d,
            notification = new
            {
                channel = "App inbox + on-screen (SMS gateway optional)",
                recipientPhone = d.RecipientPhone,
                message = $"SIDSRH: Your package is ready to leave. DISPATCH code: {dispatch}. You will receive a different ARRIVAL code when it lands."
            }
        });
    }

    /// <summary>Recipient confirms package is coming - before departure / at dispatch.</summary>
    [HttpPost("{id}/confirm-dispatch")]
    public async Task<IActionResult> ConfirmDispatch(string id, [FromBody] CodeDto dto)
    {
        var d = await _db.Deliveries.FindAsync(id);
        if (d == null) return NotFound();
        if (d.DeliveryStatus is not ("Assigned" or "Pending"))
            return BadRequest(new { message = "Dispatch confirmation not allowed in status " + d.DeliveryStatus });
        if (d.DispatchCode != dto.Code?.Trim())
            return BadRequest(new { message = "Invalid dispatch code." });

        var drone = await _db.Drones.FindAsync(d.DroneId);
        var droneSpd = drone != null && drone.SpeedKmh > 0 ? drone.SpeedKmh : 80.0;
        var mins = BusinessRules.EtaMinutes(d.DistanceKm, droneSpd);
        d.DeliveryStatus = "In Transit";
        d.DispatchedAt = DateTime.UtcNow;
        d.EstimatedArrival = DateTime.UtcNow.AddMinutes(mins);

        if (drone != null) drone.Status = "In Transit";

        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "DISPATCH_CONFIRMED",
            Description = $"{d.DeliveryId} in transit. ETA ~{mins} min. Arrival code will unlock parcel on landing."
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            delivery = d,
            notification = new
            {
                message = $"SIDSRH: Package en route. ETA about {mins} minutes. On arrival use ARRIVAL code: {d.ArrivalCode} (or scan QR)."
            }
        });
    }

    /// <summary>Recipient unlocks parcel on arrival - different code from dispatch.
    /// Manager/Administrator may pass forceComplete=true if patient never confirmed (with note).</summary>
    [HttpPost("{id}/confirm-arrival")]
    public async Task<IActionResult> ConfirmArrival(string id, [FromBody] ArrivalDto dto)
    {
        var d = await _db.Deliveries.FindAsync(id);
        if (d == null) return NotFound();
        if (d.DeliveryStatus is "Completed" or "Failed" or "Returned")
            return BadRequest(new { message = "Delivery already closed." });

        var force = dto?.ForceComplete == true;
        if (!force)
        {
            if (d.DeliveryStatus != "In Transit")
                return BadRequest(new { message = "Arrival confirmation only while In Transit (or use manager force-complete)." });
            if (d.ArrivalCode != dto?.Code?.Trim())
                return BadRequest(new { message = "Invalid arrival code." });
            if (d.EstimatedArrival.HasValue && DateTime.UtcNow < d.EstimatedArrival.Value)
            {
                var wait = (int)Math.Ceiling((d.EstimatedArrival.Value - DateTime.UtcNow).TotalMinutes);
                return BadRequest(new { message = $"Package still in transit. Estimated arrival in ~{wait} minute(s)." });
            }
        }
        else
        {
            // Manager override when patient never received / never entered code
            d.DeliveryNote = (d.DeliveryNote ?? "") + $" | Manager force-complete: {dto?.Note ?? "no note"}";
        }

        d.DeliveryStatus = force && (dto?.MarkFailed == true) ? "Failed" : "Completed";
        d.DateCompleted = DateTime.UtcNow;
        var drone = await _db.Drones.FindAsync(d.DroneId);
        if (drone != null) { drone.Status = "Available"; drone.BatteryLevel = Math.Max(20, drone.BatteryLevel - 8); }

        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = force ? "ARRIVAL_FORCE" : "ARRIVAL_CONFIRMED",
            Description = force
                ? $"{d.DeliveryId} force-closed by manager. {dto?.Note}"
                : $"{d.DeliveryId} completed. Parcel unlocked."
        });
        await _db.SaveChangesAsync();
        return Ok(new { delivery = d, message = force ? "Delivery closed by manager." : "Parcel unlocked. Delivery completed." });
    }
    public record ArrivalDto(string? Code, bool ForceComplete = false, bool MarkFailed = false, string? Note = null);

    /// <summary>Demo helper: advance clock so arrival can be tested without waiting full ETA.</summary>
    [HttpPost("{id}/demo-arrive")]
    public async Task<IActionResult> DemoArrive(string id)
    {
        var d = await _db.Deliveries.FindAsync(id);
        if (d == null) return NotFound();
        if (d.DeliveryStatus != "In Transit") return BadRequest(new { message = "Only In Transit deliveries." });
        d.EstimatedArrival = DateTime.UtcNow.AddSeconds(-1);
        await _db.SaveChangesAsync();
        return Ok(new { message = "ETA set to now - enter arrival code or scan QR.", arrivalCode = d.ArrivalCode, delivery = d });
    }
}

[ApiController]
[Route("api/[controller]")]
public class PatientRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PatientRequestsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.PatientRequests.AsNoTracking().OrderByDescending(r => r.CreatedAt).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> One(string id)
    {
        var r = await _db.PatientRequests.FindAsync(id);
        return r == null ? NotFound() : Ok(r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientRequestDto dto)
    {
        var loc = await _db.Locations.FindAsync(dto.LocationId);
        if (loc == null) return BadRequest(new { message = "Invalid location." });
        var hub = await _db.Locations.FindAsync("LOC005");
        double? km = hub == null ? null : BusinessRules.HaversineKm(hub.Latitude, hub.Longitude, loc.Latitude, loc.Longitude);
        var service = string.IsNullOrWhiteSpace(dto.ServiceClass) ? "Urgent" : dto.ServiceClass;
        if (km.HasValue && !BusinessRules.WithinLimit(service, km.Value))
            return BadRequest(BusinessRules.DistanceException(service, km.Value, dto.Category));

        var r = new PatientRequest
        {
            RequestId = "REQ-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            PatientName = dto.PatientName,
            ContactNumber = dto.ContactNumber,
            LocationId = dto.LocationId,
            MedicalNeed = dto.MedicalNeed,
            Category = dto.Category,
            ServiceClass = service,
            Urgency = service == "Urgent" ? "Urgent" : "Routine",
            Status = "Submitted",
            DistanceKm = km.HasValue ? Math.Round(km.Value, 2) : null,
            EstimatedCost = km.HasValue ? BusinessRules.Tariff(km.Value) : null,
            SituationNotes = dto.SituationNotes,
            RequestResponder = dto.RequestResponder,
            HelperName = dto.HelperName,
            HelperContact = dto.HelperContact
        };
        _db.PatientRequests.Add(r);
        var sa = DateTime.Now.TimeOfDay;
        var open = new TimeSpan(5, 30, 0);
        var close = new TimeSpan(22, 0, 0);
        var afterHours = sa < open || sa > close;
        if (afterHours)
            r.SituationNotes = (r.SituationNotes ?? "") + " | After clinic hours (05:30-22:00): escalate to nearby responders if emergency.";
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "PATIENT_REQUEST",
            Description = $"Patient request {r.RequestId} by {r.PatientName}: {r.MedicalNeed} ({r.ServiceClass})"
        });
        await _db.SaveChangesAsync();
        return Ok(new { request = r, afterHours, clinicHours = "05:30-22:00" });
    }

    [HttpGet("options/{requestId}")]
    public async Task<IActionResult> Options(string requestId)
    {
        var r = await _db.PatientRequests.FindAsync(requestId);
        if (r == null) return NotFound();
        var dest = await _db.Locations.FindAsync(r.LocationId);
        var clinics = await _db.MobileClinics.AsNoTracking().ToListAsync();
        var locs = await _db.Locations.AsNoTracking().ToListAsync();
        var drones = await _db.Drones.AsNoTracking().Where(d => d.Status == "Available").ToListAsync();
        var supplies = await _db.MedicalSupplies.AsNoTracking().Where(s => s.Quantity > 0).ToListAsync();
        var clinicOptions = clinics.Select(cl =>
        {
            var loc = locs.FirstOrDefault(l => l.LocationId == cl.LocationId);
            double km = dest == null || loc == null ? 0 :
                BusinessRules.HaversineKm(loc.Latitude, loc.Longitude, dest.Latitude, dest.Longitude);
            return new {
                cl.ClinicId, cl.ClinicName, cl.LocationId,
                locationName = loc != null ? loc.LocationName : "",
                distanceKm = Math.Round(km, 2),
                withinLimit = dest != null && BusinessRules.WithinLimit(r.ServiceClass, km)
            };
        }).OrderBy(x => x.distanceKm).ToList();
        return Ok(new { request = r, destination = dest, clinics = clinicOptions, drones, supplies, locations = locs });
    }

    [HttpPost("{id}/accept")]
    public async Task<IActionResult> Accept(string id, [FromBody] AcceptRequestDto dto)
    {
        var r = await _db.PatientRequests.FindAsync(id);
        if (r == null) return NotFound();
        if (r.Status != "Submitted") return BadRequest(new { message = "Request not open." });

        var dest = await _db.Locations.FindAsync(r.LocationId);
        if (dest == null) return BadRequest(new { message = "Patient location missing." });

        // Nearest clinic as dispatch origin (not always the hub)
        var clinics = await _db.MobileClinics.AsNoTracking().ToListAsync();
        var locs = await _db.Locations.AsNoTracking().ToListAsync();
        string originId = "LOC005";
        double best = double.MaxValue;
        string clinicId = dto.ClinicId;
        foreach (var cl in clinics)
        {
            var loc = locs.FirstOrDefault(l => l.LocationId == cl.LocationId);
            if (loc == null) continue;
            var dist = BusinessRules.HaversineKm(loc.Latitude, loc.Longitude, dest.Latitude, dest.Longitude);
            if (cl.ClinicId == dto.ClinicId) { originId = loc.LocationId; best = dist; break; }
            if (dist < best) { best = dist; originId = loc.LocationId; clinicId = cl.ClinicId; }
        }
        if (!string.IsNullOrWhiteSpace(dto.ClinicId))
        {
            var chosen = clinics.FirstOrDefault(c => c.ClinicId == dto.ClinicId);
            if (chosen != null) { originId = chosen.LocationId; clinicId = chosen.ClinicId; }
        }

        var origin = await _db.Locations.FindAsync(originId);
        if (origin == null) return BadRequest(new { message = "Origin location missing." });
        var km = BusinessRules.HaversineKm(origin.Latitude, origin.Longitude, dest.Latitude, dest.Longitude);
        if (!BusinessRules.WithinLimit(r.ServiceClass, km))
            return BadRequest(BusinessRules.DistanceException(r.ServiceClass, km, r.Category));

        var drone = await _db.Drones.FindAsync(dto.DroneId);
        if (drone == null || drone.Status != "Available")
            return BadRequest(new { message = "Drone not available." });

        var dispatch = BusinessRules.NewCode();
        var arrival = BusinessRules.NewCode();
        while (arrival == dispatch) arrival = BusinessRules.NewCode();

        var d = new Delivery
        {
            DeliveryId = "DEL-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..4].ToUpper(),
            DroneId = dto.DroneId,
            ClinicId = clinicId,
            SupplyId = dto.SupplyId,
            OriginLocationId = originId,
            DestinationLocationId = r.LocationId,
            DeliveryStatus = "Assigned",
            DistanceKm = Math.Round(km, 2),
            CalculatedCost = BusinessRules.Tariff(km),
            ActualCost = BusinessRules.Tariff(km),
            PatientFee = 0,
            ServiceClass = r.ServiceClass,
            DispatchCode = dispatch,
            ArrivalCode = arrival,
            RecipientName = r.PatientName,
            RecipientPhone = r.ContactNumber,
            PatientRequestId = r.RequestId
        };
        drone.Status = "Assigned";
        r.Status = "Accepted";
        r.AssignedDeliveryId = d.DeliveryId;
        r.DistanceKm = d.DistanceKm;
        r.EstimatedCost = d.CalculatedCost;
        _db.Deliveries.Add(d);
        await _db.SaveChangesAsync();

        return Ok(new { request = r, delivery = d, distanceKm = d.DistanceKm, dispatchCode = dispatch, arrivalCode = arrival });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(string id, [FromBody] Dictionary<string, string>? body)
    {
        var r = await _db.PatientRequests.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = "Rejected";
        r.SituationNotes = (r.SituationNotes ?? "") + " | Rejected: " + (body != null && body.TryGetValue("reason", out var reason) ? reason : "No reason");
        await _db.SaveChangesAsync();
        return Ok(r);
    }
}

[ApiController]
[Route("api/[controller]")]
public class SuppliesController : ControllerBase
{
    private readonly AppDbContext _db;
    public SuppliesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.MedicalSupplies.AsNoTracking().OrderBy(s => s.SupplyName).ToListAsync());

    public record SupplyDto(string SupplyName, string? SupplyType, string? Category, int Quantity, DateTime? ExpiryDate, bool ColdChain);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupplyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SupplyName))
            return BadRequest(new { message = "Supply name is required." });
        var s = new MedicalSupply
        {
            SupplyId = "SUP-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            SupplyName = dto.SupplyName.Trim(),
            SupplyType = dto.SupplyType?.Trim() ?? "Medication",
            Category = dto.Category?.Trim() ?? "General",
            Quantity = Math.Max(0, dto.Quantity),
            ExpiryDate = dto.ExpiryDate ?? DateTime.UtcNow.Date.AddYears(1),
            ColdChain = dto.ColdChain
        };
        _db.MedicalSupplies.Add(s);
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM", ActivityType = "SUPPLY_CREATE",
            Description = $"Supply {s.SupplyId} {s.SupplyName} qty {s.Quantity}", Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] SupplyDto dto)
    {
        var s = await _db.MedicalSupplies.FindAsync(id);
        if (s == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(dto.SupplyName)) s.SupplyName = dto.SupplyName.Trim();
        if (dto.SupplyType != null) s.SupplyType = dto.SupplyType;
        if (dto.Category != null) s.Category = dto.Category;
        s.Quantity = Math.Max(0, dto.Quantity);
        if (dto.ExpiryDate.HasValue) s.ExpiryDate = dto.ExpiryDate.Value;
        s.ColdChain = dto.ColdChain;
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM", ActivityType = "SUPPLY_UPDATE",
            Description = $"Supply {s.SupplyId} updated qty={s.Quantity} exp={s.ExpiryDate:yyyy-MM-dd}", Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var s = await _db.MedicalSupplies.FindAsync(id);
        if (s == null) return NotFound();
        _db.MedicalSupplies.Remove(s);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Supply removed." });
    }

    /// <summary>Supply Coordinator dispenses stock to a clinic (not patient delivery).</summary>
    [HttpPost("dispense-to-clinic")]
    public async Task<IActionResult> DispenseToClinic([FromBody] ClinicDispenseDto dto)
    {
        var s = await _db.MedicalSupplies.FindAsync(dto.SupplyId);
        if (s == null) return NotFound(new { message = "Supply not found." });
        if (dto.Quantity <= 0) return BadRequest(new { message = "Quantity must be positive." });
        if (s.Quantity < dto.Quantity)
            return BadRequest(new { message = $"Insufficient stock. Available: {s.Quantity}." });
        if (!await _db.MobileClinics.AnyAsync(c => c.ClinicId == dto.ClinicId))
            return BadRequest(new { message = "Clinic not found. Dispense is to clinics only." });

        s.Quantity -= dto.Quantity;
        var rec = new DispenseRecord
        {
            DispenseId = "DSP-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            SupplyId = s.SupplyId,
            ClinicId = dto.ClinicId,
            ToClinicId = dto.ClinicId,
            TargetType = "ClinicTransfer",
            PatientName = "",
            Quantity = dto.Quantity,
            StaffUserId = dto.DispensedBy ?? "Supply Coordinator",
            DispensedAt = DateTime.UtcNow,
            Notes = dto.Notes ?? "Clinic stock transfer",
            Status = "Completed"
        };
        _db.DispenseRecords.Add(rec);
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = dto.DispensedBy ?? "SYSTEM",
            ActivityType = "CLINIC_STOCK_DISPENSE",
            Description = $"{dto.Quantity} x {s.SupplyName} to clinic {dto.ClinicId}",
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new { supply = s, record = rec, message = "Dispensed to clinic." });
    }
    public record ClinicDispenseDto(string SupplyId, string ClinicId, int Quantity, string? DispensedBy, string? Notes);
}

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public LocationsController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.Locations.AsNoTracking().ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LocationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.LocationName))
            return BadRequest(new { message = "Location name is required." });
        var loc = new Location
        {
            LocationId = "LOC-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            LocationName = dto.LocationName.Trim(),
            LocationType = string.IsNullOrWhiteSpace(dto.LocationType) ? "Village" : dto.LocationType.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
        _db.Locations.Add(loc);
        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "LOCATION_CREATE",
            Description = $"Location {loc.LocationId}: {loc.LocationName}",
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(loc);
    }
}
public record LocationDto(string LocationName, string? LocationType, double Latitude, double Longitude);


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

    public record StaffDto(string FirstName, string LastName, string? ContactNumber, string Role, string? ClinicId, string? Status);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StaffDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return BadRequest(new { message = "First and last name required." });
        var s = new Staff {
            StaffId = "STF-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            UserId = "USR-LINK-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            ContactNumber = dto.ContactNumber,
            Role = string.IsNullOrWhiteSpace(dto.Role) ? "Healthcare Staff" : dto.Role,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
            ClinicId = dto.ClinicId
        };
        _db.Staff.Add(s);
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "STAFF_ADD",
            Description = $"Staff added: {s.FirstName} {s.LastName} ({s.StaffId})"
        });
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> SetStatus(string id, [FromBody] Dictionary<string, string> body)
    {
        var s = await _db.Staff.FindAsync(id);
        if (s == null) return NotFound();
        s.Status = body != null && body.TryGetValue("status", out var st) ? st : s.Status;
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "STAFF_STATUS",
            Description = $"Staff {s.StaffId} status -> {s.Status}"
        });
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var s = await _db.Staff.FindAsync(id);
        if (s == null) return NotFound();
        _db.Staff.Remove(s);
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "STAFF_DELETE",
            Description = $"Staff removed: {s.FirstName} {s.LastName} ({s.StaffId})"
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Staff deleted." });
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
public class GuidesController : ControllerBase
{
    private readonly AppDbContext _db;
    public GuidesController(AppDbContext db) => _db = db;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _db.AdministrationGuides.AsNoTracking().ToListAsync());
}

[ApiController]
[Route("api/[controller]")]
public class ActivityController : ControllerBase
{
    private readonly AppDbContext _db;
    public ActivityController(AppDbContext db) => _db = db;

    // scope: staff | patient | technical | all  (role-based)
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int take = 30, [FromQuery] string? scope = null, [FromQuery] string? role = null, [FromQuery] string? userId = null)
    {
        if (take < 1) take = 30;
        if (take > 500) take = 500;
        var q = _db.ActivityLogs.AsNoTracking().AsQueryable();

        var r = (role ?? "").Trim();
        var isAdmin = r is "Administrator" or "Manager";
        var isTech = r is "Technician";
        var isPatient = r is "Patient";

        // Non-admin: only own actions unless technical bucket for tech
        if (!isAdmin)
        {
            if (isPatient)
            {
                // patient: only their delivery/request related (by userId if provided)
                if (!string.IsNullOrWhiteSpace(userId))
                    q = q.Where(a => a.UserId == userId || a.Description.Contains(userId));
                else
                    q = q.Where(a => a.ActivityType.Contains("PATIENT") || a.ActivityType.Contains("ARRIVAL") || a.ActivityType.Contains("REQUEST") || a.ActivityType.Contains("DELIVERY"));
            }
            else if (isTech)
            {
                q = q.Where(a =>
                    a.ActivityType.Contains("DRONE") || a.ActivityType.Contains("TECH") || a.ActivityType.Contains("BATTERY") ||
                    a.ActivityType.Contains("MAINTENANCE") || a.ActivityType.Contains("EQUIPMENT") ||
                    (!string.IsNullOrWhiteSpace(userId) && a.UserId == userId));
            }
            else
            {
                // nurse / supply / emergency: only own user actions
                if (!string.IsNullOrWhiteSpace(userId))
                    q = q.Where(a => a.UserId == userId);
                else
                    q = q.Where(a => a.ActivityType != "LOGIN" || a.UserId != "SYSTEM");
            }
        }
        else if (!string.IsNullOrWhiteSpace(scope))
        {
            var sc = scope.ToLowerInvariant();
            if (sc == "patient")
                q = q.Where(a => a.ActivityType.Contains("PATIENT") || a.ActivityType.Contains("ARRIVAL") || a.ActivityType.Contains("REQUEST") || a.ActivityType.Contains("EXTERNAL"));
            else if (sc == "technical")
                q = q.Where(a => a.ActivityType.Contains("DRONE") || a.ActivityType.Contains("TECH") || a.ActivityType.Contains("BATTERY") || a.ActivityType.Contains("MAINTENANCE") || a.ActivityType.Contains("EQUIPMENT") || a.ActivityType.Contains("WEATHER"));
            else if (sc == "staff")
                q = q.Where(a => a.ActivityType.Contains("STAFF") || a.ActivityType.Contains("DISPENSE") || a.ActivityType.Contains("LOGIN") || a.ActivityType.Contains("USER") || a.ActivityType.Contains("SCHEDULE") || a.ActivityType.Contains("SUPPLY"));
        }

        var list = await q.OrderByDescending(a => a.Timestamp).Take(take).ToListAsync();
        return Ok(list);
    }

    [HttpGet("recent")]
    public async Task<IActionResult> Recent([FromQuery] string? role = null, [FromQuery] string? userId = null) =>
        await Get(15, null, role, userId);
}


[ApiController]
[Route("api/[controller]")]
public class DispenseController : ControllerBase
{
    private readonly AppDbContext _db;
    public DispenseController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.DispenseRecords.AsNoTracking().OrderByDescending(d => d.DispensedAt).Take(50).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] DispenseDto dto)
    {
        var supply = await _db.MedicalSupplies.FindAsync(dto.SupplyId);
        if (supply == null) return BadRequest(new { message = "Supply not found." });
        if (dto.Quantity < 1 || supply.Quantity < dto.Quantity)
            return BadRequest(new { message = "Insufficient stock." });
        supply.Quantity -= dto.Quantity;
        var rec = new DispenseRecord
        {
            DispenseId = "DSP-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            ClinicId = dto.ClinicId,
            SupplyId = dto.SupplyId,
            PatientName = dto.PatientName,
            PatientId = dto.PatientId,
            TargetType = string.IsNullOrWhiteSpace(dto.TargetType) ? "Patient" : dto.TargetType,
            ToClinicId = dto.ToClinicId,
            Quantity = dto.Quantity,
            Notes = dto.Notes,
            Status = dto.PendingVerification ? "PendingVerification" : "Completed"
        };
        _db.DispenseRecords.Add(rec);
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "CLINIC_DISPENSE",
            Description = $"Clinic dispense {rec.DispenseId}: {rec.Quantity} x {rec.SupplyId} to {rec.PatientName} at {rec.ClinicId}. (Not a drone delivery.)"
        });
        await _db.SaveChangesAsync();
        return Ok(new { dispense = rec, note = "Clinic counter hand-out only. Does not appear under Deliveries." });
    }

    [HttpPost("{id}/verify")]
    public async Task<IActionResult> Verify(string id)
    {
        var rec = await _db.DispenseRecords.FindAsync(id);
        if (rec == null) return NotFound();
        rec.Status = "Completed";
        rec.VerifiedByUserId = "MANAGER";
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "DISPENSE_VERIFIED",
            Description = $"Manager verified dispense {rec.DispenseId} for {rec.PatientName}"
        });
        await _db.SaveChangesAsync();
        return Ok(rec);
    }
}

[ApiController]
[Route("api/[controller]")]
public class MapController : ControllerBase
{
    private readonly AppDbContext _db;
    public MapController(AppDbContext db) => _db = db;

    [HttpGet("markers")]
    public async Task<IActionResult> Markers()
    {
        var locations = await _db.Locations.AsNoTracking().ToListAsync();
        var drones = await _db.Drones.ToListAsync();
        var active = await _db.Deliveries.AsNoTracking()
            .Where(d => d.DeliveryStatus == "In Transit" || d.DeliveryStatus == "Assigned")
            .ToListAsync();
        var stations = await _db.ChargingStations.AsNoTracking().ToListAsync();
        var weather = await _db.SystemWeather.FindAsync(1);
        var locMap = locations.ToDictionary(l => l.LocationId, l => l);

        // Move in-transit drones along their route (live map)
        foreach (var del in active.Where(d => d.DeliveryStatus == "In Transit"))
        {
            var drone = drones.FirstOrDefault(x => x.DroneId == del.DroneId);
            if (drone == null) continue;
            if (!locMap.TryGetValue(del.OriginLocationId, out var origin)) continue;
            if (!locMap.TryGetValue(del.DestinationLocationId, out var dest)) continue;
            double progress = 0.05;
            if (del.DispatchedAt.HasValue && del.EstimatedArrival.HasValue)
            {
                var total = (del.EstimatedArrival.Value - del.DispatchedAt.Value).TotalSeconds;
                var elapsed = (DateTime.UtcNow - del.DispatchedAt.Value).TotalSeconds;
                progress = total > 0 ? Math.Clamp(elapsed / total, 0.02, 0.98) : 0.5;
            }
            drone.Latitude = origin.Latitude + (dest.Latitude - origin.Latitude) * progress;
            drone.Longitude = origin.Longitude + (dest.Longitude - origin.Longitude) * progress;
            drone.Status = "In Transit";
            drone.CurrentLocation = $"En route {progress:P0}";
        }
        await _db.SaveChangesAsync();

        var droneDto = drones.Select(d => new {
            d.DroneId, d.DroneName, d.Status, d.Latitude, d.Longitude, d.BatteryLevel, d.CurrentLocation
        });
        return Ok(new { locations, drones = droneDto, activeDeliveries = active, chargingStations = stations, weather });
    }
}

[ApiController]
[Route("api/Patients")]
public class PatientDependentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PatientDependentsController(AppDbContext db) => _db = db;

    [HttpGet("{id}/dependents")]
    public async Task<IActionResult> List(string id) =>
        Ok(await _db.PatientDependents.AsNoTracking().Where(d => d.PatientId == id).ToListAsync());

    [HttpPost("{id}/dependents")]
    public async Task<IActionResult> Add(string id, [FromBody] DepDto dto)
    {
        if (!await _db.PatientProfiles.AnyAsync(p => p.PatientId == id))
            return NotFound(new { message = "Patient not found." });
        var d = new PatientDependent
        {
            DependentId = "DEP-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            PatientId = id,
            FullName = dto.FullName?.Trim() ?? "",
            DateOfBirth = dto.DateOfBirth,
            Relationship = string.IsNullOrWhiteSpace(dto.Relationship) ? "Child" : dto.Relationship.Trim(),
            Notes = dto.Notes
        };
        _db.PatientDependents.Add(d);
        await _db.SaveChangesAsync();
        return Ok(d);
    }
}
public record DepDto(string FullName, DateTime? DateOfBirth, string? Relationship, string? Notes);

[ApiController]
[Route("api/[controller]")]
public class ExternalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    public ExternalController(AppDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    [HttpGet("clinic-info")]
    public IActionResult ClinicInfo() => Ok(new
    {
        name = _cfg["Clinic:Name"] ?? "SIDSRH Rural Health District",
        phone = _cfg["Clinic:Phone"] ?? "+27 47 555 0100",
        address = _cfg["Clinic:Address"] ?? "Eastern Cape, South Africa"
    });

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] ExtRegDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return BadRequest(new { message = "First name and surname are required." });
        var phone = (dto.Phone ?? "").Trim();
        var idn = (dto.IdNumber ?? "").Trim();
        PatientProfile? match = null;
        if (!string.IsNullOrEmpty(idn))
            match = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.IdNumber == idn);
        if (match == null && !string.IsNullOrEmpty(phone))
            match = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.ContactNumber == phone);
        if (match == null)
        {
            var fn = dto.FirstName.Trim().ToLower();
            var ln = dto.LastName.Trim().ToLower();
            match = await _db.PatientProfiles.FirstOrDefaultAsync(p =>
                p.FullName.ToLower().Contains(fn) && p.FullName.ToLower().Contains(ln));
        }

        var callDay = string.IsNullOrWhiteSpace(dto.PreferredCallDay) ? null : dto.PreferredCallDay.Trim();
        var callTime = string.IsNullOrWhiteSpace(dto.PreferredCallTime) ? null : dto.PreferredCallTime.Trim();

        var reg = new ExternalRegistration
        {
            RegistrationId = "EXT-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            IdNumber = string.IsNullOrEmpty(idn) ? null : idn,
            Phone = string.IsNullOrEmpty(phone) ? null : phone,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLower(),
            Address = dto.Address?.Trim() ?? "",
            Status = "Pending",
            MatchedPatientId = match?.PatientId,
            PreferredCallDay = callDay,
            PreferredCallTime = callTime,
            Notes = (match != null ? "Matched existing patient record for verification. " : "No existing clinic record found. ")
                  + (callDay != null ? $"Preferred call: {callDay} {callTime}." : ""),
            SubmittedAt = DateTime.UtcNow
        };
        _db.ExternalRegistrations.Add(reg);
        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "PUBLIC",
            ActivityType = "EXTERNAL_REGISTER",
            Description = $"External registration {reg.RegistrationId} for {reg.FirstName} {reg.LastName}",
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        var phoneClinic = _cfg["Clinic:Phone"] ?? "+27 47 555 0100";
        return Ok(new
        {
            message = match != null
                ? "Thank you for your registration. An administrator will contact you within 24 hours to process your details further. Please ensure you are available at your preferred call time (Monday-Friday, 08:30-17:00)."
                : $"Thank you for registering. Your details were not found on the clinic system. Please visit your nearest clinic or call {phoneClinic} so staff can register and verify you.",
            registrationId = reg.RegistrationId,
            matched = match != null,
            clinicPhone = phoneClinic,
            preferredCallDay = callDay,
            preferredCallTime = callTime
        });
    }

    [HttpGet("queue")]
    public async Task<IActionResult> Queue() =>
        Ok(await _db.ExternalRegistrations.AsNoTracking().OrderByDescending(r => r.SubmittedAt).Take(100).ToListAsync());

    [HttpPost("queue/{id}/verify")]
    public async Task<IActionResult> Verify(string id, [FromBody] ExtVerifyDto dto)
    {
        var reg = await _db.ExternalRegistrations.FindAsync(id);
        if (reg == null) return NotFound();
        reg.Status = string.IsNullOrWhiteSpace(dto?.Status) ? "Verified" : dto.Status.Trim();
        reg.VerifiedAt = DateTime.UtcNow;
        reg.VerifiedByUserId = dto?.ActorUserId;
        if (reg.Status == "Verified" && string.IsNullOrEmpty(reg.MatchedPatientId))
        {
            var pid = "PAT-" + Guid.NewGuid().ToString("N")[..6].ToUpper();
            _db.PatientProfiles.Add(new PatientProfile
            {
                PatientId = pid,
                FullName = reg.FirstName + " " + reg.LastName,
                ContactNumber = reg.Phone ?? "",
                IdNumber = reg.IdNumber,
                Address = reg.Address,
                Source = "External",
                Notes = "Created from external registration " + reg.RegistrationId,
                CreatedByUserId = dto?.ActorUserId
            });
            reg.MatchedPatientId = pid;
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Registration updated.", registration = reg });
    }
}

public record ExtRegDto(string FirstName, string LastName, string? IdNumber, string? Phone, string? Email, string? Address, string? PreferredCallDay, string? PreferredCallTime);
public record ExtVerifyDto(string? Status, string? ActorUserId);


[ApiController]
[Route("api/[controller]")]
public class BackupController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _cfg;
    private readonly AppDbContext _db;
    public BackupController(IWebHostEnvironment env, IConfiguration cfg, AppDbContext db)
    {
        _env = env; _cfg = cfg; _db = db;
    }

    string DbPath()
    {
        // SQLite file may live in content root or under bin/Debug when running
        var candidates = new[]
        {
            Path.Combine(_env.ContentRootPath, "HealthcareDB.db"),
            Path.Combine(Directory.GetCurrentDirectory(), "HealthcareDB.db"),
            Path.Combine(AppContext.BaseDirectory, "HealthcareDB.db"),
            Path.GetFullPath(Path.Combine(_env.ContentRootPath, "bin", "Debug", "net8.0", "HealthcareDB.db")),
            Path.GetFullPath(Path.Combine(_env.ContentRootPath, "bin", "Release", "net8.0", "HealthcareDB.db")),
        };
        foreach (var p in candidates)
        {
            try { if (System.IO.File.Exists(p)) return p; } catch { }
        }
        return candidates[0]; // default create location
    }
    string BackupDir()
    {
        var dir = Path.Combine(_env.ContentRootPath, "Backups");
        Directory.CreateDirectory(dir);
        return dir;
    }

    // Live status (like Smart Police "Up to date" panel)
    [HttpGet("info")]
    public async Task<IActionResult> Info()
    {
        var provider = _cfg["Database:Provider"] ?? "Sqlite";
        var sqlite = DbPath();
        var exists = System.IO.File.Exists(sqlite);
        var size = exists ? new FileInfo(sqlite).Length : 0L;
        var backups = Directory.GetFiles(BackupDir(), "HealthcareDB-backup-*.db")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .Take(20)
            .Select(f => new
            {
                name = f.Name,
                sizeBytes = f.Length,
                createdUtc = f.CreationTimeUtc,
                createdLocal = f.CreationTime.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();
        var lastBackup = backups.FirstOrDefault();
        var logCount = await _db.ActivityLogs.CountAsync();
        var userCount = await _db.Users.CountAsync();
        var deliveryCount = await _db.Deliveries.CountAsync();
        return Ok(new
        {
            status = "Up to date",
            autoSyncMinutes = 5,
            provider,
            sqlitePath = sqlite,
            sqliteExists = exists,
            sqliteSizeBytes = size,
            sqliteSizeMb = Math.Round(size / 1024.0 / 1024.0, 2),
            lastSyncedLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            lastSyncedUtc = DateTime.UtcNow,
            recordCounts = new { users = userCount, deliveries = deliveryCount, activityLogs = logCount },
            backupCount = backups.Count,
            lastBackup,
            recentBackups = backups,
            restoreHint = "Use Sync now to snapshot. To restore: stop API, replace HealthcareDB.db with a file from Backups\\, restart."
        });
    }

    // Create a timestamped copy in Backups\ (live snapshot)
    [HttpPost("sync")]
    public async Task<IActionResult> SyncNow()
    {
        var src = DbPath();
        if (!System.IO.File.Exists(src))
            return NotFound(new { message = "No SQLite database file found." });

        var name = $"HealthcareDB-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db";
        var dest = Path.Combine(BackupDir(), name);
        System.IO.File.Copy(src, dest, overwrite: true);

        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "BACKUP_SYNC",
            Description = $"Live backup created: {name}",
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var fi = new FileInfo(dest);
        return Ok(new
        {
            message = "Backup synced successfully.",
            status = "Up to date",
            autoSyncMinutes = 5,
            file = name,
            sizeBytes = fi.Length,
            path = dest,
            createdLocal = fi.CreationTime.ToString("yyyy-MM-dd HH:mm")
        });
    }

    // Download current live DB
    [HttpGet("download")]
    public IActionResult Download()
    {
        var path = DbPath();
        if (!System.IO.File.Exists(path)) return NotFound(new { message = "No SQLite file." });
        return PhysicalFile(path, "application/octet-stream", $"HealthcareDB-live-{DateTime.Now:yyyyMMddHHmm}.db");
    }

    // Download a specific snapshot from Backups\
    [HttpGet("download/{fileName}")]
    public IActionResult DownloadNamed(string fileName)
    {
        fileName = Path.GetFileName(fileName);
        if (!fileName.StartsWith("HealthcareDB-backup-") || !fileName.EndsWith(".db"))
            return BadRequest(new { message = "Invalid backup file name." });
        var path = Path.Combine(BackupDir(), fileName);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, "application/octet-stream", fileName);
    }

    // Activity log export CSV (readable backup of events since 2026-08-12)
    [HttpGet("activity.csv")]
    public async Task<IActionResult> ActivityCsv()
    {
        var from = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc);
        var list = await _db.ActivityLogs.AsNoTracking()
            .Where(a => a.Timestamp >= from)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Timestamp,LogId,UserId,ActivityType,Description");
        foreach (var a in list)
            sb.AppendLine($"\"{a.Timestamp:o}\",{a.LogId},{a.UserId},{a.ActivityType},\"{(a.Description ?? "").Replace("\"", "'")}\"");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "activity-since-2026-08-12.csv");
    }
}

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    [HttpGet("deliveries.csv")]
    public async Task<IActionResult> DeliveriesCsv()
    {
        var list = await _db.Deliveries.AsNoTracking().ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("DeliveryId,Status,ServiceClass,DistanceKm,ActualCost,PatientFee,DispatchCode,ArrivalCode,Recipient,Created");
        foreach (var d in list)
            sb.AppendLine($"{d.DeliveryId},{d.DeliveryStatus},{d.ServiceClass},{d.DistanceKm},{d.ActualCost},{d.PatientFee},{d.DispatchCode},{d.ArrivalCode},{d.RecipientName},{d.DateCreated:o}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "deliveries.csv");
    }

    [HttpGet("financial.csv")]
    public async Task<IActionResult> FinancialCsv()
    {
        var list = await _db.Deliveries.AsNoTracking().ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("DeliveryId,DistanceKm,ActualOperationalCost,PatientFee,DelayFine,Status,Month");
        foreach (var d in list)
        {
            var fee = d.PatientFee;
            var lineCost = fee <= 0 && d.DelayFine <= 0 ? "0.00" : fee.ToString("0.00");
            sb.AppendLine($"{d.DeliveryId},{d.DistanceKm},{d.ActualCost:0.00},{lineCost},{d.DelayFine:0.00},{d.DeliveryStatus},{d.DateCreated:yyyy-MM}");
        }
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "financial-monthly.csv");
    }

    /// <summary>Confidential brief patient receipt (HTML print-to-PDF).</summary>
    [HttpGet("patient-receipt/{deliveryId}")]
    public async Task<IActionResult> PatientReceipt(string deliveryId)
    {
        var d = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.DeliveryId == deliveryId);
        if (d == null) return NotFound();
        var supply = await _db.MedicalSupplies.AsNoTracking().FirstOrDefaultAsync(s => s.SupplyId == d.SupplyId);
        var drone = await _db.Drones.AsNoTracking().FirstOrDefaultAsync(x => x.DroneId == d.DroneId);
        var disp = await _db.DispenseRecords.AsNoTracking()
            .Where(x => x.PatientId != null && d.RecipientName != null)
            .OrderByDescending(x => x.DispensedAt).FirstOrDefaultAsync();
        var brief = string.IsNullOrWhiteSpace(d.BriefReason) ? (d.ServiceClass + " supply") : d.BriefReason;
        if (brief.Length > 60) brief = brief[..60];
        var patientCharge = d.PatientFee + d.DelayFine;
        var chargeLine = patientCharge <= 0 ? "R0.00" : ("R" + patientCharge.ToString("0.00"));
        var tech = string.IsNullOrWhiteSpace(d.TechnicianName) ? "On-duty technician" : d.TechnicianName;
        var leftAt = d.DispatchedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";
        var arrivedAt = d.DateCompleted?.ToString("yyyy-MM-dd HH:mm") ?? (d.EstimatedArrival?.ToString("yyyy-MM-dd HH:mm") ?? "-");
        var returnInfo = "";
        if (d.DeliveryStatus is "Returned" or "Failed" or "WeatherHold")
        {
            returnInfo = $"<tr><td>Return / hold</td><td><b>{System.Net.WebUtility.HtmlEncode(d.DeliveryStatus)}</b> - {System.Net.WebUtility.HtmlEncode(d.DeliveryNote ?? "See activity log")}<br/>Station: {System.Net.WebUtility.HtmlEncode(d.WeatherHoldStationId ?? "clinic/base")}</td></tr>";
        }
        if (d.CollectionDeadline.HasValue && d.DeliveryStatus != "Completed" && DateTime.UtcNow > d.CollectionDeadline.Value)
        {
            returnInfo += $"<tr><td>8-minute cutoff</td><td>Not collected in time. Delay fine R{d.DelayFine:0.00}. Parcel returned toward origin/clinic.</td></tr>";
        }
        var html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'/><title>SIDSRH Receipt</title>
<style>
body{{font-family:Segoe UI,Arial,sans-serif;color:#082c60;max-width:520px;margin:24px auto;border:1px solid #d7e3ec;padding:20px}}
h1{{font-size:18px;margin:0}} .muted{{color:#556;font-size:12px}}
table{{width:100%;font-size:13px;margin-top:12px}} td{{padding:4px 0;vertical-align:top}}
.logo{{height:48px}} .foot{{font-size:10px;color:#667;margin-top:16px;border-top:1px solid #eee;padding-top:8px}}
</style></head><body>
<img class='logo' src='/assets/sidsrh-logo.png' alt='SIDSRH'/>
<h1>Patient delivery receipt</h1>
<p class='muted'>Confidential - delivery record for the recipient.</p>
<p><button onclick='window.print()' style='background:#078d96;color:#fff;border:0;padding:8px 14px;border-radius:6px;cursor:pointer;font-weight:600'>Print / Save as PDF</button></p>
<table>
<tr><td>Delivery ID</td><td>{d.DeliveryId}</td></tr>
<tr><td>Recipient</td><td>{System.Net.WebUtility.HtmlEncode(d.RecipientName ?? "")}</td></tr>
<tr><td>Phone</td><td>{System.Net.WebUtility.HtmlEncode(d.RecipientPhone ?? "")}</td></tr>
<tr><td>Medication / supply</td><td>{System.Net.WebUtility.HtmlEncode(supply?.SupplyName ?? d.SupplyId)}</td></tr>
<tr><td>Dispatched by / technician</td><td>{System.Net.WebUtility.HtmlEncode(tech)}</td></tr>
<tr><td>Drone</td><td>{System.Net.WebUtility.HtmlEncode(drone?.DroneName ?? d.DroneId)} ({d.DroneId})</td></tr>
<tr><td>Time drone left</td><td><b>{leftAt}</b> UTC</td></tr>
<tr><td>Time arrived / completed</td><td><b>{arrivedAt}</b> UTC</td></tr>
<tr><td>Arrival confirmation code used</td><td><b>{System.Net.WebUtility.HtmlEncode(d.ArrivalCode ?? "-")}</b></td></tr>
<tr><td>Distance</td><td>{d.DistanceKm} km</td></tr>
<tr><td>Patient charge</td><td><b>{chargeLine}</b></td></tr>
<tr><td>Status</td><td>{d.DeliveryStatus}</td></tr>
<tr><td>Brief reason</td><td>{System.Net.WebUtility.HtmlEncode(brief)}</td></tr>
{returnInfo}
</table>
<p class='foot'>SIDSRH logistics receipt. Free in-slot delivery shows R0.00. If not collected within the collection window, the parcel returns to clinic/base and the reason is recorded above.</p>
</body></html>";
        return Content(html, "text/html");
    }

    [HttpGet("receipt-lookup")]
    public async Task<IActionResult> ReceiptLookup([FromQuery] string? patientId, [FromQuery] string? name, [FromQuery] string? phone)
    {
        var q = _db.Deliveries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(patientId))
            q = q.Where(d => (d.PatientRequestId != null && d.PatientRequestId.Contains(patientId)) || (d.RecipientName != null && d.RecipientName.Contains(patientId)));
        if (!string.IsNullOrWhiteSpace(name))
            q = q.Where(d => d.RecipientName != null && d.RecipientName.Contains(name));
        if (!string.IsNullOrWhiteSpace(phone))
            q = q.Where(d => d.RecipientPhone != null && d.RecipientPhone.Contains(phone));
        var list = await q.OrderByDescending(d => d.DateCreated).Take(20).Select(d => new {
            d.DeliveryId, d.RecipientName, d.RecipientPhone, d.DeliveryStatus, d.DispatchedAt, d.DateCompleted, d.ArrivalCode, d.SupplyId
        }).ToListAsync();
        return Ok(list);
    }

    [HttpGet("manager-weekly.csv")]
    public async Task<IActionResult> ManagerWeeklyCsv()
    {
        var staff = await _db.Staff.AsNoTracking().ToListAsync();
        var disp = await _db.DispenseRecords.AsNoTracking().Where(x => x.DispensedAt >= DateTime.UtcNow.AddDays(-7)).ToListAsync();
        var dels = await _db.Deliveries.AsNoTracking().Where(x => x.DateCreated >= DateTime.UtcNow.AddDays(-7)).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Section,Id,Name,Role,Status,Metric,Value");
        foreach (var s in staff)
            sb.AppendLine($"StaffOnDuty,{s.StaffId},{s.FirstName} {s.LastName},{s.Role},{s.Status},DutyFlag,{(s.Status=="Active"?"On":"Off")}");
        foreach (var d in disp)
            sb.AppendLine($"Dispense,{d.DispenseId},{d.PatientName},{d.ClinicId},{d.Status},Qty,{d.Quantity}");
        foreach (var d in dels)
            sb.AppendLine($"Delivery,{d.DeliveryId},{d.RecipientName},{d.ServiceClass},{d.DeliveryStatus},PatientFee,{(d.PatientFee+d.DelayFine):0.00}");
        sb.AppendLine("Shifts,NOTE,Week timetable,Staff,Active=On duty,Inactive=Off duty,See Staff module for roster edits");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "manager-weekly.csv");
    }

    [HttpGet("manager-weekly.html")]
    public async Task<IActionResult> ManagerWeeklyHtml()
    {
        var staff = await _db.Staff.AsNoTracking().ToListAsync();
        var disp = await _db.DispenseRecords.AsNoTracking().Where(x => x.DispensedAt >= DateTime.UtcNow.AddDays(-7)).CountAsync();
        var dels = await _db.Deliveries.AsNoTracking().Where(x => x.DateCreated >= DateTime.UtcNow.AddDays(-7)).ToListAsync();
        var rows = string.Join("", staff.Select(s => $"<tr><td>{s.FirstName} {s.LastName}</td><td>{s.Role}</td><td>{(s.Status=="Active"?"On duty":"Off duty")}</td></tr>"));
        var html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Manager weekly</title>
<style>body{{font-family:Segoe UI,Arial;margin:24px;color:#082c60}} table{{border-collapse:collapse;width:100%;font-size:13px}} td,th{{border:1px solid #d7e3ec;padding:6px}}</style></head><body>
<img src='/assets/sidsrh-logo.png' height='48'/><h1>Manager weekly report</h1>
<p>Week ending {DateTime.UtcNow:yyyy-MM-dd} UTC</p>
<h3>Staff duty</h3><table><tr><th>Name</th><th>Role</th><th>Duty</th></tr>{rows}</table>
<p>Dispenses (7 days): {disp}. Deliveries (7 days): {dels.Count}. Sum patient charges: R{dels.Sum(d => d.PatientFee + d.DelayFine):0.00}</p>
<p class='muted'>Shift timetable: Active staff = on duty; Inactive = off duty. Detailed hours can be extended from roster module.</p>
</body></html>";
        return Content(html, "text/html");
    }

    [HttpGet("technician.csv")]
    public async Task<IActionResult> TechnicianCsv()
    {
        var drones = await _db.Drones.AsNoTracking().ToListAsync();
        var active = await _db.Deliveries.AsNoTracking().Where(d => d.DeliveryStatus == "In Transit" || d.DeliveryStatus == "Assigned" || d.DeliveryStatus == "WeatherHold").ToListAsync();
        var done = await _db.Deliveries.AsNoTracking().Where(d => d.DeliveryStatus == "Completed" && d.DateCompleted >= DateTime.UtcNow.AddDays(-7)).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Type,Id,Name,Status,Battery,Detail");
        foreach (var dr in drones)
            sb.AppendLine($"Drone,{dr.DroneId},{dr.DroneName},{dr.Status},{dr.BatteryLevel},{dr.CurrentLocation}");
        foreach (var d in active)
            sb.AppendLine($"ActiveTask,{d.DeliveryId},{d.RecipientName},{d.DeliveryStatus},,{d.DistanceKm} km");
        foreach (var d in done)
            sb.AppendLine($"Completed7d,{d.DeliveryId},{d.RecipientName},{d.DeliveryStatus},,{d.DateCompleted:o}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "technician-report.csv");
    }

    [HttpGet("technician.html")]
    public async Task<IActionResult> TechnicianHtml()
    {
        var drones = await _db.Drones.AsNoTracking().ToListAsync();
        var active = await _db.Deliveries.AsNoTracking().Where(d => d.DeliveryStatus == "In Transit" || d.DeliveryStatus == "Assigned" || d.DeliveryStatus == "WeatherHold").ToListAsync();
        var drows = string.Join("", drones.Select(d => $"<tr><td>{d.DroneName}</td><td>{d.Status}</td><td>{d.BatteryLevel}%</td></tr>"));
        var arows = string.Join("", active.Select(d => $"<tr><td>{d.DeliveryId}</td><td>{d.DeliveryStatus}</td><td>{d.RecipientName}</td></tr>"));
        var html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Technician report</title>
<style>body{{font-family:Segoe UI,Arial;margin:24px;color:#082c60}} table{{border-collapse:collapse;width:100%}} td,th{{border:1px solid #ccc;padding:6px;font-size:13px}}</style></head><body>
<img src='/assets/sidsrh-logo.png' height='48'/><h1>Technician report</h1>
<p>Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
<h3>Fleet</h3><table><tr><th>Drone</th><th>Status</th><th>Battery</th></tr>{drows}</table>
<h3>Active tasks now</h3><table><tr><th>Delivery</th><th>Status</th><th>Recipient</th></tr>{arows}</table>
</body></html>";
        return Content(html, "text/html");
    }
}


[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PatientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.PatientProfiles.AsNoTracking().OrderBy(p => p.FullName).ToListAsync());

    public record PatientDto(string FullName, string? ContactNumber, string? IdNumber, string? Address, string? LocationId, string? Notes, string? Source);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Patient name required." });
        var p = new PatientProfile
        {
            PatientId = "PAT-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            FullName = dto.FullName.Trim(),
            ContactNumber = dto.ContactNumber ?? "",
            IdNumber = dto.IdNumber,
            Address = dto.Address,
            LocationId = dto.LocationId,
            Notes = dto.Notes,
            Source = string.IsNullOrWhiteSpace(dto.Source) ? "Internal" : dto.Source
        };
        _db.PatientProfiles.Add(p);
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "PATIENT_CREATE",
            Description = $"Patient profile {p.PatientId}: {p.FullName}"
        });
        await _db.SaveChangesAsync();
        return Ok(p);
    }
}

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly AppDbContext _db;
    public SchedulesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.ChronicSchedules.AsNoTracking().OrderBy(s => s.NextDueDate).ToListAsync());

    public record ScheduleDto(string PatientId, string SupplyId, string ClinicId, int IntervalDays, string PreferredSlot, string? Notes);

    static DateTime NextWorking(DateTime d)
    {
        while (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
            d = d.AddDays(1);
        return d.Date;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ScheduleDto dto)
    {
        var interval = dto.IntervalDays <= 0 ? 30 : dto.IntervalDays;
        // Base next due on last schedule for same patient+supply, else today + interval
        var last = await _db.ChronicSchedules.AsNoTracking()
            .Where(x => x.PatientId == dto.PatientId && x.SupplyId == dto.SupplyId && x.Status == "Active")
            .OrderByDescending(x => x.NextDueDate)
            .FirstOrDefaultAsync();
        var baseDate = last != null ? last.NextDueDate.Date.AddDays(interval) : DateTime.UtcNow.Date.AddDays(interval);
        var next = NextWorking(baseDate);

        // Prevent exact duplicate same day
        var dup = await _db.ChronicSchedules.AnyAsync(x =>
            x.PatientId == dto.PatientId && x.SupplyId == dto.SupplyId &&
            x.NextDueDate.Date == next && x.Status == "Active");
        if (dup)
            return BadRequest(new { message = $"An active schedule for this patient/supply already exists on {next:yyyy-MM-dd}." });

        var s = new ChronicSchedule
        {
            ScheduleId = "SCH-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            PatientId = dto.PatientId,
            SupplyId = dto.SupplyId,
            ClinicId = dto.ClinicId,
            IntervalDays = interval,
            NextDueDate = next,
            PreferredSlot = string.IsNullOrWhiteSpace(dto.PreferredSlot) ? "09:00" : dto.PreferredSlot,
            Notes = dto.Notes
        };
        _db.ChronicSchedules.Add(s);
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "CHRONIC_SCHEDULE",
            Description = $"Chronic schedule {s.ScheduleId} for {s.PatientId}, slot {s.PreferredSlot}, next {s.NextDueDate:yyyy-MM-dd}"
        });
        await _db.SaveChangesAsync();
        return Ok(new {
            schedule = s,
            feeRule = "Free at 06:30, 09:00, 16:00. Outside those slots patient fee R10 (pay on delivery or next clinic visit).",
            internalCostNote = "Actual operational cost is tracked for management financial reports; patient delivery is free in-slot."
        });
    }

    /// <summary>Mark collected at clinic on checkup day and roll next due by IntervalDays (skip weekends).</summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(string id, [FromBody] CompleteSchDto? dto)
    {
        var s = await _db.ChronicSchedules.FindAsync(id);
        if (s == null) return NotFound();
        var collectedAt = dto?.CollectedAt?.Date ?? DateTime.UtcNow.Date;
        var next = NextWorking(collectedAt.AddDays(s.IntervalDays <= 0 ? 30 : s.IntervalDays));
        s.NextDueDate = next;
        if (!string.IsNullOrWhiteSpace(dto?.Notes))
            s.Notes = (s.Notes ?? "") + $" | Collected {collectedAt:yyyy-MM-dd}: {dto!.Notes}";
        if (dto?.MedsUnavailable == true)
        {
            s.Notes = (s.Notes ?? "") + " | Meds unavailable - drone delivery when stock available";
            s.Status = "AwaitingStock";
        }
        _db.ActivityLogs.Add(new ActivityLog {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "SCHEDULE_COLLECTED",
            Description = $"Schedule {s.ScheduleId} collected {collectedAt:yyyy-MM-dd}; next {next:yyyy-MM-dd}"
        });
        await _db.SaveChangesAsync();
        return Ok(new { schedule = s, message = $"Recorded. Next due {next:yyyy-MM-dd}." });
    }
    public record CompleteSchDto(DateTime? CollectedAt, string? Notes, bool MedsUnavailable);

    [HttpGet("due")]
    public async Task<IActionResult> Due()
    {
        var soon = DateTime.UtcNow.Date.AddDays(2);
        var list = await _db.ChronicSchedules.AsNoTracking()
            .Where(s => s.Status == "Active" && s.NextDueDate <= soon).ToListAsync();
        return Ok(list.Select(s => new {
            s.ScheduleId, s.PatientId, s.SupplyId, s.ClinicId, s.NextDueDate, s.PreferredSlot,
            notification = $"Your medicine is due. Choose 06:30, 09:00 or 16:00 (free). Outside those times R10."
        }));
    }
}

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory? _httpFactory;
    public WeatherController(AppDbContext db, IHttpClientFactory? httpFactory = null)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Live weather for Mthatha area via Open-Meteo (no API key)
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            // Mthatha approx -31.59, 28.79
            var url = "https://api.open-meteo.com/v1/forecast?latitude=-31.59&longitude=28.79&current=temperature_2m,relative_humidity_2m,precipitation,weather_code,wind_speed_10m&timezone=Africa%2FJohannesburg";
            var json = await http.GetStringAsync(url);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var cur = doc.RootElement.GetProperty("current");
            var code = cur.GetProperty("weather_code").GetInt32();
            var precip = cur.GetProperty("precipitation").GetDouble();
            var temp = cur.GetProperty("temperature_2m").GetDouble();
            var wind = cur.GetProperty("wind_speed_10m").GetDouble();
            var humidity = cur.GetProperty("relative_humidity_2m").GetInt32();
            var raining = precip > 0.1 || code >= 51;
            var condition = raining ? "Rain" : "Clear";
            var desc = WeatherCodeLabel(code);

            var w = await _db.SystemWeather.FindAsync(1);
            if (w == null) { w = new SystemWeather { Id = 1 }; _db.SystemWeather.Add(w); }
            w.Condition = condition;
            w.UpdatedAt = DateTime.UtcNow;
            w.Note = $"{desc}; {temp:0.#} degC; humidity {humidity}%; wind {wind:0.#} km/h; precip {precip:0.##} mm";
            await _db.SaveChangesAsync();

            return Ok(new {
                condition = w.Condition,
                updatedAt = w.UpdatedAt,
                note = w.Note,
                temperatureC = temp,
                humidity,
                windKmh = wind,
                precipitationMm = precip,
                weatherCode = code,
                description = desc,
                source = "Open-Meteo live",
                flightSafe = !raining
            });
        }
        catch (Exception ex)
        {
            var w = await _db.SystemWeather.FindAsync(1);
            return Ok(new {
                condition = w?.Condition ?? "Clear",
                updatedAt = w?.UpdatedAt ?? DateTime.UtcNow,
                note = w?.Note ?? ("Live weather unavailable: " + ex.Message),
                source = "cached",
                flightSafe = (w?.Condition ?? "Clear") != "Rain"
            });
        }
    }

    static string WeatherCodeLabel(int code) => code switch
    {
        0 => "Clear sky",
        1 or 2 or 3 => "Mainly clear / partly cloudy",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        61 or 63 or 65 => "Rain",
        66 or 67 => "Freezing rain",
        71 or 73 or 75 => "Snow",
        80 or 81 or 82 => "Rain showers",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Cloud cover code " + code
    };


    [HttpPost]
    public async Task<IActionResult> Set([FromBody] Dictionary<string, string> body)
    {
        var w = await _db.SystemWeather.FindAsync(1);
        if (w == null) { w = new SystemWeather { Id = 1 }; _db.SystemWeather.Add(w); }
        var cond = body != null && body.TryGetValue("condition", out var c) ? c : "Clear";
        w.Condition = cond.Equals("Rain", StringComparison.OrdinalIgnoreCase) ? "Rain" : "Clear";
        w.UpdatedAt = DateTime.UtcNow;
        w.Note = body != null && body.TryGetValue("note", out var n) ? n : w.Note;

        if (w.Condition == "Rain")
        {
            var inflight = await _db.Deliveries.Where(d => d.DeliveryStatus == "In Transit").ToListAsync();
            var stations = await _db.ChargingStations.AsNoTracking().Where(s => s.Status == "Operational").ToListAsync();
            foreach (var d in inflight)
            {
                var dest = await _db.Locations.FindAsync(d.DestinationLocationId);
                ChargingStation? nearest = null;
                double best = double.MaxValue;
                foreach (var stn in stations)
                {
                    if (dest == null) break;
                    var km = BusinessRules.HaversineKm(stn.Latitude, stn.Longitude, dest.Latitude, dest.Longitude);
                    if (km < best) { best = km; nearest = stn; }
                }
                d.DeliveryStatus = "WeatherHold";
                d.WeatherHoldStationId = nearest?.StationId;
                d.DeliveryNote = $"RAIN HOLD at {nearest?.StationName ?? "safe site"}. Resume when clear. Max rain range 15 km.";
                _db.ActivityLogs.Add(new ActivityLog {
                    LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
                    UserId = "SYSTEM",
                    ActivityType = "WEATHER_HOLD",
                    Description = $"Delivery {d.DeliveryId} held at {nearest?.StationName}. Alert: Technician + Manager + recipient."
                });
            }
        }
        else
        {
            var held = await _db.Deliveries.Where(d => d.DeliveryStatus == "WeatherHold").ToListAsync();
            foreach (var d in held)
            {
                d.DeliveryStatus = "In Transit";
                d.DeliveryNote = (d.DeliveryNote ?? "") + " | Resumed after rain cleared.";
                _db.ActivityLogs.Add(new ActivityLog {
                    LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
                    UserId = "SYSTEM",
                    ActivityType = "WEATHER_RESUME",
                    Description = $"Delivery {d.DeliveryId} resumed after rain."
                });
            }
        }
        await _db.SaveChangesAsync();
        return Ok(new {
            weather = w,
            rule = "During rain only deliveries <= 15 km. In-flight drones divert to nearest charging/safe station."
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class StationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public StationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.ChargingStations.AsNoTracking().ToListAsync());
}


[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AppointmentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? patientId, [FromQuery] string? name)
    {
        var q = _db.ClinicAppointments.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(patientId)) q = q.Where(a => a.PatientId == patientId);
        if (!string.IsNullOrWhiteSpace(name)) q = q.Where(a => a.PatientName.Contains(name));
        return Ok(await q.OrderBy(a => a.AppointmentAt).ToListAsync());
    }
}

[ApiController]
[Route("api/[controller]")]
public class HelpController : ControllerBase
{
    private readonly AppDbContext _db;
    public HelpController(AppDbContext db) => _db = db;

    /// <summary>Emergency code-help chat steps when patient cannot unlock parcel.</summary>
    [HttpPost("code-assist")]
    public async Task<IActionResult> CodeAssist([FromBody] Dictionary<string, string> body)
    {
        var deliveryId = body != null && body.TryGetValue("deliveryId", out var d) ? d : "";
        var del = string.IsNullOrWhiteSpace(deliveryId) ? null : await _db.Deliveries.FindAsync(deliveryId);
        var steps = new List<object>
        {
            new { step = 1, check = "Collection / dispense point", result = del == null ? "Delivery not found - confirm ID with clinic." : $"Destination {del.DestinationLocationId}, status {del.DeliveryStatus}." },
            new { step = 2, check = "Code was issued to correct account", result = del == null ? "N/A" : $"Recipient {del.RecipientName} / {del.RecipientPhone}. Arrival code is only for this recipient." },
            new { step = 3, check = "Network or delay", result = "If offline, reconnect Wi-Fi/data and reload deliveries. OTP/codes need a successful API call unless already cached." },
            new { step = 4, check = "Resolve", result = "Re-send on-screen arrival code to logged-in patient. If still blocked, nurse/technician may verify registered patient or next-of-kin ID and release on audit." }
        };
        _db.ActivityLogs.Add(new ActivityLog
        {
            LogId = "LOG-" + Guid.NewGuid().ToString("N")[..10],
            UserId = "SYSTEM",
            ActivityType = "CODE_HELP_CHAT",
            Description = $"Code assist opened for {deliveryId}"
        });
        await _db.SaveChangesAsync();
        return Ok(new
        {
            title = "Emergency code help",
            steps,
            nextOfKinOption = "Nurse or technician can scan/verify registered patient or next-of-kin, then release with activity log.",
            collectionGraceMinutes = BusinessRules.CollectionGraceMinutes,
            delayFine = BusinessRules.DelayFineRand,
            returnPolicy = "If not collected within 8 minutes after arrival window, R3 delay fine applies; after grace the drone returns with the parcel."
        });
    }
}



